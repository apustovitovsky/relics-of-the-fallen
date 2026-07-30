# GAS API

## Совместимость

Публичный API сохраняет назначение, жизненный цикл и основной нейминг Unreal Gameplay Ability System с адаптацией под C# и Unity.

Внутренние механизмы prediction, хранения и reconciliation могут отличаться от Unreal GAS и не являются пользовательским API.

## Сетевая репликация

`AbilitySystemComponent` остаётся единственным владельцем gameplay-состояния и не зависит от Mirror. Объект может использовать core GAS без `NetworkIdentity` и сетевых компонентов:

```text
Offline Actor
└─ AbilitySystemComponent
```

Сетевой prefab содержит одинаковый набор компонентов на сервере и всех клиентах:

```text
Networked Actor
├─ AbilitySystemComponent
├─ NetworkAbilitySystemComponent
└─ NetworkAbilitySystemObserverComponent
```

Сетевые компоненты разделены по аудитории и направлению взаимодействия, а не по типам GAS-данных:

```text
NetworkAbilitySystemComponent
→ Network Sync Mode: Owner
→ прямое взаимодействие owning client и server
→ GameplayAbilitySpec, prediction и приватное состояние ASC

NetworkAbilitySystemObserverComponent
→ Network Sync Mode: Observers
→ публичное server-to-client состояние ASC
```

Все три компонента физически присутствуют на каждой сетевой копии prefab. На authoritative server оба сетевых адаптера публикуют соответствующее состояние. Owning client получает owner- и observer-потоки, а simulated proxy получает только observer-поток; owner-компонент на нём существует, но Mirror не сериализует ему его состояние.

`GameplayAbilitySpec` относится к owner-only состоянию и реплицируется через `NetworkAbilitySystemComponent`. Публичные атрибуты, gameplay tags и Gameplay Cues относятся к observer-состоянию. Stateful-контейнеры используют Mirror `SyncDictionary`, чтобы автоматически получать начальное состояние и последующие дельты.

Зависимость направлена только из networking-слоя в GAS. `AbilitySystemComponent` не проверяет Mirror authority, не хранит `netId` или `NetworkConnection` и не требует наличия сетевых адаптеров.

### Сетевая активация Gameplay Ability

Offline-код вызывает `AbilitySystemComponent.TryActivateAbility()` напрямую. Сетевой input вызывает `NetworkAbilitySystemComponent.TryActivateAbility()`, который явно управляет локальной prediction и запросом к authoritative server:

```text
NetworkAbilitySystemComponent.TryActivateAbility
→ создать PredictionKey
→ установить activation mode Predicting
→ дождаться локального AbilitySystemComponent.TryActivateAbility
→ при успехе вызвать CallServerTryActivateAbility
→ ServerTryActivateAbility
→ InternalServerTryActivateAbility
→ дождаться authoritative AbilitySystemComponent.TryActivateAbility
→ ClientActivateAbilitySucceed или ClientActivateAbilityFailed
```

Локально отклонённая activation не отправляется серверу. Её `PredictionKey` немедленно получает reject, что запускает откат связанных predicted effects и modifiers. Серверный результат передаётся owning client явным Target RPC; в owner activation lifecycle события `OnGameplayAbilityTryActivate`, `OnGameplayAbilityActivated` и `OnGameplayAbilityFailedActivation` используются только как уведомления.

Observer RPC активации ability и legacy `syncGrantedAbilities` удалены. `GameplayAbilitySpec` реплицируется только owning client через `NetworkAbilitySystemComponent`; simulated proxies не получают и не запускают abilities. Визуальное состояние передаётся им через montage replication и Gameplay Cues.

Из-за асинхронного input buffering Unity-версия возвращает `UniTask<bool>` вместо синхронного `bool`, используемого `TryActivateAbility()` в Unreal GAS. Значение `true` означает, что ability прошла проверки и вошла в active state.

`Full`, `Mixed` и `Minimal` являются политикой маршрутизации `ActiveGameplayEffect`, а не отдельными сетевыми компонентами. На текущем этапе полные `ActiveGameplayEffect` не реплицируются observers, поэтому режим `Full` не считается реализованным и не должен неявно подменяться поведением `Mixed`.

## GameplayEffectSpec

`GameplayEffectSpec` содержит runtime-данные одного применения `GameplayEffect`.

Spec создаётся методом `AbilitySystemComponent.MakeOutgoingSpec()`. При создании захватываются snapshot-атрибуты Source.

Перед применением создаётся отдельная копия spec, захватываются необходимые данные Target и вычисляются magnitude модификаторов.

`SetSetByCallerMagnitude()` сохраняет runtime-величину по `GameplayTag`.


### GameplayEffectContext

`GameplayEffectContext` хранит происхождение конкретного применения эффекта. Он не содержит правила `GameplayEffect` или вычисленные значения `GameplayEffectSpec`.

`AbilitySystemComponent.MakeEffectContext()` создаёт базовый контекст, где `Instigator` соответствует `OwnerActor`, а `EffectCauser` — текущему `AvatarActor`.

`GameplayAbility.MakeEffectContext()` дополнительно сохраняет:

- persistent definition создавшей контекст ability;
- локальный runtime instance ability;
- ability level на момент создания;
- `SourceObject` из соответствующего `GameplayAbilitySpec`.

Участники применения имеют разное назначение:

```text
Instigator
→ непосредственный инициатор текущего эффекта

OriginalInstigator
→ первый инициатор всей цепочки эффектов

EffectCauser
→ физический источник, например avatar, weapon или projectile

SourceObject
→ объект, предоставивший ability, например weapon или item
```

`GameplayEffectContextHandle` хранит полиморфную ссылку на context и предоставляет основной API без раскрытия внутреннего объекта.

Копия `GameplayEffectSpec` по умолчанию разделяет context с исходным spec. Перед независимой модификацией context необходимо вызвать:

```csharp
copiedSpec.DuplicateEffectContext();
```

`DuplicateEffectContext()` создаёт отдельный context через виртуальный `GameplayEffectContext.Duplicate()`. Производные context-типы должны переопределять `Duplicate()`, если содержат собственные mutable-данные.

Полный локальный pipeline:

```text
GameplayAbilitySpec
→ GameplayAbility.MakeEffectContext
    → SetAbility
    → AddSourceObject
→ AbilitySystemComponent.MakeOutgoingSpec
→ GameplayEffectSpec.EffectContext
→ применение эффекта
```

`GetAbilityInstance_NotReplicated()` возвращает только локальный runtime instance. Он не является частью сетевого состояния.

Текущий Mirror transport восстанавливает для реплицированного active effect только исходный ASC через `SourceNetworkId`. Остальные поля context пока не сериализуются, поэтому удалённая копия context является частичной.


### GameplayAbility commit

```text
TryActivateAbility
→ CanActivateAbility
→ CallActivateAbility
    → PreActivate
    → NotifyAbilityActivated
    → ActivateAbility
```

`CommitAbility()` не запускает ability. Он выполняет финальный `CommitCheck()`, после чего `CommitExecute()` применяет cooldown и cost.

`CommitCheck()` повторно вызывает `CheckCooldown()` и `CheckCost()`, поскольку состояние атрибутов могло измениться между первоначальным `CanActivateAbility()` и фактическим commit.

`CommitAbilityCost()` и `CommitAbilityCooldown()` позволяют выполнить только одну часть commit. Основной `CommitAbility()` применяет обе части через `CommitExecute()`.

### Активация GameplayAbility

`AbilitySystemComponent.TryActivateAbility()` выполняет первоначальный `CanActivateAbility()`. После успешной проверки `CallActivateAbility()` переводит ability в активное состояние и вызывает её `ActivateAbility()`.

Активация сама по себе не списывает cost и не запускает cooldown. Конкретная ability вызывает `CommitAbility()` в подходящий момент. Простые Instant и Projectile abilities выполняют commit сразу, а abilities с targeting, cast или подтверждением попадания могут отложить его.

```text
TryActivateAbility
→ CanActivateAbility
→ CallActivateAbility
→ ActivateAbility
→ CommitAbility в выбранный ability момент
```

### Ability cost

Стандартный ability cost представляет собой Instant GameplayEffect с отрицательными `Additive` modifiers.

Перед активацией `CheckCost()` создаёт `GameplayEffectSpec`, захватывает Source и Target attributes и вычисляет magnitude тем же способом, который используется при последующем применении cost. Проверка выполняется относительно текущего агрегированного `CurrentValue`.

Стандартная реализация не принимает `Multiplicative`, `Division` и `Override` cost modifiers. Для нестандартных ресурсов или иной семантики стоимости следует переопределить `CheckCost()`.

### Совместимость legacy modifiers

Пока существующие GameplayEffect assets не мигрированы, `GameplayEffectSpec` поддерживает временный compatibility bridge. Если новые `ModifierDefinitions` отсутствуют, legacy `Modifier` преобразуется в `Additive` definition с `ConstantMagnitude`.

Если у GameplayEffect заполнены новые definitions, legacy-список полностью игнорируется. Одновременное исполнение двух представлений modifier-а не допускается.

Compatibility bridge предназначен только для миграции существующих assets. Новые GameplayEffect следует создавать через `AttributeModifierDefinition`.

## Применение GameplayEffect

`AbilitySystemComponent.ApplyGameplayEffectSpecToSelf()` является основной точкой применения подготовленного spec и возвращает `ActiveGameplayEffectHandle`.

После проверки application requirements и chance создаётся отдельная application-копия spec, захватываются данные Target и вычисляются modifier magnitudes.

Дальнейший жизненный цикл зависит от duration policy:

```text
Instant
→ выполняется сразу
→ не сохраняется в ActiveGameplayEffectsContainer
→ возвращает успешный, но недействительный handle

Authoritative Instant последовательно выполняет каждый рассчитанный `AttributeModifierSpec` над `BaseValue`. Перед фиксацией значения вызывается `PreAttributeBaseChange`, после чего `CurrentValue` пересчитывается с учётом активных modifiers и отправляется attribute-change notification.

Instant не добавляет modifier в агрегатор: его результат становится новой постоянной основой атрибута. Активные Duration и Infinite modifiers продолжают вычисляться поверх обновлённого `BaseValue`.

Infinite
→ создаёт ActiveGameplayEffect
→ остаётся активным до явного удаления

Duration
→ создаёт ActiveGameplayEffect
→ удаляется по истечении duration
```

## ActiveGameplayEffect

`ActiveGameplayEffect` содержит runtime-состояние применённого активного эффекта: spec, время действия, prediction identity и владение установленными модификаторами.

Обычные authoritative Instant-эффекты не должны сохраняться как `ActiveGameplayEffect`. Предсказанный Instant может временно храниться как активный overlay до подтверждения или отклонения prediction.

### Ongoing Tag Requirements

`Ongoing Tag Requirements` управляют включённым состоянием уже применённого `Duration` или `Infinite` эффекта. Они отличаются от `Application Tag Requirements`: application requirements могут полностью отклонить применение, тогда как ongoing requirements оставляют `ActiveGameplayEffect` зарегистрированным.

При регистрации эффект проверяет required и forbidden tags целевого ASC:

```text
requirements выполнены
→ IsInhibited = false
→ modifiers установлены
→ GrantedTags добавлены

requirements не выполнены
→ IsInhibited = true
→ modifiers и GrantedTags отсутствуют
→ ActiveGameplayEffect остаётся зарегистрированным
```

## ActiveGameplayEffectHandle

`ActiveGameplayEffectHandle` позволяет ссылаться на конкретный активный `GameplayEffect`, не раскрывая внутреннее числовое представление identity.

Результат применения проверяется через `IsValid` и `WasSuccessfullyApplied`:

| Результат применения | `IsValid` | `WasSuccessfullyApplied` |
| --- | ---: | ---: |
| Эффект отклонён | `false` | `false` |
| Instant выполнен | `false` | `true` |
| Duration или Infinite активен | `true` | `true` |

Успешный Instant не создаёт `ActiveGameplayEffect`, поэтому у него нет действительного handle для последующего удаления. `WasSuccessfullyApplied` позволяет отличить его от отклонённого эффекта.

Как и в Unreal GAS, handle является локальной runtime-identity и не используется для сопоставления серверного эффекта с клиентской prediction. Для сетевого reconciliation используются `PredictionKey` и внутренняя authoritative replication identity.

Один `PredictionKey` может принадлежать нескольким predicted active effects, созданным внутри одной prediction window. При reject или catch-up контейнер удаляет все локальные predicted effects с этим ключом. Реплицированные authoritative-копии имеют собственную replication identity и продолжают существовать независимо.

Активный эффект можно получить через `AbilitySystemComponent.GetActiveGameplayEffect()` и удалить через `AbilitySystemComponent.RemoveActiveGameplayEffect()`. После удаления контейнер снимает все принадлежащие эффекту модификаторы и очищает его runtime-индексы.

Текущая версия удаляет эффект целиком. Частичное удаление stacks, поддерживаемое Unreal GAS, будет добавлено вместе со stacking.

В отличие от текущего Unreal GAS, Unity-реализация пока не поддерживает глобальный поиск owning ASC по handle. Операции с handle выполняются через ASC, которому принадлежит эффект.

## ActiveGameplayEffectsContainer

`ActiveGameplayEffectsContainer` принадлежит одному `AbilitySystemComponent` и соответствует `FActiveGameplayEffectsContainer` из Unreal GAS.

Контейнер доступен через `AbilitySystemComponent.ActiveGameplayEffects` и создаётся самим ASC. Пользовательскому коду не следует самостоятельно создавать контейнеры или регистрировать в них эффекты.

Контейнер хранит authoritative и predicted active effects, устанавливает их модификаторы в агрегаторы атрибутов и удаляет только принадлежащие эффекту модификаторы.

Регистрация и reconciliation являются внутренними операциями. Пользовательский код применяет эффекты через `AbilitySystemComponent`.

## GameplayTagCountContainer

`AbilitySystemComponent.OwnedGameplayTags` хранит агрегированное runtime-состояние gameplay tags, принадлежащих ASC.

Каждый тег имеет count источников. Если несколько abilities или active effects выдают один тег, удаление одного источника уменьшает count, но не удаляет тег, пока count остаётся больше нуля.

```text
Ability A выдаёт State.Burning → count 1
Effect B выдаёт State.Burning  → count 2
Ability A завершается          → count 1
Effect B удаляется             → count 0
```

`AbilitySystemComponent.RegisterGameplayTagEvent()` регистрирует адресный callback для одного owned tag. `NewOrRemoved` вызывается только при переходе count между нулём и положительным значением, а `AnyCountChange` — при каждом изменении count.

Метод возвращает `IDisposable`, который представляет lifetime подписки. В отличие от Unreal `FDelegateHandle`, C#-подписка содержит всю информацию для удаления callback и снимается вызовом `Dispose()`.

`ActiveGameplayEffect` хранит подписки своих ongoing requirements до окончательного removal. Inhibition не снимает эти подписки, поскольку effect должен продолжать отслеживать момент восстановления требований.

## Attribute

`BaseValue` представляет постоянное значение атрибута.

`CurrentValue` вычисляется из `BaseValue` и активных модификаторов `AttributeModifierAggregator`.

`Attribute` не хранит отдельную изменяемую копию `CurrentValue`. Значение вычисляется из `BaseValue` и текущего состояния aggregator-а, поэтому добавление или удаление активного modifier-а не требует синхронизации второго поля.

### SetNumericAttributeBase

`AbilitySystemComponent.SetNumericAttributeBase()` изменяет постоянный `BaseValue` атрибута через ASC. Перед фиксацией вызывается `PreAttributeBaseChange`, после чего `CurrentValue` пересчитывается с сохранением всех активных modifiers.

Метод не является исполнением GameplayEffect, поэтому не вызывает `PostGameplayEffectExecute`. Если итоговый `CurrentValue` изменился, отправляется обычное attribute-change notification.

### PreAttributeBaseChange

`AttributeProcessor.PreAttributeBaseChange()` вызывается перед фиксацией нового `BaseValue`. Processor может изменить предлагаемое значение через `ref`, например ограничить Health диапазоном от нуля до текущего MaxHealth.

Этот callback предназначен только для constraints. В нём не следует запускать gameplay events или применять дополнительные эффекты, поскольку итоговое изменение атрибута ещё не зафиксировано.

Constraints, зависящие от другого атрибута, получают его через тот же `AbilitySystemComponent`. Processor не сохраняет runtime-ссылку на чужой `Attribute`, поэтому один definition безопасно используется разными ASC.

Все атрибуты используют одну модель хранения: authoritative и постоянные изменения записываются в `BaseValue`, а `CurrentValue` вычисляется поверх него. Тип атрибута не выбирает отдельное поле хранения и используется только для gameplay policy, например для ограничения persistent modifiers на текущих ресурсах.

Поддерживаются операции `Additive`, `Multiplicative`, `Division` и `Override`.

### PostGameplayEffectExecute

`AttributeProcessor.PostGameplayEffectExecute()` вызывается после того, как Instant modifier изменил `BaseValue`, но до итогового attribute-change notification.

Callback получает `GameplayEffectModCallbackData`:

- `EffectSpec` — применённый runtime spec;
- `EvaluatedData` — изменённый атрибут, operation и исходная magnitude;
- `Target` — ASC, на котором выполнен modifier.

`PostGameplayEffectExecute` не вызывается при обычном добавлении или удалении Duration/Infinite modifiers, поскольку они меняют вычисляемый `CurrentValue`, а не `BaseValue`.

## AbilityTask

`AbilityTask` представляет ограниченную временем асинхронную операцию, принадлежащую одной активной `GameplayAbility`. Задача создаётся и настраивается до вызова `ReadyForActivation()`.

```text
создание task
→ настройка callbacks
→ ReadyForActivation
→ Ability.OnGameplayTaskActivated
→ task.Activate
```

`EndTask()` завершает задачу по её собственной инициативе. При нормальном завершении ability все оставшиеся задачи получают `TaskOwnerEnded()`. При отмене ability задачи сначала получают `ExternalCancel()`, после чего завершается сама ability.

```text
нормальное завершение ability
→ TaskOwnerEnded
→ OnDestroy(abilityEnded: true)

отмена ability
→ ExternalCancel
→ EndTask
→ OnDestroy(abilityEnded: false)
→ OnCancelled конкретной task
```

`OnDestroy()` не вызывается напрямую. Пользовательский код завершает task через `EndTask()`, а owning ability — через `TaskOwnerEnded()`.

## Gameplay Ability Montage

`GameplayAbilityMontage` является Unity-аналогом используемого GAS animation montage asset. На текущем этапе asset содержит один `AnimationClip`; sections, slots, root motion scale и ненулевой blend-out ещё не реализованы.

Ability запускает montage через `AbilityTaskPlayMontageAndWait`, а не напрямую через `Animator` или строковый trigger:

```text
GameplayAbility
→ AbilityTaskPlayMontageAndWait
→ AbilitySystemComponent.PlayMontage
→ AnimInstance.MontagePlay
→ PlayableGraph
```

`AbilityTaskPlayMontageAndWait` предоставляет callbacks `OnBlendedIn`, `OnBlendOut`, `OnCompleted`, `OnInterrupted` и `OnCancelled`. Поскольку blend-in пока отсутствует, `OnBlendedIn` вызывается сразу после успешного запуска. При естественном окончании последовательно вызываются `OnBlendOut` и `OnCompleted`.

Если другая ability заменяет текущий montage, задача завершается через `OnInterrupted`. `ExternalCancel()` останавливает montage только тогда, когда owning ability всё ещё является его владельцем. Параметр `stopWhenAbilityEnds` определяет, следует ли останавливать montage при нормальном завершении ability.

### Montage prediction

Локально предсказанный montage хранит `PredictionKey` activation. `ASC.PlayMontage()` регистрирует rejection-only callback:

```text
prediction подтверждена
→ CatchUpTo
→ rejection callback удаляется без вызова

prediction отклонена
→ Reject
→ OnPredictiveMontageRejected
→ CurrentMontageStop
→ task.OnInterrupted
```

`RejectOrCaughtUp` callbacks используются для временных predicted effects, но не для montage: успешный catch-up не должен останавливать анимацию.

### Montage replication

Authoritative ASC формирует replicated montage state, а Mirror adapter только транспортирует его:

```text
ASC.PlayMontage
→ GameplayAbilityLocalAnimMontage
→ ASC.AnimMontageUpdateReplicatedData
→ GameplayAbilityRepAnimMontage
→ ReplicatedGameplayAbilityMontageState
→ simulated proxy
→ ASC.OnRepReplicatedAnimMontage
→ ASC.PlayMontageSimulated
```

Owning client проигрывает predicted montage локально. Owner transport не отправляет `GameplayAbilitySpec` simulated proxies, а montage transport не создаёт временную ability для визуализации.

Mirror передаёт `AssetId` montage asset вместо сериализации `ScriptableObject`. Позиция обновляется каждый Unity frame на authoritative instance, но отправляется не чаще сетевого `sendRate`. После остановки состояние перестаёт изменяться и SyncVar больше не создаёт сетевые обновления.

Текущий `Network Sync Mode: Observers` также доставляет transport state owning client, где hook его игнорирует. Это функционально безопасное, но избыточное отличие от Unreal `COND_SimulatedOnly`, которое должно быть устранено при окончательной очистке observer transport.
