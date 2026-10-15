using CommunityToolkit.Mvvm.Messaging.Messages;
using RailwayErp.MvvmDemo.Models;

namespace RailwayErp.MvvmDemo.Messages;

/// <summary>
/// Повідомлення (WeakReferenceMessenger), яким сервіс стану сповіщає ViewModel'і
/// про зміну стану життєвого циклу застосунку (Stopped / Resumed / ...).
/// </summary>
public sealed class AppLifecycleChangedMessage(AppLifecycleState state) : ValueChangedMessage<AppLifecycleState>(state);
