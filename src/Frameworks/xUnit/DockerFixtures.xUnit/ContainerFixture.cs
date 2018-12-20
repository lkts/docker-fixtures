using System.Threading.Tasks;
using Xunit;

namespace DockerFixtures.xUnit
{
    public abstract class ContainerFixture<TContainer> : IAsyncLifetime
        where TContainer: Container
    {
        public TContainer Container { get; }

        protected ContainerFixture(TContainer container)
        {
            Container = container;
        }
        
        public Task InitializeAsync() => Container.StartAsync();

        public Task DisposeAsync() => Container.StopAsync();
    }
}