namespace MobileSLI.Models;

public sealed class OperationResult
{
    public bool Success { get; init; }
    public bool AlreadySynchronized { get; init; }
    public string Message { get; init; } = string.Empty;
    public string TechnicalDetail { get; init; } = string.Empty;
    public DateTime DateResult { get; init; } = DateTime.Now;
    public int LignesEnvoyees { get; init; }

    public static OperationResult Ok(string message, int lignesEnvoyees) => new()
    {
        Success = true,
        Message = message,
        LignesEnvoyees = lignesEnvoyees,
        DateResult = DateTime.Now
    };

    public static OperationResult Fail(string message, string technicalDetail = "", bool alreadySynchronized = false) => new()
    {
        Success = false,
        AlreadySynchronized = alreadySynchronized,
        Message = message,
        TechnicalDetail = technicalDetail,
        DateResult = DateTime.Now
    };
}
