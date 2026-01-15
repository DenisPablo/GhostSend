namespace GhostSend.Domain.Errors;

public static class DomainErrors
{
    public static class StoredFile
    {
        public const string FileNameRequired = "The file name cannot be null or empty.";
        public const string ContentTypeRequired = "The content type cannot be null or empty.";
        public const string NegativeSize = "The size must be greater than 0.";
        public const string NegativeMaxDownloads = "The max downloads must be greater than 0.";
        public const string NegativeLifeTime = "The life time must be greater than 0.";
        public const string StoragePathRequired = "The storage path cannot be null or empty.";
    }

    public static class Files
    {
        public const string FileExpired = "The file has expired.";
        public const string MaxDownloadsReached = "The maximum number of downloads has been reached.";
    }

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

    public static class General
    {
        public const string DatabaseError = "A database error occurred.";
        public const string UnauthorizedAccess = "Unauthorized access.";
        public const string UnexpectedError = "An unexpected error occurred.";
    }
}
