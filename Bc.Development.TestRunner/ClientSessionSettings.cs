using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Bc.Development.Artifacts;

namespace Bc.Development.TestRunner
{
  /// <summary>
  /// Settings and configuration for creating client sessions.
  /// </summary>
  public class ClientSessionSettings
  {
    /// <summary>
    /// The default settings.
    /// </summary>
    public static readonly ClientSessionSettings Default = new ClientSessionSettings();


    /// <summary>
    /// The path to the artifact binaries for the client session.
    /// This must be set before any new client session is created and then remains valid for the entire app domain
    /// lifetime.
    /// </summary>
    public static string ClientArtifactsFolder { get; private set; }


    /// <summary>
    /// Loads the client session artifacts from the specified folder.
    /// </summary>
    /// <param name="folder">The folder to load the artifacts from.</param>
    public static async Task LoadArtifactsForClientSession(string folder)
    {
      InitializeAssemblyResolver();
      ClientArtifactsFolder = folder;
      await Task.CompletedTask;
    }

    /// <summary>
    /// Loads the client session artifacts from an artifact with the given parameters.
    /// If the artifact is not available locally, it will be downloaded.
    /// </summary>
    /// <param name="versionPrefix">The version prefix that the artifact must match.</param>
    /// <param name="type">The type of artifact to load.</param>
    /// <param name="account">The account to use for downloading the artifact.</param>
    public static async Task LoadArtifactsForClientSession(
      string versionPrefix,
      ArtifactType type,
      ArtifactStorageAccount account)
    {
      if (!String.IsNullOrEmpty(ClientArtifactsFolder)) return;
      var reader = new ArtifactReader(type, account);
      var artifact = await reader.GetLatestLocalFirst(versionPrefix, Defaults.PlatformIdentifier);
      await LoadArtifactsForClientSession(artifact);
    }

    /// <summary>
    /// Loads the client session artifacts from the specified artifact.
    /// If the artifact is not available locally, it will be downloaded.
    /// </summary>
    /// <param name="artifact">The artifact to load.</param>
    public static async Task LoadArtifactsForClientSession(BcArtifact artifact)
    {
      var result = await ArtifactDownloader.Download(artifact.CreatePlatformArtifact(), false);
      await LoadArtifactsForClientSession((await result.PlatformArtifact.GetLocalFolder()).FullName);
    }


    private static bool _initialized;

    private static void InitializeAssemblyResolver()
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

    /// <summary>
    /// Specifies the culture to use for the client session.
    /// </summary>
    public CultureInfo Culture { get; set; } = new CultureInfo("en-US");

    /// <summary>
    /// Specifies the time zone to use for the client session.
    /// </summary>
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    /// <summary>
    /// Specifies the timeout for the client session.
    /// </summary>
    public TimeSpan ClientTimeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Specifies the timeout for the awaiting a session state after a command invocation.
    /// </summary>
    public TimeSpan AwaitStatusTimeout { get; set; } = TimeSpan.FromMinutes(1);
  }
}