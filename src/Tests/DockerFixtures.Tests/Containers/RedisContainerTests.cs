using System.Threading.Tasks;
using DockerFixtures.xUnit.Redis;
using StackExchange.Redis;
using Xunit;

namespace DockerFixtures.Tests.Containers
{
    public class RedisContainerTests: IClassFixture<RedisContainerFixture>
    {
        private readonly IDatabase _database;
        public RedisContainerTests(RedisContainerFixture fixture) => _database = fixture.GetDatabase();

        [Theory]
        [InlineData("name", "DockerFixtures")]
        [InlineData("container", "Redis")]
        public async Task StringGetAsync(string key, string value)
        {
            var nonExistingKey = await _database.StringGetAsync(key);
            
            Assert.Equal(RedisValue.Null, nonExistingKey);
            
            await _database.StringSetAsync(key, value);

            var updatedKeyValue = await _database.StringGetAsync(key);

            Assert.Equal(value, updatedKeyValue);
        }
    }
}