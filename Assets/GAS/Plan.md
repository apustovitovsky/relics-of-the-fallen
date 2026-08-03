Примерный план такой.

1. Завершить текущую миграцию

- Проверить компиляцию после восстановления методов.
- Запустить Instant и Periodic сценарии.
- Убедиться, что в `GameplayAbility` осталась только TargetData-ветка применения.
- Обновить документацию.

2. Убрать `target ASC` из activation pipeline

Сейчас главный legacy-хвост:

```text
TryActivateAbility(handle, target)
→ ActivateAbility(source, target)
```

Должно стать ближе к GAS:

```text
TryActivateAbility(handle)
→ ActivateAbility(...)
→ конкретная ability / AbilityTask получает TargetData
```

`CommitAbility` также не должен зависеть от готового target, если стоимость и cooldown относятся к владельцу.

3. Добавить минимальный аналог `AbilityTask_WaitTargetData`

Для DoT:

```text
PeriodicDamageAbility активируется
→ targeting/task получает выбранного actor
→ создаёт GameplayAbilityTargetDataHandle
→ ability продолжает выполнение
→ создаёт spec
→ применяет через TargetData
```

Сначала реализуем только direct actor targeting. `SingleTargetHit` и `LocationInfo` добавим вместе с соответствующими сценариями.

4. Реализовать replicated TargetData cache в ASC

По аналогии с GAS:

```text
AbilitySpecHandle + PredictionKey
→ TargetData cache
→ delegate ожидающей ability task
→ ConsumeClientReplicatedTargetData
```

Это позволит серверной ability либо сразу получить данные, либо дождаться их прихода.

5. Перевести Mirror на `ServerSetReplicatedTargetData`

```text
Client:
активирует ability
→ собирает TargetData
→ отправляет ServerSetReplicatedTargetData

Server:
активирует ту же ability
→ получает/ожидает TargetData
→ применяет authoritative periodic GE
```

Из activation RPC удалится `targetNetworkId`. Mirror будет сериализовать `ActorArray` через `netId`, а core продолжит работать с `GameObject`.

6. Завершить сетевой DoT

Итоговая цепочка:

```text
client input
→ predicted ability activation
→ TargetData
→ ServerSetReplicatedTargetData
→ authoritative GameplayEffectSpec
→ server-only periodic ticks
→ BaseValue replication
→ Health обновляется на клиентах
```

Периодический эффект на клиенте предиктивно не создаётся — это уже реализовано.

7. После готового DoT

Дальнейшие крупные части GAS:

- stacking;
- live non-snapshot captures;
- дополнительные TargetData-типы;
- Gameplay Cues для observer-ов;
- execution calculations и `Damage` meta-attribute;
- удаление оставшегося legacy API и старых ability implementations.

Ближайшая цель: **скомпилировать текущий шаг, затем удалить `target` из core activation lifecycle и ввести минимальный `AbilityTask_WaitTargetData`**. Это главный оставшийся архитектурный разрыв перед полноценным сетевым DoT.