using System.Text.RegularExpressions;

namespace CineAboutApp.Services.General
{
    public static class ProfileImages
    {
        public const string Folder = "UserProfile";

        public static string ToUrl(int userId, string fileName) => $"/api/UserProfile/{userId}/image/{fileName}";

        public static bool IsSafeFileName(string? fileName) =>
            !string.IsNullOrEmpty(fileName) && fileName.Length <= 200 && Regex.IsMatch(fileName, @"^[A-Za-z0-9_-]+(\.[A-Za-z0-9_-]+)*$");

        public static string? FileNameOf(string? stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return null;
            var name = stored.Trim().Replace('\\', '/');
            name = name[(name.LastIndexOf('/') + 1)..];
            return IsSafeFileName(name) ? name : null;
        }

        public static string? ToRelativePath(string? stored)
        {
            var name = FileNameOf(stored);
            if (name == null) return null;
            var s = stored!.Trim().Replace('\\', '/');
            var isOurs = s.StartsWith($"{Folder}/", StringComparison.OrdinalIgnoreCase) ||
                         Regex.IsMatch(s, @"^/api/UserProfile/\d+/image/[^/]+$", RegexOptions.IgnoreCase);
            return isOurs ? $"{Folder}/{name}" : null;
        }

        public static string ContentTypeOf(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };
    }
}
