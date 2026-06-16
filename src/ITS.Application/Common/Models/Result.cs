namespace ITS.Application.Common.Models;

public class Result
{
    protected Result(bool succeeded, IEnumerable<string> errors)
    {
        Succeeded = succeeded;
        Errors = [.. errors];
    }

    public bool Succeeded { get; }
    public string[] Errors { get; }

    public static Result Success() => new(true, []);
    public static Result Failure(params string[] errors) => new(false, errors);
}

public class Result<T> : Result
{
    private Result(bool succeeded, T? value, IEnumerable<string> errors)
        : base(succeeded, errors)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(true, value, []);
    public new static Result<T> Failure(params string[] errors) => new(false, default, errors);
}
