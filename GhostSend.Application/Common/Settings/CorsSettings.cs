namespace GhostSend.Application.Common.Settings;

public class CorsSettings
{
    public const string SectionName = "CorsSettings";

    public string[] AllowedOrigins { get; init; } = Array.Empty<string>();
}
