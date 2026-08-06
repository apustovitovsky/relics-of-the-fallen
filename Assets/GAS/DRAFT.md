Итог: Fireball можно реализовать на текущем GAS-пайплайне без очередной полной переделки. Базовые механизмы уже готовы, но перед самой ability нужно закрыть несколько точечных пробелов — главным образом cast state/UI, Lyra activation groups и границу server-side projectile spawn.

Lyra не содержит готового projectile-примера: её ranged weapon сейчас явно работает как hitscan — `bProjectileWeapon = false` в [LyraGameplayAbility_RangedWeapon.cpp](C:/Users/NATALY/Documents/unity/lyra-starter-game-ue5/Source/LyraGame/Weapons/LyraGameplayAbility_RangedWeapon.cpp:497). Поэтому здесь опираемся на vanilla GAS/tranek для spec/TargetData и на Mirror для projectile actor.

## Правильный pipeline

```text
Input tag
→ CommonAbilitySystemComponent
→ predicted FireballAbility activation
→ зафиксировать выбранный TargetActor
→ отправить ActorArray TargetData серверу
→ применить владельцу Duration GE_Casting
→ запустить cast montage и WaitDelay

Authority после окончания cast
→ повторно проверить target/range/state
→ CommitAbility
→ вычислить направление к текущей позиции TargetAnchor
→ создать projectile
→ создать GameplayEffectSpec урона
   Instigator   = caster
   EffectCauser = projectile
→ NetworkServer.Spawn

Projectile
→ запоминает начальное направление один раз
→ движется без ссылки на выбранную цель
→ authoritative collision
   ├─ противник с ASC → применить Instant damage spec
   ├─ препятствие     → не применять effect
   └─ оба случая      → NetworkServer.Destroy
```

Ability после spawn может завершиться. Projectile хранит серверный `GameplayEffectSpec` и больше не зависит от runtime instance ability. Применение GE снарядом напрямую через ASC соответствует GAS-подходу: [tranek README](C:/Users/NATALY/Documents/unity/lyra-starter-game-ue5/GASDocumentation-master/README.md:864).

## Что уже готово

- Активация по input tag и predicted activation в `GAS.Common`.
- `CommitAbility`, cost и cooldown.
- `AbilityTask_WaitDelay`: [AbilityTask_WaitDelay.cs](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/Code/GameplayAbilities/Tasks/AbilityTask_WaitDelay.cs:41).
- Predicted/replicated montage pipeline.
- Ручное создание `GameplayAbilityTargetData_ActorArray`, уже используемое channel ability: [ChannelDamageAbility.cs](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/RelicsOfTheFallen/Scripts/Abilities/ChannelDamageAbility.cs:120).
- Передача ActorArray через Mirror: [AbilitySystemNetworkSerializationExtensions.cs](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/Mirror/Serialization/AbilitySystemNetworkSerializationExtensions.cs:277).
- Создание и применение Instant GameplayEffect.
- `GameplayEffectContext` уже различает Instigator и EffectCauser.
- Репликация Duration GE владельцу и расчёт оставшегося времени.

Для начального направления существующего ActorArray достаточно. Новые `SingleTargetHit` или `LocationInfo` передавать от клиента не нужно: клиент выбирает направление, но фактическое попадание определяет серверный projectile.

## Что нужно подготовить

### 1. Activation groups в `GAS.Common`

Текущий `CommonGameplayAbility` содержит только activation policy: [CommonGameplayAbility.cs](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/Common/Code/GameplayAbilities/CommonGameplayAbility.cs:7).

В Lyra дополнительно существуют:

```text
Independent
Exclusive_Replaceable
Exclusive_Blocking
```

Они определены в [LyraGameplayAbility.h](C:/Users/NATALY/Documents/unity/lyra-starter-game-ue5/Source/LyraGame/AbilitySystem/Abilities/LyraGameplayAbility.h:50), а ASC добавляет и удаляет ability из группы вместе с activation lifecycle: [LyraAbilitySystemComponent.cpp](C:/Users/NATALY/Documents/unity/lyra-starter-game-ue5/Source/LyraGame/AbilitySystem/LyraAbilitySystemComponent.cpp:318).

Fireball разумно сделать `Exclusive_Replaceable`: другой exclusive action, stun или смерть сможет отменить текущий cast.

### 2. GAS-совместимый источник данных для cast bar

Cast bar не является встроенным типом GAS. Правильная GAS-модель:

```text
GE_Casting
├─ Duration
└─ GrantedTag: State.Casting
```

Cast time должен браться из созданного `GameplayEffectSpec.Duration`, чтобы duration GE, `WaitDelay` и UI не хранили три разных числа.

UI должен наблюдать локальный ASC:

```text
Active GE added
→ получить Duration

State.Casting NewOrRemoved
→ показать/скрыть bar

GetActiveEffectsTimeRemainingAndDuration
→ вычислять progress
```

Сейчас есть только `GetActiveEffectsTimeRemaining()`: [AbilitySystemComponent.cs](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/Code/AbilitySystemComponent.cs:2361). Отсутствуют GAS-аналоги:

- `OnActiveGameplayEffectAddedDelegateToSelf`;
- `GetActiveEffectsTimeRemainingAndDuration`.

Это реальный framework-пробел. Tranek использует именно эти API для duration UI: [добавление эффекта](C:/Users/NATALY/Documents/unity/lyra-starter-game-ue5/GASDocumentation-master/README.md:866), [remaining time + duration](C:/Users/NATALY/Documents/unity/lyra-starter-game-ue5/GASDocumentation-master/README.md:1517).

Скрывать bar нужно по tag count, а не по удалению конкретного GE: при reconciliation predicted GE удаляется и заменяется серверным, хотя каст продолжается. Это тот же паттерн, что рекомендован для cooldown: [README.md](C:/Users/NATALY/Documents/unity/lyra-starter-game-ue5/GASDocumentation-master/README.md:1554).

Owner уже получает active effects через [NetworkAbilitySystemComponent.cs](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/Mirror/Components/NetworkAbilitySystemComponent.cs:32).

Если cast bar должны видеть и другие игроки, это отдельное требование: observer-компонент сейчас реплицирует атрибуты и montage, но не active effects: [NetworkAbilitySystemObserverComponent.cs](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/Mirror/Components/NetworkAbilitySystemObserverComponent.cs:20).

### 3. Projectile spawn остаётся вне core GAS

В оригинале `AbilityTask_SpawnActor` создаёт actor только на сервере. Предиктивный gameplay-projectile GAS автоматически не предоставляет: [README.md](C:/Users/NATALY/Documents/unity/lyra-starter-game-ue5/GASDocumentation-master/README.md:2632).

В Mirror сервер должен:

- создать prefab с `NetworkIdentity`;
- вызвать `NetworkServer.Spawn`;
- после столкновения вызвать `NetworkServer.Destroy`;
- зарегистрировать prefab в spawnable prefabs.

Это подтверждается актуальной [документацией Mirror по NetworkIdentity](https://mirror-networking.gitbook.io/docs/manual/components/network-identity).

Но помещать `NetworkServer.Spawn` в `GAS` или `GAS.Common` нельзя. Projectile является игровой сущностью. Рекомендуемая граница:

```text
RelicsOfTheFallen.Abilities
└─ FireballAbility
   └─ обращается к game-specific projectile spawner

RelicsOfTheFallen.Networking
├─ NetworkFireballProjectileSpawner
└─ FireballProjectile : NetworkBehaviour
```

Generic `IProjectileSpawner` в core GAS не нужен. Если понадобится offline acceptance-тест, небольшой game-level spawner contract можно добавить между этими двумя игровыми assembly.

### 4. Валидация цели и понятие «противник»

Текущий targeting проверяет только `IsTargetable` и исключает самого себя: [TargetableFilter.cs](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/RelicsOfTheFallen/Scripts/Targeting/Runtime/TargetableFilter.cs:11), [TargetingSensor.cs](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/RelicsOfTheFallen/Scripts/Targeting/Components/TargetingSensor.cs:71).

Team/faction relationship пока отсутствует. Поэтому сейчас нельзя корректно отличить:

```text
противник
союзник
нейтральный объект
```

Это не задача GAS core. Нужен проектный relationship/team механизм, который используют и target selection, и projectile collision.

Сервер обязан дважды проверить выбранную цель:

1. До начала либо сразу после получения TargetData.
2. Перед spawn после окончания cast.

Проверяются существование, targetable state, range и при необходимости line of sight.

### 5. EffectContext попадания пока неполный

Текущий context не содержит `HitResult`, world origin или TargetData, что уже зафиксировано в [Documentation.md](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/Documentation.md:217).

Для простого разового урона это не блокирует Fireball. Projectile может получить ASC столкнувшегося объекта и применить сохранённый spec.

Но для:

- impact GameplayCue в точке столкновения;
- normal поверхности;
- hit bone;
- execution calculation, зависящего от попадания;

понадобится Unity-аналог `GameplayAbilityTargetData_SingleTargetHit`/`HitResult` и добавление TargetData в EffectContext. Сейчас `GameplayAbilityTargetData.ApplyGameplayEffectSpec()` просто применяет общий spec, не добавляя payload в его context: [GameplayAbilityTargetData.cs](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/Code/GameplayAbilities/Targeting/GameplayAbilityTargetData.cs:14).

Это следующий уровень, не обязательный для первого рабочего damage projectile.

## Что пока не нужно рефакторить

- `WaitGameplayEvent` и расширенный `GameplayEventData` не нужны, если выпуск fireball определяется `WaitDelay`.
- Montage sections не обязательны: можно использовать отдельные `AM_FireballCast` и `AM_FireballRelease`.
- `WaitNetSync` не нужен, пока после delay только authority создаёт projectile и выполняет gameplay.
- Predicted projectile пока не нужен. Owner увидит server projectile с сетевой задержкой, но gameplay останется корректным.
- `GameplayEffectContainer` для одного damage spec избыточен. Это рекомендованный QoL-паттерн, а не vanilla GAS: [README.md](C:/Users/NATALY/Documents/unity/lyra-starter-game-ue5/GASDocumentation-master/README.md:3108).

## Рекомендуемый порядок

1. Добавить Lyra activation groups в `GAS.Common`.
2. Добавить GAS-compatible observation API для active GE и `TimeRemaining + Duration`.
3. Сделать `GE_Casting` и локальный cast-bar presenter.
4. Реализовать FireballAbility с ActorArray TargetData и authority-only завершением cast.
5. Добавить игровой projectile spawner и серверный `FireballProjectile`.
6. Добавить team/relationship validation.
7. Покрыть сценариями: успешный cast, cancel, obstacle, enemy hit, frozen direction.
8. Затем расширять HitResult/context, GameplayCues и animation events.

То есть ближайшая цель — не сам projectile, а небольшой точечный этап: **activation groups + корректно наблюдаемый Duration GE для cast bar**. После него ability и projectile можно собирать без временной параллельной архитектуры.