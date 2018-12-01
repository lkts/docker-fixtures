using Xunit;

namespace DockerFixtures.Tests
{
    public class ItWorks
    {
        [Fact]
        public void Container_Can_Create_New_Instance()
        {
            var container = new Container("name");
            
            Assert.NotNull(container);
            
            Assert.Equal("name", container.Name);
        }
    }
}