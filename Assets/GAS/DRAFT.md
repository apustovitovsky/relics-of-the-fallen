В целом lifecycle уже узнаваемо соответствует `UGameplayAbility`, но класс всё ещё примерно наполовину состоит из legacy definition/runtime-смешения. Главная следующая работа — не добавление новых способностей, а нормализация самой модели ability.

Что уже соответствует GAS:

- `CurrentSpecHandle`, `CurrentActorInfo`, `CurrentActivationInfo`;
- `MakeEffectContext()` и `GetSourceObject()`;
- `ApplyGameplayEffectSpecToOwner/Target()` с извлечением `PredictionKey`;
- `CommitAbility → CommitCheck → CommitExecute`;
- отдельные `Check/Apply Cost` и `Cooldown`;
- регистрация `AbilityTask` и завершение задач;
- `CancelAbility()` и `EndAbility()`;
- `ActivationOwnedTags`, block/cancel семантика — пока через legacy-контейнер.

Это совпадает с основным API оригинала: [GameplayAbility.h:127](<C:/Users/NATALY/Documents/unity/ue5-docs/UnrealEngine/Engine/Plugins/Runtime/GameplayAbilities/Source/GameplayAbilities/Public/Abilities/GameplayAbility.h:127>).

## 1. Убрать legacy-поля из базового GameplayAbility

Наиболее проблемный участок сейчас: [GameplayAbility.cs:233](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/Code/GameplayAbility.cs:233).

Из базового класса должны уйти:

```text
effectsSO
effects
source
owner
cuesTags
Guid
ClassName
Level
SerializeAdditionalData
DeserializeAdditionalData
```

Причины:

- В оригинальном `UGameplayAbility` нет универсального списка применяемых эффектов. Конкретная ability сама решает, какие specs создавать и куда применять.
- `Level` принадлежит `GameplayAbilitySpec`, а не runtime ability.
- `source/owner` дублируют `CurrentActorInfo.AbilitySystemComponent`.
- `Guid` и `ClassName` не участвуют в GAS identity. Для этого существуют `GameplayAbilitySpecHandle`, definition asset и prediction key.
- Gameplay Cues не являются произвольным списком внутри базовой ability.
- Методы сериализации без writer/reader не соответствуют ни GAS, ни нормальной Mirror-сериализации.

Например, [AbilityTask.cs:7](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/Code/GameplayAbilities/Tasks/AbilityTask.cs:7) должен получать ASC через:

```text
Ability.CurrentActorInfo.AbilitySystemComponent
```

а не через `Ability.owner`.

Особенно важно убрать дублирование `Level`: сейчас [GetAbilityLevel():139](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/Code/GameplayAbility.cs:139) возвращает локальное поле, хотя правильным источником уже является [GameplayAbilitySpec.Level:22](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/Code/GameplayAbilities/GameplayAbilitySpec.cs:22).

## 2. Полностью заменить AbilityTags

Текущий [AbilityTags:1218](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/Code/GameplayAbility.cs:1218) — legacy-контейнер:

- использует `List<GameplayTag>`;
- содержит дублирующие string-списки;
- требует `FillTags/ClearStrings`;
- смешивает разные категории;
- часть полей вообще не используется.

Вместо него на `GameplayAbility` должны находиться отдельные `GameplayTagContainer` с GAS-совместимыми именами:

```text
AssetTags
CancelAbilitiesWithTag
BlockAbilitiesWithTag
ActivationOwnedTags
ActivationRequiredTags
ActivationBlockedTags
SourceRequiredTags
SourceBlockedTags
TargetRequiredTags
TargetBlockedTags
```

И метод:

```text
DoesAbilitySatisfyTagRequirements
```

Оригинальная реализация проверяет четыре группы:

```text
AssetTags против заблокированных ability tags ASC
OwnedTags против ActivationRequired/Blocked
SourceTags против SourceRequired/Blocked
TargetTags против TargetRequired/Blocked
```

Референс: [GameplayAbility.cpp:349](<C:/Users/NATALY/Documents/unity/ue5-docs/UnrealEngine/Engine/Plugins/Runtime/GameplayAbilities/Source/GameplayAbilities/Private/Abilities/GameplayAbility.cpp:349>).

Сейчас [CanActivateAbility():1039](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/Code/GameplayAbility.cs:1039) проверяет только блокировку description tags, cooldown и cost. Существующие `SourceTagsRequired/Forbidden` и `TargetTagsRequired/Forbidden` фактически мёртвые.

Это наиболее важная функциональная недостача.

## 3. Добавить core-политики GameplayAbility

Сейчас любая выданная ability сразу получает один `PrimaryInstance`, поэтому архитектура жёстко соответствует только:

```text
InstancedPerActor
```

В оригинале существуют:

```csharp
GameplayAbilityInstancingPolicy
{
    NonInstanced,
    InstancedPerActor,
    InstancedPerExecution
}

GameplayAbilityNetExecutionPolicy
{
    LocalPredicted,
    LocalOnly,
    ServerInitiated,
    ServerOnly
}

GameplayAbilityNetSecurityPolicy
{
    ClientOrServer,
    ServerOnlyExecution,
    ServerOnlyTermination,
    ServerOnly
}

GameplayAbilityReplicationPolicy
{
    ReplicateNo,
    ReplicateYes
}
```

Референс: [GameplayAbilityTypes.h:37](<C:/Users/NATALY/Documents/unity/ue5-docs/UnrealEngine/Engine/Plugins/Runtime/GameplayAbilities/Source/GameplayAbilities/Public/Abilities/GameplayAbilityTypes.h:37>).

Особенно нужен `NetExecutionPolicy`. Сейчас ASC практически любую клиентскую активацию рассматривает как predicted и отправляет серверу: [AbilitySystemComponent.cs:2584](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/Code/AbilitySystemComponent.cs:2584). Поэтому невозможно корректно выразить `ServerOnly`, `LocalOnly` и `ServerInitiated`.

В Lyra базовые настройки такие:

```text
ReplicationPolicy = ReplicateNo
InstancingPolicy = InstancedPerActor
NetExecutionPolicy = LocalPredicted
NetSecurityPolicy = ClientOrServer
```

Референс: [LyraGameplayAbility.cpp:39](<C:/Users/NATALY/Documents/unity/ue5-docs/UnrealEngine/Samples/Games/Lyra/Source/LyraGame/AbilitySystem/Abilities/LyraGameplayAbility.cpp:39>).

Именно эти значения логично установить в `CommonGameplayAbility`, тогда как сами enum и их lifecycle принадлежат core GAS.

## 4. Исправить активность и повторную активацию

Сейчас [CanActivateAbility():1049](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/Code/GameplayAbility.cs:1049) безусловно запрещает активацию, если `IsActive == true`.

В оригинале `CanActivateAbility()` такого универсального запрета не содержит: [GameplayAbility.cpp:457](<C:/Users/NATALY/Documents/unity/ue5-docs/UnrealEngine/Engine/Plugins/Runtime/GameplayAbilities/Source/GameplayAbilities/Private/Abilities/GameplayAbility.cpp:457>).

Поведение зависит от:

- `InstancingPolicy`;
- активных instances в `GameplayAbilitySpec`;
- `bRetriggerInstancedAbility`;
- возможности нескольких `InstancedPerExecution`.

Поэтому нужны:

```text
GameplayAbilitySpec.ActiveCount
GameplayAbilitySpec.Instances
RetriggerInstancedAbility
```

А `PrimaryInstance` должен оставаться только удобным доступом к основной `InstancedPerActor` копии.

## 5. Добавить управляемое runtime-состояние ability

В оригинале ability во время выполнения хранит:

```text
IsActive
IsAbilityEnding
CanBeCanceled
IsBlockingOtherAbilities
RemoteInstanceEnded
CurrentEventData
TrackedGameplayCues
```

Референсы:

- поля: [GameplayAbility.h:697](<C:/Users/NATALY/Documents/unity/ue5-docs/UnrealEngine/Engine/Plugins/Runtime/GameplayAbilities/Source/GameplayAbilities/Public/Abilities/GameplayAbility.h:697>);
- end lifecycle: [GameplayAbility.cpp:802](<C:/Users/NATALY/Documents/unity/ue5-docs/UnrealEngine/Engine/Plugins/Runtime/GameplayAbilities/Source/GameplayAbilities/Private/Abilities/GameplayAbility.cpp:802>).

Для нас наиболее полезны:

```csharp
public bool CanBeCanceled();
public void SetCanBeCanceled(bool canBeCanceled);

public bool IsBlockingOtherAbilities();
public void SetShouldBlockOtherAbilities(bool shouldBlockAbilities);
```

Это позволит channel/cast abilities запрещать отмену на определённой фазе, а `CommonGameplayAbility` — корректно управлять `ExclusiveReplaceable`.

`IsActive` при этом должен иметь закрытый setter, а не быть публичным изменяемым полем.

## 6. Добавить стандартное создание GameplayEffectSpec через ability

Сейчас `ApplyGameplayEffectToOwner/Target()` непосредственно вызывает `ASC.MakeOutgoingSpec()`: [GameplayAbility.cs:564](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/Code/GameplayAbility.cs:564).

В оригинале между ними существует:

```text
GameplayAbility.MakeOutgoingGameplayEffectSpec
→ ASC.MakeOutgoingSpec
→ ApplyAbilityTagsToGameplayEffectSpec
→ перенести SetByCaller из AbilitySpec
```

Референс: [GameplayAbility.cpp:1365](<C:/Users/NATALY/Documents/unity/ue5-docs/UnrealEngine/Engine/Plugins/Runtime/GameplayAbilities/Source/GameplayAbilities/Private/Abilities/GameplayAbility.cpp:1365>).

Это нужно, чтобы GameplayEffectSpec получил:

- asset tags ability;
- dynamic spec source tags;
- теги `SourceObject`;
- `SetByCaller` magnitudes, сохранённые в ability spec.

Пока наши specs эту часть происхождения ability теряют.

## 7. Ability triggers — позже

В оригинале ability может автоматически активироваться:

```text
GameplayEvent
OwnedTagAdded
OwnedTagPresent
```

Через `AbilityTriggers`: [GameplayAbilityTriggerType.h:9](<C:/Users/NATALY/Documents/unity/ue5-docs/UnrealEngine/Engine/Plugins/Runtime/GameplayAbilities/Source/GameplayAbilities/Public/Abilities/GameplayAbilityTriggerType.h:9>).

С ними связаны:

```text
AbilityTriggerData
ShouldAbilityRespondToEvent
CurrentEventData
```

Это понадобится для пассивных способностей, реакций на попадание, смерть, получение тега и подобных сценариев. Для текущих Fireball/Frostbolt это не блокирует разработку.

## Что не следует переносить сейчас

Не нужно буквально копировать:

- `K2_*` Blueprint-обёртки;
- `bReplicateInputDirectly` — мы уже используем более правильные generic replicated events;
- UObject RPC/replicated-property infrastructure;
- `NonInstanced`, пока для него нет реального сценария;
- task management по `InstanceName`;
- все animation montage convenience wrappers.

## Рекомендуемый порядок

1. Заменить `AbilityTags` на прямые `GameplayTagContainer` и реализовать `DoesAbilitySatisfyTagRequirements`.
2. Убрать `Level`, `owner/source`, GUID/ClassName и остальные legacy-поля.
3. Добавить `InstancingPolicy`, `NetExecutionPolicy`, `NetSecurityPolicy` и перенести решения об активации в ASC.
4. Добавить `CanBeCanceled`, blocking state, active count и retrigger.
5. Добавить `MakeOutgoingGameplayEffectSpec`.
6. Затем реализовать event/tag triggers.

Итог: эффектный, commit-, task- и prediction-pipeline уже имеют правильный каркас. Главные расхождения сейчас находятся в полях definition/runtime, теговых требованиях и отсутствии execution/instancing policies. Следующей целью лучше сделать именно `AbilityTags → GameplayTagContainer + DoesAbilitySatisfyTagRequirements`, потому что это закрывает уже существующую, но сейчас неработающую часть публичного API.