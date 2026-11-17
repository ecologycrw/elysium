using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using Elysium.Core.Client.Input;
using Elysium.Core.Client.Rendering;
using Elysium.Core.Client.Sdl;

// The Elysium Demo client. Slice 1, T08 checkpoint: draws a fixed scene (a floor of tiles and a few blocks)
// through the scene renderer, prints each click in pixels, and shows the graphics API and the frames per second
// in the title. T09 turns it into the real client. Close the window to quit.
// Usage: dotnet run --project samples/Elysium.Demo.Client

const string Title = "Elysium Demo";

using var window = SdlWindow.Open(Title, 1280, 720);
using var renderer = SdlGpuRenderer.Create(window);
Console.WriteLine($"Drawing with {renderer.DriverName}. Click the window to see the pixel; close it to quit.");

var input = new FrameInput();
var scene = new Scene
{
    Light = new DirectionalLight(new Vector3(-0.4f, -1, -0.6f), new ColorRgb(1, 0.95f, 0.85f), new ColorRgb(0.35f, 0.35f, 0.4f)),
    Background = new ColorRgb(0.12f, 0.14f, 0.18f),
};
for (var row = -5; row <= 5; row++)
{
    for (var column = -5; column <= 5; column++)
    {
        var shade = (row + column) % 2 == 0 ? 0.45f : 0.4f;
        scene.Add(new SceneBlock(new Vector3(column, -0.05f, row), new Vector3(0.96f, 0.1f, 0.96f), new ColorRgb(0.3f, shade, 0.3f)));
    }
}

scene.Add(new SceneBlock(new Vector3(2, 0.3f, -1), new Vector3(0.96f, 0.6f, 0.96f), new ColorRgb(0.45f, 0.45f, 0.5f)));
scene.Add(new SceneBlock(new Vector3(0, 0.7f, 0), new Vector3(0.5f, 1.4f, 0.5f), new ColorRgb(0.2f, 0.45f, 0.9f)));
scene.Add(new SceneBlock(new Vector3(-2, 0.7f, 1), new Vector3(0.5f, 1.4f, 0.5f), new ColorRgb(0.9f, 0.55f, 0.2f)));

var clock = Stopwatch.StartNew();
var frames = 0;
var lastTitleUpdate = TimeSpan.Zero;
while (true)
{
    window.PollEvents(input);
    if (input.CloseRequested)
    {
        break;
    }

    foreach (var click in input.Clicks)
    {
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{click.Button} click at ({click.Position.X:0}, {click.Position.Y:0})"));
    }

    var view = Matrix4x4.CreateLookAt(new Vector3(0, 9, 8), Vector3.Zero, Vector3.UnitY);
    var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4, (float)input.ViewportWidth / input.ViewportHeight, 0.1f, 100f);
    scene.ViewProjection = view * projection;
    renderer.Draw(scene);

    frames++;
    if (clock.Elapsed - lastTitleUpdate >= TimeSpan.FromSeconds(1))
    {
        var fps = frames / (clock.Elapsed - lastTitleUpdate).TotalSeconds;
        window.SetTitle(string.Create(CultureInfo.InvariantCulture, $"{Title} - {renderer.DriverName} - {fps:0} FPS"));
        frames = 0;
        lastTitleUpdate = clock.Elapsed;
    }
}
