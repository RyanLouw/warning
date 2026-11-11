using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
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
        return new(new UserGraphClient(new GraphServiceClient(adapter)));
    }

    private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json") };

    [Fact]
    public async Task GetMatchedGroupIdsAsync_ReturnsEmpty_WhenNoGroupsAreProvided()
    {
        var handler = new Handler(_ => throw new Xunit.Sdk.XunitException("Unexpected Graph request"));
        var result = await Create(handler).GetMatchedGroupIdsAsync("actor", []);
        Assert.Empty(result);
        Assert.Empty(handler.Calls);
    }

    [Fact]
    public async Task GetMatchedGroupIdsAsync_ReturnsGroupsMatchedByGraph()
    {
        var handler = new Handler(_ => Json(new { value = new[] { "group-one" } }));
        var result = await Create(handler).GetMatchedGroupIdsAsync("actor", ["group-one", "group-two"]);
        Assert.Equal(new[] { "group-one" }, result);
    }

    [Fact]
    public async Task GetMeAsync_ReturnsCurrentGraphUser()
    {
        var handler = new Handler(_ => Json(new { id = "actor", displayName = "Actor" }));
        var result = await Create(handler).GetMeAsync();
        Assert.NotNull(result);
        Assert.Equal("actor", result.Id);
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

}
