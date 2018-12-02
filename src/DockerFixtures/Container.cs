using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using DockerFixtures.Infrastructure;
using Polly;

namespace DockerFixtures
{
    public class Container
    {
        private readonly DockerClient _dockerClient;
        
        private readonly TimeSpan _startTimeout = TimeSpan.FromMinutes(1);
        
        private readonly ContainerState _unknownState = new ContainerState();
        
        private ContainerInspectResponse _containerInspectResponse;

        public ContainerState State => _containerInspectResponse?.State ?? _unknownState;
        
        private readonly HostConfig  _hostConfig = new HostConfig
        {
            PublishAllPorts = true     
        };

        private string ContainerId { get; set; }
        
        /// <summary>
        /// Docker image name
        /// </summary>
        public string ImageName { get; set; }
        
        /// <summary>
        /// Ports exposed by container
        /// </summary>
        public int[] ExposedPorts { get; set; }
        public (string key, string value)[] EnvironmentVariables { get; set; }
        
        public string[] Commands { get; set; }
        
        public Container() =>
            _dockerClient = DockerClientFactory.GetClient();

        public Container(string imageName)
            : this()
        {
            if (string.IsNullOrWhiteSpace(imageName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(imageName));
            
            ImageName = imageName;
        }
        
        public async Task StartAsync()
        {
            ContainerId = await Create();
            await TryStart();
        }

        private async Task TryStart()
        {
            var progress = new Progress<string>(m =>
            {
                Debug.WriteLine(m);

                // Debug.WriteLineIf(m.Error != null, m.ErrorMessage);
            });

            var started = await _dockerClient.Containers.StartContainerAsync(ContainerId, new ContainerStartParameters());

            if(started)
            {
                await _dockerClient.Containers.GetContainerLogsAsync(ContainerId, new ContainerLogsParameters
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
                .ExecuteAndCaptureAsync(async () => _containerInspectResponse = await _dockerClient.Containers.InspectContainerAsync(ContainerId));

            if (containerInspectPolicy.Outcome == OutcomeType.Failure)
                throw new Exception("Container startup failed", containerInspectPolicy.FinalException);
        }

        private async Task<string> Create()
        {
            var progress = new Progress<JSONMessage>(async (m) =>
            {
                Console.WriteLine(m.Status);
                if (m.Error != null)
                    await Console.Error.WriteLineAsync(m.ErrorMessage);
            });

            var tag = ImageName.Contains(":")
                ? ImageName.Split(new[] {":"}, StringSplitOptions.RemoveEmptyEntries).ToArray().Last()
                : "latest";
            
            var imagesCreateParameters = new ImagesCreateParameters
            {
                FromImage = ImageName,
                Tag = tag,
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
            var exposedPorts = ExposedPorts?.ToList() ?? new List<int>();

            var cfg = new Config
            {
                Image = ImageName,
                Env = EnvironmentVariables?.Select(ev => $"{ev.key}={ev.value}").ToList(),
                ExposedPorts = exposedPorts.ToDictionary(e => $"{e}/tcp", e => default(EmptyStruct)),
                Tty = true,
                Cmd = Commands,
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
            if (string.IsNullOrWhiteSpace(ContainerId)) 
                return;

            var stopped = await _dockerClient.Containers.StopContainerAsync(_containerInspectResponse.ID, new ContainerStopParameters());

            if (stopped)
            {
                _containerInspectResponse.State.Running = false;
            }

            await _dockerClient.Containers.RemoveContainerAsync(_containerInspectResponse.ID, new ContainerRemoveParameters());
        }
    }
}