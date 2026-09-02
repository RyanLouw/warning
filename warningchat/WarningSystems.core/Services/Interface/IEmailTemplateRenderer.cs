namespace WarningSystems.core.Services.Interface;

public interface IEmailTemplateRenderer
{
    public string Render(
        string templateName,
        IReadOnlyDictionary<string, string?> values);
}