namespace GhostSend.Application.Common.Settings;

public class FileSettings
{
    public const string SectionName = "FileSettings";

    public long MaxFileSizeInBytes { get; init; }
    public string MaxFileSizeDescription { get; init; } = string.Empty;
}
