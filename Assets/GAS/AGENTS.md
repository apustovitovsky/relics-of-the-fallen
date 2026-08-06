## Цель разработки

Развиваем переиспользуемый GAS-фреймворк для Unity с явным разделением оригинальных механизмов Unreal GAS, reusable orchestration по образцу Lyra, сетевой интеграции Mirror и логики конкретной игры.

Критерий готовности: legacy-код удалён, слои имеют односторонние зависимости, а релевантный проекту функционал Unreal GAS и Lyra реализован с сохранением назначения API, поведения, жизненного цикла и семантики соответствующего референса.

## Архитектурные слои

```text
GAS
├─ оригинальные механизмы Unreal GAS
│
GAS.Common
├─ переиспользуемые opinionated-механизмы
│  по образцу Lyra
│
GAS.Mirror
├─ сетевые адаптеры для Mirror
│
RelicsOfTheFallen
└─ логика конкретной игры
```

Каждый новый тип, метод и жизненный цикл сначала должен быть отнесён к одному из этих слоёв. Механика не должна добавляться в core `GAS` только потому, что она нужна `GAS.Common`, Mirror или конкретной игре.

## Документация источников

Выбирай источник в зависимости от слоя. При необходимости можешь запросить sparse-checkout Engine папки оригинала.

### `GAS`

Для публичного API, основных типов, нейминга и жизненного цикла используй Unreal GAS:

- исходный код Unreal Engine `5.8.1-release`:
  `C:\Users\NATALY\Documents\unity\ue5-docs\UnrealEngine\Engine\Plugins\Runtime\GameplayAbilities\Source`;
- зависимые runtime-модули `GameplayTags`, `GameplayTasks`, `NetCore` и Iris:
  `C:\Users\NATALY\Documents\unity\ue5-docs\UnrealEngine\Engine\Source\Runtime`;
- официальную API-документацию Epic Games;
- документацию tranek:
  `C:\Users\NATALY\Documents\unity\ue5-docs\GASDocumentation-master`;
- `README.md` и `AbilitySystemQuestions.md` как основные концептуальные материалы.

Публичный API и основные типы core `GAS` сохраняй совместимыми с Unreal GAS. Нейминг должен соответствовать GAS с адаптацией стандартных C++-префиксов и конструкций под C#.

### `GAS.Common`

Для reusable orchestration используй исходный код Lyra:

`C:\Users\NATALY\Documents\unity\ue5-docs\UnrealEngine\Samples\Games\Lyra\Source`

Сохраняй назначение, поведение и жизненный цикл Lyra-механик, но убирай привязку к бренду и игровому контенту Lyra. Namespace `GAS.Common` выполняет роль контекстного префикса.

### `GAS.Mirror`

Для сетевого транспорта используй официальную документацию Mirror и проверенные Unity multiplayer-примеры. Mirror-слой адаптирует сетевой lifecycle к core GAS, но не переносит Mirror-зависимости в `GAS` или `GAS.Common`.

### `RelicsOfTheFallen`

Игровые способности, input bindings, визуальное представление и правила конкретной игры могут зависеть от reusable-слоёв, но не должны становиться частью framework без подтверждённого повторного применения.

Для архитектуры игрового слоя предпочтительно используй проверенные решения из следующих источников:

- Lyra — для композиции abilities, gameplay lifecycle, разделения gameplay и presentation, организации игрового контента и интеграции с GAS;
- Boss Room — для Unity-специфичных паттернов, prefab composition, server-authoritative gameplay, разделения runtime-логики и визуального представления;
- официальная документация и примеры Mirror — для конкретной реализации сетевого транспорта, RPC, authority, spawning и replication lifecycle.

Boss Room использует Netcode for GameObjects, поэтому его сетевой API не переносится буквально. Из него заимствуются архитектурные решения, а их транспортная реализация адаптируется под Mirror.

При конфликте источников соблюдай следующий приоритет:

```text
семантика GAS и ability lifecycle
→ Unreal GAS и Lyra

Unity-архитектура и presentation
→ Boss Room

сетевой транспорт
→ Mirror

Не копируй решения только ради формального сходства с референсом. Учитывай границы текущих слоёв, особенности Unity и потребности проекта. Перед каждым крупным архитектурным шагом сверяй решение с источником соответствующего слоя; значимые отклонения явно указывай и обосновывай.
```


## Требования к коду

Используй адаптированный под Unity стек референса. Для асинхронного кода используй `com.cysharp.unitask`.

1. Не изменяй код проекта самостоятельно: предоставляй готовые атомарные блоки на уровне целого метода, группы свойств и тп., а правки вношу я. Старайся избегать неоднозначных правок, где не очевидно куда именно вставлять код.
3. Для каждого изменяемого или создаваемого файла указывай кликабельную ссылку с номером строки для открытия в VS Code.
4. Предлагай цельные логические правки без лишнего дробления. При изменении тела приводи метод целиком; если меняется только сигнатура — только её.
5. Для значимых методов добавляй английский XML `summary`: ровно одна полная строка без описаний параметров и свойств.
6. Строго соблюдай `.editorconfig`.
7. Высокоуровневый orchestration-код не должен дублировать `null`-проверки: их выполняет вызываемая низкоуровневая сущность.
8. По возможности выбирай производительные решения без ущерба корректности и ясности.
9. Удаляй ненужный код и другие «хвосты» как можно раньше.
10. В конце ответа кратко указывай результат и цель следующего шага. Общий процент готовности добавляй только при продолжительном рефакторинге.
11. Терминал не подключён; диагностические сообщения и ошибки проверяй в `Editor.log`.

## Форматирование

1. Не переноси выражение после =, если присваивание помещается в допустимую длину строки 90 символов; используй такой перенос только при необходимости.
2. Каждый аргумент в объявлении или вызове метода размещай на отдельной строке.
3. Вызов метода с одним единственным аргументом не переносится если вмещается в допустимую длину строки.

## Архитектура

### 0. Классификация перед реализацией

Перед добавлением типа, метода, свойства или поля сначала определи его слой и найди референс именно для этого слоя:

- `GAS` → Unreal GAS;
- `GAS.Common` → Lyra;
- `GAS.Mirror` → Mirror и сетевой lifecycle GAS;
- `RelicsOfTheFallen` → требования конкретной игры.

Если сущность существует в оригинальном GAS, она остаётся в core `GAS`, даже если Lyra активно её использует. Например:

```text
GameplayAbility
AbilitySystemComponent
GameplayAbilitySpec
GameplayAbilitySpecHandle
DynamicAbilityTags
InputPressed
AbilitySpecInputPressed
AbilitySpecInputReleased
generic replicated events
prediction lifecycle
```

Lyra-derived orchestration не добавляй в core `GAS`. Например:

```text
CommonGameplayAbility
CommonAbilitySystemComponent
GameplayAbilityActivationPolicy
AbilityInputTagPressed
AbilityInputTagReleased
ProcessAbilityInput
ClearAbilityInput
GameplayAbilitySet
```

### 1. Соответствие типов

Используй следующее соответствие:

| Unreal | Unity framework |
|---|---|
| `UGameplayAbility` | `GAS.GameplayAbility` |
| `UAbilitySystemComponent` | `GAS.AbilitySystemComponent` |
| `ULyraGameplayAbility` | `GAS.Common.CommonGameplayAbility` |
| `ULyraAbilitySystemComponent` | `GAS.Common.CommonAbilitySystemComponent` |
| `ELyraAbilityActivationPolicy` | `GAS.Common.GameplayAbilityActivationPolicy` |

В namespace `GAS.Common` не добавляй `Lyra` в имена типов. Префикс `Common` используй для основных расширяемых базовых классов, когда он различает их с core-аналогом.

### 2. Направление зависимостей

Допустимы только следующие зависимости:

```text
GAS.Common ─────────→ GAS
GAS.Mirror ─────────→ GAS
RelicsOfTheFallen ──→ GAS.Common
RelicsOfTheFallen ──→ GAS.Mirror
```

Запрещены зависимости:

```text
GAS → GAS.Common
GAS → GAS.Mirror
GAS → RelicsOfTheFallen
GAS.Common → GAS.Mirror
GAS.Mirror → GAS.Common
```

`GAS.Mirror` работает с базовым `AbilitySystemComponent`. Наследники из `GAS.Common` должны поддерживаться без специальной Mirror-интеграции.

### 3. Управление жизненным циклом

Не используй события как скрытый control flow для обязательных этапов операции. Основной жизненный цикл выражай явными вызовами методов. События предназначены для необязательных наблюдателей и уведомлений, отсутствие которых не меняет корректность операции.

### 4. Подписки

Долгоживущие подписки реализуй по disposable-модели:

- регистрация возвращает подписку с `IDisposable` и `IsDisposed`;
- владелец освобождает подписку через `Dispose()`;
- `Dispose()` идемпотентен;
- прямые `+=` и `-=` допустимы только внутри реализации подписки или при ограничениях Unity/Mirror;
- внутри framework используй `DisposableSubscription` и `DisposableGroup`.

### 5. Сериализуемые свойства

Для новых сериализуемых свойств без дополнительной логики используй auto-property с `[field: SerializeField]`. Явное backing field используй только для собственной логики доступа, управления сериализованным состоянием или миграции существующих данных.

## Tests

Тесты организуй по архитектурным слоям:

- core `GAS` tests не зависят от `GAS.Common`, Mirror или игры;
- `GAS.Common` tests проверяют Lyra-derived orchestration через публичный API core GAS;
- `GAS.Mirror` tests проверяют только сетевую доставку и восстановление состояния;
- игровые acceptance-тесты проверяют законченные сценарии способностей.

Не тестируй внутреннее копирование отдельно без наблюдаемого поведения. Основные тесты строятся как acceptance-сценарии: тест выдаёт способность, активирует её штатным pipeline и проверяет конечное состояние мира. Внутренние spec, handle и transport DTO остаются деталями реализации.
