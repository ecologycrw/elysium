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

    /// <summary>Handles the pending window events. Returns false once the player asked to close the window.</summary>
    public bool PollEvents()
    {
        ObjectDisposedException.ThrowIf(_window is null, this);
        SDL_Event sdlEvent;
        var keepRunning = true;
        while (SDL3.SDL_PollEvent(&sdlEvent))
        {
            if ((SDL_EventType)sdlEvent.type == SDL_EventType.SDL_EVENT_QUIT)
            {
                keepRunning = false;
            }
        }

        return keepRunning;
    }

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
