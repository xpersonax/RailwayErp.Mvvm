using RailwayErp.MvvmDemo.Models;

namespace RailwayErp.MvvmDemo.Messages;

/// <summary>
/// Повідомлення (WeakReferenceMessenger), яким сервіс стану сповіщає ViewModel'і
/// про зміну стану життєвого циклу застосунку (Stopped / Resumed / ...).
/// </summary>
/// <param name="State">Новий стан.</param>
/// <param name="ReturnedFromBackground">
/// Для Resumed: true, якщо перед цим був Stopped. На Windows MAUI викликає Resumed також
/// при кожному поверненні фокусу у вікно — такі випадки не потребують відновлення стану.
/// </param>
public sealed record AppLifecycleChangedMessage(AppLifecycleState State, bool ReturnedFromBackground = false);
