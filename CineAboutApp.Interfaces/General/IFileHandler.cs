namespace CineAboutApp.Interfaces.General
{
    public interface IFileHandler<T>
    {
        public Task<string> UploadFileAsync(string path, T file, int maxFileSizeInBytes = 0, string allowedFileTypes = "", CancellationToken cancellationToken = default);
        public Task<bool> DeleteFile(string path);
        public Task<Stream> GetFileAsStreamAsync(string path, CancellationToken cancellationToken = default);
    }
}
