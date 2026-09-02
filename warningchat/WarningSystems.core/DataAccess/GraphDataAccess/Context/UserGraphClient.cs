using Microsoft.Graph;

namespace WarningSystems.Core.DataAccess.GraphDataAccess.Context;

public interface IUserGraphClient
{
    GraphServiceClient Client { get; }
}

public sealed class UserGraphClient : IUserGraphClient
{
    public GraphServiceClient Client { get; }

    public UserGraphClient(GraphServiceClient client)
    {
        Client = client;
    }
}