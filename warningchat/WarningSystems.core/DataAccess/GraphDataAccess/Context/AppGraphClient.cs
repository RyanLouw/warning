using Microsoft.Graph;

namespace WarningSystems.Core.DataAccess.GraphDataAccess.Context;

public interface IAppGraphClient
{
    GraphServiceClient Client { get; }
}

public class AppGraphClient : IAppGraphClient
{
    public GraphServiceClient Client { get; }

    public AppGraphClient(GraphServiceClient client)
    {
        Client = client;
    }
}