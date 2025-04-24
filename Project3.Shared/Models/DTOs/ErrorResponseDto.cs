namespace Project3.Shared.Models.DTOs
{
    // DTO for data transfer
    public class ErrorResponseDto // Does NOT inherit from Controller
    {
        public string Message { get; set; }
        public ErrorResponseDto(string message)
        {
            Message = message;
        }
        public ErrorResponseDto() { }
    }
}

