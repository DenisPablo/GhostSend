namespace GhostSend.Infrastructure.Common.Errors;

public static class InfrastructureErrors
{
    public static class Persistence
    {
        public const string FileUploadError = "An error occurred while preparing the file for upload.";
        public const string FileRetrieveError = "An error occurred while retrieving the file.";
        public const string FileUpdateError = "An error occurred while updating the file metadata.";
        public const string FileDeleteError = "An error occurred while marking the file for deletion.";
        public const string ExpiredFilesRetrieveError = "An error occurred while retrieving expired files.";
    }

    public static class Storage
    {
        public const string FileNotFound = "File not found in storage.";
    }
}
