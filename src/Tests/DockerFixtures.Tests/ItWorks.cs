using System.Threading.Tasks;
using Xunit;

namespace DockerFixtures.Tests
{
    public class ContainerTests
    {
        [Theory]
        [InlineData("redis:latest")]
        [InlineData("nginx")]
        [InlineData("alpine:3.7")]
        public async Task Container_Can_StartAsync(string imageName)
        {
            var container = new Container(imageName);
            
            Assert.NotNull(container);
            
            Assert.Equal(imageName, container.ImageName);
            
            Assert.False(container.State.Running);

            await container.StartAsync();

            Assert.True(container.State.Running);
            
            await container.StopAsync();
            
            Assert.False(container.State.Running);
        }
    }
}