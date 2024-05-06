using System;
using System.Runtime.InteropServices;
using Docker.DotNet;

namespace Bc.Development.Containers
{
  internal static class DockerClientFactory
  {
    public static DockerClient GetClient()
    {
      return new DockerClientConfiguration(GetDockerHostUrl()).CreateClient();
    }

    public static Uri GetDockerHostUrl()
    {
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