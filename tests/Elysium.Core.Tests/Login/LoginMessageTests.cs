using Elysium.Core.Login;
using Elysium.Core.Protocol;

namespace Elysium.Core.Tests.Login;

public sealed class LoginMessageTests
{
    [Fact]
    [Trait("CA", "0000-CA-01")]
    public void Login_request_round_trips()
    {
        var sent = new LoginRequest(ProtocolVersion.Current, "Ana");

        var received = ToFrame(sent).Decode<LoginRequest>();

        Assert.Equal(sent, received);
    }

    [Fact]
    [Trait("CA", "0000-CA-01")]
    public void Login_accepted_round_trips()
    {
        var sent = new LoginAccepted(new EntityId(7));

        Assert.Equal(sent, ToFrame(sent).Decode<LoginAccepted>());
    }

    [Theory]
    [Trait("CA", "0000-CA-02")]
    [InlineData(LoginRejectReason.VersionMismatch)]
    [InlineData(LoginRejectReason.InvalidName)]
    [InlineData(LoginRejectReason.NameTaken)]
    public void Login_rejected_round_trips(LoginRejectReason reason)
    {
        var sent = new LoginRejected(reason);

        Assert.Equal(sent, ToFrame(sent).Decode<LoginRejected>());
    }

    [Fact]
    [Trait("CA", "0000-CA-02")]
    public void The_protocol_version_is_readable_even_when_the_rest_is_unknown()
    {
        var writer = new MessageWriter();
        writer.WriteUInt16(99);
        writer.WriteInt32(12345);

        Assert.Equal(99, LoginRequest.ReadProtocolVersion(writer.WrittenSpan));
    }

    [Fact]
    [Trait("CA", "0000-CA-08")]
    public void An_unknown_reject_reason_throws_protocol_exception()
    {
        var frame = new Frame(LoginRejected.TypeId, new byte[] { 200 });

        Assert.Throws<ProtocolException>(() => frame.Decode<LoginRejected>());
    }

    [Fact]
    [Trait("CA", "0000-CA-08")]
    public void Decoding_a_frame_as_the_wrong_type_throws_protocol_exception()
    {
        var frame = ToFrame(new LoginAccepted(new EntityId(1)));

        Assert.Throws<ProtocolException>(() => frame.Decode<LoginRequest>());
    }

    private static Frame ToFrame(IMessage message)
    {
        var writer = new MessageWriter();
        message.Write(writer);
        return new Frame(message.MessageType, writer.WrittenSpan.ToArray());
    }
}
