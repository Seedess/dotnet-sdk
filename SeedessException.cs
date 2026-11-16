namespace Seedess;

/// <summary>A non-2xx Seedess response.</summary>
public sealed class SeedessException : Exception
{
    public int Status { get; }
    public string ErrorCode { get; }

    public SeedessException(int status, string errorCode, string message) : base(message)
    {
        Status = status;
        ErrorCode = errorCode;
    }
}
