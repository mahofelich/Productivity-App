using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;

namespace ProductivityHub.Services;

/// <summary>Lightweight display model for the message list.</summary>
public record EmailSummary(
    uint Uid,
    string From,
    string FromAddress,
    string Subject,
    DateTimeOffset Date,
    bool Seen,
    bool HasAttachments);

/// <summary>Full content for the reading pane.</summary>
public record EmailContent(
    uint Uid,
    string From,
    string To,
    string Subject,
    DateTimeOffset Date,
    string? TextBody,
    string? HtmlBody);

public interface IEmailService
{
    bool Configured { get; }
    /// <summary>Fetches headers for the most recent messages in the configured folder.</summary>
    Task<IReadOnlyList<EmailSummary>> GetRecentAsync(CancellationToken ct = default);
    /// <summary>Downloads the full body of one message.</summary>
    Task<EmailContent> GetMessageAsync(uint uid, CancellationToken ct = default);
}

/// <summary>
/// Read-only IMAP client. Opens the mailbox in ReadOnly mode, so browsing here
/// never marks messages as read, moves them, or changes anything on the server.
/// A fresh connection is made per operation — simple and stateless, which suits
/// a tab you glance at rather than a full mail client.
/// </summary>
public class EmailService : IEmailService
{
    public bool Configured => EmailConfig.Configured;

    private static async Task<(ImapClient Client, IMailFolder Folder)> OpenAsync(CancellationToken ct)
    {
        var client = new ImapClient { Timeout = 30_000 };
        try
        {
            var security = EmailConfig.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.SslOnConnect;

            await client.ConnectAsync(EmailConfig.Host, EmailConfig.Port, security, ct);
            await client.AuthenticateAsync(EmailConfig.Username, EmailConfig.Password, ct);

            var folder = EmailConfig.Folder.Equals("INBOX", StringComparison.OrdinalIgnoreCase)
                ? client.Inbox
                : await client.GetFolderAsync(EmailConfig.Folder, ct);

            await folder.OpenAsync(FolderAccess.ReadOnly, ct);
            return (client, folder);
        }
        catch (AuthenticationException)
        {
            client.Dispose();
            throw new Exception("The mail server rejected the login. For Gmail/iCloud/Fastmail you need an app password, not your normal account password — see EmailConfig.Secrets.cs.example.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            client.Dispose();
            throw new Exception($"Couldn't reach {EmailConfig.Host}:{EmailConfig.Port}. Check the host/port in EmailConfig.Secrets.cs and your network. ({ex.Message})");
        }
    }

    public async Task<IReadOnlyList<EmailSummary>> GetRecentAsync(CancellationToken ct = default)
    {
        if (!Configured)
            throw new InvalidOperationException("Email isn't configured yet. Copy Services/EmailConfig.Secrets.cs.example to EmailConfig.Secrets.cs and fill it in.");

        var (client, folder) = await OpenAsync(ct);
        using (client)
        {
            if (folder.Count == 0) return Array.Empty<EmailSummary>();

            int first = Math.Max(0, folder.Count - EmailConfig.FetchCount);
            var summaries = await folder.FetchAsync(first, -1,
                MessageSummaryItems.Envelope |
                MessageSummaryItems.UniqueId |
                MessageSummaryItems.Flags |
                MessageSummaryItems.BodyStructure, ct);

            var list = summaries
                .OrderByDescending(s => s.Date)
                .Select(s =>
                {
                    var sender = s.Envelope?.From?.Mailboxes?.FirstOrDefault();
                    return new EmailSummary(
                        Uid: s.UniqueId.Id,
                        From: string.IsNullOrWhiteSpace(sender?.Name) ? (sender?.Address ?? "(unknown)") : sender!.Name,
                        FromAddress: sender?.Address ?? "",
                        Subject: string.IsNullOrWhiteSpace(s.Envelope?.Subject) ? "(no subject)" : s.Envelope!.Subject!,
                        Date: s.Date,
                        Seen: s.Flags?.HasFlag(MessageFlags.Seen) ?? false,
                        HasAttachments: s.Attachments?.Any() ?? false);
                })
                .ToList();

            await client.DisconnectAsync(true, ct);
            return list;
        }
    }

    public async Task<EmailContent> GetMessageAsync(uint uid, CancellationToken ct = default)
    {
        if (!Configured)
            throw new InvalidOperationException("Email isn't configured yet.");

        var (client, folder) = await OpenAsync(ct);
        using (client)
        {
            var message = await folder.GetMessageAsync(new UniqueId(uid), ct);
            var content = new EmailContent(
                Uid: uid,
                From: message.From.ToString(),
                To: message.To.ToString(),
                Subject: string.IsNullOrWhiteSpace(message.Subject) ? "(no subject)" : message.Subject,
                Date: message.Date,
                TextBody: message.TextBody,
                HtmlBody: message.HtmlBody);

            await client.DisconnectAsync(true, ct);
            return content;
        }
    }
}
