using System;

namespace DockerFixtures.Exceptions
{
    public class ContainerException: Exception
    {
        public ContainerException(string message)
        : this(message, null)
        {
        }
        
        public ContainerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}