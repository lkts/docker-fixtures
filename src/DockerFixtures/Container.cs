namespace DockerFixtures
{
    public sealed class Container
    {
        public string Name { get; }

        public Container(string name)
        {
            Name = name;
        }
    }
}