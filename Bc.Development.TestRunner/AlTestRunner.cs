using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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

    /// <summary>
    /// The name of the suite to use.
    /// </summary>
    public string SuiteName { get; set; } = "DEFAULT";


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
    /// <param name="appId">The ID of the app.</param>
    /// <returns>The test results.</returns>
    public async Task<IEnumerable<CommandLineTestToolCodeunit>> RunTests(Guid appId)
    {
      var page = await _session.OpenForm(TestPageId);
      await _session.SaveValue(page.GetControlByName("CurrentSuiteName"), SuiteName);
      await _session.SaveValue(page.GetControlByName("ExtensionId"), $"{appId}");
      await _session.Invoke(page.GetActionByName("ClearTestResults"));

      return await ExecuteTests(page);
    }

    /// <summary>
    /// Run a specific (or all tests) in the specified codeunit.
    /// </summary>
    /// <param name="codeunitId">The ID of the test codeunit to run tests for.</param>
    /// <param name="methodName">If specified, runs only the given test method.</param>
    /// <returns>The test results.</returns>
    public async Task<IEnumerable<CommandLineTestToolCodeunit>> RunTests(int codeunitId, string methodName = null)
    {
      var page = await _session.OpenForm(TestPageId);
      await _session.SaveValue(page.GetControlByName("CurrentSuiteName"), SuiteName);
      await _session.SaveValue(page.GetControlByName("TestCodeunitRangeFilter"), $"{codeunitId}");
      if (!String.IsNullOrEmpty(methodName))
      {
        await _session.SaveValue(page.GetControlByName("TestProcedureRangeFilter"), methodName);
      }

      return await ExecuteTests(page);
    }

    /// <summary>
    /// Run a specific (or all tests) in the specified codeunit.
    /// </summary>
    /// <param name="playlist">Specifies a list of codeunits and methods to run.</param>
    /// <param name="groupByCodeunit">
    /// Specifies if the results should be aggregated per codeunit (true) or returned the same order they were specified
    /// in the playlist (false).
    /// </param>
    /// <returns>The test results.</returns>
    public async Task<IEnumerable<CommandLineTestToolCodeunit>> RunTests(IEnumerable<TestPlaylistEntry> playlist, bool groupByCodeunit = false)
    {
      var results = new List<CommandLineTestToolCodeunit>();
      var page = await _session.OpenForm(TestPageId);
      foreach (var entry in playlist)
      {
        await _session.SaveValue(page.GetControlByName("CurrentSuiteName"), SuiteName);
        await _session.SaveValue(page.GetControlByName("TestCodeunitRangeFilter"), $"{entry.CodeunitId}");
        if (!string.IsNullOrEmpty(entry.MethodName))
        {
          await _session.SaveValue(page.GetControlByName("TestProcedureRangeFilter"), entry.MethodName);
        }

        await ExecuteTests(page, results, groupByCodeunit);
      }

      return results;
    }

    /// <summary>
    /// Returns a list of tests on the server.
    /// </summary>
    /// <param name="codeunitFilter"></param>
    /// <returns></returns>
    public async Task<IEnumerable<ServerTestItem>> GetTests(string codeunitFilter = "")
    {
      var tests = new List<ServerTestItem>();

      var page = await _session.OpenForm(TestPageId);
      var suiteControl = page.GetControlByName("CurrentSuiteName");
      await _session.SaveValue(suiteControl, SuiteName);
      await _session.SaveValue(page.GetControlByName("TestCodeunitRangeFilter"), codeunitFilter);
      await _session.Invoke(page.GetActionByName("ClearTestResults"));

      var repeater = page.GetControlByType<ClientRepeaterControl>();
      await _session.SelectFirstRow(repeater);
      await _session.Refresh(repeater);
      page.ValidationResults.ThrowIfAny();

      var index = 0;
      var codeunits = new Dictionary<int, string>();
      while (true)
      {
        if (index >= repeater.Offset + repeater.DefaultViewport.Count)
          await _session.ScrollRepeater(repeater, 1);

        var rowIndex = index - repeater.Offset;
        index++;
        if (rowIndex >= repeater.DefaultViewport.Count) break;

        var row = repeater.DefaultViewport[(int)rowIndex];

        if (!int.TryParse(row.GetControlByName("LineType").StringValue, out var lineType))
          lineType = -1;
        if (lineType < 0) continue;

        var name = row.GetControlByName("Name").StringValue;
        var codeunitId = int.Parse(row.GetControlByName("TestCodeunit").StringValue);
        var run = row.GetControlByName("Run").StringValue.Equals("yes", StringComparison.OrdinalIgnoreCase);
        if (lineType == 0)
        {
          codeunits.Add(codeunitId, name);
          continue;
        }

        var item = new ServerTestItem
        {
          CodeunitId = codeunitId,
          Run = run,
          MethodName = name,
          CodeunitName = codeunits.TryGetValue(codeunitId, out var codeunitName) ? codeunitName : string.Empty
        };
        tests.Add(item);
      }

      return tests;
    }

    private async Task ExecuteTests(ClientLogicalControl page, ICollection<CommandLineTestToolCodeunit> results, bool groupByCodeunit)
    {
      var actualCodeunits = await ExecuteTests(page);
      foreach (var actualCodeunit in actualCodeunits)
      {
        var foundCodeunit = groupByCodeunit
          ? results.FirstOrDefault(codeunit => codeunit.CodeunitId == actualCodeunit.CodeunitId)
          : null;
        if (foundCodeunit == null)
        {
          foundCodeunit = new CommandLineTestToolCodeunit
          {
            CodeunitId = actualCodeunit.CodeunitId,
            Name = actualCodeunit.Name
          };
          results.Add(foundCodeunit);
        }

        foreach (var method in actualCodeunit.Methods)
        {
          foundCodeunit.Methods.Add(method);
          if (foundCodeunit.StartTime == default || method.StartTime < foundCodeunit.StartTime)
            foundCodeunit.StartTime = method.StartTime;
          if (foundCodeunit.FinishTime == default || method.FinishTime > foundCodeunit.FinishTime)
            foundCodeunit.FinishTime = method.FinishTime;
        }
      }
    }

    private async Task<IEnumerable<CommandLineTestToolCodeunit>> ExecuteTests(ClientLogicalControl page)
    {
      var responses = new List<CommandLineTestToolCodeunit>();

      await _session.Invoke(page.GetActionByName("ClearTestResults"));
      while (true)
      {
        await _session.Invoke(page.GetActionByName("RunNextTest"));
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