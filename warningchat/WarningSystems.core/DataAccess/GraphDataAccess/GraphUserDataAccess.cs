using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Web;
using WarningSystems.Core.DataAccess.GraphDataAccess.Context;
using WarningSystems.Core.Services;
using WarningSystems.Core.ViewModels;

namespace WarningSystems.Core.DataAccess.GraphDataAccess;

public class GraphUserDataAccess : IGraphUserDataAccess
{
    private readonly GraphServiceClient _graph;
    private readonly ITokenAcquisition _tokenAcquisition;

    public GraphUserDataAccess(IUserGraphClient userGraph, ITokenAcquisition tokenAcquisition)
    {
        _graph = userGraph.Client;
        _tokenAcquisition = tokenAcquisition;
    }

    public async Task<IReadOnlyList<string>> GetMatchedGroupIdsAsync(string userObjectId, IEnumerable<string> groupIdsToCheck)
    {
        var groupsToCheck = groupIdsToCheck
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (groupsToCheck.Length == 0)
            return Array.Empty<string>();

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

    public async Task<List<User>> GetUsersUnderMeAsync()
    {
        var me = await GetMeAsync();
        if (me?.Id is null) return new List<User>();

        var users = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);
        var visitedManagers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await AddReportsRecursiveAsync(me.Id, users, visitedManagers);

        return users.Values
            .OrderBy(u => u.DisplayName)
            .ThenBy(u => u.UserPrincipalName)
            .ToList();
    }

    /// <summary>
    /// Returns the signed-in user's descendants together with the descendants
    /// of users who report to the same manager. Same-level users are excluded.
    /// If the signed-in user has no manager (for example, the CEO), all of
    /// their descendants are returned.
    /// </summary>
    public async Task<List<User>> GetUsersBelowMyLevelAsync()
    {
        var me = await GetMeAsync();
        if (string.IsNullOrWhiteSpace(me?.Id))
            return [];

        var users = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);
        var visitedManagers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Always include the user's own reporting branch.
        await AddReportsRecursiveAsync(me.Id, users, visitedManagers);

        var manager = await GetManagerAsync(me.Id);
        if (!string.IsNullOrWhiteSpace(manager?.Id))
        {
            var sameLevelUsers = await GetDirectReportsUsersAsync(manager.Id);

            foreach (var sameLevelUser in sameLevelUsers)
            {
                if (string.IsNullOrWhiteSpace(sameLevelUser.Id) ||
                    sameLevelUser.Id.Equals(me.Id, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Add only the peer's descendants, never the peer.
                await AddReportsRecursiveAsync(
                    sameLevelUser.Id,
                    users,
                    visitedManagers);
            }
        }

        users.Remove(me.Id);

        return users.Values
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.UserPrincipalName)
            .ToList();
    }

    public async Task<User?> GetManagerAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        try
        {
            var manager = await _graph.Users[userId].Manager.GetAsync(cfg =>
            {
                cfg.QueryParameters.Select =
                [
                    "id",
                    "displayName",
                    "mail",
                    "userPrincipalName",
                    "jobTitle",
                    "department"
                ];
            });

            return manager as User;
        }
        catch (Microsoft.Kiota.Abstractions.ApiException ex)
            when (ex.ResponseStatusCode == 404)
        {
            // A top-level employee, such as the CEO, has no manager.
            return null;
        }
    }

    private async Task AddReportsRecursiveAsync(
        string managerId,
        Dictionary<string, User> users,
        HashSet<string> visitedManagers)
    {
        if (!visitedManagers.Add(managerId))
            return;

        var directReports = await GetDirectReportsUsersAsync(managerId);

        foreach (var u in directReports)
        {
            if (u.Id is null) continue;

            users[u.Id] = u;

            await AddReportsRecursiveAsync(u.Id, users, visitedManagers);
        }
    }

    public async Task<List<User>> GetDirectReportsUsersAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return [];

        var response = await _graph.Users[userId].DirectReports.GetAsync(cfg =>
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

            cfg.QueryParameters.Top = 999;
        });

        var results = new List<User>();

        if (response?.Value is null)
            return results;

        var pageIterator =
            PageIterator<DirectoryObject, DirectoryObjectCollectionResponse>.CreatePageIterator(
                _graph,
                response,
                (obj) =>
                {
                    if (obj is User user && user.Id is not null)
                        results.Add(user);

                    return true;
                });

        await pageIterator.IterateAsync();

        return results;
    }

    public async Task<User?> GetUserAsync(string userIdOrUpn)
    {
        if (string.IsNullOrWhiteSpace(userIdOrUpn))
            return null;

        return await _graph.Users[userIdOrUpn].GetAsync(cfg =>
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
            throw new ArgumentNullException(nameof(request));

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
