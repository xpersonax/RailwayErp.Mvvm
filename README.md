# Лабораторна робота №3 — Життєвий цикл компонентів застосунку

**Дисципліна:** Мобільна розробка (.NET MAUI, C#, патерн MVVM)
**Наскрізний приклад:** `RailwayErp.MvvmDemo` — ERP-система керування залізничною компанією
**Гілка:** `lab3`
**Виконав(ла):** ______________________ (ПІБ, група)

---

## Зміст

1. [Мета роботи](#мета-роботи)
2. [Середовище та запуск](#середовище-та-запуск)
3. [Структура проєкту](#структура-проєкту)
4. [Реалізовані сценарії](#реалізовані-сценарії)
5. [Скріншоти](#скріншоти)
6. [Відповіді на контрольні питання](#відповіді-на-контрольні-питання)
7. [Висновки](#висновки)

---

## Мета роботи

Ознайомитися з життєвим циклом .NET MAUI-застосунку та окремих сторінок, навчитися коректно зберігати
й відновлювати стан застосунку, реєструвати сервіси та ViewModel'і у DI-контейнері, а також реалізувати
додаткові сценарії, пов'язані зі станом застосунку: фонові задачі, перемикання теми та deep linking.

## Середовище та запуск

| Компонент | Версія |
|---|---|
| .NET SDK | 10.0.401 |
| .NET MAUI (workload) | 10.0.20 |
| CommunityToolkit.Mvvm | 8.4.2 (`[ObservableProperty]`, `[RelayCommand]`, `WeakReferenceMessenger`) |
| Цільові платформи | Android, iOS, Mac Catalyst, Windows (WinUI 3, unpackaged) |
| Перевірено на | Windows 11 (скріншоти нижче зроблено з Windows-збірки) |

**Запуск у Visual Studio:** відкрити `RailwayErp.MvvmDemo.slnx`, обрати *Windows Machine* або Android-емулятор, F5.

**Запуск з командного рядка (Windows):**

```bash
dotnet build RailwayErp.MvvmDemo/RailwayErp.MvvmDemo.csproj -f net10.0-windows10.0.19041.0 -t:Run
```

**Запуск на Android-емуляторі:**

```bash
dotnet build RailwayErp.MvvmDemo/RailwayErp.MvvmDemo.csproj -f net10.0-android -t:Run
```

## Структура проєкту

```
RailwayErp.MvvmDemo/
├── App.xaml(.cs)              ← події Window (Created/Activated/Deactivated/Stopped/Resumed/Destroying),
│                                збереження/відновлення стану, тема, OnAppLinkRequestReceived
├── AppShell.xaml(.cs)         ← вкладки //trains, //route, //log, //settings + маршрут traindetails
├── MauiProgram.cs             ← DI-контейнер, логування, нативні lifecycle-події платформ
├── Models/                    ← Train, RouteDraft (чернетка форми), LogEntry, AppLifecycleState
├── Messages/                  ← AppLifecycleChangedMessage (WeakReferenceMessenger)
├── Services/
│   ├── AppStateService.cs     ← IAppStateService (Singleton): стан сесії + Preferences
│   ├── ThemeService.cs        ← IThemeService: UserAppTheme + Preferences["app_theme"]
│   ├── DeepLinkService.cs     ← IDeepLinkService: URI → маршрут Shell, відкладені посилання
│   ├── NavigationService.cs   ← INavigationService / IDialogService (Shell)
│   ├── TrainRepository.cs     ← дані поїздів (in-memory, імітація затримки мережі)
│   ├── RouteRepository.cs     ← збережені маршрути
│   ├── ProtocolRegistrationService.cs
│   └── Logging/               ← ILogStore + ILoggerProvider (сторінка «Журнал» + файл lifecycle.log)
├── ViewModels/                ← BaseViewModel, TrainList, TrainDetails, RouteEdit, Settings, Log
├── Views/                     ← TrainListPage, TrainDetailsPage, RouteEditPage, SettingsPage, LogPage
├── Converters/                ← TrainStatusToColorConverter
├── Resources/Styles/          ← Colors.xaml, Styles.xaml (усі кольори через AppThemeBinding)
└── Platforms/
    ├── Android/MainActivity.cs        ← IntentFilter (railwayerp:// та https App Links), OnNewIntent
    └── Windows/WindowsDeepLinks.cs    ← protocol-активація, реєстрація схеми для unpackaged
```

## Реалізовані сценарії

### 1. Обробка подій життєвого циклу App/Window з логуванням *(аудиторна, п. 1)*

У `App.CreateWindow` вікно створюється з `AppShell`, і на нього підписуються **всі** події життєвого циклу:

```csharp
var window = new Window(shell) { Title = "Railway ERP · MVVM Demo" };
window.Created     += OnWindowCreated;
window.Activated   += OnWindowActivated;
window.Deactivated += OnWindowDeactivated;
window.Stopped     += OnWindowStopped;     // згорнуто → зберігаємо стан у Preferences
window.Resumed     += OnWindowResumed;     // повернення з фону → відновлюємо стан
window.Destroying  += OnWindowDestroying;  // завершення → зберігаємо стан, відписуємося від подій
```

Логування виконується через стандартний `ILogger<App>` (Microsoft.Extensions.Logging). У `MauiProgram`
підключено два провайдери: `AddDebug()` (вікно *Output* у Visual Studio) та власний `LogStoreLoggerProvider`,
який пише записи на сторінку **«Журнал»** і у файл `lifecycle.log` у `FileSystem.AppDataDirectory`.
Завдяки файлу видно навіть `Window.Destroying` попереднього запуску (такі записи показуються приглушено).

Додатково через `ConfigureLifecycleEvents` логуються **нативні** події платформи, щоб було видно,
з чого MAUI «збирає» свої події:

| Подія MAUI (`Window`) | Android (`Activity`) | Windows (WinUI) — спостерігалось у журналі |
|---|---|---|
| `Created` | `OnPostCreate` (після `OnCreate`) | після створення вікна й першої навігації Shell |
| `Activated` | `OnResume` | `Activated` (вікно отримало фокус) |
| `Deactivated` | `OnPause` | `Activated` зі станом *Deactivated* (втрата фокусу) |
| `Stopped` | `OnStop` | `VisibilityChanged(Visible=False)` — згортання |
| `Resumed` | `OnRestart` (повернення після `OnStop`) | повторна активація вікна |
| `Destroying` | `OnDestroy` | закриття вікна, далі `Window.Closed` |

> **Особливість Windows, знайдена під час тестування.** WinUI викликає `Window.Resumed` при *кожному*
> поверненні фокусу у вікно, навіть якщо його не згортали. Тому `IAppStateService` відстежує прапорець
> `IsInBackground`, і ViewModel'і відновлюють стан лише при `Resumed` після справжнього `Stopped`
> (поле `ReturnedFromBackground` у `AppLifecycleChangedMessage`).

### 2. Збереження та відновлення стану форми при згортанні *(аудиторна, п. 2)*

Форма **«Маршрут»** (`RouteEditPage` + `RouteEditViewModel`) — редагування нового маршруту: номер, поїзд,
станції, дата/час, проміжні зупинки, «експрес», примітки.

1. Кожна зміна поля (`OnPropertyChanged`) оновлює чернетку `RouteDraft` у `IAppStateService` — у пам'яті, дешево.
2. **`Window.Stopped`**: `App` спочатку розсилає `AppLifecycleChangedMessage(Stopped)` (форма віддає актуальну
   чернетку, таймери зупиняються), а потім викликає `SaveState()` — чернетка серіалізується в JSON
   (source-generated `JsonSerializerContext`, безпечно для trimming) і записується в `Preferences`
   разом із поточною сторінкою (`Preferences["last_page"]`).
3. **`Window.Resumed`** (після `Stopped`): форма відновлюється зі стану і показує банер
   «Повернення о … (Window.Resumed): форму відновлено…» (скріншот 03).
4. **Процес знищено у фоні** (типово для Android): при наступному холодному старті `App` у конструкторі
   викликає `RestoreState()`, Shell відкриває останню сторінку (`//route`), а форма заповнюється
   з чернетки з банером «Відновлено незбережену чернетку…» (скріншот 05). Перевірено так: згорнути →
   примусово завершити процес → запустити знову.
5. Після **«Зберегти маршрут»** (з валідацією) чернетка видаляється з пам'яті та `Preferences`.

### 3. OnAppearing / OnDisappearing на сторінці списку *(аудиторна, п. 3)*

`TrainListPage.xaml.cs`:

```csharp
protected override async void OnAppearing()
{
    base.OnAppearing();
    _logger.LogInformation("TrainListPage.OnAppearing → LoadCommand");
    _viewModel.Activate();                          // підписка на повідомлення + запуск автооновлення
    await _viewModel.LoadCommand.ExecuteAsync(null); // завантаження даних через ViewModel
}

protected override void OnDisappearing()
{
    base.OnDisappearing();
    _viewModel.Deactivate();                        // зупинка таймера, відписка
    _logger.LogInformation("TrainListPage.OnDisappearing: перехід зі сторінки списку");
}
```

Також перевизначено `OnNavigatedTo` / `OnNavigatedFrom` — у журналі видно тип навігації (`Push`, `Replace`)
і сторінку, з якої/на яку перейшли (скріншот 14). `LoadCommand` згенеровано атрибутом `[RelayCommand]`.

### 4. Сервіс стану застосунку `IAppStateService` у DI як Singleton *(аудиторна, п. 4)*

```csharp
// MauiProgram.cs
builder.Services.AddSingleton(Preferences.Default);
builder.Services.AddSingleton<IAppStateService, AppStateService>();
builder.Services.AddSingleton<IThemeService, ThemeService>();
builder.Services.AddSingleton<IDeepLinkService, DeepLinkService>();
// ...
builder.Services.AddTransient<TrainListViewModel>();
builder.Services.AddTransient<TrainListPage>();
```

`IAppStateService` зберігає стан **поточної сесії**: ідентифікатор і час старту сесії, поточний стан
життєвого циклу, лічильники згортань/повернень, час останнього збереження, поточну сторінку, останній
переглянутий поїзд і чернетку форми. Сервіс отримують через конструктор `App`, `AppShell`, `RouteEditViewModel`,
`TrainDetailsViewModel`, `LogViewModel`; картка сесії показана на сторінці «Журнал» (скріншот 04).
Сторінки та ViewModel'і зареєстровані як `Transient` і створюються контейнером (Shell `ContentTemplate`
і `Routing.RegisterRoute` беруть їх із DI).

### 5. Перемикання теми (світла / темна) *(самостійна, п. 5)*

* Екран **«Налаштування»** → група `RadioButton` («Як у системі», «Світла», «Темна»), прив'язана до
  `SettingsViewModel.SelectedTheme` через `RadioButtonGroup.SelectedValue`.
* `ThemeService.SetTheme` встановлює `Application.Current.UserAppTheme` (діє на **всі** сторінки) і зберігає
  вибір у `Preferences["app_theme"]`; при старті `App.CreateWindow` викликає `ApplySavedTheme()`
  **до** створення вікна.
* Усі кольори у `Styles.xaml` задано через `AppThemeBinding`, тому сторінки перефарбовуються автоматично
  (скріншоти 06–09b).
* Знайдено й обійдено особливість WinUI: нативний `CalendarDatePicker` не перечитує ресурси теми при зміні
  теми «на льоту» (текст дати лишався білим на білому). `RouteEditPage` при появі та при
  `RequestedThemeChanged` явно задає пікерам поточну тему.

### 6. Deep link для відкриття конкретного запису *(самостійна, п. 6)*

Підтримувані посилання (розбирає `DeepLinkService.TryResolve` — чиста функція, без навігації):

| Посилання | Екран |
|---|---|
| `railwayerp://train/3`, `railwayerp://trains/3`, `railwayerp://train?id=3` | деталі поїзда #3 |
| `https://railwayerp.example.com/train/5` (Android App Link) | деталі поїзда #5 |
| `railwayerp://trains` | список поїздів |
| `railwayerp://route`, `railwayerp://log`, `railwayerp://settings` | відповідні вкладки |

Ланцюжок обробки:

```
Android: Intent(ACTION_VIEW) → MainActivity.OnCreate / OnNewIntent ┐
Windows: protocol-активація / аргумент командного рядка (OnLaunched) ┼→ Application.SendOnAppLinkRequestReceived(uri)
Налаштування: поле «Deep link» → «Відкрити»                         ┘          ↓
                                   App.OnAppLinkRequestReceived → IDeepLinkService.HandleAsync
                                   (якщо Shell ще не готовий — посилання відкладається до першої навігації)
                                                                            ↓
                                   Shell.GoToAsync("//trains/traindetails", { id, source, link })
                                                                            ↓
                                   TrainDetailsViewModel.ApplyQueryAttributes → банер «Відкрито за deep link»
```

**Android** (`Platforms/Android/MainActivity.cs`): два `IntentFilter` — власна схема `railwayerp` і
`https://railwayerp.example.com/train…` з `AutoVerify = true` (для App Links потрібен файл
`/.well-known/assetlinks.json` на домені). `LaunchMode.SingleTop` + `OnNewIntent` — посилання, що прийшло
у вже відкритий застосунок, не створює другу Activity. Перевірка:

```bash
adb shell am start -W -a android.intent.action.VIEW -d "railwayerp://train/3" com.railwayerp.mvvmdemo
```

**Windows**: схема оголошена в `Package.appxmanifest` (для MSIX) і може бути зареєстрована кнопкою
на екрані «Налаштування» для unpackaged-збірки (`ActivationRegistrationManager`, HKCU). Запуск із посиланням:

```bash
RailwayErp.MvvmDemo.exe railwayerp://train/3
```

### Додатково: фонова задача з урахуванням життєвого циклу

`TrainListViewModel` запускає автооновлення статусів поїздів (`PeriodicTimer` у `Task.Run`, кожні 15 с).
Задача скасовується через `CancellationToken` у `OnDisappearing` і при `Window.Stopped`, а при
поверненні з фону (`Resumed`) запускається знову з негайним оновленням даних — так застосунок не працює
у фоні, що відповідає обмеженням Android (Doze / App Standby).

## Скріншоти

| | |
|---|---|
| **01.** Список поїздів (дані завантажено в `OnAppearing`) <br> ![](docs/screenshots/01_train_list.png) | **02.** Заповнена форма маршруту <br> ![](docs/screenshots/02_route_form_filled.png) |
| **03.** Після згортання і відновлення (`Stopped` → `Resumed`) <br> ![](docs/screenshots/03_route_form_after_resume.png) | **04.** Журнал: `Stopped`, збереження в Preferences, `Resumed`, картка сесії <br> ![](docs/screenshots/04_log_minimize_restore.png) |
| **05.** Процес знищено у фоні → холодний старт: відкрито останню сторінку, чернетку відновлено <br> ![](docs/screenshots/05_restored_after_restart.png) | **06.** Налаштування: обрано світлу тему <br> ![](docs/screenshots/06_settings_light_theme.png) |
| **07.** Світла тема на сторінці списку <br> ![](docs/screenshots/07_trains_light_theme.png) | **08.** Світла тема на формі <br> ![](docs/screenshots/08_route_light_theme.png) |
| **09.** Обрано темну тему <br> ![](docs/screenshots/09_settings_dark_theme.png) | **09b.** Темна тема на формі <br> ![](docs/screenshots/09b_route_dark_theme.png) |
| **10.** Запуск за deep link `railwayerp://train/3` <br> ![](docs/screenshots/10_deeplink_train3.png) | **11.** Deep link на неіснуючий запис (#42) <br> ![](docs/screenshots/11_deeplink_not_found.png) |
| **12.** Журнал обробки deep link (зокрема відкладене посилання та App Link) <br> ![](docs/screenshots/12_log_deeplinks.png) | **13.** Деталі поїзда, відкриті тапом зі списку <br> ![](docs/screenshots/13_details_from_list.png) |
| **14.** Журнал: `OnAppearing` / `OnDisappearing` / `OnNavigatedTo` / `OnNavigatedFrom` <br> ![](docs/screenshots/14_log_page_lifecycle.png) | **15.** Збереження маршруту: чернетку очищено <br> ![](docs/screenshots/15_route_saved_dialog.png) |

## Відповіді на контрольні питання

**7. Які основні стани проходить .NET MAUI-застосунок протягом свого життєвого циклу?**

Застосунок може бути *не запущеним*, *запущеним (активним)*, *деактивованим* або *зупиненим*.
Переходи між ними MAUI повідомляє подіями класу `Window`:
`Created` (вікно створено, ініціалізація ресурсів) → `Activated` (на передньому плані, у фокусі) →
`Deactivated` (втратив фокус, але може бути видимим: дзвінок, системний діалог) → `Stopped`
(згорнуто / невидимий — тут треба зберігати стан, бо далі ОС може знищити процес без попередження) →
`Resumed` (повернення зі `Stopped`) → … → `Destroying` (вікно знищується, звільнення ресурсів).
Для сумісності з Xamarin.Forms лишилися також `Application.OnStart`, `OnSleep`, `OnResume`.
Кожна подія MAUI відповідає нативним подіям платформи (див. таблицю в п. 1 вище).

**8. У чому різниця між подіями OnAppearing і OnNavigatedTo?**

`OnAppearing()` — сторінка стає видимою користувачу з будь-якої причини (перший показ, повернення назад,
перемикання вкладки); аргументів не має, тому підходить для завантаження/оновлення даних і запуску
ресурсів сторінки (у нас — `LoadCommand` і таймер). `OnNavigatedTo(NavigatedToEventArgs)` викликається
**після завершення навігації** на сторінку і містить контекст переходу: `PreviousPage` та `NavigationType`
(`Push`, `Pop`, `Replace`…). Парна до неї `OnNavigatedFrom` знає `DestinationPage`. У нашому журналі
порядок такий: `OnAppearing` → `Shell.Navigated` → `OnNavigatedTo`, а при виході `OnDisappearing` →
`OnNavigatedFrom`. Ці події стосуються навігації та видимості сторінки; згортання всього застосунку
відстежується подіями `Window` (`Stopped`/`Resumed`).

**9. Навіщо потрібна реєстрація сервісів у DI-контейнері та чим відрізняються Singleton і Transient?**

DI-контейнер (`builder.Services` у `MauiProgram`) створює об'єкти й автоматично передає залежності через
конструктор. Це прибирає жорсткі зв'язки (ViewModel залежить від інтерфейсу `ITrainRepository`,
а не від конкретного класу), дозволяє підміняти реалізації (тести, платформи — у нас
`IProtocolRegistrationService` має окрему реалізацію для Windows) і керувати часом життя об'єктів в одному місці.
**Singleton** — один екземпляр на весь час роботи процесу; підходить для спільного стану й сервісів
(`IAppStateService`, `IThemeService`, репозиторії). **Transient** — новий екземпляр при кожному запиті;
підходить для сторінок і ViewModel'ей, щоб кожна сторінка мала «чистий» стан і не тримала зайвих
посилань. Є ще **Scoped** — у MAUI це один екземпляр на вікно (`Window`).

**10. Які обмеження накладає Android на роботу застосунків у фоновому режимі?**

* **Doze** — коли пристрій нерухомий і з вимкненим екраном, мережа, `JobScheduler`, синхронізація й звичайні
  будильники відкладаються до коротких «вікон обслуговування».
* **App Standby / Standby Buckets** — рідко використовувані застосунки отримують менше ресурсів
  (обмеження на jobs, alarms, мережу).
* **Background Execution Limits (Android 8+)** — не можна тримати фонові сервіси; довгу роботу треба робити
  через **Foreground Service** зі сповіщенням, з Android 12 його не можна стартувати з фону (крім винятків),
  з Android 14 потрібно вказувати тип foreground service.
* Процес у фоні може бути знищений системою (Low Memory Killer) **без** виклику `Destroying` — тому стан
  зберігаємо в `Stopped`.

Тому періодичну гарантовану роботу роблять через **WorkManager** (з обмеженнями: мережа, заряд),
а `Task.Run` придатний лише для коротких операцій без гарантії завершення після згортання. У нашому
застосунку фоновий таймер зупиняється в `Stopped` і запускається знову в `Resumed`.

**11. Як реалізується підтримка світлої та темної теми в MAUI?**

* `AppThemeBinding` у XAML: `TextColor="{AppThemeBinding Light=Black, Dark=White}"` — значення
  автоматично змінюється разом із темою; у коді — `SetAppThemeColor(...)`.
* Окремі `ResourceDictionary` для тем, які підміняються в `Application.Resources.MergedDictionaries`.
* `Application.Current.UserAppTheme = AppTheme.Light | Dark | Unspecified` — примусова тема для всього
  застосунку; `RequestedTheme` — фактична тема; подія `RequestedThemeChanged` — реакція на зміну.
* Вибір користувача зберігається в `Preferences` і застосовується при старті (до створення вікна).
* На Android у `MainActivity` має бути `ConfigChanges.UiMode`, щоб Activity не перестворювалась при зміні теми.

**12. Що таке deep linking і які механізми використовуються для його реалізації на Android?**

Deep link — URI, який відкриває **конкретний екран** застосунку ззовні (з push-сповіщення, браузера,
листа, іншого застосунку), а не лише головну сторінку. На Android:

* **Intent Filter** з `ACTION_VIEW` і категоріями `DEFAULT` + `BROWSABLE` та `<data>` (scheme/host/path).
  У MAUI задається атрибутом `[IntentFilter]` над `MainActivity`.
* **Власна схема** (`railwayerp://train/3`) — просто, але будь-який застосунок може оголосити таку саму.
* **Android App Links** — `https`-посилання на власний домен з `android:autoVerify="true"`; домен
  підтверджується файлом `/.well-known/assetlinks.json` (SHA-256 сертифіката), тоді посилання відкривається
  одразу в застосунку без діалогу вибору.
* `launchMode="singleTop"` + `OnNewIntent` — обробка посилання, коли застосунок уже запущений.
* Далі в MAUI: `Application.SendOnAppLinkRequestReceived(uri)` → `OnAppLinkRequestReceived` → розбір URI
  → `Shell.GoToAsync(маршрут, параметри)`.
* Перевірка: `adb shell am start -W -a android.intent.action.VIEW -d "railwayerp://train/3" com.railwayerp.mvvmdemo`.

## Висновки

У роботі реалізовано повний цикл керування станом .NET MAUI-застосунку: обробку всіх подій життєвого
циклу вікна з логуванням (у Debug Output, на окрему сторінку та у файл), збереження стану форми при
згортанні та його відновлення як після повернення, так і після знищення процесу, коректний розподіл
логіки між подіями сторінки (`OnAppearing`/`OnDisappearing`, `OnNavigatedTo`/`OnNavigatedFrom`) і вікна,
сервіс стану сесії в DI-контейнері як Singleton, перемикання світлої/темної теми зі збереженням вибору
та deep linking (Android Intent Filters / App Links, Windows protocol activation) з відкладеною обробкою
посилань на холодному старті. Під час тестування виявлено та враховано платформні особливості Windows
(`Resumed` при поверненні фокусу, ненадійний `Shell.Loaded`, оновлення теми в `CalendarDatePicker`).
