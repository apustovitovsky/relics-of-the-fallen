Начать лучше с **безопасного рефакторинга без изменения rollback-алгоритма**. `AbilityActivationTracker` заменяет:

```csharp
localAbilityActivationsBuffer
localEffectsBuffer
```

Он хранит состояние каждой активации и связанные predicted effects. Mirror, ASC и фактический rollback пока остаются в `NetworkAbilitySystemComponent`.

Особенно важно разделить `Requested` и `Predicted`: GASify вызывает `OnGameplayAbilityTryActivate` **до** buffering и `CanActivateAbility`, а `OnGameplayAbilityActivated` — только после успешного `CommitAbility`. Сейчас ваш класс кладёт активацию в prediction buffer слишком рано. ([GitHub][1])

## 1. Модель активации

```csharp
using System;
using System.Collections.Generic;
using GAS;

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
    public string Key { get; }
    public GameplayEffect LocalEffect { get; }
    public AbilitySystemComponent Target { get; }

    public PredictedEffectRecord(
        string key,
        GameplayEffect localEffect,
        AbilitySystemComponent target)
    {
        Key = key;
        LocalEffect = localEffect;
        Target = target;
    }
}

public sealed class AbilityActivationRecord
{
    public string Id { get; }
    public GameplayAbility Ability { get; }
    public AbilitySystemComponent Source { get; }
    public AbilitySystemComponent Target { get; }

    public AbilityActivationState State { get; internal set; }
    public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;

    public List<PredictedEffectRecord> Effects { get; } = new();

    public AbilityActivationRecord(
        string id,
        GameplayAbility ability,
        AbilitySystemComponent source,
        AbilitySystemComponent target)
    {
        Id = id;
        Ability = ability;
        Source = source;
        Target = target;
        State = AbilityActivationState.Requested;
    }
}
```

## 2. Сам `AbilityActivationTracker`

```csharp
using System;
using System.Collections.Generic;
using GAS;

public sealed class AbilityActivationTracker
{
    private readonly Dictionary<string, AbilityActivationRecord> _records = new();
    private readonly Dictionary<string, PredictedEffectRecord> _effects = new();

    public event Action<AbilityActivationRecord> StateChanged;

    public AbilityActivationRecord BeginRequest(
        string activationId,
        GameplayAbility ability,
        AbilitySystemComponent source,
        AbilitySystemComponent target)
    {
        var record = new AbilityActivationRecord(
            activationId,
            ability,
            source,
            target);

        if (!_records.TryAdd(activationId, record))
        {
            throw new InvalidOperationException(
                $"Activation '{activationId}' is already registered.");
        }

        Notify(record);
        return record;
    }

    public bool MarkPredicted(string activationId)
    {
        return SetState(
            activationId,
            AbilityActivationState.Predicted);
    }

    public bool Confirm(string activationId)
    {
        return SetState(
            activationId,
            AbilityActivationState.Confirmed);
    }

    public bool Reject(
        string activationId,
        out AbilityActivationRecord record)
    {
        if (!_records.TryGetValue(activationId, out record))
            return false;

        record.State = AbilityActivationState.Rejected;
        Notify(record);
        return true;
    }

    public bool Complete(string activationId)
    {
        return SetState(
            activationId,
            AbilityActivationState.Completed);
    }

    public bool TryGet(
        string activationId,
        out AbilityActivationRecord record)
    {
        return _records.TryGetValue(activationId, out record);
    }

    public bool IsPredicted(string activationId)
    {
        return _records.TryGetValue(activationId, out var record) &&
               record.State is AbilityActivationState.Predicted
                   or AbilityActivationState.Confirmed;
    }

    public bool TrackEffect(GameplayEffect effect)
    {
        if (string.IsNullOrEmpty(effect.applicationGUID))
            return false;

        if (!_records.TryGetValue(
                effect.applicationGUID,
                out var activation))
        {
            return false;
        }

        var key = BuildEffectKey(activation, effect);

        if (_effects.ContainsKey(key))
            return false;

        var effectRecord = new PredictedEffectRecord(
            key,
            effect,
            effect.target);

        _effects.Add(key, effectRecord);
        activation.Effects.Add(effectRecord);

        return true;
    }

    public bool TryConfirmEffect(
        GameplayEffect authoritativeEffect,
        out PredictedEffectRecord predictedEffect)
    {
        predictedEffect = null;

        if (string.IsNullOrEmpty(authoritativeEffect.applicationGUID))
            return false;

        if (!_records.TryGetValue(
                authoritativeEffect.applicationGUID,
                out var activation))
        {
            return false;
        }

        var key = BuildEffectKey(
            activation,
            authoritativeEffect);

        if (!_effects.Remove(key, out predictedEffect))
            return false;

        // Локальный effect теперь получает серверный GUID.
        predictedEffect.LocalEffect.guid =
            authoritativeEffect.guid;

        activation.Effects.Remove(predictedEffect);
        return true;
    }

    public void Remove(string activationId)
    {
        if (!_records.Remove(
                activationId,
                out var record))
        {
            return;
        }

        foreach (var effect in record.Effects)
            _effects.Remove(effect.Key);
    }

    private bool SetState(
        string activationId,
        AbilityActivationState state)
    {
        if (!_records.TryGetValue(
                activationId,
                out var record))
        {
            return false;
        }

        record.State = state;
        Notify(record);
        return true;
    }

    private void Notify(AbilityActivationRecord record)
    {
        StateChanged?.Invoke(record);
    }

    private static string BuildEffectKey(
        AbilityActivationRecord activation,
        GameplayEffect effect)
    {
        var ability = activation.Ability;

        if (ability.cost != null &&
            ability.cost.name == effect.name)
        {
            return $"COST_{activation.Id}";
        }

        if (ability.cooldown != null &&
            ability.cooldown.name == effect.name)
        {
            return $"CD_{activation.Id}";
        }

        var index = ability.effects.FindIndex(
            candidate => candidate.name == effect.name);

        return $"{index}_{activation.Id}";
    }
}
```

Это пока сохраняет текущую схему ключей `effectIndex_activationGUID`. Позднее лучше дать каждому effect slot стабильный ID, поскольку поиск по имени неоднозначен.

---

## 3. Подключение к `NetworkAbilitySystemComponent`

```csharp
public class NetworkAbilitySystemComponent : NetworkBehaviour
{
    public AbilitySystemComponent asc;

    private AbilityActivationTracker _activationTracker;

    private void Awake()
    {
        _activationTracker = new AbilityActivationTracker();
        _activationTracker.StateChanged += OnActivationStateChanged;
    }

    private void OnDestroy()
    {
        if (_activationTracker != null)
        {
            _activationTracker.StateChanged -=
                OnActivationStateChanged;
        }
    }
}
```

На локальном клиенте подписываемся и на попытку, и на успешный commit:

```csharp
private void InitializeClient()
{
    asc.OnGameplayAbilityTryActivate +=
        LocalTryActivateAbility;

    asc.OnGameplayAbilityActivated +=
        OnLocalAbilityActivated;

    asc.OnGameplayAbilityDeactivated +=
        OnLocalAbilityDeactivated;

    asc.OnGameplayEffectApplied +=
        OnClientGameplayEffectApplied;
}
```

## 4. Попытка активации

Здесь создаётся только `Requested`:

```csharp
private void LocalTryActivateAbility(
    GameplayAbility ability,
    string ignoredActivationId)
{
    if (ability.source != localPlayerAsc)
        return;

    var targetIdentity =
        ability.target?.GetComponentInParent<NetworkIdentity>();

    if (targetIdentity == null)
        return;

    var activationId = Guid.NewGuid().ToString();

    ability.activationGUID = activationId;

    _activationTracker.BeginRequest(
        activationId,
        ability,
        ability.source,
        ability.target);

    CmdTryActivateAbility(
        ability.guid,
        targetIdentity.netId,
        activationId);

    if (!predictGameplayAbilityActivations)
    {
        // Не даём локальному TryActivateAbility пройти дальше.
        ability.isActive = true;
    }
}
```

Пока buffering и `CanActivateAbility` ещё не завершились:

```text
Requested ≠ Predicted
```

## 5. Успешный локальный commit

```csharp
private void OnLocalAbilityActivated(
    GameplayAbility ability,
    string activationId)
{
    if (!isLocalPlayer)
        return;

    if (ability.source != asc)
        return;

    if (string.IsNullOrEmpty(activationId))
        return;

    _activationTracker.MarkPredicted(activationId);
}
```

Теперь анимация может подписываться на tracker:

```csharp
private void OnActivationStateChanged(
    AbilityActivationRecord activation)
{
    switch (activation.State)
    {
        case AbilityActivationState.Predicted:
            // Начать predicted presentation.
            break;

        case AbilityActivationState.Confirmed:
            // Продолжить уже начатую presentation.
            break;

        case AbilityActivationState.Rejected:
            // Отменить presentation.
            break;
    }
}
```

---

## 6. Подтверждение сервера

```csharp
[ClientRpc]
private void RpcOnGameplayAbilityActivated(
    string abilityGuid,
    uint sourceNetId,
    uint targetNetId,
    string activationId)
{
    if (!isClientOnly)
        return;

    var source = ascmDictionary[sourceNetId];
    var target = ascmDictionary[targetNetId];

    // Это owner-клиент, который уже выполнил Commit.
    if (isLocalPlayer &&
        activationId != null &&
        _activationTracker.Confirm(activationId))
    {
        return;
    }

    // Remote client: prediction отсутствует.
    var ability = asc.grantedGameplayAbilities.Find(
        candidate => candidate.guid == abilityGuid);

    ability?.CommitAbility(
        source.asc,
        target.asc,
        activationId);
}
```

## 7. Регистрация predicted effect

Effect может применяться к чужому ASC, поэтому искать tracker нужно через **source ASC**, а не через target:

```csharp
private void OnClientGameplayEffectApplied(
    GameplayEffect effect)
{
    if (effect.source == null)
        return;

    var sourceNetwork =
        effect.source.GetComponentInParent<
            NetworkAbilitySystemComponent>();

    if (sourceNetwork == null ||
        !sourceNetwork.isLocalPlayer)
    {
        return;
    }

    sourceNetwork._activationTracker
        .TrackEffect(effect);
}
```

Например:

```text
Activation 42
├── COST_42
├── CD_42
└── 0_42 — дебафф на чужом target ASC
```

---

## 8. Подтверждение predicted effect

В `SynchronizeGE`:

```csharp
private void SynchronizeGE(
    SyncDictionary<string, GameplayEffect>.Operation operation,
    string key,
    GameplayEffect authoritativeEffect)
{
    if (!isClientOnly)
        return;

    if (operation == SyncDictionary<string, GameplayEffect>
            .Operation.OP_ADD)
    {
        var sourceNetwork =
            authoritativeEffect.source?
                .GetComponentInParent<
                    NetworkAbilitySystemComponent>();

        if (sourceNetwork != null &&
            sourceNetwork.isLocalPlayer &&
            sourceNetwork._activationTracker.TryConfirmEffect(
                authoritativeEffect,
                out _))
        {
            // Effect уже был применён prediction.
            // Второй раз его не применяем.
            return;
        }

        asc.ApplyGameplayEffect(
            authoritativeEffect.source,
            authoritativeEffect.target,
            authoritativeEffect,
            authoritativeEffect.guid);
    }
}
```

---

## 9. Reject

```csharp
[ClientRpc]
private void RpcGameplayAbilityUndo(
    string activationId,
    ActivationFailure failure)
{
    if (!isClientOnly)
        return;

    if (!_activationTracker.Reject(
            activationId,
            out var activation))
    {
        return;
    }

    activation.Ability.DeactivateAbility(activationId);

    asc.OnGameplayAbilityFailedActivation?.Invoke(
        activation.Ability,
        activationId,
        failure);

    RollbackPredictedActivation(activation);

    _activationTracker.Remove(activationId);
}
```

На первом этапе `RollbackPredictedActivation` может содержать ваш текущий reset к `syncAttributes`. После этого его можно заменить на нормальный rebase:

```csharp
private void RollbackPredictedActivation(
    AbilityActivationRecord rejected)
{
    // 1. Удалить predicted effects rejected.Effects.
    // 2. Взять authoritative snapshot каждого затронутого ASC.
    // 3. Переиграть оставшиеся pending predictions.
    // 4. Отменить связанные presentation/VFX.
}
```

## Что получилось

```text
NetworkAbilitySystemComponent
├── Mirror Commands/RPC
├── SyncDictionary
└── вызывает Tracker

AbilityActivationTracker
├── Requested/Predicted/Confirmed/Rejected
├── activationId → ability
└── activationId → predicted effects

ASC
├── buffering
├── CanActivate
├── Commit
└── gameplay execution
```

Главное улучшение уже на этом этапе: **серверное подтверждение больше не сопоставляется с безликим статическим словарём**. Оно сопоставляется с конкретной записью активации, которая знает ability, source, target, состояние и все созданные ею predicted effects.

[1]: https://raw.githubusercontent.com/felipeggrod/gasify/main/Assets/GAS/Code/AbilitySystemComponent.cs "raw.githubusercontent.com"
