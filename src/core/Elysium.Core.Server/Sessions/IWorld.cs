using Elysium.Core.Protocol;

namespace Elysium.Core.Server.Sessions;

/// <summary>
/// Where the gateway sends logged-in players and what they send. Called from network threads:
/// implementations only queue the call for their actor, and never block.
/// </summary>
public interface IWorld
{
    void PlayerJoined(EntityId id, string name);

    /// <summary>A message the player sent after the login. The world decodes it and may disconnect the player if it is invalid.</summary>
    void PlayerFrame(EntityId id, Frame frame);

    void PlayerLeft(EntityId id);
}
