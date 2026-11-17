using Elysium.Core.Server.Actors;

namespace Elysium.Core.Server.Tests.Actors;

public sealed class MailboxTests
{
    [Fact]
    public void Messages_come_out_in_the_order_they_were_posted()
    {
        var mailbox = new Mailbox<int>();
        mailbox.Post(1);
        mailbox.Post(2);
        mailbox.Post(3);

        var taken = new List<int>();
        while (mailbox.TryTake(out var message))
        {
            taken.Add(message);
        }

        Assert.Equal([1, 2, 3], taken);
    }

    [Fact]
    public async Task Many_threads_can_post_at_the_same_time_without_losing_messages()
    {
        var mailbox = new Mailbox<int>();

        await Task.WhenAll(Enumerable.Range(0, 8).Select(worker => Task.Run(
            () =>
            {
                for (var i = 0; i < 1000; i++)
                {
                    mailbox.Post((worker * 1000) + i);
                }
            },
            TestContext.Current.CancellationToken)));

        var taken = new HashSet<int>();
        while (mailbox.TryTake(out var message))
        {
            taken.Add(message);
        }

        Assert.Equal(8000, taken.Count);
    }
}
