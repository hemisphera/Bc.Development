using System;
using System.Runtime.InteropServices;
using Docker.DotNet;

namespace Bc.Development.Containers
{
  public class DockerClientFactory
  {
    public static DockerClientFactory Default { get; set; } = new DockerClientFactory();

    public Uri HostUri { get; set; }


    public DockerClient GetClient(DockerClientConfiguration config = null)
    {
      if (config == null)
        config = new DockerClientConfiguration(GetDockerHostUrl());
      return config.CreateClient();
    }

    public Uri GetDockerHostUrl()
    {
      if (HostUri != null) return HostUri;

      var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
      var dockerHostUrl = isWindows ? new Uri("npipe://./pipe/docker_engine") : new Uri("unix:/var/run/docker.sock");

      var env = Environment.GetEnvironmentVariable("DOCKER_HOST");
      if (env != null)
      {
        dockerHostUrl = new Uri(env);
      }

      return dockerHostUrl;
    }
  }
}