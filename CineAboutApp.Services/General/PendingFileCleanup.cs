using CineAboutApp.Interfaces.General;
using CineAboutApp.Services.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CineAboutApp.Services.General
{
    public class PendingFileCleanup(ApplicationDbContext db, IWebHostEnvironment env, ILogger<PendingFileCleanup> logger) : IPendingFileCleanup
    {
        private const int MaxErrorLength = 400;

        public void Enqueue(string? storedPath)
        {
            var relative = Normalize(storedPath);
            if (relative == null)
            {
                return;
            }

            db.PendingFileDeletions.Add(new PendingFileDeletion { RelativePath = relative });
        }

        public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
        {
            var rows = await db.PendingFileDeletions.OrderBy(p => p.Id).Take(200).ToListAsync(cancellationToken);
            if (rows.Count == 0)
            {
                return 0;
            }

            var root = Path.GetFullPath(env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
            foreach (var row in rows)
            {
                try
                {
                    var full = Path.GetFullPath(Path.Combine(root, row.RelativePath));
                    if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogWarning("Pending file deletion {Id} points outside the web root and is dropped.", row.Id);
                        db.PendingFileDeletions.Remove(row);
                        continue;
                    }

                    if (File.Exists(full))
                    {
                        File.Delete(full);
                    }

                    db.PendingFileDeletions.Remove(row);
                }
                catch (Exception ex)
                {
                    row.Attempts++;
                    row.LastError = ex.Message.Length > MaxErrorLength ? ex.Message[..MaxErrorLength] : ex.Message;
                    logger.LogWarning(ex, "Could not delete file for pending deletion {Id}; it stays queued (attempt {Attempts}).", row.Id, row.Attempts);
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            return rows.Count;
        }

        private static string? Normalize(string? storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath) || storedPath.Contains("://", StringComparison.Ordinal))
            {
                return null;
            }

            var path = storedPath.Trim().Replace("\\", "/").TrimStart('/');
            if (path.StartsWith("media/", StringComparison.OrdinalIgnoreCase))
            {
                path = path["media/".Length..];
            }

            return path.Length == 0 || path.Length > 400 ? null : path;
        }
    }
}
