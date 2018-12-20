using DockerFixtures.Containers.Redis;
using StackExchange.Redis;

namespace DockerFixtures.xUnit.Redis
{
    public class RedisContainerFixture : ContainerFixture<RedisContainer>
    {
        public RedisContainerFixture()
            : base(RedisContainer.Create("redis"))
        {
        }

        public IDatabase GetDatabase() => ConnectionMultiplexer.Connect(Container.ConnectionString).GetDatabase();
    }
}