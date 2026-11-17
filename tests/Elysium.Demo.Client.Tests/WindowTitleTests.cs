namespace Elysium.Demo.Client.Tests;

public sealed class WindowTitleTests
{
    [Fact]
    [Trait("CA", "0001-CA-01")]
    public void The_title_shows_the_character_and_the_frames_per_second()
    {
        Assert.Equal("Elysium Demo - Ana - 60 FPS", WindowTitle.Format("Ana", 59.7, lostReason: null));
    }

    [Fact]
    [Trait("CA", "0001-CA-06")]
    public void When_the_connection_falls_the_title_says_so_and_why()
    {
        var title = WindowTitle.Format("Ana", 60, "the server closed the connection");

        Assert.Equal("Elysium Demo - Ana - 60 FPS - CONNECTION LOST: the server closed the connection", title);
    }
}
