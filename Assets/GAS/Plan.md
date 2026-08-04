Сейчас TargetData MVP и оба локальных сценария работают. Перед добавлением новых крупных механик я бы выбрал один из четырёх путей.

| Путь | Что реализуем | Результат |
|---|---|---|
| 1. Ability lifecycle — рекомендую | `EndAbility`, корректный `CancelAbility`, удаление `activationGUID`, идентификация через `GameplayAbilitySpecHandle + ActivationInfo/PredictionKey` | Убираем главный legacy-каркас и приближаем всю цепочку к GAS |
| 2. Prediction | `ScopedPredictionWindow`, отдельный current prediction key для отложенного TargetData | Корректная prediction для `AbilityTask`, подтверждения цели и других latent-операций |
| 3. Полный WaitTargetData | `ShouldProduceTargetDataOnServer`, `GenericConfirm`, `GenericCancel`, replicated generic events | Получаем обе оригинальные модели: клиент передаёт TargetData либо сервер создаёт его сам |
| 4. Gameplay Effects | live NonSnapshot captures, затем stacking | Duration/Infinite-эффекты динамически реагируют на атрибуты; несколько одинаковых эффектов складываются по GAS-правилам |

### 1. Ability lifecycle — лучший следующий шаг

Сейчас `activationGUID` проходит через core и Mirror примерно в 73 местах. Это собственная строковая identity, которой нет в GAS. Оригинальная цепочка строится вокруг:

```text
GameplayAbilitySpecHandle
+ GameplayAbilityActivationInfo
+ PredictionKey
```

Нужно постепенно получить:

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

Текущий `DeactivateAbility` заменяется GAS-совместимым `EndAbility`. Mirror-методы после этого тоже перестают передавать строковый GUID.

Это соответствует API [`UGameplayAbility`](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Plugins/GameplayAbilities/UGameplayAbility) и цепочке Lyra в [LyraGameplayAbility.cpp](C:/Users/NATALY/Documents/unity/lyra-starter-game-ue5/Source/LyraGame/AbilitySystem/Abilities/LyraGameplayAbility.cpp:194).

Почему сейчас: зелёные сценарные тесты дают безопасную опору, а Scoped Prediction, replicated events и batching лучше не строить поверх `activationGUID`.

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
1. EndAbility/CancelAbility и удаление activationGUID
2. ScopedPredictionWindow
3. GenericConfirm/GenericCancel при реальной необходимости
4. Live NonSnapshot captures
5. Stacking
```

Саму конкретную DoT-способность уже можно собрать на существующем TargetData pipeline, но сначала я бы небольшими правками очистил lifecycle. Текущий [Plan.md](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/Plan.md:1) также пора актуализировать: этапы cache и TargetData RPC в нём уже фактически выполнены.