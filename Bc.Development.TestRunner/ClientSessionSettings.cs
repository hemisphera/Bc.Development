using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Bc.Development.Artifacts;

namespace Bc.Development.TestRunner
{
  public class ClientSessionSettings
  {
    public static readonly ClientSessionSettings Default = new ClientSessionSettings();

    public static string ClientArtifactsFolder { get; private set; }

    public static Task LoadArtifactsForClientSession(string folder)
    {
      InitializeAssemblyResolver();
      ClientArtifactsFolder = folder;
      return Task.CompletedTask;
    }

    public static async Task LoadArtifactsForClientSession(
      string versionPrefix,
      ArtifactType type,
      ArtifactStorageAccount account)
    {
      if (!String.IsNullOrEmpty(ClientArtifactsFolder)) return;
      var reader = new ArtifactReader(type, account);
      var artifact = await reader.GetLatest(versionPrefix, Defaults.PlatformIdentifier);
      var result = await ArtifactDownloader.Download(artifact, false);
      await LoadArtifactsForClientSession((await result.PlatformArtifact.GetLocalFolder()).FullName);
    }


    private static bool _initialized;

    public static void InitializeAssemblyResolver()
    {
      if (_initialized) return;

      AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs args)
      {
        var name = new AssemblyName(args.Name);
        switch (name.Name)
        {
          case "Microsoft.Dynamics.Framework.UI.Client":
          {
            var filename = Path.Combine(
              ClientArtifactsFolder,
              "Test Assemblies",
              "Microsoft.Dynamics.Framework.UI.Client.dll");
            return Assembly.LoadFile(filename);
          }
        }

        return null;
      };

      _initialized = true;
    }

    public CultureInfo Culture { get; set; } = new CultureInfo("en-US");

    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;
  }
}