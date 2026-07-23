```
Этап 2 — исправить GAS effect lifecycle
Примерно 2–4 файла в GAS:
ActiveGameplayEffectHandle;
корректный duration lifecycle;
удаление по handle/GUID;
GameplayEffectRemovalReason;
отмена timer;
reconciliation/suppression mode;
детерминированный effect spec.
Это обязательный фундамент настоящего rollback.
```

В целом **согласен**. Концептуальный алгоритм мы уже покрыли, но приведённое утверждение точнее описывает технические препятствия текущего GASify.

Сейчас действительно:

* `Instant`-эффект не попадает в `appliedGameplayEffects`, а сразу изменяет атрибут — удалить его невозможно, нужен authoritative restore;
* `RemoveDurationGE` — это `async void`, который после `Task.Delay` безусловно удаляет effect и вызывает `OnGameplayEffectRemoved`;
* `ApplyGameplayEffect` повторно проверяет tag requirements и `chanceToApply`;
* `OnGameplayEffectRemoved` используется для пересчёта тегов, обновления списка effects и может иметь другие внешние подписки. ([GitHub][1])

Поэтому этот алгоритм правильный:

```text
1. Снять pending Duration/Infinite effects в обратном порядке
2. Удалить rejected activation из prediction timeline
3. Восстановить authoritative attributes каждого затронутого ASC
4. Переиграть оставшиеся pending effects в прямом порядке
```

Но я бы внёс **три уточнения**.

## 1. `RemoveGameplayEffect` должен быть идемпотентным

Недостаточно просто удалить effect из списка. Старый `RemoveDurationGE` позже проснётся и повторно вызовет removal event.

Минимальная безопасная реализация:

```csharp
public bool RemoveGameplayEffect(string guid)
{
    var effect = appliedGameplayEffects
        .FirstOrDefault(x => x.guid == guid);

    if (effect == null)
        return false;

    appliedGameplayEffects.Remove(effect);

    RebuildDerivedGameplayState(effect);
    RaiseGameplayEffectRemoved(effect);

    return true;
}

private async void ExpireGameplayEffect(string guid, int milliseconds)
{
    await Task.Delay(milliseconds);

    // Если effect уже снят reconciliation — ничего не произойдёт.
    RemoveGameplayEffect(guid);
}
```

То есть таймер должен хранить или проверять **GUID**, а не безусловно вызывать removal для старой ссылки.

`ActiveGameplayEffectHandle` для этого действительно не обязателен.

## 2. `isReconciling` должен разделять внутренние и внешние события

Во время reconciliation всё ещё нужно:

* пересчитать modifiers;
* пересчитать granted tags;
* обновить `appliedGameplayEffects`.

Но не нужно повторно:

* создавать prediction records;
* проигрывать cues;
* запускать VFX/UI;
* реплицировать изменения;
* сообщать gameplay-коду, будто effect применился впервые.

Поэтому одного такого кода недостаточно:

```csharp
if (isReconciling)
    return;
```

если он отключит также пересчёт тегов.

Нужна структура:

```csharp
private void ApplyEffectInternal(
    GameplayEffect effect,
    ReconciliationContext context)
{
    AddToAppliedEffects(effect);
    RebuildModifiersAndTags();

    if (!context.SuppressExternalEvents)
        OnGameplayEffectApplied?.Invoke(effect);
}
```

То есть:

```text
внутреннее состояние GAS → обновляем всегда
внешние side effects      → подавляем при reconciliation
```

В текущем GASify внутренний пересчёт и внешние реакции смешаны через `OnGameplayEffectApplied/Removed`, поэтому эту часть действительно придётся немного разделить. ([GitHub][1])

## 3. Replay не должен перезапускать duration

В приведённом списке не хватает ещё одного обязательного поля:

```csharp
public double ExpiresAt { get; }
```

Иначе было:

```text
Invulnerable применён на 5 секунд
через 2 секунды произошёл reconciliation
replay снова запускает 5 секунд
итого effect проживёт 7 секунд
```

При replay нужно сохранить оставшееся время:

```csharp
double remaining = record.ExpiresAt - Time.timeAsDouble;

ApplyPredictedEffectSilently(
    record.EffectSpec,
    remainingDuration: remaining);
```

Поэтому финальная запись примерно такая:

```csharp
public sealed class PredictedEffectRecord
{
    public string Key { get; }
    public string ActivationId { get; }
    public long Sequence { get; }

    public AbilitySystemComponent Source { get; }
    public AbilitySystemComponent Target { get; }

    public GameplayEffect EffectSpec { get; }

    public string LocalEffectGuid { get; internal set; }

    // Для Duration. Для Infinite можно использовать null.
    public double? ExpiresAt { get; }
}
```

## Requirements и random

При техническом replay нельзя вызывать обычный:

```csharp
asc.ApplyGameplayEffect(...)
```

потому что он повторно:

```text
проверит ApplicationTagRequirements
перебросит chanceToApply
создаст новый timer
вызовет обычные events
```

Это видно непосредственно в текущем `ApplyGameplayEffect`. ([GitHub][1])

Нужна отдельная внутренняя операция:

```csharp
ReplayPredictedEffect(
    EffectSpec resolvedSpec,
    double? remainingDuration);
```

Она:

```text
не перебрасывает chance;
не создаёт новый prediction record;
не запускает внешние cues/events;
сохраняет оставшийся duration;
но восстанавливает modifiers и tags.
```

Причём `EffectSpec` должен представлять уже **разрешённый результат prediction**, а не исходный mutable effect.

## Итоговый обязательный минимум

```text
1. AbilityActivationRecord с упорядоченными PredictedEffectRecord

2. Идемпотентный RemoveGameplayEffect(guid)
   └── старый timer после ручного removal ничего не делает

3. Reconciliation scope
   ├── внутреннее состояние GAS обновляется
   └── внешние side effects подавляются

4. Resolved EffectSpec
   └── replay без повторного random chance

5. ExpiresAt / remaining duration
   └── replay не продлевает эффект

6. Authoritative attributes конкретного target ASC
   └── откат Instant effects
```

Таким образом, утверждение правильное, но список обязательных опор я бы дополнил **сохранением оставшегося duration**. Полностью переписывать duration-систему не требуется, однако текущий безусловный `async RemoveDurationGE` обязательно нужно заменить на централизованное идемпотентное удаление.

[1]: https://raw.githubusercontent.com/felipeggrod/gasify/main/Assets/GAS/Code/AbilitySystemComponent.cs "raw.githubusercontent.com"
