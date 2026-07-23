Да. Но **теги не надо хранить и откатывать отдельным snapshot’ом**, если они выдаются через GameplayEffect.

В GASify `ASC.tags` — производное состояние. При `OnGameplayEffectApplied/Removed` вызывается `UpdateTagsOnEffectChange`, который пересчитывает теги из активных effects и abilities. При активации/деактивации ability теги тоже пересчитываются. ([GitHub][1])

То есть правильный rollback:

```text
снять все pending GameplayEffects
→ GASify автоматически пересчитает tags
→ удалить rejected prediction
→ восстановить authoritative attributes
→ переиграть оставшиеся pending GameplayEffects
→ GASify снова пересчитает tags
```

Например:

```text
Authoritative:
ShieldEffect → Invulnerable

Pending 42:
DodgeEffect → Invulnerable

Pending 43:
StunEffect → Stunned
```

Отклонён `42`:

```text
снимаем pending 43 и 42
→ остаётся ShieldEffect
→ Invulnerable остаётся

удаляем activation 42
переигрываем 43
→ Invulnerable + Stunned
```

Поэтому нельзя делать:

```csharp
asc.tags.Remove(invulnerableTag);
```

Потому что тот же тег мог быть выдан другим эффектом.

## Что требуется от удаления effect

Нужен нормальный API:

```csharp
public bool RemoveGameplayEffect(GameplayEffect effect)
{
    if (!appliedGameplayEffects.Remove(effect))
        return false;

    OnGameplayEffectRemoved?.Invoke(effect);
    return true;
}
```

Или по GUID:

```csharp
public bool RemoveGameplayEffect(string guid)
{
    var effect = appliedGameplayEffects.Find(x => x.guid == guid);

    return effect != null &&
           RemoveGameplayEffect(effect);
}
```

Именно вызов `OnGameplayEffectRemoved` важен: на него уже подписан пересчёт тегов GASify. Сейчас `NetworkAbilitySystemComponent` при reject только находит связанные effects, но фактически их не удаляет, поэтому `Invulnerable` действительно может остаться висеть. ([GitHub][2])

Rollback тогда выглядит так:

```csharp
private void RemovePendingEffects(
    IReadOnlyList<PredictedEffectRecord> effects)
{
    for (int i = effects.Count - 1; i >= 0; i--)
    {
        var record = effects[i];

        if (string.IsNullOrEmpty(record.LocalEffectGuid))
            continue; // Instant effect отсутствует в appliedGameplayEffects.

        record.Target.RemoveGameplayEffect(
            record.LocalEffectGuid);

        record.LocalEffectGuid = null;
    }
}
```

## Instant tags

У GASify `GrantedTags` являются постоянными только для `Duration` и `Infinite` effects. Для `Instant` effect они вызывают `OnTagsInstant` как одноразовое событие, но не остаются в `ASC.tags`. ([GitHub][1])

Поэтому `Invulnerable` должен выдаваться так:

```text
Duration/Infinite GameplayEffect
└── GrantedTags: State.Invulnerable
```

А не через `Instant GameplayEffect`.

Если `OnTagsInstant` запускает необратимую игровую логику, такое событие либо нельзя предсказывать, либо его последствия тоже должны регистрироваться как отдельные reversible prediction operations.

Итого:

```text
Attributes
→ restore authoritative + replay pending effects

Tags
→ автоматически пересчитываются при remove/reapply effects

Не нужно:
→ отдельный tag snapshot
→ ручной Add/Remove тегов
```

Но для этого первым обязательным исправлением становится корректное удаление predicted duration/infinite effects через событие `OnGameplayEffectRemoved`.

[1]: https://raw.githubusercontent.com/felipeggrod/gasify/main/Assets/GAS/Code/AbilitySystemComponent.cs "raw.githubusercontent.com"
[2]: https://raw.githubusercontent.com/apustovitovsky/relics-of-the-fallen/main/Assets/RelicsOfTheFallen/Scripts/Networking/AbilitySystem/NetworkAbilitySystemComponent.cs "raw.githubusercontent.com"
