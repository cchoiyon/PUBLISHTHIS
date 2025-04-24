namespace Project3.Shared.Models.DTOs
{
    // DTO for login response data
    public class LoginResponseDto
    {
        public bool IsAuthenticated { get; set; } = false;
        public bool IsVerified { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }
}

