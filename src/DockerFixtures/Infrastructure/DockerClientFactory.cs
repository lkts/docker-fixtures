using System;
using System.Runtime.InteropServices;
using Docker.DotNet;

namespace DockerFixtures.Infrastructure
{
    public static class DockerClientFactory
    {
        private static readonly Lazy<DockerClientConfiguration> _configuration
         = new Lazy<DockerClientConfiguration>(GetConfiguration);

        private static DockerClientConfiguration GetConfiguration()
        {
            var uri = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new Uri("npipe://./pipe/docker_engine")
                : new Uri("unix:///var/run/docker.sock");

            return new DockerClientConfiguration(uri);
        }

        public static DockerClient GetClient() => _configuration.Value.CreateClient();
    }
}