using System;
using System.Collections.Generic;
using System.Net;
using Bc.Development.Configuration;
using Microsoft.Dynamics.Framework.UI.Client;
using Newtonsoft.Json;

namespace Bc.Development.TestRunner
{
  public class AlTestRunner : IDisposable
  {
    private const int TestPageId = 130455;

    private readonly ClientSession _session;

    public AlTestRunner(Uri serverUri, string serverInstance, NetworkCredential credential, ClientSessionSettings settings = null)
      : this(new Uri(serverUri.NormalizeBcServerUri(), serverInstance), credential, settings)
    {
    }

    public AlTestRunner(Uri fullServiceUri, NetworkCredential credential, ClientSessionSettings settings = null)
    {
      _session = ClientSessionFactory.CreateUserPassword(fullServiceUri, credential, settings);
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

    public void Dispose()
    {
      ClientSessionFactory.Close(_session);
    }
  }
}