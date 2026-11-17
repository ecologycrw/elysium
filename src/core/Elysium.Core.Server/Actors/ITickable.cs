namespace Elysium.Core.Server.Actors;

/// <summary>
/// An actor that runs one step of its simulation per tick. Ticks of one actor never run at the same time.
/// </summary>
public interface ITickable
{
    void Tick();
}
