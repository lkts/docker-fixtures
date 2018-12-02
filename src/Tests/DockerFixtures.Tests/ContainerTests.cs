using System;
using System.Threading.Tasks;
using Docker.DotNet;
using DockerFixtures.Exceptions;
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

        [Fact]
        public async Task Container_Can_Stop_If_Not_Started()
        {
            var container = new Container("redis");
            
            Assert.NotNull(container);
            
            Assert.False(container.State.Running);

            await container.StopAsync();
            
            Assert.False(container.State.Running);
        }
        
        [Fact]
        public async Task Container_Exception_When_Unable_ToStart()
        {
            var container = new Container("some-non-existing-image-name");

            await Assert.ThrowsAsync<DockerApiException>(async () => await container.StartAsync());
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Container_Throws_ArgumentException(string imageName)
        {
            Assert.Throws<ArgumentException>(() => new Container(imageName));
        }
    }
}