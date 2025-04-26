namespace universal_print.Models
{
    // Simple class to serialize user details
    public class CachedUser
    {
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public string Avatar { get; set; }
    }
}