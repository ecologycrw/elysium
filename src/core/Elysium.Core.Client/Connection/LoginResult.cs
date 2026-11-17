using Elysium.Core.Login;

namespace Elysium.Core.Client.Connection;

/// <summary>
/// The server's answer to a login: either the player's own entity, or the reason for refusing it.
/// </summary>
public sealed record LoginResult(EntityId? EntityId, LoginRejectReason? RejectReason)
{
    public bool IsAccepted => EntityId is not null;
}
