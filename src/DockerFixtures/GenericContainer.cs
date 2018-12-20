using DockerFixtures.Configuration;

namespace DockerFixtures
{
    public class GenericContainer: Container
    {
        public GenericContainer(string imageName)
            : base(new ContainerConfiguration().WithImage(imageName))
        {   
        }
    }
}