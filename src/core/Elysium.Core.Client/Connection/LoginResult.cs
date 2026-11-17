using Elysium.Core.Login;

namespace Elysium.Core.Client.Connection;

/// <summary>
/// The server's answer to a login: either the player's own entity, or the reason for refusing it. Never both.
/// </summary>
public sealed record LoginResult
{
    private LoginResult(EntityId? entityId, LoginRejectReason? rejectReason)
    {
        EntityId = entityId;
        RejectReason = rejectReason;
    }

    /// <summary>The player's own entity, when the login was accepted.</summary>
    public EntityId? EntityId { get; }

    /// <summary>Why the server refused the login, when it did.</summary>
    public LoginRejectReason? RejectReason { get; }

    public bool IsAccepted => EntityId is not null;

    public static LoginResult Accepted(EntityId entityId) => new(entityId, null);

    public static LoginResult Rejected(LoginRejectReason reason) => new(null, reason);
}
