using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using DockerFixtures.Configuration;
using DockerFixtures.Exceptions;
using DockerFixtures.Infrastructure;
using Polly;

namespace DockerFixtures
{
    public abstract class Container
    {
        public ContainerConfiguration Configuration { get; }
        private readonly DockerClient _dockerClient;
        
        private readonly TimeSpan _startTimeout = TimeSpan.FromMinutes(1);
        
        private readonly ContainerState _unknownState = new ContainerState();
        
        private string _containerId;
        
        private ContainerInspectResponse _containerInspectResponse;

        public ContainerState State => _containerInspectResponse?.State ?? _unknownState;
        
        private readonly HostConfig  _hostConfig = new HostConfig
        {
            PublishAllPorts = true     
        };
        
        protected Container(ContainerConfiguration configuration)
        {
            Configuration = configuration;
            _dockerClient = DockerClientFactory.GetClient();
        }
        
        public async Task StartAsync()
        {
            _containerId = await Create();
            await TryStart();
        }

        private async Task TryStart()
        {
            var progress = new Progress<string>(m =>
            {
                Debug.WriteLine(m);
            });

            var started = await _dockerClient.Containers.StartContainerAsync(_containerId, new ContainerStartParameters());

            if(started)
            {
                await _dockerClient.Containers.GetContainerLogsAsync(_containerId, new ContainerLogsParameters
                {
                    ShowStderr = true,
                    ShowStdout = true,
                }, default(CancellationToken), progress: progress);
            }

            await WaitUntilContainerStarted();
        }
        
        protected virtual async Task WaitUntilContainerStarted()
        {
            var retryUntilContainerStateIsRunning = Policy
                .HandleResult<ContainerInspectResponse>(c => !c.State.Running)
                .RetryForeverAsync();
            
            var containerInspectPolicy = await Policy
                .TimeoutAsync(_startTimeout)
                .WrapAsync(retryUntilContainerStateIsRunning)
                .ExecuteAndCaptureAsync(async () => _containerInspectResponse = await _dockerClient.Containers.InspectContainerAsync(_containerId));

            if (containerInspectPolicy.Outcome == OutcomeType.Failure)
                throw new ContainerException("Container startup failed", containerInspectPolicy.FinalException);
        }

        private async Task<string> Create()
        {
            var progress = new Progress<JSONMessage>(async (m) =>
            {
                Console.WriteLine(m.Status);
                if (m.Error != null)
                    await Console.Error.WriteLineAsync(m.ErrorMessage);
            });
      
            var imagesCreateParameters = new ImagesCreateParameters
            {
                FromImage = Configuration.ImageName,
                Tag = Configuration.Tag,
            };
            
            await _dockerClient.Images.CreateImageAsync(
                imagesCreateParameters,
                new AuthConfig(),
                progress,
                CancellationToken.None);

            var createContainersParams = ApplyConfiguration();
            var containerCreated = await _dockerClient.Containers.CreateContainerAsync(createContainersParams);

            return containerCreated.ID;
        }

        private CreateContainerParameters ApplyConfiguration()
        {
            var cfg = new Config
            {
                Image = $"{Configuration.ImageName}:{Configuration.Tag}",
                Env = Configuration.EnvironmentVariables.Select(ev => $"{ev.key}={ev.value}").ToList(),
                ExposedPorts = Configuration.ExposedPorts.ToDictionary(e => $"{e}/tcp", e => default(EmptyStruct)),
                Tty = true,
                Cmd = Configuration.Commands,
                AttachStderr = true,
                AttachStdout= true,
            };
           
            return new CreateContainerParameters(cfg)
            {
                HostConfig = _hostConfig
            };
        }
        
        public async Task StopAsync()
        {
            if (string.IsNullOrWhiteSpace(_containerId)) 
                return;

            var stopped = await _dockerClient.Containers.StopContainerAsync(_containerInspectResponse.ID, new ContainerStopParameters());

            if (stopped)
            {
                _containerInspectResponse.State.Running = false;
            }

            await _dockerClient.Containers.RemoveContainerAsync(_containerInspectResponse.ID, new ContainerRemoveParameters());
        }
        
        protected string GetDockerHostIpAddress()
        {
            var dockerHostUri = _dockerClient.Configuration.EndpointBaseUri;

            switch (dockerHostUri.Scheme)
            {
                case "http":
                case "https":
                case "tcp":
                    return dockerHostUri.Host;
                case "npipe": //will have to revisit this for LCOW/WCOW
                case "unix":
                    return File.Exists("/.dockerenv") 
                        ? _containerInspectResponse.NetworkSettings.Gateway
                        : "localhost";
                default:
                    return null;
            }
        }

        protected string GetDockerHostExposedPort(int port)
        {
            if (!Configuration.ExposedPorts.Any())
                throw new Exception("Container does not expose any ports.");

            var ports =  _containerInspectResponse.NetworkSettings.Ports;

            var key = $"{port}/tcp";

            if (!ports.TryGetValue(key, out var hostPortMappings))
            {
                throw new Exception($"Container does not expose port {port}.");
            }
            
            return hostPortMappings.First().HostPort;
        }
    }
}