using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Moq;
using WarningSystems.Core.DataAccess.GraphDataAccess;
using WarningSystems.Core.DataAccess.GraphDataAccess.Context;
using WarningSystems.Core.ViewModels;

namespace WarningSystemTest.Unit;

[Trait("Category", "Unit")]
public class GraphTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public List<string> Calls { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(request.RequestUri!.PathAndQuery);
            return Task.FromResult(response(request));
        }
    }

    private static GraphUserDataAccess Create(Handler handler)
    {
        var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: new HttpClient(handler));
        return new(new UserGraphClient(new GraphServiceClient(adapter)), Mock.Of<ITokenAcquisition>());
    }

    private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json") };

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Empty_identifiers_do_not_call_Graph(string id)
    {
        var handler = new Handler(_ => throw new Xunit.Sdk.XunitException("Unexpected Graph request"));
        var graph = Create(handler);
        Assert.Null(await graph.GetUserAsync(id));
        Assert.Null(await graph.GetManagerAsync(id));
        Assert.Empty(await graph.GetDirectReportsUsersAsync(id));
        Assert.Empty(handler.Calls);
    }

    [Fact]
    public async Task Missing_manager_is_a_valid_404_response()
    {
        var handler = new Handler(_ => new HttpResponseMessage(HttpStatusCode.NotFound) {
            Content = new StringContent("{\"error\":{\"code\":\"Request_ResourceNotFound\",\"message\":\"No manager\"}}", Encoding.UTF8, "application/json")
        });
        Assert.Null(await Create(handler).GetManagerAsync("employee"));
    }

    [Fact]
    public async Task Directory_hierarchy_follows_pagination_ignores_non_users_and_stops_cycles()
    {
        var handler = new Handler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/v1.0/me") return Json(new { id = "manager", displayName = "Manager" });
            if (path == "/v1.0/users/manager/directReports") return Json(new Dictionary<string, object> {
                ["@odata.nextLink"] = "https://graph.microsoft.com/v1.0/next-page",
                ["value"] = new object[] { User("alice", "Alice"), new Dictionary<string, object> { ["@odata.type"] = "#microsoft.graph.group", ["id"] = "group" } }
            });
            if (path == "/v1.0/next-page") return Json(new { value = new[] { User("bob", "Bob") } });
            if (path == "/v1.0/users/alice/directReports") return Json(new { value = new[] { User("bob", "Bob") } });
            if (path == "/v1.0/users/bob/directReports") return Json(new { value = new[] { User("alice", "Alice") } });
            throw new Xunit.Sdk.XunitException($"Unexpected Graph route {path}");
        });
        var users = await Create(handler).GetUsersUnderMeAsync();
        Assert.Equal(new[] { "alice", "bob" }, users.Select(u => u.Id));
        Assert.Equal(1, handler.Calls.Count(c => c.StartsWith("/v1.0/users/alice/directReports")));
        Assert.Equal(1, handler.Calls.Count(c => c.StartsWith("/v1.0/users/bob/directReports")));
    }

    [Fact]
    public async Task Empty_current_user_has_no_reports()
    {
        var handler = new Handler(_ => Json(new { displayName = "No object id" }));
        var graph = Create(handler);
        Assert.Empty(await graph.GetUsersUnderMeAsync());
        Assert.Empty(await graph.GetUsersBelowMyLevelAsync());
    }

    [Fact]
    public async Task Send_mail_serializes_normalized_recipients_body_and_attachment()
    {
        JsonElement? payload = null;
        var handler = new Handler(request => {
            Assert.Equal("/v1.0/me/sendMail", request.RequestUri!.AbsolutePath);
            Assert.Equal(HttpMethod.Post, request.Method);
            using var json = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
            payload = json.RootElement.Clone();
            return new(HttpStatusCode.Accepted);
        });
        await Create(handler).SendEmailAsync(new SendEmailRequest {
            Subject = "Test message", BodyHtml = "<p>Body</p>", SaveToSentItems = false,
            ToRecipients = [" user@example.test ", "USER@example.test", "invalid email"],
            Attachments = [new() { FileName = "proof.bin", ContentBytes = [1, 2], ContentType = "" }]
        });
        Assert.NotNull(payload);
        var message = payload.Value.EnumerateObject().Single(p => p.Name.Equals("message", StringComparison.OrdinalIgnoreCase)).Value;
        Assert.Equal("Test message", message.GetProperty("subject").GetString());
        Assert.Equal("<p>Body</p>", message.GetProperty("body").GetProperty("content").GetString());
        Assert.Equal(1, message.GetProperty("toRecipients").GetArrayLength());
        Assert.Equal("application/octet-stream", message.GetProperty("attachments")[0].GetProperty("contentType").GetString());
        Assert.Equal("AQI=", message.GetProperty("attachments")[0].GetProperty("contentBytes").GetString());
        Assert.False(payload.Value.EnumerateObject().Single(p => p.Name.Equals("saveToSentItems", StringComparison.OrdinalIgnoreCase)).Value.GetBoolean());
    }

    [Fact]
    public async Task Invalid_mail_is_rejected_before_network_request()
    {
        var handler = new Handler(_ => throw new Xunit.Sdk.XunitException("Unexpected Graph request"));
        var graph = Create(handler);
        await Assert.ThrowsAsync<ArgumentNullException>(() => graph.SendEmailAsync((SendEmailRequest)null!));
        await Assert.ThrowsAsync<InvalidOperationException>(() => graph.SendEmailAsync(new SendEmailRequest { ToRecipients = ["not an email"] }));
        Assert.Empty(handler.Calls);
    }

    private static Dictionary<string, object> User(string id, string name) => new() {
        ["@odata.type"] = "#microsoft.graph.user", ["id"] = id, ["displayName"] = name
    };
}
