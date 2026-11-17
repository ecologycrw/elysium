using Elysium.Core.Login;
using Elysium.Core.Protocol;

namespace Elysium.Core.Tests.Login;

public sealed class LoginMessageTests
{
    [Fact]
    public void Login_request_round_trips()
    {
        var sent = new LoginRequest(ProtocolVersion.Current, "Ana");

        var received = Frame.FromMessage(sent).Decode<LoginRequest>();

        Assert.Equal(sent, received);
    }

    [Fact]
    public void Login_accepted_round_trips()
    {
        var sent = new LoginAccepted(new EntityId(7));

        Assert.Equal(sent, Frame.FromMessage(sent).Decode<LoginAccepted>());
    }

    [Theory]
    [InlineData(LoginRejectReason.VersionMismatch)]
    [InlineData(LoginRejectReason.InvalidName)]
    [InlineData(LoginRejectReason.NameTaken)]
    public void Login_rejected_round_trips(LoginRejectReason reason)
    {
        var sent = new LoginRejected(reason);

        Assert.Equal(sent, Frame.FromMessage(sent).Decode<LoginRejected>());
    }

    [Fact]
    public void The_protocol_version_is_readable_even_when_the_rest_is_unknown()
    {
        var writer = new MessageWriter();
        writer.WriteUInt16(99);
        writer.WriteInt32(12345);

        Assert.Equal(99, LoginRequest.ReadProtocolVersion(writer.WrittenSpan));
    }

    [Fact]
    public void An_unknown_reject_reason_throws_protocol_exception()
    {
        var frame = new Frame(LoginRejected.TypeId, new byte[] { 200 });

        Assert.Throws<ProtocolException>(() => frame.Decode<LoginRejected>());
    }

    [Fact]
    public void Decoding_a_frame_as_the_wrong_type_throws_protocol_exception()
    {
        var frame = Frame.FromMessage(new LoginAccepted(new EntityId(1)));

        Assert.Throws<ProtocolException>(() => frame.Decode<LoginRequest>());
    }

    [Fact]
    public void Core_message_type_numbers_never_change()
    {
        Assert.Equal(1, LoginRequest.TypeId);
        Assert.Equal(2, LoginAccepted.TypeId);
        Assert.Equal(3, LoginRejected.TypeId);
    }

    [Fact]
    public void Login_request_bytes_are_fixed_with_the_version_first()
    {
        Assert.Equal([0x01, 0x00, 0x03, (byte)'A', (byte)'n', (byte)'a'], PayloadOf(new LoginRequest(1, "Ana")));
    }

    [Fact]
    public void Login_accepted_bytes_are_fixed()
    {
        Assert.Equal([0x07, 0x00, 0x00, 0x00], PayloadOf(new LoginAccepted(new EntityId(7))));
    }

    [Theory]
    [InlineData(LoginRejectReason.VersionMismatch, 0x01)]
    [InlineData(LoginRejectReason.InvalidName, 0x02)]
    [InlineData(LoginRejectReason.NameTaken, 0x03)]
    public void Login_rejected_bytes_are_fixed(LoginRejectReason reason, byte expected)
    {
        Assert.Equal([expected], PayloadOf(new LoginRejected(reason)));
    }

    private static byte[] PayloadOf(IMessage message) => Frame.FromMessage(message).Payload.ToArray();
}
