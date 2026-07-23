Да. Сейчас у тебя фактически **два разных механизма reconciliation**, и оба нужно заменить одним алгоритмом.

## Как сделано сейчас

### Обычное обновление атрибута

Для каждого атрибута хранится очередь готовых значений:

```text
Mana: [80, 60, 40]
```

Когда сервер присылает `60`, код удаляет из очереди всё до `60` включительно и оставляет локальное значение `40`. Если сервер прислал значение, которого в очереди нет, очередь очищается, а атрибут напрямую устанавливается в серверное значение. ([GitHub][1])

Это работает только пока последовательность значений совпадает. Связи с конкретными `activationGUID` нет.

### Явный Reject способности

`RpcGameplayAbilityUndo`:

```text
1. Деактивирует ability
2. Находит эффекты по activationGUID
3. Собирает source и target ASC
4. Перезаписывает все их атрибуты из syncAttributes
```

Но predicted effects фактически не удаляются, несмотря на комментарий `Remove any durational GE`. Кроме того, для всех затронутых ASC используется `syncAttributes` того `NetworkAbilitySystemComponent`, на котором пришёл RPC — то есть для target потенциально используются атрибуты source. Оставшиеся predictions не переигрываются. ([GitHub][1])

Подтверждение эффекта сейчас работает отдельно: predicted effect находится по ключу `effectIndex_activationGUID`, ему заменяется локальный GUID на серверный, и повторное применение пропускается. ([GitHub][1])

---

# Как правильно для текущего pipeline

Нужна одна модель:

```text
Authoritative snapshot конкретного ASC
+
упорядоченные неподтверждённые GameplayEffects
=
текущее локальное состояние ASC
```

При Reject нельзя откатывать только одну числовую дельту. Нужно выполнить:

```text
1. Снять все pending predictions с затронутого ASC
2. Удалить rejected activation из Tracker
3. Восстановить authoritative snapshot этого ASC
4. Повторно применить остальные pending predictions по порядку
```

Именно **все pending**, а не только rejected, потому что иначе порядок снятия эффектов, clamp, множители и зависимые атрибуты могут дать неправильный результат.

## Какие данные нужны

```csharp
public sealed class PredictedEffectRecord
{
    public long Sequence;
    public string ActivationId;

    public NetworkAbilitySystemComponent Target;
    public GameplayEffect Effect;

    // Текущий локальный экземпляр после Apply/Reapply.
    public string LocalEffectGuid;
}

public sealed class AbilityActivationRecord
{
    public string Id;
    public long Sequence;
    public ActivationState State;

    public List<PredictedEffectRecord> Effects = new();
}
```

`Target` должен быть именно `NetworkAbilitySystemComponent`, потому что authoritative snapshot находится у **target-компонента**:

```csharp
effectRecord.Target.syncAttributes
```

а не у владельца способности.

---

# Rollback

Пример основной функции:

```csharp
public void RejectActivation(
    string activationId,
    ActivationFailure failure)
{
    if (!_tracker.TryGet(activationId, out var rejected))
        return;

    rejected.State = ActivationState.Rejected;

    // Сначала строим планы, пока rejected ещё находится в tracker.
    var plans = rejected.Effects
        .Select(effect => effect.Target)
        .Distinct()
        .Select(CreateReconciliationPlan)
        .ToArray();

    using (_predictionCapture.Suppress())
    {
        // Снять все pending predictions, включая rejected.
        foreach (var plan in plans)
        {
            RemovePendingEffects(plan);
        }

        // Теперь rejected больше не участвует в replay.
        _tracker.Remove(activationId);

        foreach (var plan in plans)
        {
            RestoreAuthoritativeAttributes(plan.Target);
            ReplayRemainingPredictions(plan.Target);
        }
    }

    rejected.Ability.DeactivateAbility(activationId);

    asc.OnGameplayAbilityFailedActivation?.Invoke(
        rejected.Ability,
        activationId,
        failure);
}
```

## План reconciliation

```csharp
private sealed class ReconciliationPlan
{
    public NetworkAbilitySystemComponent Target;
    public List<PredictedEffectRecord> PendingEffects;
}

private ReconciliationPlan CreateReconciliationPlan(
    NetworkAbilitySystemComponent target)
{
    return new ReconciliationPlan
    {
        Target = target,

        // Включает rejected и все последующие predictions.
        PendingEffects = _tracker
            .GetPendingEffectsFor(target)
            .OrderBy(effect => effect.Sequence)
            .ToList()
    };
}
```

## Снятие pending effects

Снимать нужно в обратном порядке:

```csharp
private void RemovePendingEffects(ReconciliationPlan plan)
{
    for (int i = plan.PendingEffects.Count - 1; i >= 0; i--)
    {
        var predicted = plan.PendingEffects[i];

        if (string.IsNullOrEmpty(predicted.LocalEffectGuid))
            continue;

        RemoveGameplayEffectWithoutReplication(
            plan.Target.asc,
            predicted.LocalEffectGuid);

        predicted.LocalEffectGuid = null;
    }
}
```

Здесь должен использоваться нормальный GASify API удаления duration/infinite effect, чтобы убрались его modifiers и granted tags.

Для `Instant` effect удалять нечего: он уже изменил атрибут. Его результат исчезнет на следующем шаге — при восстановлении authoritative snapshot.

## Восстановление authoritative состояния

```csharp
private void RestoreAuthoritativeAttributes(
    NetworkAbilitySystemComponent target)
{
    foreach (var pair in target.syncAttributes)
    {
        if (!target.asc.attributesDictionary.TryGetValue(
                pair.Key,
                out var attribute))
        {
            continue;
        }

        float oldValue = attribute.GetValue();
        float authoritativeValue = pair.Value;

        if (attribute.attributeName.attributeType ==
            AttributeType.RESOURCE)
        {
            attribute.baseValue = authoritativeValue;
        }
        else
        {
            attribute.currentValue = authoritativeValue;
        }

        if (!Mathf.Approximately(oldValue, authoritativeValue))
        {
            target.asc.OnAttributeChanged?.Invoke(
                attribute.attributeName,
                oldValue,
                authoritativeValue,
                null);
        }
    }

    target.localAttributesBuffer
        .Values
        .ToList()
        .ForEach(queue => queue.Clear());
}
```

Главное отличие от текущего кода:

```csharp
target.syncAttributes
```

а не просто `this.syncAttributes`.

## Replay оставшихся predictions

```csharp
private void ReplayRemainingPredictions(
    NetworkAbilitySystemComponent target)
{
    var remaining = _tracker
        .GetPendingEffectsFor(target)
        .OrderBy(effect => effect.Sequence);

    foreach (var predicted in remaining)
    {
        GameplayEffect reapplied =
            CloneForPrediction(predicted.Effect);

        reapplied.applicationGUID =
            predicted.ActivationId;

        target.asc.ApplyGameplayEffect(
            reapplied.source,
            target.asc,
            reapplied);

        predicted.LocalEffectGuid = reapplied.guid;
        predicted.Effect = reapplied;
    }
}
```

Важно: переигрывается **GameplayEffect**, а не `GameplayAbility`.

Иначе повторно запустятся:

* ability lifecycle;
* анимация;
* VFX;
* Command;
* cost/cooldown orchestration.

---

# Нужен suppress-режим

Во время удаления, восстановления и replay события ASC снова вызовут:

```text
OnGameplayEffectApplied
OnGameplayEffectRemoved
OnAttributeChanged
```

Поэтому они не должны повторно попасть в prediction buffers:

```csharp
public sealed class PredictionCapture
{
    private int _suppressionDepth;

    public bool IsSuppressed => _suppressionDepth > 0;

    public IDisposable Suppress()
    {
        _suppressionDepth++;
        return new Scope(() => _suppressionDepth--);
    }
}
```

И в callbacks:

```csharp
private void AddAttributeToPredictionBuffer(
    AttributeName name,
    float oldValue,
    float newValue,
    GameplayEffect effect)
{
    if (_predictionCapture.IsSuppressed)
        return;

    // Обычная регистрация prediction.
}
```

---

# На примере дебаффа

Имеются:

```text
Activation 42 → Weakness ×0.7
Activation 43 → Vulnerability ×1.2
```

Обе применены к цели:

```text
Damage = 100 × 0.7 × 1.2 = 84
```

Сервер отклонил `42`:

```text
снять 43
снять 42
удалить 42 из Tracker
восстановить target Damage = 100 из target.syncAttributes
переиграть 43
итог Damage = 120
```

Итого правильный rollback для твоего pipeline — это не «вернуть старое значение» и не «удалить только rejected effect», а:

```text
remove pending
→ restore authoritative
→ replay remaining
```

При этом `localAttributesBuffer<Queue<float>>` после такого перехода становится почти не нужен: главным источником prediction должны быть записи GameplayEffects, привязанные к `activationGUID`.

Первый вариант был **минимальным tracker’ом**, достаточным только для `Confirm/Reject`. Для правильного rollback он неполный.

Для текущего пайплайна правильнее использовать такую итоговую модель:

```csharp
public enum AbilityActivationState
{
    Requested,
    Predicted,
    Confirmed,
    Rejected,
    Cancelled,
    Completed
}

public sealed class PredictedEffectRecord
{
    // Для сопоставления predicted и authoritative effect.
    public string Key { get; }

    public string ActivationId { get; }

    // Определяет порядок replay между разными predictions.
    public long Sequence { get; }

    public AbilitySystemComponent Source { get; }
    public AbilitySystemComponent Target { get; }

    // Неизменяемая копия effect, используемая при replay.
    public GameplayEffect EffectSpec { get; }

    // GUID реально применённого локального duration/infinite effect.
    // Для Instant может быть null.
    public string LocalEffectGuid { get; internal set; }

    public bool IsConfirmed { get; internal set; }

    public PredictedEffectRecord(
        string key,
        string activationId,
        long sequence,
        AbilitySystemComponent source,
        AbilitySystemComponent target,
        GameplayEffect effectSpec,
        string localEffectGuid)
    {
        Key = key;
        ActivationId = activationId;
        Sequence = sequence;
        Source = source;
        Target = target;
        EffectSpec = effectSpec;
        LocalEffectGuid = localEffectGuid;
    }
}
```

```csharp
public sealed class AbilityActivationRecord
{
    public string Id { get; }

    // Порядок самих активаций.
    public long Sequence { get; }

    public GameplayAbility Ability { get; }

    public AbilitySystemComponent Source { get; }

    // Основная цель запроса ability.
    // Конкретные effects могут иметь другие Target.
    public AbilitySystemComponent Target { get; }

    public AbilityActivationState State { get; internal set; }

    public List<PredictedEffectRecord> Effects { get; } = new();

    public AbilityActivationRecord(
        string id,
        long sequence,
        GameplayAbility ability,
        AbilitySystemComponent source,
        AbilitySystemComponent target)
    {
        Id = id;
        Sequence = sequence;
        Ability = ability;
        Source = source;
        Target = target;
        State = AbilityActivationState.Requested;
    }
}
```

### Почему не прежний `LocalEffect`

Прежнее поле:

```csharp
public GameplayEffect LocalEffect { get; }
```

смешивало две сущности:

1. описание операции, необходимое для повторного применения;
2. уже применённый локальный экземпляр, который удаляется и получает новый GUID.

Лучше разделить:

```text
EffectSpec
→ что нужно применить при replay

LocalEffectGuid
→ какой текущий локальный effect удалить
```

Для instant effect:

```text
EffectSpec = есть
LocalEffectGuid = null
```

Он не удаляется как активный effect. Его результат исчезает при восстановлении authoritative attributes, после чего `EffectSpec` может быть переигран.

Для duration effect:

```text
EffectSpec = есть
LocalEffectGuid = GUID локального effect
```

При reconciliation он удаляется по `LocalEffectGuid`, затем переигрывается, и GUID обновляется.

Таким образом, **второй вариант является развитием первого**, а не другой моделью. В окончательной реализации нужны:

```text
ActivationRecord
├── Id и Sequence
├── Ability, Source, Target
├── State
└── Effects

PredictedEffectRecord
├── Key
├── ActivationId и Sequence
├── Source и реальный Target
├── EffectSpec для replay
└── LocalEffectGuid для удаления
```


Сейчас **единой хронологии prediction нет**:

* `localAttributesBuffer` хранит отдельную `Queue<float>` для каждого атрибута;
* `localEffectsBuffer` — словарь effects по ключу `effectIndex_activationGUID`;
* `appliedGameplayEffects` в GASify является списком, но это список активных effects, а не явный журнал prediction. ([GitHub][1])

Правильнее ввести собственный порядок.

## Это временная линия, не стек

Каждому **фактически применённому predicted effect** присваиваем монотонный номер:

```csharp
private long _nextEffectSequence;

private void TrackPredictedEffect(GameplayEffect effect)
{
    var record = new PredictedEffectRecord(
        activationId: effect.applicationGUID,
        sequence: ++_nextEffectSequence,
        effectSpec: Clone(effect),
        target: effect.target,
        localEffectGuid: effect.guid);

    _tracker.AddEffect(record);
}
```

Получается временная линия конкретного ASC:

```text
101: ArmorBuff
102: Weakness
103: DamageBoost
104: Stun
```

Порядок назначается в `OnGameplayEffectApplied`, а не по позиции effect в `ability.effects`, потому что это отражает реальный порядок применения.

## Почему снимать в обратном порядке

Хранилище — timeline:

```text
A → B → C
```

Но отменяем:

```text
C → B → A
```

То есть используем стековый порядок только для демонтажа.

Причина: более поздний эффект мог быть рассчитан или применён уже поверх более раннего состояния. Снимать зависимости безопаснее в обратном порядке. Даже если GASify в итоге полностью пересчитает modifiers, так будет меньше некорректных промежуточных состояний и событий.

После восстановления переигрываем в прямом порядке:

```text
A → C
```

если `B` был отклонён.

---

## Почему временно снимаем все pending effects

Допустим, серверное состояние:

```text
Damage = 100
```

Локальные predictions:

```text
A: +20       → 120
B: ×0.5      → 60    // сервер отклонил
C: +30       → 90
```

Нельзя просто удалить `B`, особенно если это instant effect или изменение уже записано непосредственно в атрибут. `C` применялся к состоянию после `B`.

Правильный rebase:

```text
снять C
снять B
снять A
восстановить authoritative Damage = 100
удалить B из pending
переиграть A: 100 → 120
переиграть C: 120 → 150
```

Важно: мы снимаем не вообще все эффекты персонажа, а только **все неподтверждённые predicted effects на затронутом ASC**.

Авторитетные и уже подтверждённые effects не трогаются.

## Почему нельзя откатить только от конца до `B`

Можно было бы:

```text
снять C
снять B
оставить A
переиграть C
```

но только если у нас есть достоверное состояние после `A`.

В текущем пайплайне есть лишь:

```text
последний authoritative snapshot
+
набор pending predictions
```

Snapshot «сразу перед B» не хранится. Если восстановить серверное значение `100`, contribution от оставленного `A` исчезнет из атрибута, хотя сам effect останется активным. Состояние разойдётся.

Оптимизированный откат суффикса возможен, но потребует checkpoints:

```text
snapshot после A
→ снять C и B
→ восстановить snapshot после A
→ переиграть C
```

Для текущей системы это лишняя сложность.

## Практическая структура

```csharp
public sealed class PredictedEffectRecord
{
    public string ActivationId { get; }
    public long Sequence { get; }

    public AbilitySystemComponent Target { get; }
    public GameplayEffect EffectSpec { get; }

    public string LocalEffectGuid { get; internal set; }
}
```

Reconciliation выполняется отдельно для каждого затронутого ASC:

```csharp
var pending = tracker
    .GetPendingEffects(targetAsc)
    .OrderBy(x => x.Sequence)
    .ToList();

// Временно снять все pending.
for (int i = pending.Count - 1; i >= 0; i--)
    Remove(pending[i]);

RestoreAuthoritativeState(targetAsc);

tracker.RemoveActivation(rejectedActivationId);

// Переиграть оставшиеся.
foreach (var effect in tracker
             .GetPendingEffects(targetAsc)
             .OrderBy(x => x.Sequence))
{
    Reapply(effect);
}
```

Итоговая формула:

```text
Хранение: временная линия
Снятие: обратный порядок
Replay: прямой порядок

Authoritative state
+ ordered pending predictions
= локальное predicted state
```

Такой полный rebase — наиболее надёжный вариант для текущего пайплайна. Откат только части timeline становится разумной оптимизацией позже, если появятся промежуточные checkpoints.

[1]: https://raw.githubusercontent.com/apustovitovsky/relics-of-the-fallen/main/Assets/RelicsOfTheFallen/Scripts/Networking/AbilitySystem/NetworkAbilitySystemComponent.cs "raw.githubusercontent.com"
