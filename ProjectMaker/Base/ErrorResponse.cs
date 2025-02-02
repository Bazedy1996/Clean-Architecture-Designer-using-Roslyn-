namespace ProjectMaker.Base
{
    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public string ExceptionType { get; set; } = string.Empty;
        public string? StackTrace { get; set; } = string.Empty;
    }
}
