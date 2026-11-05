using Microsoft.Extensions.Caching.Memory;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Web;
using Serilog;
using System.Diagnostics;
using WarningSystems.Core.DataAccess.GraphDataAccess.Context;
using WarningSystems.Core.Services;
using WarningSystems.Core.ViewModels;

namespace WarningSystems.Core.DataAccess.GraphDataAccess;

public class GraphUserDataAccess : IGraphUserDataAccess
{
    private readonly GraphServiceClient _graph;
    private readonly IMemoryCache _cache;

    public GraphUserDataAccess(
        IUserGraphClient userGraph,
        IMemoryCache cache)
    {
        _graph = userGraph.Client;
        _cache = cache;
    }

    public async Task<IReadOnlyList<string>> GetMatchedGroupIdsAsync(string userObjectId, IEnumerable<string> groupIdsToCheck)
    {
        var groupsToCheck = groupIdsToCheck
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (groupsToCheck.Length == 0)
        {
            return Array.Empty<string>();
        }

        var cacheKey = $"grpcheck:{userObjectId}:{string.Join("|", groupsToCheck)}";

        var body = new Microsoft.Graph.Users.Item.CheckMemberGroups.CheckMemberGroupsPostRequestBody
        {
            GroupIds = groupsToCheck.ToList()
        };

        var result = await _graph.Users[userObjectId].CheckMemberGroups.PostAsync(body);

        var matched = result?.Value ?? new List<string>();
        return matched;
    }

    public async Task<User?> GetMeAsync()
    {
        return await _graph.Me.GetAsync(cfg =>
        {
            cfg.QueryParameters.Select = new[]
            {
                "id",
                "displayName",
                "mail",
                "userPrincipalName",
                "jobTitle",
                "department"
            };
        });
    }

    public async Task SendEmailAsync(SendEmailRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var message = new Message
        {
            Subject = request.Subject,

            Body = new ItemBody
            {
                ContentType = BodyType.Html,
                Content = request.BodyHtml
            },

            ToRecipients = EmailRecipientNormalizer.Normalize(request.ToRecipients)
                .Select(email => new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = email.Trim()
                    }
                })
                .ToList(),

            Attachments = request.Attachments
                .Select(attachment => (Attachment)new FileAttachment
                {
                    OdataType = "#microsoft.graph.fileAttachment",
                    Name = attachment.FileName,
                    ContentType = string.IsNullOrWhiteSpace(attachment.ContentType)
                        ? "application/octet-stream"
                        : attachment.ContentType,
                    ContentBytes = attachment.ContentBytes
                })
                .ToList()
        };

        if (message.ToRecipients.Count == 0)
        {
            throw new InvalidOperationException(
                "No valid email recipients are configured.");
        }

        await SendEmailAsync(message, request.SaveToSentItems);
    }

    public async Task SendEmailAsync(Message message, bool saveToSentItems = true)
    {
        await _graph.Me.SendMail.PostAsync(new Microsoft.Graph.Me.SendMail.SendMailPostRequestBody
        {
            Message = message,
            SaveToSentItems = saveToSentItems
        });
    }
}
