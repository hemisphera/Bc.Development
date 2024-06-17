using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bc.Development.Configuration;
using Docker.DotNet.Models;
using Version = System.Version;

namespace Bc.Development.Containers
{
  public sealed class BcContainer
  {
    public static async Task<BcContainer[]> Enumerate()
    {
      var containers = await ContainerIdentity.EnumerateContainers();
      var bccs = await Task.WhenAll(containers.Select(async container => await TryGet(await ContainerIdentity.FromId(container.ID))));
      return bccs.Where(bcc => bcc != null).ToArray();
    }

    public static async Task<BcContainer> TryGet(ContainerIdentity id)
    {
      try
      {
        return await Get(id);
      }
      catch
      {
        return null;
      }
    }

    public static async Task<BcContainer> Get(ContainerIdentity id)
    {
      var instance = new BcContainer(id);
      await instance.Load();
      return instance;
    }


    public ContainerIdentity Identity { get; }

    public string Country { get; private set; }

    public Version Version { get; private set; }


    private BcContainer(ContainerIdentity identity)
    {
      Identity = identity;
      Identity.AssertResolved();
    }


    private async Task Load()
    {
      var ir = await Inspect();
      Country = ir.Config.Labels["country"];
      if (Version.TryParse(ir.Config.Labels["version"], out var ver))
        Version = ver;
    }


    private static IEnumerable<string> ParseCommand(string command)
    {
      var builder = new StringBuilder();
      bool quoted = false, escaped = false;
      foreach (char c in command)
      {
        if (c == '\\')
        {
          escaped = true;
        }
        else if (c == '"')
        {
          if (quoted && !escaped)
          {
            quoted = false;
          }
          else
          {
            quoted = true;
          }
        }
        else if (c == ' ' && !quoted)
        {
          yield return builder.ToString();
          builder.Clear();
          continue;
        }

        if (escaped && c != '\\')
        {
          escaped = false;
        }

        if (c != '"' || escaped)
        {
          builder.Append(c);
        }
      }

      if (quoted)
      {
        throw new ArgumentException("Unmatched quotes in string literal");
      }

      yield return builder.ToString();
    }


    public async Task<string> GetSharedFolderPath(params string[] subPaths)
    {
      var root = await GetSharedFolder();
      return Path.Combine(new[] { root.FullName }.Concat(subPaths).ToArray());
    }

    public async Task<DirectoryInfo> GetSharedFolder()
    {
      return await FolderExtensions.GetSharedFolder(Identity.Name);
    }

    public async Task<ContainerInspectResponse> Inspect()
    {
      try
      {
        var cl = DockerClientFactory.Default.GetClient();
        return await cl.Containers.InspectContainerAsync(Identity.Id);
      }
      catch
      {
        // ignore
      }

      return null;
    }


    public async Task<long> RunCommand(string command, CancellationToken cancellationToken = default)
    {
      return await RunCommand(command, Stream.Null, Stream.Null, cancellationToken);
    }

    public async Task<long> RunCommand(string command, Stream standardOutput = null, Stream standardError = null, CancellationToken cancellationToken = default)
    {
      var client = DockerClientFactory.Default.GetClient();
      var execCreateResponse = await client.Exec.ExecCreateContainerAsync(Identity.Id, new ContainerExecCreateParameters
      {
        AttachStderr = standardError != Stream.Null,
        AttachStdout = standardOutput != Stream.Null,
        Cmd = ParseCommand(command).ToArray(),
      }, cancellationToken);

      var attachResponse = await client.Exec.StartAndAttachContainerExecAsync(execCreateResponse.ID, false, cancellationToken);

      var streamOutputTask = Task.CompletedTask;
      if (standardOutput != Stream.Null || standardError != Stream.Null)
      {
        streamOutputTask = attachResponse.CopyOutputToAsync(Stream.Null, standardOutput, standardError, cancellationToken);
      }

      ContainerExecInspectResponse execInspectResponse;
      do
      {
        execInspectResponse = await client.Exec.InspectContainerExecAsync(execCreateResponse.ID, cancellationToken);
      } while (execInspectResponse.Running);

      await streamOutputTask;

      return execInspectResponse.ExitCode;
    }
  }
}