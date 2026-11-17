using System.Numerics;
using Elysium.Core.Client.Input;
using SDL;

namespace Elysium.Core.Client.Sdl;

/// <summary>
/// The game window. Owns SDL itself: SDL starts when the window opens and shuts down when it closes,
/// so there is at most one window per process. Not thread-safe: use it from the thread that opened it.
/// </summary>
public sealed unsafe class SdlWindow : IDisposable
{
    private SDL_Window* _window;

    private SdlWindow(SDL_Window* window)
    {
        _window = window;
    }

    internal SDL_Window* Handle => _window;

    /// <summary>Starts SDL and opens a resizable window.</summary>
    public static SdlWindow Open(string title, int width, int height)
    {
        if (!SDL3.SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO))
        {
            throw SdlException.FromLastError(nameof(SDL3.SDL_Init));
        }

        var window = SDL3.SDL_CreateWindow(title, width, height, SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
        if (window is null)
        {
            var error = SdlException.FromLastError(nameof(SDL3.SDL_CreateWindow));
            SDL3.SDL_Quit();
            throw error;
        }

        return new SdlWindow(window);
    }

    /// <summary>
    /// Starts a new frame of <paramref name="input"/> with what happened since the last one: the clicks, a request
    /// to close, and the size of the drawing area. Click positions are in pixels of the drawing area, so they
    /// match the scene on screens that scale the window (high DPI). Does not allocate.
    /// </summary>
    public void PollEvents(FrameInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ObjectDisposedException.ThrowIf(_window is null, this);
        input.BeginFrame();
        var pixelsPerPoint = SDL3.SDL_GetWindowPixelDensity(_window);
        SDL_Event sdlEvent;
        while (SDL3.SDL_PollEvent(&sdlEvent))
        {
            switch ((SDL_EventType)sdlEvent.type)
            {
                case SDL_EventType.SDL_EVENT_QUIT:
                    input.RequestClose();
                    break;
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN when ToPointerButton(sdlEvent.button.button) is { } button:
                    input.AddClick(new Vector2(sdlEvent.button.x, sdlEvent.button.y) * pixelsPerPoint, button);
                    break;
                default:
                    break;
            }
        }

        int width;
        int height;
        if (SDL3.SDL_GetWindowSizeInPixels(_window, &width, &height))
        {
            input.SetViewport(width, height);
        }
    }

    private static PointerButton? ToPointerButton(byte sdlButton) => sdlButton switch
    {
        (byte)SDL3.SDL_BUTTON_LEFT => PointerButton.Primary,
        (byte)SDL3.SDL_BUTTON_RIGHT => PointerButton.Secondary,
        _ => null,
    };

    public void SetTitle(string title) => SDL3.SDL_SetWindowTitle(_window, title);

    public void Dispose()
    {
        if (_window is null)
        {
            return;
        }

        SDL3.SDL_DestroyWindow(_window);
        _window = null;
        SDL3.SDL_Quit();
    }
}
