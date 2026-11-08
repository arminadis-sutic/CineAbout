using CineAboutApp.Interfaces.General;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CineAboutApp.Services.General
{
    public class FileHandler(IWebHostEnvironment env, ILogger<FileHandler> logger) : IFileHandler<IFormFile>
    {
        private static readonly Dictionary<string, List<byte[]>> _fileSignatures = new()
        {
            { ".jpeg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".jpg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
            { ".gif", new List<byte[]> { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
            { ".pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } } },
            { ".zip", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 }, new byte[] { 0x50, 0x4B, 0x05, 0x06 }, new byte[] { 0x50, 0x4B, 0x07, 0x08 } } },
            { ".docx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
            { ".mp4", new List<byte[]> { new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 }, new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70 } } },
            { ".webp", new List<byte[]> { new byte[] { 0x52, 0x49, 0x46, 0x46 } } }
        };
        public Task<bool> DeleteFile(string path)
        {
            try
            {
                File.Delete(Path.Combine(env.WebRootPath, path));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete file at path {Path}", path);
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }

        public async Task<Stream> GetFileAsStreamAsync(string path, CancellationToken cancellationToken = default)
        {
            Stream stream = new MemoryStream();
            using (var reader = new FileStream(Path.Combine(env.WebRootPath, path), FileMode.Open, FileAccess.Read))
            {
                await reader.CopyToAsync(stream, cancellationToken);
                await reader.FlushAsync(cancellationToken);
            }
            stream.Position = 0;
            return stream;
        }

        public async Task<string> UploadFileAsync(string path, IFormFile file, int maxFileSizeInBytes = 0, string allowedFileTypes = "", CancellationToken cancellationToken = default)
        {
            string fullPath = Path.Combine(env.WebRootPath, path);

            string extension = Path.GetExtension(file.FileName);

            string fileName = Path.GetRandomFileName() + DateTime.UtcNow.ToString("yyyy-MM-dd_HH_mm") + extension;

            if (string.IsNullOrEmpty(allowedFileTypes))
            {
                allowedFileTypes = ".jpg,.jpeg,.png,.gif,.webp,.pdf,.docx,.zip";
            }

            var allowedExtensions = allowedFileTypes.Split(',').Select(e => e.Trim().ToLowerInvariant()).ToList();
            if (!allowedExtensions.Contains(extension.ToLowerInvariant()))
            {
                throw new ArgumentException($"{extension} file type is not allowed, allowed types are {allowedFileTypes}");
            }

            ValidateMagicBytes(file, extension);

            if (file.Length > maxFileSizeInBytes && maxFileSizeInBytes > 0 || file.Length <= 0)
            {
                throw new ArgumentException($"Provided file size was {file.Length}, maximum allowed is {maxFileSizeInBytes}");
            }

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            using (var fs = new FileStream(Path.Combine(fullPath, fileName), FileMode.CreateNew, FileAccess.Write))
            {
                await file.CopyToAsync(fs, cancellationToken);
            }
            return fileName;
        }

        private void ValidateMagicBytes(IFormFile file, string extension)
        {
            var ext = extension.ToLowerInvariant();
            if (!_fileSignatures.ContainsKey(ext)) return;

            using var stream = file.OpenReadStream();
            using var reader = new BinaryReader(stream);
            
            var signatures = _fileSignatures[ext];
            var maxSignatureLength = Math.Max(12, signatures.Max(s => s.Length));
            var headerBytes = reader.ReadBytes(maxSignatureLength);

            if (ext == ".webp")
            {
                var riff = new byte[] { 0x52, 0x49, 0x46, 0x46 };
                var webp = new byte[] { 0x57, 0x45, 0x42, 0x50 };
                var okWebp = headerBytes.Length >= 12 && headerBytes.Take(4).SequenceEqual(riff) && headerBytes.Skip(8).Take(4).SequenceEqual(webp);
                if (!okWebp)
                {
                    logger.LogWarning("File signature mismatch for extension {Extension}.", extension);
                    throw new ArgumentException("Invalid file content for the given extension (Magic Bytes mismatch).");
                }

                return;
            }

            if (!signatures.Any(signature => headerBytes.Take(signature.Length).SequenceEqual(signature)))
            {
                logger.LogWarning("File signature mismatch for extension {Extension}. Header: {Header}", extension, BitConverter.ToString(headerBytes));
                throw new ArgumentException("Invalid file content for the given extension (Magic Bytes mismatch).");
            }
        }
    }
}

