using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Encodings.Web;
using WarningSystems.core.Services.Interface;
using WarningSystems.Core.Services.Interface;

namespace WarningSystems.Core.Services;

public sealed class EmbeddedEmailTemplateRenderer
    : IEmailTemplateRenderer
{
    private readonly Assembly _assembly;

    private readonly ConcurrentDictionary<string, string> _templateCache =
        new(StringComparer.OrdinalIgnoreCase);

    public EmbeddedEmailTemplateRenderer()
    {
        _assembly = typeof(EmbeddedEmailTemplateRenderer).Assembly;
    }

    public string Render(
        string templateName,
        IReadOnlyDictionary<string, string?> values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        ArgumentNullException.ThrowIfNull(values);

        var template = _templateCache.GetOrAdd(
            templateName,
            LoadTemplate);

        var renderedTemplate = template;

        foreach (var value in values)
        {
            var placeholder = $"{{{{{value.Key}}}}}";

            var encodedValue = HtmlEncoder.Default.Encode(
                value.Value ?? string.Empty);

            renderedTemplate = renderedTemplate.Replace(
                placeholder,
                encodedValue,
                StringComparison.Ordinal);
        }

        return renderedTemplate;
    }

    private string LoadTemplate(string templateName)
    {
        var expectedSuffix =
            $".EmailTemplates.{templateName}.html";

        var resourceName = _assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name =>
                name.EndsWith(
                    expectedSuffix,
                    StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            throw new InvalidOperationException(
                $"Email template '{templateName}' could not be found.");
        }

        using var stream =
            _assembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            throw new InvalidOperationException(
                $"Email template resource '{resourceName}' could not be opened.");
        }

        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}