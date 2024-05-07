using System;
using System.IO;
using System.Threading.Tasks;
using Bc.Development.Configuration;

namespace Bc.Development.Containers
{
  public static class FolderExtensions
  {
    public const string InContainerRoot = @"c:\run\my";


    public static async Task<DirectoryInfo> ToHostFolder(this DirectoryInfo folderPath, string containerName)
    {
      await folderPath.TestShareableWithHost();
      var sharedFolderRoot = await GetSharedFolder(containerName);
      var folder = folderPath.FullName.Substring(InContainerRoot.Length + 1);
      return new DirectoryInfo(Path.Combine(sharedFolderRoot.FullName, folder));
    }

    public static async Task<bool> IsShareableWithHost(this DirectoryInfo folder)
    {
      var result = await Task.FromResult(folder.FullName.StartsWith(InContainerRoot, StringComparison.OrdinalIgnoreCase));
      return result;
    }

    public static async Task TestShareableWithHost(this DirectoryInfo folder)
    {
      if (await folder.IsShareableWithHost()) return;
      throw new NotSupportedException($"The folder '{folder.FullName}' is not shareable with the host.");
    }

    public static async Task<DirectoryInfo> ToContainerFolder(this DirectoryInfo folderPath, string containerName)
    {
      await folderPath.TestShareableWithContainer(containerName);
      var sharedFolderRoot = await GetSharedFolder(containerName);
      var folder = folderPath.FullName.Substring(sharedFolderRoot.FullName.Length + 1);
      return new DirectoryInfo(Path.Combine(InContainerRoot, folder));
    }

    public static async Task<bool> IsShareableWithContainer(this DirectoryInfo folder, string containerName)
    {
      var sharedFolderRoot = await GetSharedFolder(containerName);
      return folder.FullName.StartsWith(sharedFolderRoot.FullName, StringComparison.OrdinalIgnoreCase);
    }

    public static async Task TestShareableWithContainer(this DirectoryInfo folder, string containerName)
    {
      if (await folder.IsShareableWithContainer(containerName)) return;
      throw new NotSupportedException($"The folder '{folder.FullName}' is not shareable with container '{containerName}'.");
    }

    public static async Task<DirectoryInfo> GetSharedFolder(string containerName)
    {
      var config = await BcContainerHelperConfiguration.Load();
      return new DirectoryInfo(
        Path.Combine(
          config.HostHelperFolder,
          "Extensions",
          containerName,
          "my"));
    }
  }
}