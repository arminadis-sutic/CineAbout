using CineAboutApp.Model;

namespace CineAboutApp.Services.Controller
{
    internal static class ReferenceNameGuard
    {
        public const int MaxLength = 200;

        public static string Normalize(string? name, string noun)
        {
            var trimmed = name?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                throw new UserException($"{noun}: naziv je obavezan.");
            }

            if (trimmed.Length > MaxLength)
            {
                throw new UserException($"{noun}: naziv može imati najviše {MaxLength} znakova.");
            }

            return trimmed;
        }
    }
}
