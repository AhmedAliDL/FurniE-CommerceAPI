namespace App.Application.Responses
{
    public class ResponseResult
    {
        public bool IsSuccess { get; set; }
        public List<string> Errors { get; set; } = [];

        public static ResponseResult Success()
            => new() { IsSuccess = true };

        public static ResponseResult Failure(params string[] errors)
            => new() { IsSuccess = false, Errors = [.. errors] };
    }
}
