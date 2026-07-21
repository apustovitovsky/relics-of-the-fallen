## Направление разработки
1. Развиваем свою игру основываясь на референс прокете Boss Room
2. Наш режим игры: third person extraction multiplayer co-op action rpg.
3. Основной референс:
    C:\Users\NATALY\Documents\unity\com.unity.multiplayer.samples.coop

## Tech Stack
Используем тот же стек что и референс, но с различиями:
1. com.cysharp.unitask - асинхронный код
2. Assets\Mirror - нетворкинг
3. Assets\GAS - Gameplay Ability System

## Правила имплементации

1. Не изменяй код самостоятельно — все правки вношу только я.
2. Перед каждым шагом обязательно сверяйся с тем как то что мы хотим добавить реализовано в референс проекте.
3. За одно сообщение присылай полное содержимое не более 1–3 файлов. Где основной принцип это 1 - вероятность что у меня возникнут вопросы или я захочу внести корректировки высока, а 3 - дальнейшие шаги вполне очевидны.
4. Перед каждым файлом указывай кликабельную ссылку для открытия в VS Code, даже если файл ещё не создан.
5. Не присылай отдельные фрагменты кода — всегда показывай файл целиком.
7. Старайся чистить хвосты как можно раньше.
8. В конце каждого сообщения кратко указывай что именно было получено и цель следующего шага, периодически добавляй общий процент завершенности (если мы делаем рефактор).
10. Терминал не подключен, необходимо смотреть логи в Editor.log
11. Избегаем использование [FormerlySerializedAs("...")] - всегда стараемся пересоздавать ассеты заново.
12. Не использовать deprecated методы.

## Пример разбиения кода

```text
Scripts/
└── Application/
    ├── RelicsOfTheFallen.Application.asmdef
    ├── Runtime/
    ├── Composition/
    │   └── RelicsOfTheFallen.Application.Composition.asmdef
    └── Editor/
        └── RelicsOfTheFallen.Application.Editor.asmdef
└── Character/
    ├── RelicsOfTheFallen.Application.asmdef
    ├── Runtime/
    ├── Cinemachine/
    │   └── RelicsOfTheFallen.Character.Cinemachine.asmdef
    └── Composition/
        └── RelicsOfTheFallen.Character.Composition.asmdef
```

Зависимости направляются от специализированных модулей к базовым. Базовый модуль не зависит от дочерних, а дочерние модули разных веток не зависят друг от друга. Допускается не более одного уровня вложенности.

* VContainer используй только в `*.Composition`;
* Editor-код помещай в `*.Editor` с `includePlatforms: ["Editor"]`;

