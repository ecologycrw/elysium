using System.Collections.Concurrent;
using Elysium.Classic.Shared.Grid;
using Elysium.Classic.Shared.Messages;
using Elysium.Core;
using Elysium.Core.Protocol;
using Elysium.Core.Server.Actors;
using Elysium.Core.Server.Sessions;

namespace Elysium.Classic.Server.Maps;

/// <summary>
/// One grid map as an actor (ADR 0003): the network queues commands in its mailbox, and each tick applies them
/// in arrival order and then advances the <see cref="MapSimulation"/>. All map state changes happen inside the tick.
/// Work is bounded (<see cref="MapActorLimits"/>): a tick takes a limited number of commands, only the last move
/// request of each player in a tick is kept, and a player who floods the mailbox is disconnected.
/// A player that sends an invalid message is disconnected and taken off the map; the others keep playing.
/// </summary>
public sealed class MapActor : IWorld, ITickable
{
    private readonly Mailbox<MapCommand> _inbox = new();
    private readonly MapSimulation _simulation;
    private readonly MapActorLimits _limits;
    private readonly IClientOutbox _outbox;

    // Shared with the network threads only as lock-free counters, to refuse floods before they reach the mailbox.
    private readonly ConcurrentDictionary<EntityId, int> _pendingFrames = new();

    private readonly SortedDictionary<EntityId, TilePosition> _movesThisTick = [];

    public MapActor(GridMap map, MapRules rules, MapActorLimits limits, IClientOutbox outbox)
    {
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(outbox);
        _limits = limits;
        _outbox = outbox;
        _simulation = new MapSimulation(map, rules, outbox);
    }

    public void PlayerJoined(EntityId id, string name) => _inbox.Post(new JoinCommand(id, name));

    public void PlayerFrame(EntityId id, Frame frame)
    {
        var pending = _pendingFrames.AddOrUpdate(id, 1, (_, count) => count + 1);
        if (pending > _limits.MaxPendingFramesPerPlayer)
        {
            _outbox.Disconnect(id, "sent messages faster than the map can process them");
            return;
        }

        _inbox.Post(new FrameCommand(id, frame));
    }

    public void PlayerLeft(EntityId id) => _inbox.Post(new LeaveCommand(id));

    /// <summary>
    /// Applies up to <see cref="MapActorLimits.MaxCommandsPerTick"/> commands, then the last move request of each
    /// player (in entity order), then advances the simulation by one tick.
    /// </summary>
    public void Tick()
    {
        for (var taken = 0; taken < _limits.MaxCommandsPerTick && _inbox.TryTake(out var command); taken++)
        {
            Apply(command);
        }

        foreach (var (id, destination) in _movesThisTick)
        {
            _simulation.RequestMove(id, destination);
        }

        _movesThisTick.Clear();
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
                RemovePlayer(leave.Id);
                break;
            case FrameCommand frame:
                _pendingFrames.AddOrUpdate(frame.Id, 0, (_, count) => count - 1);
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
                    _movesThisTick[id] = frame.Decode<MoveRequest>().Destination;
                    break;
                default:
                    throw new ProtocolException($"Message type {frame.Type} is not accepted by the map.");
            }
        }
        catch (ProtocolException exception)
        {
            RemovePlayer(id);
            _outbox.Disconnect(id, exception.Message);
        }
    }

    private void RemovePlayer(EntityId id)
    {
        _movesThisTick.Remove(id);
        _pendingFrames.TryRemove(id, out _);
        _simulation.Leave(id);
    }

    private abstract record MapCommand;

    private sealed record JoinCommand(EntityId Id, string Name) : MapCommand;

    private sealed record LeaveCommand(EntityId Id) : MapCommand;

    private sealed record FrameCommand(EntityId Id, Frame Frame) : MapCommand;
}
