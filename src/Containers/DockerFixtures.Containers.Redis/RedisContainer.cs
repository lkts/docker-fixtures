using System;
using System.Threading.Tasks;
using DockerFixtures.Configuration;
using Polly;
using StackExchange.Redis;

namespace DockerFixtures.Containers.Redis
{
    public class RedisContainer: Container
    {
        public const int Port = 6379;
        public string ConnectionString => $"{GetDockerHostIpAddress()}:{GetDockerHostExposedPort(Port)}";

        private RedisContainer(ContainerConfiguration configuration) 
            : base(configuration)
        {
        }
        
        public static RedisContainer Create(string imageName)
        {
            var configuration = new ContainerConfiguration()
                .WithImage(imageName)
                .WithExposedPorts(Port);
            
            return new RedisContainer(configuration);
        }

        protected override async Task WaitUntilContainerStarted()
        {
            await base.WaitUntilContainerStarted();

            var policyResult = await Policy
                .TimeoutAsync(TimeSpan.FromMinutes(2))
                .WrapAsync(Policy
                    .Handle<RedisConnectionException>()
                    .WaitAndRetryForeverAsync(
                        iteration => TimeSpan.FromSeconds(10),
                        (exception, timespan) => Console.WriteLine(exception.Message)))
                .ExecuteAndCaptureAsync(() =>
                    ConnectionMultiplexer.ConnectAsync(ConnectionString));

            if (policyResult.Outcome == OutcomeType.Failure)
                throw new Exception(policyResult.FinalException.Message);
        }
    }
}