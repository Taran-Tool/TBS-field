# TBS-field

# Задание
  1. Нужно реализовать многопользовательскую пошаговую стратегическую игру на два игрока.
  2. Игровое поле генерируется на старте сессии.
  3. Два игрока начинают с некоторым определенным набором юнитов (зеркальный набор) по разные стороны игрового поля.
  4. Каждый игрок в свой ход может совершить два действия: **Перемещение и Атаку**.  
  **Перемещение:** Игрок может приказать одному любому подконтрольному ему юниту переместится в указанную точку в пределах “скорости” юнита. Чем больше “скорость” у юнита - тем большее расстояние юнит может пройти за один ход.  
  **Атака:** Игрок может приказать одному любому подконтрольному ему юниту атаковать юнита противника в пределах “дальности атаки” юнита.
  Игрок может завершить свой ход до того как потратит оба действия. Игрок не может использовать два перемещения или две атаки за один ход.  
  5. На каждый ход игроку выделяется 60 секунд.  
  Для разрешения ситуаций, когда один игрок избегает боя сделать следующий механизм разрешения ничьей:
  - Если на ход номер 15 (или позже) у игроков разное количество юнитов - побеждает игрок у которого осталось больше юнитов
  - Если на ход номер 15 у игроков одинаковое количество юнитов - всем юнитам дается бесконечная “скорость” передвижения.  
# Подробности реализации
## Игровое поле
  1. Игровое поле произвольного размера
     1. Можно задать какие препятствия могут появляться, и зону в пределах которой препятствия могут появляться
     2. В каждом отдельном препятствии можно указать минимальное и максимальное количество препятствий этого типа
     3. Можно задать точки появления юнитов для обоих игроков (Допускается зона с рандомизацией положения юнитов)
     4. Можно задать стартовый состав армий игроков
## Юниты
  1. Два типа юнитов - [медленный но дальнобойный] и [быстрый но с малым радиусом атаки]. Здоровье и урон не обязательны, считаем, что у всех одно очко здоровья и одна единица урона
  2. Левой кнопкой мыши игрок выбирает юнита. Выбранный юнит подсвечивается (Подсветку можно сделать в произвольном виде. Индикатор над юнитом, аутлайн, кружок под юнитом)
  3. Правой кнопкой мыши прогнозируется путь для данного юнита в точку нажатия (Если нажата левая кнопка мыши, то прогнозируемый путь стирается)
  4. Двойным нажатием правой кнопки юнит отправляется в указанную точку
  5. Игрок не может построить путь, который по длине превосходит скорость передвижения юнита и юнит не может начать движение в точку, если длина пути к ней превосходит его скорость
  6. Юнит не может проходить сквозь препятствия
  7. Юнит во время движения не может толкать других юнитов или проходить их сквозь, путь должен строится вокруг других юнитов
  8. У выбранного юнита есть отображение радиуса его атаки. Если построен путь, то радиус атаки отображается из финальной точки пути
  9. Все вражеские юниты внутри радиуса атаки (вокруг юнита когда нет пути и вокруг финальной точки когда путь есть) подсвечиваются
  10. Правой кнопкой мыши по вражескому юниту отдается приказ на атаку. При успешной атаке юнит-цель уничтожается
  11. **Опционально** Нельзя атаковать противников, которые за препятствием
  12. **Опционально** Радиус атаки должен учитывать размер юнитов. Юнит-цель может частично находится в радиусе атаки и быть валидной целью
  13. **Опционально** Не использовать коллайдеры или триггеры для определения целей в радиусе атаки
  14. **Опционально** Сервер не разрешает совершать невалидные действия (примитивная защита от читов)
## Интерфейс
На интерфейсе отображается:  
  1. Текущий таймер хода
  2. Текущий номер хода
  3. Чей сейчас ход
  4. Есть ли возможность совершить передвижение
  5. Есть ли возможность совершить атаку
## Технические требования
  1. Проект разрабатывать на версии Unity 2022 LTS (URP, SRP или HDRP)
  2. Многопользовательское взаимодействие реализовывать на основе Netcode for GameObjects
  3. Лобби, в котором оба игрока должны подтвердить свою готовность не нужно, сессию можно начинать, сразу когда клиент подключится к серверу
  4. **Опционально** На старте сессии сервер не должен передавать состояние всего игрового поля (Препятствия и положение препятствий) клиенту ни в открытом, ни сжатом, ни шифрованном виде. (Считаем, что игровое поле может быть достаточно обширным и содержать достаточно большое количество объектов, что отправка по сети всего поля может вызвать нежелательную нагрузку на сеть и очень долгое время загрузки клиента). Передача по сети данных о юнитах допускается.

# День первый
## Мысли
  1. Проект разделю на модули и подмодули которые будут взаимодействовать между собой, но будут узкоспециализировнными;
  2. Необходим редактор юнитов - пока два типа;
  3. Необходим редактор карт - различный размер + наборы препятствий;
  4. Необходим редактор армий - наборов юнитов для карт;
  5. Заранее создаю шаблон карты, для нее возможные армии из ранее созданных юнитов;

## Модули и их подмодули
  1. GameLogic - управление игровой логикой, ходами, правилами, действиями;
     1. TurnManager - очередность ходов, таймер;
     2. ActionSystem - действия (перемещение, атака);
     3. VictorySystem - проверка условий победы;
     4. GameRules - настройки правил игры;
  2. Networking - сетевое взаимодействие, RPC, обработка входящих команд;
     1. GameNetworkManager - управление сессиями, подключениями;
     2. NetworkCommandHandler - исполнение сетевых команд;
     3. NetworkValidator - валидация сетевых команд;
     4. NetworkSyncHandler - синхронизация состояний игры;
     5. NetworkPlayer - непосредственно сам сетевой игрок;
  3. World - представление игрового мира
     1. WorldGenerator - генерация карты, размещение препятствий;
     2. UnitSystem - размещение юнитов;
     3. Pathfinding - расчет путей;
     4. ObstacleSystem - управление препятствиями и столкновениями;
  4. UI - пользовательский интерфейс
     1. GameHUD - графический игровой интерфейс;
     2. UnitUI - информация о юнитах;
     3. InputHandler -  управление юнитами - перемещение, атака;
     4. SelectionSystem  - управление юнитами - выбор, отмена выбора;
     5. MainMenu - основное меню игры;
  5. Editor - редакторы конфигураций;
     1. UnitConfigEditor - редактор конфигураций юнитов;
     2. MapPresetEditor - редактор пресетов карт;
     3. ArmyCompositionEditor - редактор стартовых армий;

# День второй
Создал:
  - Networking
    - GameNetworkManager
    - NetworkPlayer
    - NetworkCommandHandler
    - NetworkSyncHandler
    - NetworkValidator
  - UI
    - GameHUD
    - MainMenu

На текущем этапе получается создавать хост и реализовывать клиентское подключение:
  1. Пользователь запускает игру и видит главное меню (MainMenu).
  2. При нажатии на кнопку "Host" или "Join" вызываются соответствующие методы в GameNetworkManager.
  3. Для хоста:
     1. Создается хост, регистрируются обработчики событий.
     2. После успешного запуска хоста создаются игроки (NetworkPlayer) и синхронизируется состояние игры.
  4. Для клиента:
     1. Клиент подключается к указанному хосту.
     2. После подключения регистрируется локальный игрок и синхронизируется состояние игры.

## Процесс
**Для хоста**
  1. Пользователь нажимает "Host" в MainMenu.
  2. Вызывается GameNetworkManager.StartHostGame().
  3. Регистрируются обработчики событий:
     1. OnHostStarted: вызывается при успешном запуске хоста.
     2. OnHostClientConnected: вызывается при подключении клиента.
  4. Хост запускается (NetworkManager.Singleton.StartHost()).
  5. После запуска хоста:
     1. Создаются NetworkCommandHandler и NetworkSyncHandler.
     2. Создаются игроки (SpawnPlayers): Player1 для хоста, Player2 для клиента.
  6. При подключении клиента:
     1. Вызывается NetworkSyncHandler.SyncGameStateServerRpc для синхронизации состояния.

**Для клиента:**
  1. Пользователь нажимает "Join" в MainMenu, вводит IP (по умолчанию "localhost").
  2. Вызывается GameNetworkManager.JoinGame(ipAddress).
  3. Клиент подключается к хосту (NetworkManager.Singleton.StartClient()).
  4. После подключения:
     1. Вызывается OnClientConnected.
     2. Регистрируется локальный игрок (RegisterLocalPlayer).
     3. Вызывается OnLocalPlayerReady для обновления UI (например, в GameHUD).
  5. Хост синхронизирует состояние игры через NetworkSyncHandler.
## Подмодули

**MainMenu**
  - Управляет UI главного меню.
  - Обрабатывает нажатия кнопок: Host, Join, Quit.
  - Переключает панели для выбора хоста или подключения к игре.
  - Вызывает методы GameNetworkManager для запуска хоста или подключения клиента.

**GameHUD**
  - Отчвечает за отображение HUD во время игры.
  - Управляет визуальными элементами, такими как индикатор игрока и текст текущего хода.
  - Синглтон, сохраняется между сценами.
  - Обновляет визуальные элементы в зависимости от текущего игрока.

**NetworkValidator**
  - (Пока не реализован - заглушка, на будущее.) Предназначен для валидации сетевых действий, например, проверки, является ли текущий ход локального игрока.

**GameNetworkManager**
  - Управляет сетевыми подключениями (хост и клиент).
  - Создает и синхронизирует игроков (NetworkPlayer).
  - Регистрирует обработчики событий подключения.
  - Запускает синхронизацию состояния игры через NetworkSyncHandler.

**NetworkCommandHandler**
  - (Пока не реализован полностью.) Служит для обработки сетевых команд.
  - Хранит ссылки на игроков (NetworkPlayer).

**NetworkSyncHandler**
  - Обеспечивает синхронизацию состояния игры между хостом и клиентами.
  - Использует ServerRpc и ClientRpc для передачи данных.
  - Пока содержит заглушки для синхронизации карты, юнитов и текущего хода.

**NetworkPlayer**
  - Представляет игрока в сети.
  - Содержит информацию о команде игрока (Player.Player1 или Player.Player2).
  - Синхронизирует состояние команды (какая из них) через NetworkVariable.