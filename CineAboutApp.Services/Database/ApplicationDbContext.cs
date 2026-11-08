using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using CineAboutApp.Models.Entities;
using Microsoft.AspNetCore.Http;
using CineAboutApp.Services.General;

namespace CineAboutApp.Services.Database
{
    public class ApplicationDbContext(DbContextOptions options, IHttpContextAccessor? httpContextAccessor = null) : DbContext(options)
    {
        private IHttpContextAccessor? _httpContextAccessor = httpContextAccessor;

        
        
        public DbSet<Country> Countries { get; set; }
        public DbSet<City> Cities { get; set; } = null!;
        public DbSet<UserList> UserLists { get; set; } = null!;
        public DbSet<UserListItem> UserListItems { get; set; } = null!;
        public DbSet<QuizCategory> QuizCategories { get; set; } = null!;
        
        
        public DbSet<MyAppUser> MyAppUsers { get; set; }
        
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<MyRefreshToken> MyRefreshTokens { get; set; }
        public DbSet<MyResetToken> MyResetTokens { get; set; }
        public DbSet<AppSettings> AppSettings { get; set; }
        
        public DbSet<UserDeviceToken> UserDeviceTokens { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }

        public DbSet<Movie> Movies { get; set; } = null!;
        public DbSet<QuizSession> QuizSessions { get; set; } = null!;
        public DbSet<WatchlistItem> WatchlistItems { get; set; } = null!;
        public DbSet<ThreadVote> ThreadVotes { get; set; } = null!;
        public DbSet<CommentVote> CommentVotes { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;

        public DbSet<Genre> Genres { get; set; } = null!;
        public DbSet<Language> Languages { get; set; } = null!;
        public DbSet<AwardType> AwardTypes { get; set; } = null!;
        public DbSet<Award> Awards { get; set; } = null!;
        public DbSet<Person> Persons { get; set; } = null!;
        public DbSet<Studio> Studios { get; set; } = null!;
        public DbSet<MovieGenre> MovieGenres { get; set; } = null!;
        public DbSet<MovieCast> MovieCasts { get; set; } = null!;
        
        public DbSet<QuizQuestion> QuizQuestions { get; set; } = null!;
        public DbSet<QuizSessionAnswer> QuizSessionAnswers { get; set; } = null!;
        
        public DbSet<ReviewVote> ReviewVotes { get; set; } = null!;
        public DbSet<Streak> Streaks { get; set; } = null!;
        public DbSet<SwipeHistory> SwipeHistories { get; set; } = null!;
        public DbSet<Reminder> Reminders { get; set; } = null!;
        public DbSet<Report> Reports { get; set; } = null!;

        public DbSet<CineAboutApp.Services.Database.Thread> Threads { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<Follow> Follows { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<ThreadTag> ThreadTags { get; set; } = null!;
        public DbSet<UserGenrePreference> UserGenrePreferences { get; set; } = null!;

        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<PendingFileDeletion> PendingFileDeletions { get; set; } = null!;
        public DbSet<SeedHistory> SeedHistories { get; set; } = null!;
        
        public DbSet<BlacklistedToken> BlacklistedTokens { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            if (_httpContextAccessor != null)
            {
                optionsBuilder.AddInterceptors(
                    new AuditLogSaveChangesInterceptor(_httpContextAccessor),
                    new SoftDeleteSaveChangesInterceptor(_httpContextAccessor)
                );
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Notification>()
                .Property(n => n.Data)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>(),
                    new ValueComparer<Dictionary<string, string>>(
                        (c1, c2) => c1!.SequenceEqual(c2!),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => new Dictionary<string, string>(c)
                    )
                );

            modelBuilder.Entity<UserNotification>()
                .HasOne(n => n.Notification)
                .WithMany(n => n.Users)
                .HasForeignKey(n => n.NotificationId);

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.PerformedByUser)
                .WithMany()
                .HasForeignKey(a => a.PerformedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>().HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>().HasOne(ur => ur.Role)
                .WithMany(u => u.Users)
                .HasForeignKey(ur => ur.RoleId);

            modelBuilder.Entity<UserProfile>().HasOne(up => up.User)
                .WithOne(u => u.UserProfile)
                .HasForeignKey<UserProfile>(up => up.UserId);

            
           

            modelBuilder.Entity<Follow>(entity =>
            {
                entity.HasOne(f => f.Follower)
                    .WithMany(u => u.Following)
                    .HasForeignKey(f => f.FollowerId);

                entity.HasOne(f => f.Following)
                    .WithMany(u => u.Followers)
                    .HasForeignKey(f => f.FollowingId);

                entity.HasIndex(f => new { f.FollowerId, f.FollowingId }).IsUnique();
            });

            modelBuilder.Entity<Streak>()
                .HasOne(s => s.User)
                .WithOne(u => u.Streak)
                .HasForeignKey<Streak>(s => s.UserId);

            modelBuilder.Entity<Streak>().HasIndex(s => s.UserId).IsUnique();
            modelBuilder.Entity<Review>().HasIndex(r => new { r.UserId, r.MovieId }).IsUnique();
            modelBuilder.Entity<ReviewVote>().HasIndex(v => new { v.ReviewId, v.UserId }).IsUnique();
            modelBuilder.Entity<ThreadVote>().HasIndex(v => new { v.ThreadId, v.UserId }).IsUnique();
            modelBuilder.Entity<CommentVote>().HasIndex(v => new { v.CommentId, v.UserId }).IsUnique();
            modelBuilder.Entity<WatchlistItem>().HasIndex(w => new { w.UserId, w.MovieId }).IsUnique();
            modelBuilder.Entity<SwipeHistory>().HasIndex(s => new { s.UserId, s.MovieId }).IsUnique();
            modelBuilder.Entity<Reminder>().HasIndex(r => new { r.UserId, r.TmdbId }).IsUnique();
            modelBuilder.Entity<Tag>().HasIndex(t => t.Name).IsUnique();

            modelBuilder.Entity<Genre>().Property(g => g.Name).HasMaxLength(200);
            modelBuilder.Entity<Genre>().HasIndex(g => g.Name).IsUnique();
            modelBuilder.Entity<Language>().Property(l => l.Name).HasMaxLength(200);
            modelBuilder.Entity<Language>().HasIndex(l => l.Name).IsUnique();
            modelBuilder.Entity<Country>().Property(c => c.Name).HasMaxLength(200);
            modelBuilder.Entity<Country>().HasIndex(c => c.Name).IsUnique();
            modelBuilder.Entity<AwardType>().Property(a => a.Name).HasMaxLength(200);
            modelBuilder.Entity<AwardType>().HasIndex(a => a.Name).IsUnique();
            modelBuilder.Entity<Studio>().Property(s => s.Name).HasMaxLength(200);
            modelBuilder.Entity<Studio>().HasIndex(s => new { s.Name, s.CountryId }).IsUnique();
            modelBuilder.Entity<UserList>().Property(l => l.Name).HasMaxLength(100);
            modelBuilder.Entity<UserList>().HasIndex(l => new { l.UserId, l.Name }).IsUnique();
            modelBuilder.Entity<UserListItem>().HasIndex(i => new { i.UserListId, i.MovieId }).IsUnique();
            modelBuilder.Entity<City>().Property(c => c.Name).HasMaxLength(200);
            modelBuilder.Entity<City>().HasIndex(c => new { c.Name, c.CountryId }).IsUnique();
            modelBuilder.Entity<QuizCategory>().Property(c => c.Name).HasMaxLength(200);
            modelBuilder.Entity<QuizCategory>().HasIndex(c => c.Name).IsUnique();
            modelBuilder.Entity<ThreadTag>().HasIndex(t => new { t.ThreadId, t.TagId }).IsUnique();
            modelBuilder.Entity<UserGenrePreference>().HasIndex(p => new { p.UserId, p.GenreId }).IsUnique();
            modelBuilder.Entity<QuizSession>().HasIndex(s => new { s.UserId, s.PlayedAt });
            modelBuilder.Entity<Report>()
                .HasOne(r => r.ReviewedByUser)
                .WithMany()
                .HasForeignKey(r => r.ReviewedByUserId);
            modelBuilder.Entity<CineAboutApp.Services.Database.Thread>()
                .HasOne(t => t.Movie)
                .WithMany(m => m.Threads)
                .HasForeignKey(t => t.MovieId);
            modelBuilder.Entity<MovieGenre>().HasIndex(g => new { g.MovieId, g.GenreId }).IsUnique();
            modelBuilder.Entity<MovieCast>().HasIndex(c => new { c.MovieId, c.PersonId }).IsUnique();

            modelBuilder.Entity<AppSettings>().HasKey(x => x.Id);

            

            modelBuilder.HasSequence<int>("SingleFileEntitySequence").StartsAt(1).IncrementsBy(1);

            

            modelBuilder.Entity<BlacklistedToken>()
                .HasIndex(t => t.Jti)
                .IsUnique();

            modelBuilder.Entity<BlacklistedToken>()
                .HasOne<MyAppUser>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.NoAction;
            }
        }
    }
}

