namespace Elysium.Core.Protocol;

/// <summary>
/// Thrown when bytes received from the other side do not form a valid message.
/// The server closes only the connection that sent them.
/// </summary>
public sealed class ProtocolException : Exception
{
    public ProtocolException()
    {
    }

    public ProtocolException(string message)
        : base(message)
    {
    }

    public ProtocolException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
