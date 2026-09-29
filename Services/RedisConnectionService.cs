using StackExchange.Redis;

namespace SKF_Product_Assistant.Services;

public sealed class RedisConnectionService
{
    private readonly Lazy<ConnectionMultiplexer> _connection;

    public RedisConnectionService()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")
            ?? "localhost:6379";

        _connection = new Lazy<ConnectionMultiplexer>(
            () => ConnectionMultiplexer.Connect(connectionString));
    }

    public IDatabase Database => _connection.Value.GetDatabase();
}