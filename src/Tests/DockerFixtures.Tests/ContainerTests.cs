using System;
using System.Threading.Tasks;
using Docker.DotNet;
using Xunit;

namespace DockerFixtures.Tests
{
    public class ContainerTests
    {
        [Theory]
        [InlineData("redis:latest", "redis", "latest")]
        [InlineData("nginx", "nginx", "latest")]
        [InlineData("alpine:3.7", "alpine", "3.7")]
        [InlineData("emilevauge/whoami", "emilevauge/whoami", "latest")]
        public async Task Container_Can_StartAsync(string imageName, string expectedImageName, string expectedTag)
        {
            var container = new GenericContainer(imageName);
            
            Assert.NotNull(container);
            
            Assert.Equal(expectedImageName, container.Configuration.ImageName);
            Assert.Equal(expectedTag, container.Configuration.Tag);
            
            Assert.False(container.State.Running);

            await container.StartAsync();

            Assert.True(container.State.Running);
            
            await container.StopAsync();
            
            Assert.False(container.State.Running);
        }

        [Fact]
        public async Task Container_Can_Stop_If_Not_Started()
        {
            var container = new GenericContainer("redis");
            
            Assert.NotNull(container);
            
            Assert.False(container.State.Running);

            await container.StopAsync();
            
            Assert.False(container.State.Running);
        }
        
        [Fact]
        public async Task Container_Exception_When_Unable_ToStart()
        {
            var container = new GenericContainer("some-non-existing-image-name");

            await Assert.ThrowsAsync<DockerApiException>(async () => await container.StartAsync());
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Container_Throws_ArgumentException(string imageName)
        {
            Assert.Throws<ArgumentException>(() => new GenericContainer(imageName));
        }
    }
}