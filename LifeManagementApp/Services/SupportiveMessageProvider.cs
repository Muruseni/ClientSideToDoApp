namespace LifeManagementApp.Services;

/// <summary>
/// Supplies short, positive messages for the Home and To-dos screens.
/// Keeping the choices here avoids duplicating the message list in ViewModels.
/// </summary>
public static class SupportiveMessageProvider
{
    private static readonly string[] Messages =
    [
        "Keep your important things moving.",
        "Small steps still move you forward.",
        "You can make today count.",
        "One task at a time is enough.",
        "You are doing great work.",
        "Make space for what matters most.",
        "Progress is progress, however small."
    ];

    public static string GetRandom() => Messages[Random.Shared.Next(Messages.Length)];
}
