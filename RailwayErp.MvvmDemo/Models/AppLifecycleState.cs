namespace RailwayErp.MvvmDemo.Models;

/// <summary>Стани застосунку, що відповідають подіям класу Window.</summary>
public enum AppLifecycleState
{
    NotStarted,
    Created,
    Activated,
    Deactivated,
    Stopped,
    Resumed,
    Destroying
}
