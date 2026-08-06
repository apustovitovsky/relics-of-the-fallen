Да, система Gameplay Cues у нас сейчас фактически не готова. Существующий [GameplayCue.cs](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/Code/GameplayCues/GameplayCue.cs:8) — legacy и архитектуре Unreal GAS не соответствует.

В оригинальном GAS impact устроен так:

```text
Authoritative projectile detects impact
→ Source ASC.ExecuteGameplayCue(
      GameplayCue.Fireball.Impact,
      GameplayCueParameters)
→ GameplayCue распространяется наблюдателям
→ на каждом клиенте GameplayCueManager
  локально создаёт VFX/SFX
```

Для однократного impact используется событие `Executed` и аналог `GameplayCueNotify_Burst`/`GameplayCueNotify_Static`. Сам VFX не является сетевым объектом. `Executed` cues передаются ненадёжным multicast — они не должны содержать gameplay-логику. [Epic ASC API](https://dev.epicgames.com/documentation/unreal-engine/API/Plugins/GameplayAbilities/UAbilitySystemComponent?lang=en-US), [Mirror ClientRpc](https://mirror-networking.gitbook.io/docs/manual/guides/communications/remote-actions).

Важное уточнение к [DRAFT.md](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/DRAFT.md:139): полный `HitResult` для первого impact cue не обязателен. Оригинальный `FGameplayCueParameters` отдельно содержит `Location` и `Normal`. `HitResult` понадобится позже для bone, physical material и расширенной информации о попадании.

Что реализуем:

1. В core `GAS`:

```text
GameplayCueEvent
GameplayCueParameters
GameplayCueNotify
GameplayCueNotify_Burst
GameplayCueSet
GameplayCueManager
ASC.ExecuteGameplayCue(...)
ASC.InvokeGameplayCueEvent(...)
```

2. В `GAS.Mirror`:

```text
NetworkAbilitySystemObserverComponent
→ ненадёжный ClientRpc
→ tag + Location + Normal + необходимые context-поля
```

RPC получат только observers сетевого ASC, что соответствует Mirror и роли observer-компонента.

3. В Fireball:

```text
столкновение
→ применить damage GE, если найден target ASC
→ source ASC.ExecuteGameplayCue(
      GameplayCue.Fireball.Impact,
      parameters)
→ уничтожить projectile
```

Так impact проиграется и при попадании в персонажа, и при попадании в стену.

Старые `GameplayCue`, `CuesLibrary`, `instancedCues` и автоматическую подписку на все abilities/effects будем удалять, а не адаптировать.

Следующий шаг — добавить два фундаментальных core-типа: `GameplayCueEvent` и `GameplayCueParameters`.

Да, `CuesLibrary` нужно переписать, но не объединять с `AssetRegistry`.

Это не чисто Unity-зона:

- семантика `GameplayTag → GameplayCueNotify` принадлежит GAS;
- способ хранения и загрузки Unity-ассетов — Unity-адаптация.

Правильное разделение:

```text
AssetRegistry
→ AssetId ↔ ScriptableObject
→ стабильная identity для сети и сохранений

GameplayCueSet
→ GameplayTag → GameplayCueNotify
→ семантическая маршрутизация cues

GameplayCueManager
→ принимает tag + event + parameters
→ находит notify через GameplayCueSet
→ выполняет его локально
```

В Unreal именно `UGameplayCueSet : UDataAsset` хранит `FGameplayCueNotifyData` и ускоренную map `GameplayTag → entry`: [GameplayCueSet.h](C:/Users/NATALY/Documents/unity/ue5-docs/UnrealEngine/Engine/Plugins/Runtime/GameplayAbilities/Source/GameplayAbilities/Public/GameplayCueSet.h:16). Он также заранее строит fallback дочерних тегов на родительские notify: [GameplayCueSet.cpp](C:/Users/NATALY/Documents/unity/ue5-docs/UnrealEngine/Engine/Plugins/Runtime/GameplayAbilities/Source/GameplayAbilities/Private/GameplayCueSet.cpp:397).

Lyra не заменяет эту модель общим asset registry. Она расширяет `GameplayCueManager` главным образом политикой загрузки и preload: [LyraGameplayCueManager.cpp](C:/Users/NATALY/Documents/unity/ue5-docs/UnrealEngine/Samples/Games/Lyra/Source/LyraGame/AbilitySystem/LyraGameplayCueManager.cpp:95).

Почему текущий [CuesLibrary.cs](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/Code/GameplayCues/CuesLibrary.cs:16) лучше удалить после миграции:

- глобальная зависимость через `SingletonScriptableObject`;
- LINQ и создание нескольких списков при каждом вызове;
- копирование изменяемых `GameplayCue`;
- нет `GameplayCueEvent`;
- нет `GameplayCueParameters`;
- нет parent-tag fallback;
- библиотека одновременно хранит definitions и создаёт runtime state;
- дублирующее поле `name` нужно только для оформления Inspector.

Что делать с [AssetRegistry.cs](C:/Users/NATALY/Documents/unity/relics-of-the-fallen/Assets/GAS/Code/AssetRegistry/AssetRegistry.cs:11):

- оставить единым сгенерированным каталогом сетевых GAS-ассетов;
- не добавлять в него маршрутизацию cues;
- не передавать по сети `GameplayCueNotify` или prefab ID;
- по сети передавать cue tag и `GameplayCueParameters`;
- на каждом клиенте локальный `GameplayCueSet` разрешает tag в тот же notify.

То есть `GameplayCueNotify` вообще не обязан иметь `AssetId`. Прямая ссылка из `GameplayCueSet` гарантирует включение notify и его prefab-зависимостей в build.

Для текущего фиксированного контента прямые ссылки из `ScriptableObject` подходят. Unity рекомендует ScriptableObject как общий runtime-контейнер данных; Addressables нужны, когда требуется асинхронная загрузка, удалённый контент или выгрузка больших наборов ассетов. [Unity ScriptableObject](https://docs.unity3d.com/6000.1/Documentation/Manual/class-ScriptableObject.html), [Unity runtime asset management](https://docs.unity3d.com/ja/current/Manual/assets-managing-introduction.html).

Итоговая рекомендация:

```text
CuesLibrary
→ удалить

GameplayTagsWithCue
→ GameplayCueNotifyData

_CuesLibrary.asset
→ GameplayCueSet.asset

AssetRegistry
→ оставить отдельным и общим
```

`GameplayCueSet` будет GAS-аналогом, а его реализация через `ScriptableObject` и прямые Unity-ссылки — Unity-адаптацией.