using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.Responses
{
    public class UserListSummaryResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> PosterUrls { get; set; } = new List<string>();
    }

    public class UserListItemResponse
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PosterUrl { get; set; } = string.Empty;
        public int ReleaseYear { get; set; }
        public MovieType Type { get; set; }
        public double AverageRating { get; set; }
        public DateTime AddedAt { get; set; }
    }

    public class UserListDetailResponse : UserListSummaryResponse
    {
        public List<UserListItemResponse> Items { get; set; } = new List<UserListItemResponse>();
    }

    public class MovieSummaryResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int ReleaseYear { get; set; }
        public MovieType Type { get; set; }
        public string PosterUrl { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public string? RoleInMovie { get; set; }
    }
}
