using System;
using System.Collections.Generic;
using System.Net;
using Bc.Development.Configuration;
using Microsoft.Dynamics.Framework.UI.Client;
using Newtonsoft.Json;

namespace Bc.Development.TestRunner
{
  /// <summary>
  /// A test runner for running tests in Business Central.
  /// </summary>
  public class AlTestRunner : IDisposable
  {
    /// <summary>
    /// Specifies the ID of the test page to use.
    /// </summary>
    public int TestPageId { get; set; } = 130455;


    private readonly ClientSession _session;


    /// <summary>
    /// Creates a new instance.
    /// You must make sure that artifacts are available and loaded in <see cref="ClientSessionSettings"/> before running tests.
    /// </summary>
    /// <param name="serverUri">The URI to the server.</param>
    /// <param name="serverInstance">The name of the server instance.</param>
    /// <param name="credential">The credentials to use for connecting to the server.</param>
    /// <param name="settings">Additional settings for the client session.</param>
    public AlTestRunner(Uri serverUri, string serverInstance, NetworkCredential credential, ClientSessionSettings settings = null)
      : this(new Uri(serverUri.NormalizeBcServerUri(), serverInstance), credential, settings)
    {
    }

    /// <summary>
    /// Creates a new instance.
    /// You must make sure that artifacts are available and loaded in <see cref="ClientSessionSettings"/> before running tests.
    /// </summary>
    /// <param name="fullServiceUri">The full URI (including server instance) to the server.</param>
    /// <param name="credential">The credentials to use for connecting to the server.</param>
    /// <param name="settings">Additional settings for the client session.</param>
    public AlTestRunner(Uri fullServiceUri, NetworkCredential credential, ClientSessionSettings settings = null)
    {
      _session = ClientSessionFactory.CreateUserPassword(fullServiceUri, credential, settings);
    }

    /// <summary>
    /// Run all tests in the specified app.
    /// </summary>
    /// <param name="suiteName">The ID of the suite to use for running the tests.</param>
    /// <param name="appId">The ID of the app.</param>
    /// <returns>The test results.</returns>
    public CommandLineTestToolCodeunit[] RunTests(string suiteName, Guid appId)
    {
      var responses = new List<CommandLineTestToolCodeunit>();
      var page = _session.OpenForm(TestPageId);
      page.GetControlByName("CurrentSuiteName").SaveValue(suiteName);
      page.GetControlByName("ExtensionId").SaveValue($"{appId}");
      page.GetActionByName("ClearTestResults").Invoke();

      while (true)
      {
        page.GetActionByName("RunNextTest").Invoke();
        var response = page.GetControlByName("TestResultJson").StringValue;
        if (response.Equals("All tests executed.", StringComparison.OrdinalIgnoreCase)) break;
        responses.Add(JsonConvert.DeserializeObject<CommandLineTestToolCodeunit>(response));
      }

      return responses.ToArray();
    }

    public void Dispose()
    {
      ClientSessionFactory.Close(_session);
    }
  }
}