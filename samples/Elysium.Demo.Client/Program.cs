using System.Diagnostics;
using Elysium.Classic.Client.Presentation;
using Elysium.Core.Client.Input;
using Elysium.Core.Client.Rendering;
using Elysium.Core.Client.Sdl;
using Elysium.Demo.Client;

// The Elysium Demo client: logs in with the given name, opens the window and lets the player click the ground to
// walk. The title shows the name, the frames per second and when the connection to the server fell.
// Usage: dotnet run --project samples/Elysium.Demo.Client -- --name Ana [--host 127.0.0.1] [--port 7000]
// Everything runs on the main thread, as the window requires; only the network works in the background.

if (!ClientArguments.TryParse(args, out var arguments, out var error))
{
    Console.Error.WriteLine(error);
    return 2;
}

// Waiting here keeps the program on the main thread, where the window must be opened and used.
var login = DemoLogin.LogInAsync(arguments!, CancellationToken.None).GetAwaiter().GetResult();
if (login.Link is null)
{
    Console.Error.WriteLine($"Could not log in as '{arguments!.Name}': {login.Failure}.");
    return 1;
}

var session = new ClientSession(login.Link, DemoLook.CreateWorld());
try
{
    using var window = SdlWindow.Open(WindowTitle.Format(arguments!.Name, 0, null), 1280, 720);
    using var renderer = SdlGpuRenderer.Create(window);
    Console.WriteLine($"Logged in as {arguments.Name}, drawing with {renderer.DriverName}. Close the window to quit.");
    RunFrames(window, renderer, session, arguments.Name);
}
finally
{
    session.DisposeAsync().AsTask().GetAwaiter().GetResult();
}

return 0;

// Runs frames until the player closes the window, updating the title once a second.
static void RunFrames(SdlWindow window, SdlGpuRenderer renderer, ClientSession session, string name)
{
    var input = new FrameInput();
    var scene = new Scene();
    var clock = Stopwatch.StartNew();
    var lastFrame = TimeSpan.Zero;
    var lastTitle = TimeSpan.Zero;
    var framesSinceTitle = 0;
    while (true)
    {
        window.PollEvents(input);
        if (input.CloseRequested)
        {
            return;
        }

        var now = clock.Elapsed;
        session.RunFrame(input, now - lastFrame, scene);
        renderer.Draw(scene);
        lastFrame = now;

        framesSinceTitle++;
        if (now - lastTitle >= TimeSpan.FromSeconds(1))
        {
            window.SetTitle(WindowTitle.Format(name, framesSinceTitle / (now - lastTitle).TotalSeconds, session.LostReason));
            framesSinceTitle = 0;
            lastTitle = now;
        }
    }
}
