TargetData MVP и локальные acceptance-сценарии работают. Первый путь завершён; остальные пути остаются отдельными следующими этапами.

| Путь | Что реализуем | Результат |
|---|---|---|
| 1. Ability lifecycle — завершён | `EndAbility`, корректный `CancelAbility`, удаление `activationGUID`, идентификация через `GameplayAbilitySpecHandle + ActivationInfo/PredictionKey` | Главный legacy-каркас активации удалён; lifecycle и RPC завершения соответствуют GAS |
| 2. Prediction | `ScopedPredictionWindow`, отдельный current prediction key для отложенного TargetData | Корректная prediction для `AbilityTask`, подтверждения цели и других latent-операций |
| 3. Полный WaitTargetData | `ShouldProduceTargetDataOnServer`, `GenericConfirm`, `GenericCancel`, replicated generic events | Получаем обе оригинальные модели: клиент передаёт TargetData либо сервер создаёт его сам |
| 4. Gameplay Effects | live NonSnapshot captures, затем stacking | Duration/Infinite-эффекты динамически реагируют на атрибуты; несколько одинаковых эффектов складываются по GAS-правилам |

### 1. Ability lifecycle — завершён

Строковая identity `activationGUID` удалена из core, Mirror и gameplay events. Активация строится вокруг:

```text
GameplayAbilitySpecHandle
+ GameplayAbilityActivationInfo
+ PredictionKey
```

Реализована основная цепочка:

```text
TryActivateAbility
→ ActivateAbility
→ CommitAbility
→ EndAbility
```

и отдельный путь:

```text
CancelAbility
→ EndAbility(wasCancelled: true)
```

`DeactivateAbility` удалён и заменён GAS-совместимым `EndAbility`. `CancelAbility` отменяет активные tasks и вызывает `EndAbility(wasCancelled: true)`. При включённой репликации завершение проходит через `ReplicateEndOrCancelAbility`, `ServerEndAbility`/`ServerCancelAbility`, `ClientEndAbility`/`ClientCancelAbility` и `RemoteEndOrCancelAbility`.

Это соответствует API [`UGameplayAbility`](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Plugins/GameplayAbilities/UGameplayAbility) и цепочке Lyra в [LyraGameplayAbility.cpp](C:/Users/NATALY/Documents/unity/lyra-starter-game-ue5/Source/LyraGame/AbilitySystem/Abilities/LyraGameplayAbility.cpp:194).

Локальные acceptance-сценарии после миграции проходят. Эта lifecycle-основа больше не блокирует Scoped Prediction, replicated events и batching.

### 2. Scoped Prediction

После lifecycle добавляем аналог `FScopedPredictionWindow`.

Текущий простой DoT по заранее известной цели создаёт TargetData сразу, поэтому может работать с ключом активации. Но если TargetData появляется позже:

```text
ActivateAbility
→ WaitTargetData
→ игрок выбрал цель через секунду
```

исходный prediction key уже не должен автоматически использоваться. В GAS для такой новой атомарной predictive-операции открывается новое prediction window. Именно это делает Lyra в [LyraGameplayAbility_RangedWeapon.cpp](C:/Users/NATALY/Documents/unity/lyra-starter-game-ue5/Source/LyraGame/Weapons/LyraGameplayAbility_RangedWeapon.cpp:484). Это также описано в [GASDocumentation — Prediction Key](https://github.com/tranek/GASDocumentation#concepts-pk).

### 3. Завершить WaitTargetData

Текущий MVP реализует основной вариант:

```text
Client TargetData
→ ServerSetReplicatedTargetData
→ replicated data cache
→ server ability
```

У оригинала есть ещё:

```text
ShouldProduceTargetDataOnServer = true
→ клиент отправляет GenericConfirm
→ сервер самостоятельно вычисляет TargetData
```

Отмена в оригинальном `WaitTargetData` проходит через `GenericCancel`. Наш прямой RPC отмены функционален, но не полностью повторяет эту цепочку. Для заранее известной цели этот путь пока не нужен. См. [GASDocumentation — Targeting](https://github.com/tranek/GASDocumentation#concepts-targeting).

### 4. Углубить Gameplay Effects

Можно вместо networking/lifecycle заняться игровой семантикой:

- live Source/Target NonSnapshot dependencies;
- обновление magnitude активного Duration/Infinite GE;
- stacking;
- stack duration/period policies.

Сейчас snapshot и начальный non-snapshot capture существуют, но live dependency ещё не поддерживается. Это даст больше возможностей для бафов и DoT, однако оставит legacy activation lifecycle.

Моя рекомендация:

```text
1. EndAbility/CancelAbility и удаление activationGUID — завершено
2. ScopedPredictionWindow
3. GenericConfirm/GenericCancel при реальной необходимости
4. Live NonSnapshot captures
5. Stacking
```

Конкретную DoT-способность уже можно собирать на существующем TargetData pipeline. Lifecycle больше не содержит строковой identity и готов служить основой для следующих этапов.
