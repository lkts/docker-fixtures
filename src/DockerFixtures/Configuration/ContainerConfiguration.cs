using System;

namespace DockerFixtures.Configuration
{
    public class ContainerConfiguration
    {
        /// <summary>
        /// Docker image name
        /// </summary>
        public string ImageName { get; private set; }
        public string Tag { get; private set; }
        
        /// <summary>
        /// Ports exposed by container
        /// </summary>
        public int[] ExposedPorts { get; private set;}

        public (string key, string value)[] EnvironmentVariables { get;private set; }
        
        public string[] Commands { get; private set; }

        public ContainerConfiguration()
        {
            ImageName = "";
            Tag = "";
            ExposedPorts = new int[0];
            EnvironmentVariables = new (string key, string value)[0];
            Commands = new string[0];
        }

        public ContainerConfiguration WithImage(string imageName)
        {
            if (string.IsNullOrWhiteSpace(imageName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(imageName));

            ImageName = imageName;
            Tag = "latest";
            
            var colonIndex = imageName.LastIndexOf(":", StringComparison.InvariantCultureIgnoreCase);
            
            if (colonIndex != -1)
            {
                ImageName = imageName.Substring(0, colonIndex);
                
                if (colonIndex != imageName.Length)
                    Tag = imageName.Substring(colonIndex + 1);
            }
            
            return this;
        }
        
        public ContainerConfiguration WithExposedPorts(params int[] ports)
        {
            ExposedPorts = ports;
            return this;
        }
    }
}