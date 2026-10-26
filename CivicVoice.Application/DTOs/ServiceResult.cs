namespace CivicVoice.Application.DTOs;

/// <summary>
/// Generic result type — demonstrates C# generics for communicating success/failure
/// from the service layer without throwing exceptions for expected validation failures.
/// </summary>
/// <typeparam name="T">The data type on success</typeparam>
public class ServiceResult<T>
{
    public bool Success { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }
    public List<string> ValidationErrors { get; private set; } = new();

    public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };

    public static ServiceResult<T> Fail(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };

    public static ServiceResult<T> ValidationFail(params string[] errors) =>
        new() { Success = false, ValidationErrors = errors.ToList(), ErrorMessage = errors.FirstOrDefault() };
}
