using Elysium.Core.Protocol;

namespace Elysium.Core.Tests.Protocol;

public sealed class FrameIoTests
{
    private const int Limit = 64;

    [Fact]
    public async Task Frames_round_trip_through_a_stream()
    {
        using var stream = new MemoryStream();
        stream.Write(FrameIo.Encode(5, [1, 2, 3]));
        stream.Write(FrameIo.Encode(6, []));
        stream.Position = 0;

        var first = await FrameIo.ReadAsync(stream, Limit, TestContext.Current.CancellationToken);
        var second = await FrameIo.ReadAsync(stream, Limit, TestContext.Current.CancellationToken);
        var end = await FrameIo.ReadAsync(stream, Limit, TestContext.Current.CancellationToken);

        Assert.NotNull(first);
        Assert.Equal(5, first.Value.Type);
        Assert.Equal([1, 2, 3], first.Value.Payload.ToArray());
        Assert.NotNull(second);
        Assert.Equal(6, second.Value.Type);
        Assert.True(second.Value.Payload.IsEmpty);
        Assert.Null(end);
    }

    [Fact]
    public void The_frame_header_bytes_are_fixed()
    {
        // Length of type + payload (2 bytes), then the type (2 bytes), little-endian. This header never changes
        // between protocol versions, so a client of any version can always reach the version check in the login.
        Assert.Equal([0x05, 0x00, 0x2A, 0x01, 0x01, 0x02, 0x03], FrameIo.Encode(0x012A, [1, 2, 3]));
    }

    [Fact]
    public async Task A_frame_larger_than_the_limit_throws_protocol_exception()
    {
        using var stream = new MemoryStream(FrameIo.Encode(5, new byte[Limit]));

        await Assert.ThrowsAsync<ProtocolException>(
            async () => await FrameIo.ReadAsync(stream, Limit, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_frame_too_short_to_hold_a_type_throws_protocol_exception()
    {
        using var stream = new MemoryStream([1, 0, 9]);

        await Assert.ThrowsAsync<ProtocolException>(
            async () => await FrameIo.ReadAsync(stream, Limit, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_frame_cut_off_in_the_middle_throws_protocol_exception()
    {
        var bytes = FrameIo.Encode(5, [1, 2, 3, 4]);
        using var stream = new MemoryStream(bytes[..^2]);

        await Assert.ThrowsAsync<ProtocolException>(
            async () => await FrameIo.ReadAsync(stream, Limit, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_stream_that_ends_inside_the_length_field_throws_protocol_exception()
    {
        using var stream = new MemoryStream([4]);

        await Assert.ThrowsAsync<ProtocolException>(
            async () => await FrameIo.ReadAsync(stream, Limit, TestContext.Current.CancellationToken));
    }
}
