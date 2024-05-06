using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace Bc.Development.Containers
{
  public sealed class ContainerIdentity
  {
    public string Id { get; private set; }

    public string Name { get; private set; }

    public bool Resolved => !string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(Name);


    public static async Task<ContainerIdentity> FromIdAndName(string id, string name, bool withError = false)
    {
      var instance = new ContainerIdentity()
      {
        Id = id,
        Name = name
      };
      await instance.Resolve(withError);
      return instance;
    }

    public static async Task<ContainerIdentity> FromId(string id, bool withError = false)
    {
      return (await FromId(new[] { id }, withError)).Single();
    }

    public static async Task<ContainerIdentity[]> FromId(IEnumerable<string> ids, bool withError = false)
    {
      return await Task.WhenAll(ids.Select(async item =>
      {
        var instance = new ContainerIdentity()
        {
          Id = item
        };
        await instance.Resolve(withError);
        return instance;
      }));
    }

    public static async Task<ContainerIdentity> FromName(string name, bool withError = false)
    {
      return (await FromName(new[] { name }, withError)).Single();
    }

    public static async Task<ContainerIdentity[]> FromName(IEnumerable<string> names, bool withError = false)
    {
      return await Task.WhenAll(names.Select(async name =>
      {
        var instance = new ContainerIdentity()
        {
          Name = name
        };
        await instance.Resolve(withError);
        return instance;
      }));
    }


    private ContainerIdentity()
    {
    }


    private async Task Resolve(bool withError)
    {
      if (Resolved) return;

      if (string.IsNullOrEmpty(Id))
        Id = await GetContainerId(Name, withError);
      if (string.IsNullOrEmpty(Name))
        Name = await GetContainerName(Id, withError);
    }

    public async Task<ContainerState> GetState()
    {
      return (await Inspect()).State;
    }

    public async Task<ContainerInspectResponse> Inspect(bool withError = false)
    {
      try
      {
        AssertResolved();
        var cl = DockerClientFactory.GetClient();
        return await cl.Containers.InspectContainerAsync(Id);
      }
      catch
      {
        if (withError) throw;
        return null;
      }
    }

    private static async Task<string> GetContainerName(string containerId, bool withError = true)
    {
      try
      {
        var cl = DockerClientFactory.GetClient();
        var info = await cl.Containers.InspectContainerAsync(containerId);
        return info.Name.StartsWith("/") ? info.Name.Substring(1) : info.Name;
      }
      catch (Exception)
      {
        if (withError) throw;
        return null;
      }
    }

    private static async Task<string> GetContainerId(string containerName, bool withError = true)
    {
      var containerInfo = (await EnumerateContainers()).FirstOrDefault(c =>
      {
        var cname = c.Names.First();
        if (cname.StartsWith("/")) cname = cname.Substring(1);
        return cname.Equals(containerName, StringComparison.OrdinalIgnoreCase);
      });
      var id = containerInfo?.ID;
      if (withError && string.IsNullOrEmpty(id))
      {
        throw new Exception($"Container {containerName} not found");
      }

      return id;
    }

    public static async Task<ContainerListResponse[]> EnumerateContainers()
    {
      try
      {
        var cl = DockerClientFactory.GetClient();
        var containers = await cl.Containers.ListContainersAsync(new ContainersListParameters { All = true });
        return containers.Where(c => c.Labels.ContainsKey("nav")).ToArray();
      }
      catch
      {
        // ignore
      }

      return Array.Empty<ContainerListResponse>();
    }

    public void AssertResolved()
    {
      if (Resolved) return;
      throw new InvalidOperationException("The container ID or name was not found.");
    }


    public override string ToString()
    {
      return $"{Name} ({Id})";
    }
  }
}