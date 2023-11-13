using System;
using System.Collections.Generic;
using Microsoft.Dynamics.Framework.UI.Client;
using Newtonsoft.Json;

namespace Bc.Development.TestRunner
{
  public class TestRunner
  {
    private const int TestPageId = 130455;

    private readonly ClientSession _session;

    public TestRunner(ClientSession session)
    {
      _session = session;
    }

    public CommandLineTestToolResponse[] RunTests(string suiteName, Guid appId)
    {
      var responses = new List<CommandLineTestToolResponse>();
      var page = _session.OpenForm(TestPageId);
      page.GetControlByName("CurrentSuiteName").SaveValue(suiteName);
      page.GetControlByName("ExtensionId").SaveValue($"{appId}");
      page.GetActionByName("ClearTestResults").Invoke();

      while (true)
      {
        page.GetActionByName("RunNextTest").Invoke();
        var response = page.GetControlByName("TestResultJson").StringValue;
        if (response.Equals("All tests executed.", StringComparison.OrdinalIgnoreCase)) break;
        responses.Add(JsonConvert.DeserializeObject<CommandLineTestToolResponse>(response));
      }

      return responses.ToArray();
    }
  }
}