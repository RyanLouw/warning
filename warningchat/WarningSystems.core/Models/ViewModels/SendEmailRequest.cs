namespace WarningSystems.Core.ViewModels;

public class SendEmailRequest
{
    public string Subject { get; set; } = string.Empty;

    public string BodyHtml { get; set; } = string.Empty;

    public List<string> ToRecipients { get; set; } = new();

    public List<string>? CcRecipients { get; set; }

    public List<string>? BccRecipients { get; set; }

    public bool SaveToSentItems { get; set; } = true;
    public List<EmailAttachment> Attachments { get; set; } = [];
}
