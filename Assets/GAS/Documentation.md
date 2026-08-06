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

Core GAS не зависит от Mirror: `AbilitySystemComponent` не хранит `netId` или
`NetworkConnection` и не требует сетевых компонентов. Для исходящих сетевых
операций ASC использует необязательный `IAbilitySystemReplicationTransport`,
который реализует Mirror-адаптер. Роль локальной копии передаётся в
`GameplayAbilityActorInfo`; без адаптера ASC работает как authoritative offline
экземпляр.

### Сетевая активация Gameplay Ability

Offline-, AI- и player-код используют одну gameplay-точку входа —
`AbilitySystemComponent.TryActivateAbility()`. На locally controlled client ASC
сам создаёт prediction key, выполняет локальную activation и после успеха
передаёт готовый запрос transport-слою:

```text
Input
→ AbilitySystemComponent.AbilitySpecInputPressed
→ AbilitySystemComponent.TryActivateAbility
→ создать PredictionKey и установить activation mode Predicting
→ выполнить локальную predicted activation
→ IAbilitySystemReplicationTransport.CallServerTryActivateAbility
→ NetworkAbilitySystemComponent.ServerTryActivateAbility
→ AbilitySystemComponent.InternalServerTryActivateAbility
→ выполнить authoritative activation
→ ClientActivateAbilitySucceed или ClientActivateAbilityFailed
```

Локально отклонённая activation не отправляется серверу. Её `PredictionKey` немедленно получает reject, что запускает откат связанных predicted effects и modifiers. Серверный результат передаётся owning client явным Target RPC. Активация идентифицируется только через `GameplayAbilitySpecHandle`, `GameplayAbilityActivationInfo` и `PredictionKey`; отдельная строковая identity не используется.

`GameplayAbilitySpec.InputPressed` хранит owner-only состояние ввода конкретного
выданного spec и не входит в его обычную репликацию. Клиентский ASC устанавливает
значение, а Mirror передаёт его отдельным параметром activation RPC вместе с
handle и prediction key. Серверный ASC устанавливает значение в собственной
копии spec. Observer-клиенты это состояние не получают.

Завершение с `replicateEndAbility: true` проходит по GAS-совместимой цепочке:

```text
GameplayAbility.EndAbility
→ AbilitySystemComponent.ReplicateEndOrCancelAbility
→ IAbilitySystemReplicationTransport
→ ServerEndAbility / ServerCancelAbility
  или ClientEndAbility / ClientCancelAbility
→ AbilitySystemComponent.RemoteEndOrCancelAbility
→ GameplayAbility.EndAbility(replicateEndAbility: false)
```

Удалённая сторона находит ability по `GameplayAbilitySpecHandle` и принимает завершение только для совпадающего `PredictionKey`. Повторная репликация отключается последним вызовом `EndAbility`, поэтому RPC не образуют цикл. Без сетевого transport тот же API выполняет только локальное завершение.

Observer RPC активации ability и legacy `syncGrantedAbilities` удалены. `GameplayAbilitySpec` реплицируется только owning client через `NetworkAbilitySystemComponent`; simulated proxies не получают и не запускают abilities. Визуальное состояние передаётся им через montage replication и Gameplay Cues.

Из-за асинхронного input buffering Unity-версия возвращает `UniTask<bool>` вместо синхронного `bool`, используемого `TryActivateAbility()` в Unreal GAS. Значение `true` означает, что ability прошла проверки и вошла в active state.

`Full`, `Mixed` и `Minimal` являются политикой маршрутизации `ActiveGameplayEffect`, а не отдельными сетевыми компонентами. На текущем этапе полные `ActiveGameplayEffect` не реплицируются observers, поэтому режим `Full` не считается реализованным и не должен неявно подменяться поведением `Mixed`.

## GameplayEffectSpec

`GameplayEffectSpec` содержит runtime-данные одного применения `GameplayEffect`.

Spec создаётся методом `AbilitySystemComponent.MakeOutgoingSpec()`. При создании захватываются snapshot-атрибуты Source.

Перед применением создаётся отдельная копия spec, захватываются необходимые данные Target и вычисляются magnitude модификаторов.

`SetSetByCallerMagnitude()` сохраняет runtime-величину по `GameplayTag`.


#### 4.5.10 Gameplay Effect Context

`GameplayEffectContext` хранит происхождение конкретного применения
`GameplayEffect`: инициатора, физический источник, предоставивший ability объект и
связанную ability. Он передаётся внутри `GameplayEffectSpec` и доступен расчётам
magnitude, execution calculations, attribute callbacks и Gameplay Cues.

`AbilitySystemComponent.MakeEffectContext()` создаёт базовый context, где
`Instigator` соответствует `OwnerActor`, а `EffectCauser` — текущему
`AvatarActor`.

`GameplayAbility.MakeEffectContext()` дополнительно сохраняет:

- persistent definition создавшей context ability;
- локальный runtime instance ability;
- ability level на момент создания;
- `SourceObject` из соответствующего `GameplayAbilitySpec`.

Объектные ссылки имеют разное назначение:

```text
Instigator
→ непосредственный инициатор текущего эффекта

OriginalInstigator
→ в базовом context совпадает с Instigator; производный context может изменить семантику

EffectCauser
→ физический источник, например avatar, weapon или projectile

SourceObject
→ объект, предоставивший ability, например weapon или item
```

`GameplayEffectContextHandle` хранит полиморфную ссылку на context и
предоставляет GAS-совместимые методы `GetInstigator()`,
`GetOriginalInstigator()`, `GetEffectCauser()` и `GetSourceObject()`. Обычное
копирование handle разделяет один context, `Duplicate()` создаёт независимую
копию, а `Clear()` инвалидирует только очищаемую копию handle.

Копия `GameplayEffectSpec` по умолчанию разделяет context с исходным spec. Перед
независимой модификацией context необходимо вызвать:

```csharp
copiedSpec.DuplicateEffectContext();
```

`DuplicateEffectContext()` создаёт отдельный context через виртуальный
`GameplayEffectContext.Duplicate()`. Производный context должен переопределить
`Duplicate()`, если содержит собственные mutable-данные.

Локальный pipeline:

```text
GameplayAbilitySpec
→ GameplayAbility.MakeEffectContext
    → SetAbility
    → AddSourceObject
→ AbilitySystemComponent.MakeOutgoingSpec
→ GameplayEffectSpec.EffectContext
→ применение эффекта
```

`GetAbilityInstance_NotReplicated()` возвращает только локальный runtime instance
ability и не входит в сетевое состояние.

Доступ к объектным ссылкам адаптирован под Unity через
`IGameplayEffectContextObjectProvider`:

```text
GameplayEffectContext
└─ IGameplayEffectContextObjectProvider
   ├─ GameplayEffectContextObjectContainer
   └─ GameplayEffectContextReplicationState
```

`GameplayEffectContextObjectContainer` хранит непосредственные Unity-ссылки.
`GameplayEffectContextReplicationState` хранит `netId` для `Instigator` и
`EffectCauser` и разрешает объекты через `NetworkClient.spawned` при каждом
обращении к context. Если объект ещё не spawned на клиенте, getter временно
возвращает `null`; следующее обращение может разрешить его без повторного создания
context. Базовый `GetOriginalInstigator()` возвращает тот же объект, что и
`GetInstigator()`.

```text
ActiveGameplayEffectReplicationState.Context
→ GameplayEffectContextReplicationState
→ GameplayEffectContext(provider)
→ GetInstigator / GetEffectCauser
→ NetworkClient.spawned
```

`SourceObject` может быть `GameObject`, `Component` или `ScriptableObject`, поэтому
для него нет единого Mirror `netId`. Текущий active-effect transport его не
сериализует, и удалённый context возвращает `null`. Authority и локальная
prediction продолжают использовать непосредственный `SourceObject`.

Транспорт active effects также пока не восстанавливает ability definition,
runtime ability instance и ability level внутри удалённого context. Рассчитанные
modifier magnitudes передаются отдельно внутри состояния active effect, поэтому
эти данные не требуются для корректного observer-применения текущего MVP.

Для создания производного context в Unreal переопределяется
`AbilitySystemGlobals.AllocGameplayEffectContext()`. Настраиваемый allocator в
текущей Unity-реализации ещё не добавлен; статическая прокладка над
`new GameplayEffectContext()` намеренно не используется.

Текущий MVP не реализует расширенные данные Unreal context: actor array,
`HitResult`, world origin и `TargetData`. Они добавляются при появлении gameplay,
которому эти данные действительно нужны; базовый lifecycle context и возможность
создать производный `GameplayEffectContext` уже сохранены.


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

#### 4.6.4 Activating Abilities

`AbilitySystemComponent.TryActivateAbility()` выполняет первоначальный `CanActivateAbility()`. После успешной проверки `CallActivateAbility()` переводит ability в активное состояние и вызывает её `ActivateAbility()`.

Активация сама по себе не списывает cost и не запускает cooldown. Конкретная ability вызывает `CommitAbility()` в подходящий момент. Простые Instant и Projectile abilities выполняют commit сразу, а abilities с targeting, cast или подтверждением попадания могут отложить его.

```text
TryActivateAbility
→ CanActivateAbility
→ CallActivateAbility
→ ActivateAbility
→ CommitAbility в выбранный ability момент
```

##### GAS.Common: Activation Groups

`GameplayAbilityActivationGroup` — reusable-расширение `GAS.Common`,
основанное на activation groups Lyra. Группа задаёт отношение
ability к другим активным abilities одного
`CommonAbilitySystemComponent`:

- `Independent` не участвует в exclusive-блокировке;
- `ExclusiveReplaceable` может быть отменена другой exclusive ability;
- `ExclusiveBlocking` отменяет active replaceable ability и блокирует
  новые exclusive activations до своего завершения.

ASC добавляет ability в группу внутри `NotifyAbilityActivated()` и
удаляет внутри `NotifyAbilityEnded()`. Поэтому обычное завершение и
cancel освобождают группу через один lifecycle. При отказе
`CanActivateAbility()` добавляет причину
`Ability.ActivateFail.ActivationGroup` из `CommonGameplayTags`.
Asset этого тега принадлежит reusable-слою и хранится в
`Assets/GAS/Common/Resources/GameplayTags`, а не в core-ресурсах `GAS`.

```text
ExclusiveReplaceable active
→ activate ExclusiveBlocking
→ cancel ExclusiveReplaceable
→ reject new exclusive activations
→ end ExclusiveBlocking
→ allow exclusive activations again
```

#### 4.6.5 Canceling Abilities

`CancelAbility()` отменяет активные `AbilityTask`, после чего вызывает `EndAbility()` с `wasCancelled: true`. Обычное и отменённое завершение используют одну cleanup-цепочку: очищают replicated data cache, снимают blocking и activation-owned tags, переводят ability в неактивное состояние и вызывают `NotifyAbilityEnded()`.

```text
CancelAbility
→ AbilityTask.ExternalCancel
→ EndAbility(wasCancelled: true)
```

Если `replicateCancelAbility` включён, удалённая сторона получает `ServerCancelAbility` или `ClientCancelAbility`. Identity активации задаётся парой `GameplayAbilitySpecHandle + PredictionKey`; отмена другой или уже завершённой активации игнорируется.

#### 4.5.14 Cost Gameplay Effect

Gameplay Ability может иметь отдельный `GameplayEffect`, определяющий её
стоимость. Обычный Cost GE — это Instant-эффект с одним или несколькими
отрицательными `Additive` modifiers. Ability возвращает его через
`GetCostGameplayEffect()`.

`CanActivateAbility()` вызывает `CheckCost()` до активации. `CommitCheck()`
повторяет проверку непосредственно перед списанием, поскольку за время
targeting или cast значение ресурса могло измениться. После успешного
commit `ApplyCost()` создаёт новый `GameplayEffectSpec` и применяет его к owner.

```text
CanActivateAbility
→ CheckCost
→ ActivateAbility
→ CommitAbility
    → CommitCheck
        → CheckCost
    → CommitExecute
        → ApplyCost
```

`CheckCost()` формирует spec с тем же ability level и effect context, захватывает
Source и Target attributes и вычисляет magnitude тем же способом, который
использует `ApplyCost()`. Доступность проверяется по текущему
агрегированному `CurrentValue`.

Стандартная реализация не принимает `Multiplicative`, `Division` и `Override`
cost modifiers. Для другой математики стоимости следует переопределить
`CheckCost()` и согласованный с ним `ApplyCost()`.

#### 4.5.15 Cooldown Gameplay Effect

Cooldown также задаётся отдельным `GameplayEffect`, который ability возвращает
через `GetCooldownGameplayEffect()`. Обычный Cooldown GE имеет Duration policy,
не имеет modifiers и выдаёт уникальный cooldown tag через `GrantedTags`.

Ability проверяет не наличие конкретного effect instance, а наличие cooldown tag
в owned tags ASC. `GetCooldownTags()` возвращает `GrantedTags` definition, а
`CheckCooldown()` проверяет их через `HasAnyMatchingGameplayTags()`. Один и тот же
тег можно использовать для общего cooldown нескольких abilities.

```text
CommitAbility
→ CommitCheck
    → CheckCooldown
→ CommitExecute
    → ApplyCooldown
        → MakeOutgoingSpec
        → ApplyGameplayEffectSpecToOwner
```

Cooldown хранится только как `ActiveGameplayEffect`. Отдельного таймера внутри
`GameplayAbility` нет. При истечении Duration GE контейнер удаляет эффект,
а вместе с ним уменьшает owned-tag count.

Текущая base-реализация берёт duration и cooldown tags из definition. Dynamic duration
и `DynamicGrantedTags` для переиспользуемого Cooldown GE через `SetByCaller` ещё не
реализованы.

##### 4.5.15.1 Get the Cooldown Gameplay Effect's Remaining Time

`GetCooldownTimeRemaining()` создаёт `GameplayEffectQuery` через
`MakeQuery_MatchAnyOwningTags()` и запрашивает у ASC время всех подходящих
active effects. Если один cooldown tag выдают несколько эффектов, метод
возвращает наибольшее оставшееся время.

```text
GetCooldownTimeRemaining
→ GameplayEffectQuery.MakeQuery_MatchAnyOwningTags
→ AbilitySystemComponent.GetActiveEffectsTimeRemaining
→ ActiveGameplayEffect.GetTimeRemaining
```

Значение отражает только те active effects, которые присутствуют на локальном ASC.
На observer-клиенте без репликации `ActiveGameplayEffect` этот API не может показать
удалённый cooldown.

### Совместимость legacy modifiers

Пока существующие GameplayEffect assets не мигрированы, `GameplayEffectSpec` поддерживает временный compatibility bridge. Если новые `ModifierDefinitions` отсутствуют, legacy `Modifier` преобразуется в `Additive` definition с `ConstantMagnitude`.

Если у GameplayEffect заполнены новые definitions, legacy-список полностью игнорируется. Одновременное исполнение двух представлений modifier-а не допускается.

Compatibility bridge предназначен только для миграции существующих assets. Новые GameplayEffect следует создавать через `AttributeModifierDefinition`.

## Применение GameplayEffect

`AbilitySystemComponent.ApplyGameplayEffectSpecToSelf()` является основной точкой применения подготовленного spec и возвращает `ActiveGameplayEffectHandle`.

При применении эффекта из ability полный `GameplayAbilityActivationInfo` остаётся на уровне `GameplayAbility`. Ability извлекает из него `PredictionKey` и передаёт целевым данным только этот ключ:

```text
GameplayAbility.ApplyGameplayEffectSpecToTarget
→ GameplayAbilityActivationInfo.GetActivationPredictionKey
→ GameplayAbilityTargetData.ApplyGameplayEffectSpec
→ AbilitySystemComponent.ApplyGameplayEffectSpecToSelf
```

`RegisterActiveGameplayEffectAddedDelegateToSelf()` возвращает
`IDisposable`-подписку и уведомляет наблюдателя после полной
регистрации `ActiveGameplayEffect`. Callback получает target ASC,
применённый `GameplayEffectSpec` и локальный
`ActiveGameplayEffectHandle`. Уведомление вызывается для predicted,
локального authoritative и восстановленного replicated effect.

`GetActiveEffectsTimeRemainingAndDuration()` возвращает для каждого
совпавшего effect именованную пару `(TimeRemaining, Duration)`.
Это позволяет UI брать total duration и progress из того же
evaluated spec, который хранит active effect.

Базовый `GameplayAbility` не создаёт конкретный TargetData из готового целевого ASC. Цель получает `GameplayAbilityTargetActor`, а `AbilityTask_WaitTargetData` управляет ожиданием, подтверждением и отменой таргетинга:

```text
GameplayAbilityTargetActor.StartTargeting
→ ConfirmTargetingAndContinue
→ GameplayAbilityTargetDataHandle
→ AbilityTask_WaitTargetData.ValidData
→ GameplayAbility.ApplyGameplayEffectSpecToTarget
```

`WaitTargetData()` создаёт отдельный экземпляр Unity prefab `GameplayAbilityTargetActor` для одной активации. `WaitTargetDataUsingActor()` принимает уже созданный экземпляр. Task владеет TargetActor, снимает свои подписки и уничтожает его при завершении.

`GameplayTargetingConfirmation.Instant` подтверждает цель сразу после `StartTargeting()`. `UserConfirmed` и `Custom` ожидают внешнего подтверждения, а `CustomMulti` допускает несколько выдач TargetData до явного завершения task.

Acceptance-сценарии Instant и Periodic GameplayEffect получают `GameplayAbilityTargetData_ActorArray` через `AbilityTask_WaitTargetData`. Несетевой ASC по умолчанию выполняет тот же task как локально управляемый authoritative instance и не требует replication transport.

Для predicted activation локально управляемый client создаёт TargetData и передаёт его через единый optional `IAbilitySystemReplicationTransport`. Mirror-адаптер кодирует тип каждого payload и его сетевые поля, после чего authoritative ASC сохраняет результат в `AbilityReplicatedDataCache`, адресованный парой `GameplayAbilitySpecHandle + original PredictionKey`:

```text
client GameplayAbilityTargetActor
→ client AbilityTask_WaitTargetData
→ AbilitySystemComponent.CallServerSetReplicatedTargetData
→ IAbilitySystemReplicationTransport
→ NetworkAbilitySystemComponent.ServerSetReplicatedTargetData
→ server AbilityReplicatedDataCache
→ server AbilityTask_WaitTargetData
→ authoritative ability
```

Cache различает подтверждённые TargetData и отмену. `CallReplicatedTargetDataDelegatesIfSet()` обрабатывает данные, которые пришли раньше регистрации server task, а `ConsumeClientReplicatedTargetData()` очищает использованное состояние, сохраняя подписки текущей activation. При завершении ability весь cache этой activation удаляется.

Текущая Mirror-сериализация поддерживает `GameplayAbilityTargetData_ActorArray`. Каждый target обязан быть spawned `GameObject` с `NetworkIdentity`; по сети передаётся его `netId`, а принимающая сторона восстанавливает локальную копию объекта. Пока `ScopedPredictionWindow` не реализован, `currentPredictionKey` совпадает с original activation key.

Целевой `AbilitySystemComponent` самостоятельно определяет authoritative или predicted применение через `IsOwnerActorAuthoritative()`. Несетевой ASC по умолчанию считается authoritative. В сетевой конфигурации Mirror-адаптер сообщает `GameplayAbilityActorInfo` роль локальной копии, не передавая Mirror-типы в core GAS. На неавторитетной копии эффект применяется только при наличии действительного `PredictionKey`; без него применение отклоняется.

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

### Periodic GameplayEffect

`Duration` и `Infinite` GameplayEffect с `Period` больше `NoPeriod` остаётся активным, но его modifiers не устанавливаются в агрегатор. Каждое periodic-исполнение работает как `Instant`: изменяет authoritative `BaseValue` атрибута через обычный execution pipeline.

```text
применение periodic GameplayEffect
→ создаётся ActiveGameplayEffect
→ запускается authority-only period scheduler
→ каждый Period вызывает ExecutePeriodicEffect
→ modifiers изменяют BaseValue
→ removal останавливает scheduler
```

`ExecutePeriodicEffectOnApplication` выполняет первый periodic-тик сразу при применении. Если свойство выключено, первое исполнение происходит через один `Period`. Отдельного «последнего тика» нет: поведение при естественном или преждевременном завершении эффекта оформляется отдельной expiration-механикой.

Periodic GameplayEffect не создаёт predicted-копию: ability activation может быть predicted, но сам periodic effect и все его тики выполняет authority. `Period` не передаётся в replication state и восстанавливается из локальной GameplayEffect definition, что соответствует `NotReplicated`-семантике `FGameplayEffectSpec.Period` в Unreal GAS.

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
