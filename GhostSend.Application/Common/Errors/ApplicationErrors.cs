namespace GhostSend.Application.Common.Errors;

public static class ApplicationErrors
{
    public static class Files
    {
        public const string UploadError = "An error occurred while uploading the file.";
        public const string DownloadError = "An error occurred while preparing the file for download.";
        public const string DeleteError = "An error occurred while deleting the file.";
        public const string FileNotFound = "The requested file was not found.";
        public const string InvalidDeleteToken = "The provided delete token is invalid.";
    }
}
