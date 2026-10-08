namespace LearningEdge.Application.Common.Results;

public enum ResultErrorKind
{
    None = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unexpected = 4,
    Forbidden = 5
}

public interface IAppResult<TSelf> where TSelf : IAppResult<TSelf>
{
    static abstract TSelf Validation(string error);
}

public class Result<T> : IAppResult<Result<T>>
{
    public bool Success { get; }
    public string Error { get; }
    public T? Data { get; }
    public ResultErrorKind Kind { get; }

    private Result(bool success, T? data, string error, ResultErrorKind kind)
    {
        Success = success;
        Data = data;
        Error = error;
        Kind = kind;
    }

    public static Result<T> Ok(T data) => new(true, data, string.Empty, ResultErrorKind.None);

    public static Result<T> Validation(string error) => new(false, default, error, ResultErrorKind.Validation);

    public static Result<T> NotFound(string error) => new(false, default, error, ResultErrorKind.NotFound);

    public static Result<T> Conflict(string error) => new(false, default, error, ResultErrorKind.Conflict);

    public static Result<T> Forbidden(string error) => new(false, default, error, ResultErrorKind.Forbidden);

    public static Result<T> Fail(string error) => new(false, default, error, ResultErrorKind.Unexpected);
}
