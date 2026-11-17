using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using Elysium.Core.Client.Sdl;

// The Elysium Demo client. Slice 1, T02 prototype: opens a window and draws a spinning cube with SDL3 GPU,
// showing the graphics API and the frames per second in the title. Close the window to quit.
// Usage: dotnet run --project samples/Elysium.Demo.Client

const string Title = "Elysium Demo";

using var window = SdlWindow.Open(Title, 1280, 720);
using var renderer = SdlGpuRenderer.Create(window);
Console.WriteLine($"Drawing with {renderer.DriverName}. Close the window to quit.");

var clock = Stopwatch.StartNew();
var frames = 0;
var lastTitleUpdate = TimeSpan.Zero;
while (window.PollEvents())
{
    var seconds = (float)clock.Elapsed.TotalSeconds;
    var model = Matrix4x4.CreateRotationY(seconds) * Matrix4x4.CreateRotationX(0.4f);
    var view = Matrix4x4.CreateLookAt(new Vector3(0, 1.5f, 3f), Vector3.Zero, Vector3.UnitY);
    var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4, 1280f / 720f, 0.1f, 100f);
    renderer.DrawCube(view * projection, model);

    frames++;
    if (clock.Elapsed - lastTitleUpdate >= TimeSpan.FromSeconds(1))
    {
        var fps = frames / (clock.Elapsed - lastTitleUpdate).TotalSeconds;
        window.SetTitle(string.Create(CultureInfo.InvariantCulture, $"{Title} - {renderer.DriverName} - {fps:0} FPS"));
        frames = 0;
        lastTitleUpdate = clock.Elapsed;
    }
}
