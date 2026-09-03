using System.Net.Mail;

namespace WarningSystems.Core.Services;

internal static class EmailRecipientNormalizer
{
    public static List<string> Normalize(IEnumerable<string>? addresses)
    {
        if (addresses is null)
        {
            return [];
        }

        var normalizedAddresses = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var address in addresses)
        {
            if (string.IsNullOrWhiteSpace(address) ||
                !MailAddress.TryCreate(address.Trim(), out var parsedAddress))
            {
                continue;
            }

            normalizedAddresses.Add(parsedAddress.Address);
        }

        return normalizedAddresses.ToList();
    }
}
