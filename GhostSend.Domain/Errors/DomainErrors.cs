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
        public const string FileRequired = "The file is required.";
        public const string FileTooLarge = "The file size exceeds the maximum limit.";
        public const string FileExpired = "The file has expired.";
        public const string MaxDownloadsReached = "The maximum number of downloads has been reached.";
        public const string FileNotFound = "The requested file was not found.";
        public const string InvalidDeleteToken = "The provided delete token is invalid.";
        public const string ConcurrentDownload = "The file is being downloaded concurrently. Please try again.";
        public const string FileNameInvalidCharacters = "The file name contains invalid characters.";
        public const string ContentTypeNotAllowed = "The content type is not allowed.";
    }


    public static class General
    {
        public const string DatabaseError = "A database error occurred.";
        public const string UnauthorizedAccess = "Unauthorized access.";
        public const string UnexpectedError = "An unexpected error occurred.";
    }
}
