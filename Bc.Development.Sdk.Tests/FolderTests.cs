using Bc.Development.Containers;

namespace Bc.Development.Sdk.Tests;

public class FolderTests
{
  private const string ContainerName = "bcserver";

  [Fact]
  public async Task ConvertToHost()
  {
    var shfolder = await FolderExtensions.GetSharedFolder(ContainerName);
    var folder = new DirectoryInfo(@"c:\run\my\yada");
    var a = await folder.ToHostFolder(ContainerName);
    Assert.StartsWith(shfolder.FullName, a.FullName, StringComparison.OrdinalIgnoreCase);

    var folder2 = new DirectoryInfo(@"c:\temp\run\my\yada");
    await Assert.ThrowsAsync<NotSupportedException>(async () => await folder2.TestShareableWithHost());
  }

  [Fact]
  public async Task ConvertToContainer()
  {
    var shfolder = await FolderExtensions.GetSharedFolder(ContainerName);
    var folder = new DirectoryInfo(Path.Combine(shfolder.FullName, "some", "path"));
    var a = await folder.ToContainerFolder(ContainerName);
    Assert.StartsWith(FolderExtensions.InContainerRoot, a.FullName, StringComparison.OrdinalIgnoreCase);

    var folder2 = new DirectoryInfo(@"c:\temp\run\my\yada");
    await Assert.ThrowsAsync<NotSupportedException>(async () => await folder2.TestShareableWithContainer(ContainerName));
  }
}