namespace MobileSLI.Models;

/// <summary>
/// Represents the result of an operation such as a synchronization or API call.
/// Contains success state, any relevant codes or messages, and contextual details.
/// </summary>
public sealed class OperationResult
{
    /// <summary>
    /// Indicates whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Indicates whether the operation had already been synchronized previously.
    /// </summary>
    public bool AlreadySynchronized { get; init; }

    /// <summary>
    /// Optional code describing the result or error.
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// User-facing message describing the outcome.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Technical details for debugging or logging purposes.
    /// </summary>
    public string TechnicalDetail { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp at which the result was produced.
    /// </summary>
    public DateTime DateResult { get; init; } = DateTime.Now;

    /// <summary>
    /// Number of lines (records) that were sent during the operation.
    /// </summary>
    public int LignesEnvoyees { get; init; }

    /// <summary>
    /// Factory method to create a successful result.
    /// </summary>
    /// <param name="message">Success message to include.</param>
    /// <param name="lignesEnvoyees">Number of lines sent.</param>
    /// <returns>A new successful <see cref="OperationResult"/>.</returns>
    public static OperationResult Ok(string message, int lignesEnvoyees) => new()
    {
        Success = true,
        Code = "SUCCESS",
        Message = message,
        LignesEnvoyees = lignesEnvoyees,
        DateResult = DateTime.Now
    };

    /// <summary>
    /// Factory method to create a failed result.
    /// </summary>
    /// <param name="message">Error message to include.</param>
    /// <param name="technicalDetail">Optional technical detail.</param>
    /// <param name="alreadySynchronized">Whether the operation failed because it was already synchronized.</param>
    /// <param name="code">Optional error code.</param>
    /// <returns>A new failed <see cref="OperationResult"/>.</returns>
    public static OperationResult Fail(
        string message,
        string technicalDetail = "",
        bool alreadySynchronized = false,
        string code = "") => new()
    {
        Success = false,
        AlreadySynchronized = alreadySynchronized,
        Code = code,
        Message = message,
        TechnicalDetail = technicalDetail,
        DateResult = DateTime.Now
    };
}
