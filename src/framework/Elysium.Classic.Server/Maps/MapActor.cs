using Elysium.Classic.Shared.Grid;
using Elysium.Classic.Shared.Messages;
using Elysium.Core;
using Elysium.Core.Protocol;
using Elysium.Core.Server.Actors;
using Elysium.Core.Server.Sessions;

namespace Elysium.Classic.Server.Maps;

/// <summary>
/// One grid map as an actor (ADR 0003): the network queues commands in its mailbox, and each tick applies them
/// in arrival order and then advances the <see cref="MapSimulation"/>. All state changes happen inside the tick.
/// A player that sends an invalid message is disconnected and taken off the map; the others keep playing.
/// </summary>
public sealed class MapActor : IWorld, ITickable
{
    private readonly Mailbox<MapCommand> _inbox = new();
    private readonly MapSimulation _simulation;
    private readonly IClientOutbox _outbox;

    public MapActor(GridMap map, MapRules rules, IClientOutbox outbox)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        _outbox = outbox;
        _simulation = new MapSimulation(map, rules, outbox);
    }

    public void PlayerJoined(EntityId id, string name) => _inbox.Post(new JoinCommand(id, name));

    public void PlayerFrame(EntityId id, Frame frame) => _inbox.Post(new FrameCommand(id, frame));

    public void PlayerLeft(EntityId id) => _inbox.Post(new LeaveCommand(id));

    public void Tick()
    {
        while (_inbox.TryTake(out var command))
        {
            Apply(command);
        }

        _simulation.Tick();
    }

    private void Apply(MapCommand command)
    {
        switch (command)
        {
            case JoinCommand join:
                _simulation.Join(join.Id, join.Name);
                break;
            case LeaveCommand leave:
                _simulation.Leave(leave.Id);
                break;
            case FrameCommand frame:
                ApplyFrame(frame.Id, frame.Frame);
                break;
            default:
                throw new InvalidOperationException($"Unknown map command {command}.");
        }
    }

    private void ApplyFrame(EntityId id, Frame frame)
    {
        try
        {
            switch (frame.Type)
            {
                case ClassicMessageTypes.MoveRequest:
                    _simulation.RequestMove(id, frame.Decode<MoveRequest>().Destination);
                    break;
                default:
                    throw new ProtocolException($"Message type {frame.Type} is not accepted by the map.");
            }
        }
        catch (ProtocolException exception)
        {
            _simulation.Leave(id);
            _outbox.Disconnect(id, exception.Message);
        }
    }

    private abstract record MapCommand;

    private sealed record JoinCommand(EntityId Id, string Name) : MapCommand;

    private sealed record LeaveCommand(EntityId Id) : MapCommand;

    private sealed record FrameCommand(EntityId Id, Frame Frame) : MapCommand;
}
