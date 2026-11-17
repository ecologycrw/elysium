using SDL;

namespace Elysium.Core.Client.Sdl;

/// <summary>An SDL call failed. The message carries SDL's own explanation.</summary>
public sealed class SdlException : Exception
{
    public SdlException()
    {
    }

    public SdlException(string message)
        : base(message)
    {
    }

    public SdlException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Builds the exception for a failed call, with the last error SDL reported.</summary>
    internal static SdlException FromLastError(string call) => new($"{call} failed: {SDL3.SDL_GetError()}");
}
