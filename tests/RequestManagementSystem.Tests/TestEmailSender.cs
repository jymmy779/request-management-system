using System.Collections.Concurrent;
using RequestManagementSystem.Api.Services;

namespace RequestManagementSystem.Tests;

public record SentEmail(
    string RecipientEmail,
    string Subject,
    string HtmlBody);

public class TestEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentEmail> _sentEmails = new();

    public IReadOnlyCollection<SentEmail> SentEmails =>
        _sentEmails.ToArray();

    public bool ThrowOnSend { get; set; }

    public Task SendAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (ThrowOnSend)
        {
            throw new InvalidOperationException(
                "Simulated email delivery failure.");
        }

        _sentEmails.Enqueue(new SentEmail(
            recipientEmail,
            subject,
            htmlBody));

        return Task.CompletedTask;
    }

    public void Clear()
    {
        _sentEmails.Clear();
        ThrowOnSend = false;
    }
}
