using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Bc.Development.Util;
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


    private readonly AlTestRunnerSession _session;


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
      _session = AlTestRunnerSession.CreateUserPassword(fullServiceUri, credential, settings);
    }

    /// <summary>
    /// Run all tests in the specified app.
    /// </summary>
    /// <param name="suiteName">The ID of the suite to use for running the tests.</param>
    /// <param name="appId">The ID of the app.</param>
    /// <returns>The test results.</returns>
    public IEnumerable<CommandLineTestToolCodeunit> RunTests(string suiteName, Guid appId)
    {
      var page = _session.OpenForm(TestPageId);
      _session.SaveValue(page.GetControlByName("CurrentSuiteName"), suiteName);
      _session.SaveValue(page.GetControlByName("ExtensionId"), $"{appId}");
      _session.Invoke(page.GetActionByName("ClearTestResults"));

      return ExecuteTests(page);
    }

    /// <summary>
    /// Run a specific (or all tests) in the specified codeunit.
    /// </summary>
    /// <param name="suiteName">The ID of the suite to use for running the tests.</param>
    /// <param name="codeunitId">The ID of the test codeunit to run tests for.</param>
    /// <param name="methodName">If specified, runs only the given test method.</param>
    /// <returns>The test results.</returns>
    public IEnumerable<CommandLineTestToolCodeunit> RunTests(string suiteName, int codeunitId, string methodName = null)
    {
      var page = _session.OpenForm(TestPageId);
      _session.SaveValue(page.GetControlByName("CurrentSuiteName"), suiteName);
      _session.SaveValue(page.GetControlByName("TestCodeunitRangeFilter"), $"{codeunitId}");
      if (!String.IsNullOrEmpty(methodName))
      {
        _session.SaveValue(page.GetControlByName("TestProcedureRangeFilter"), methodName);
      }

      return ExecuteTests(page);
    }

    /// <summary>
    /// Run a specific (or all tests) in the specified codeunit.
    /// </summary>
    /// <param name="suiteName">The ID of the suite to use for running the tests.</param>
    /// <param name="playlist">Specifies a list of codeunits and methods to run.</param>
    /// <param name="groupByCodeunit">
    /// Specifies if the results should be aggregated per codeunit (true) or returned the same order they were specified
    /// in the playlist (false).
    /// </param>
    /// <returns>The test results.</returns>
    public IEnumerable<CommandLineTestToolCodeunit> RunTests(string suiteName, IEnumerable<TestPlaylistEntry> playlist, bool groupByCodeunit = false)
    {
      var results = new List<CommandLineTestToolCodeunit>();
      var page = _session.OpenForm(TestPageId);
      foreach (var entry in playlist)
      {
        _session.SaveValue(page.GetControlByName("CurrentSuiteName"), suiteName);
        _session.SaveValue(page.GetControlByName("TestCodeunitRangeFilter"), $"{entry.CodeunitId}");
        if (!String.IsNullOrEmpty(entry.MethodName))
        {
          _session.SaveValue(page.GetControlByName("TestProcedureRangeFilter"), entry.MethodName);
        }

        ExecuteTests(page, results, groupByCodeunit);
      }

      return results;
    }

    private void ExecuteTests(ClientLogicalControl page, ICollection<CommandLineTestToolCodeunit> results, bool groupByCodeunit)
    {
      var actualCodeunits = ExecuteTests(page);
      foreach (var actualCodeunit in actualCodeunits)
      {
        var foundCodeunit = groupByCodeunit
          ? results.FirstOrDefault(codeunit => codeunit.CodeunitId == actualCodeunit.CodeunitId)
          : null;
        if (foundCodeunit == null)
        {
          foundCodeunit = new CommandLineTestToolCodeunit();
          results.Add(foundCodeunit);
        }

        foreach (var method in actualCodeunit.Methods)
        {
          foundCodeunit.Methods.Add(method);
        }
      }
    }

    private IEnumerable<CommandLineTestToolCodeunit> ExecuteTests(ClientLogicalControl page)
    {
      var responses = new List<CommandLineTestToolCodeunit>();

      _session.Invoke(page.GetActionByName("ClearTestResults"));
      while (true)
      {
        _session.Invoke(page.GetActionByName("RunNextTest"));
        var response = page.GetControlByName("TestResultJson").StringValue;
        if (response.Equals("All tests executed.", StringComparison.OrdinalIgnoreCase)) break;
        responses.Add(JsonConvert.DeserializeObject<CommandLineTestToolCodeunit>(response));
      }

      return responses.ToArray();
    }

    /// <inheritdoc />
    public void Dispose()
    {
      _session.CloseSession();
    }
  }
}