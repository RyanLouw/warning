using Microsoft.Extensions.Caching.Memory;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using System.Net;
using System.Text;
using System.Text.Json;

// use fake it easy
namespace WarningSystemTest.ServicesTest
{
    public class AppGroupLookupTests
    {
        private sealed class FakeGraphHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

            public int CallCount { get; private set; }

            public FakeGraphHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
            {
                _handler = handler;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CallCount++;
                return await _handler(request);
            }
        }

        private static GraphServiceClient CreateGraphClient(FakeGraphHandler handler)
        {
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://graph.microsoft.com/v1.0/")
            };

            // No auth needed because we never go out to the internet
            var authProvider = new AnonymousAuthenticationProvider();

            var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient)
            {
                BaseUrl = "https://graph.microsoft.com/v1.0"
            };

            return new GraphServiceClient(adapter);
        }

        private static IMemoryCache CreateCache() => new MemoryCache(new MemoryCacheOptions());

        private static HttpResponseMessage JsonOk(object obj)
        {
            var json = JsonSerializer.Serialize(obj);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        [Fact]
        public async Task GetMatchedGroupIdsAsync_ReturnsEmpty_WhenNoGroupsProvided()
        {
            var handler = new FakeGraphHandler(_ =>
                Task.FromResult(JsonOk(new { value = new[] { "should-not-be-used" } })));

            var graph = CreateGraphClient(handler);
            var cache = CreateCache();
            var sut = new AppGroupLookup(graph, cache);

            var result = await sut.GetMatchedGroupIdsAsync("user-1", Array.Empty<string>());

            Assert.Empty(result);
            Assert.Equal(0, handler.CallCount);
        }

        [Fact]
        public async Task GetMatchedGroupIdsAsync_CallsGraph_AndReturnsMatchedIds()
        {
            var handler = new FakeGraphHandler(async req =>
            {
                Assert.Equal(HttpMethod.Post, req.Method);
                Assert.Contains("/users/user-1/checkMemberGroups", req.RequestUri!.AbsoluteUri, StringComparison.OrdinalIgnoreCase);

                var body = await req.Content!.ReadAsStringAsync();
                Assert.Contains("groupIds", body, StringComparison.OrdinalIgnoreCase);

                return JsonOk(new { value = new[] { "gid-1", "gid-3" } });
            });

            var graph = CreateGraphClient(handler);
            var cache = CreateCache();
            var sut = new AppGroupLookup(graph, cache);

            var result = await sut.GetMatchedGroupIdsAsync("user-1", new[] { "gid-1", "gid-2", "gid-3" });

            Assert.Equal(new[] { "gid-1", "gid-3" }, result);
            Assert.Equal(1, handler.CallCount);
        }

        [Fact]
        public async Task GetMatchedGroupIdsAsync_UsesCache_OnSecondCall_SameUserSameGroups()
        {
            var handler = new FakeGraphHandler(_ =>
                Task.FromResult(JsonOk(new { value = new[] { "gid-9" } })));

            var graph = CreateGraphClient(handler);
            var cache = CreateCache();
            var sut = new AppGroupLookup(graph, cache);

            var groups = new[] { "gid-9", "gid-10" };

            var first = await sut.GetMatchedGroupIdsAsync("user-1", groups);
            var second = await sut.GetMatchedGroupIdsAsync("user-1", groups);

            Assert.Equal(new[] { "gid-9" }, first);
            Assert.Equal(new[] { "gid-9" }, second);
            Assert.Equal(1, handler.CallCount);
        }

        [Fact]
        public async Task GetMatchedGroupIdsAsync_DeduplicatesAndIgnoresBlankIds()
        {
            var handler = new FakeGraphHandler(async req =>
            {
                var body = await req.Content!.ReadAsStringAsync();
                Assert.DoesNotContain("\"\"", body);

                return JsonOk(new { value = new[] { "gid-a" } });
            });

            var graph = CreateGraphClient(handler);
            var cache = CreateCache();
            var sut = new AppGroupLookup(graph, cache);

            var result = await sut.GetMatchedGroupIdsAsync("user-1",
                new[] { "gid-a", "GID-A", "", "   ", null, "gid-b", "gid-b" });

            Assert.Equal(new[] { "gid-a" }, result);
            Assert.Equal(1, handler.CallCount);
        }
    }
}