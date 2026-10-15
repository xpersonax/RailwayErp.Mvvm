# Защита ЛР №3 — шпаргалка (как сделано и что говорить)

> Отчёт для сдачи — `README.md` (на украинском). Этот файл — для подготовки к защите.

## 1. Идея в двух предложениях

Приложение `RailwayErp.MvvmDemo` — ERP железнодорожной компании на .NET MAUI по MVVM: список поездов,
детали поезда, форма нового маршрута, журнал событий и настройки. Вся логика лабораторной построена
вокруг событий жизненного цикла `Window` и одного singleton-сервиса состояния `IAppStateService`,
который получает каждая ViewModel через DI.

## 2. Как я делал (по шагам)

1. **Прочитал задание** и выписал 6 пунктов оценивания (4 аудиторных + 2 самостоятельных).
2. **Создал проект** `dotnet new maui` (.NET 10), добавил `CommunityToolkit.Mvvm` — генераторы
   `[ObservableProperty]`, `[RelayCommand]` и `WeakReferenceMessenger`.
3. **Спроектировал слои**: `Models` → `Services` (интерфейсы + реализации) → `ViewModels` → `Views`;
   всё регистрируется в `MauiProgram.cs`.
4. **Реализовал пункты задания** (подробно ниже).
5. **Протестировал на Windows** (сборка под `net10.0-windows`), прогнал все сценарии и нашёл 4 реальные
   проблемы, исправил их (раздел 5 — хорошо рассказать на защите).
6. **Снял скриншоты** автоматически (скрипт через UI Automation) и написал отчёт.

## 3. Пункты задания: где смотреть и что сказать

| № | Что | Файл | Что сказать |
|---|---|---|---|
| 1 | Stopped/Resumed/Destroying + лог | `App.xaml.cs` → `CreateWindow` | «Подписываюсь на все 6 событий Window. Логирую через `ILogger<App>`: в Output (AddDebug), на страницу “Журнал” и в файл. Дополнительно в `MauiProgram.ConfigureLifecycleEvents` логирую нативные события Android/WinUI — видно, из чего MAUI собирает свои события.» |
| 2 | Сохранение формы при сворачивании | `RouteEditViewModel.cs`, `AppStateService.cs`, `App.OnWindowStopped` | «Каждое изменение поля обновляет черновик в сервисе состояния (в памяти). При `Stopped` сервис пишет его в `Preferences` как JSON. При `Resumed` форма восстанавливается. Если ОС убила процесс в фоне — при следующем запуске `RestoreState()` в конструкторе App, Shell открывает последнюю страницу, форма заполняется.» |
| 3 | OnAppearing/OnDisappearing | `TrainListPage.xaml.cs` | «В `OnAppearing` вызываю `LoadCommand` у ViewModel и запускаю автообновление, в `OnDisappearing` останавливаю таймер и логирую переход. Плюс `OnNavigatedTo/From` — там видно тип навигации и куда ушли.» |
| 4 | IAppStateService singleton | `MauiProgram.cs`, `Services/AppStateService.cs` | «`AddSingleton<IAppStateService, AppStateService>()` — один экземпляр на процесс. Хранит сессию: id, время старта, текущее состояние, счётчики сворачиваний, последнюю страницу, черновик. Карточка сессии — на странице “Журнал”.» |
| 5 | Тема | `ThemeService.cs`, `SettingsPage.xaml`, `Styles.xaml` | «`Application.Current.UserAppTheme` действует на всё приложение, все цвета в стилях через `AppThemeBinding`, выбор сохраняется в `Preferences["app_theme"]` и применяется при старте до создания окна.» |
| 6 | Deep link | `DeepLinkService.cs`, `Platforms/Android/MainActivity.cs`, `App.OnAppLinkRequestReceived` | «Android: `IntentFilter` со схемой `railwayerp` и https App Link. Ссылка → `SendOnAppLinkRequestReceived` → `DeepLinkService` разбирает URI в маршрут Shell → `GoToAsync("//trains/traindetails", {id})`. Если ссылка пришла до готовности Shell (холодный старт) — откладываю и открываю после первой навигации.» |

## 4. Демонстрация на защите (≈3 минуты)

1. Запустить → вкладка **«Журнал»**: показать `Window.Created`, `Activated`, `TrainListPage.OnAppearing → LoadCommand`.
2. Вкладка **«Маршрут»** → ввести пару полей → **свернуть** окно → **развернуть**: баннер
   «Повернення … (Window.Resumed)». В журнале: `Stopped` → «Стан збережено в Preferences» → `Resumed`.
3. Свернуть → **убить процесс** (Диспетчер задач / на Android смахнуть из «недавних» после сворачивания
   или `adb shell am kill com.railwayerp.mvvmdemo`) → запустить: открывается «Маршрут» с восстановленным черновиком.
4. **«Налаштування»** → «Світла» → пройтись по вкладкам → перезапустить: тема сохранилась.
5. Deep link:
   * Windows: `RailwayErp.MvvmDemo.exe railwayerp://train/3` или поле «Deep link» в настройках;
   * Android: `adb shell am start -W -a android.intent.action.VIEW -d "railwayerp://train/3" com.railwayerp.mvvmdemo`.
6. Закрыть и снова открыть → в «Журнале» приглушённые записи прошлого запуска, включая `Window.Destroying`.

## 5. Проблемы, которые нашёл при тестировании (плюс к оценке)

1. **В Windows `Resumed` приходит при каждом возврате фокуса**, даже без сворачивания
   (`Deactivated → Resumed → Activated`). Решение: флаг `IsInBackground` в сервисе состояния, ViewModel'и
   реагируют только на `Resumed` после настоящего `Stopped` (`ReturnedFromBackground` в сообщении).
2. **`Shell.Loaded` в Windows не срабатывал**, из-за чего отложенный deep link не обработался бы.
   Решение: событие `InitialNavigationCompleted` по первой навигации Shell + `Dispatcher.Dispatch`.
3. **Последняя страница перезаписывалась**: первая навигация Shell сразу пишет `//trains` в `LastPage`.
   Решение: запоминать восстановленную страницу в конструкторе App до первой навигации.
4. **Баг темы в WinUI `CalendarDatePicker`**: после переключения темы «на лету» текст даты оставался белым
   (белым по белому). Решение: на `RouteEditPage` при появлении и `RequestedThemeChanged` явно выставляю
   нативному пикеру `RequestedTheme`.

## 6. Вероятные вопросы преподавателя

**Почему сохраняете в `Stopped`, а не в `Destroying`?**
Потому что после `Stopped` ОС (особенно Android) может убить процесс без вызова `Destroying`. `Stopped` —
последний надёжный момент. `Destroying` я тоже обрабатываю, но как дополнительный.

**Зачем держать черновик в памяти, если есть Preferences?**
Писать на диск на каждую букву дорого. В памяти — мгновенно, на диск — один раз при `Stopped`/`Destroying`.

**Почему `Preferences`, а не `SecureStorage`?**
Черновик маршрута не секретный. `SecureStorage` — для токенов/паролей (Keystore/Keychain/DPAPI) и медленнее.

**Почему `IAppStateService` — Singleton, а ViewModel — Transient?**
Состояние сессии должно быть общим и жить весь процесс. ViewModel привязана к странице: новый экземпляр —
чистое состояние и нет «висящих» ссылок.

**Как ViewModel узнаёт, что приложение свернули?**
Через `WeakReferenceMessenger`: сервис состояния отправляет `AppLifecycleChangedMessage`, ViewModel
реализует `IRecipient<>`. Слабые ссылки — нет утечек памяти, ViewModel не зависит от `App`/`Window`.
Подписка в `OnAppearing`, отписка в `OnDisappearing`.

**Что делает `[ObservableProperty]` / `[RelayCommand]`?**
Source generator генерирует свойство с `OnPropertyChanged` и `partial`-методы `OnXxxChanged`, а для метода
`LoadAsync` — свойство `LoadCommand` (`IAsyncRelayCommand`).

**Почему `async void OnAppearing`?**
Это переопределение метода фреймворка с сигнатурой `void`; исключения внутри `LoadAsync` перехвачены и залогированы.

**Чем отличается `OnAppearing` от `OnNavigatedTo`?**
`OnAppearing` — страница стала видимой (любой причиной), без аргументов. `OnNavigatedTo` — после
завершения навигации, есть `PreviousPage` и `NavigationType`. В журнале: сначала `OnAppearing`, потом `OnNavigatedTo`.

**Что такое `JsonSerializerContext` в `AppStateService`?**
Source-generated сериализация без рефлексии — работает при trimming/AOT (Release-сборка Android).

**Как работает App Link без диалога выбора?**
`AutoVerify = true` в `IntentFilter` + файл `https://railwayerp.example.com/.well-known/assetlinks.json`
с package name и SHA-256 отпечатком сертификата. Тогда Android открывает ссылку сразу в приложении.

**Что будет, если ссылка на несуществующий поезд?**
Открывается страница «Поїзд #42 не знайдено» с кнопкой «До списку поїздів» (скриншот 11).

**Фоновые задачи?**
Автообновление статусов — `PeriodicTimer` в `Task.Run` с `CancellationToken`; отменяется в `OnDisappearing`
и `Stopped`. Для гарантированной периодической работы на Android нужен WorkManager (Doze/App Standby).

## 7. Что важно знать честно

* Скриншоты и ручное тестирование — на **Windows**. Android-код (IntentFilter, OnNewIntent, логирование
  нативных событий) написан, но на машине, где делалась работа, не было Android SDK, поэтому Android-сборку
  нужно проверить в Visual Studio с эмулятором (команда `adb` выше).
* Реальный домен `railwayerp.example.com` не существует — App Link показан как пример настройки.
