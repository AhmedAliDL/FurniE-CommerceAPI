namespace App.Application.Responses
{
    public class ResponseResult<T>
    {
        public bool IsSuccess { get; set; }
        public T? Value { get; set; }
        public List<string> Errors { get; set; } = [];

        public static ResponseResult<T> Success(T value)
            => new() { IsSuccess = true, Value = value };

        public static ResponseResult<T> Failure(params string[] errors)
            => new() { IsSuccess = false, Errors = [.. errors] };

    }
}
