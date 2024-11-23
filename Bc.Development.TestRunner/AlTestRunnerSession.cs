using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Dynamics.Framework.UI.Client;
using Microsoft.Dynamics.Framework.UI.Client.Interactions;

namespace Bc.Development.TestRunner
{
  internal class AlTestRunnerSession : IDisposable
  {
    public static AlTestRunnerSession CreateUserPassword(
      Uri serverUri, NetworkCredential credential,
      ClientSessionSettings settings = null)
    {
      var provider = ServiceAddressProvider.ServiceAddress(serverUri);
      var client = new JsonHttpClient(provider, credential, AuthenticationScheme.UserNamePassword);
      var actualSettings = settings ?? ClientSessionSettings.Default;
      ConfigureUnderlyingHttpClient(client, actualSettings);
      var session = new AlTestRunnerSession(
        new ClientSession(client, new NonDispatcher(), new TimerFactory<TaskTimer>()),
        actualSettings);
      session.Open();
      return session;
    }

    private static void ConfigureUnderlyingHttpClient(JsonHttpClient client, ClientSessionSettings settings)
    {
      var httpClientField = client.GetType().GetField("httpClient", BindingFlags.NonPublic | BindingFlags.Instance);
      if (!(httpClientField?.GetValue(client) is HttpClient httpClient)) return;

      httpClient.Timeout = settings.ClientTimeout;
    }


    public const string ErrorControlIdentifier = "00000000-0000-0000-0800-0000836bd2d2";

    public const string WarningControlIdentifier = "00000000-0000-0000-0300-0000836bd2d2";


    private readonly ClientSession _session;

    private readonly ClientSessionSettings _settings;

    private ClientLogicalForm _lastCaughtForm;

    private readonly ConcurrentBag<Exception> _exceptions = new ConcurrentBag<Exception>();

    //private readonly Queue<Func<Task>> _callbackQueue = new Queue<Func<Task>>();


    public AlTestRunnerSession(ClientSession session, ClientSessionSettings settings)
    {
      _session = session;
      _settings = settings;
    }


    public void Open()
    {
      var sessionParams = new ClientSessionParameters
      {
        CultureId = _settings.Culture.Name,
        UICultureId = _settings.Culture.Name,
        TimeZoneId = _settings.TimeZone.Id,
        AdditionalSettings =
        {
          { "IncludeControlIdentifier", true }
        }
      };
      _session.MessageToShow += ClientSessionOnMessageToShow;
      _session.CommunicationError += ExceptionHandler;
      _session.UnhandledException += ExceptionHandler;
      _session.InvalidCredentialsError += ClientSessionOnInvalidCredentialsError;
      _session.UriToShow += ClientSessionOnUriToShow;
      _session.LookupFormToShow += ClientSessionOnLookupFormToShow;
      _session.DialogToShow += ClientSessionOnDialogToShow;
      _session.FormToShow += CatchFormHandler;
      _ = _session.RunMethod("OpenSessionAsync", sessionParams);
      AwaitStateReady();
    }


    public void AwaitStateReady()
    {
      AwaitState(ClientSessionState.Ready);
    }

    public void AwaitState(ClientSessionState targetState)
    {
      var sw = Stopwatch.StartNew();
      while (_session.State != targetState)
      {
        ThrowExceptions();
        Thread.Sleep(TimeSpan.FromMilliseconds(100));
        switch (_session.State)
        {
          case ClientSessionState.InError:
            throw new Exception("Session error.");
          case ClientSessionState.TimedOut:
            throw new Exception("Session timed out.");
        }

        if (sw.Elapsed > _settings.AwaitStatusTimeout)
          throw new TimeoutException($"Timeout waiting for state '{targetState}'");
      }
    }

    private async Task InvokeInteractionAsync(ClientInteraction interaction)
    {
      await _session.RunMethod("InvokeInteractionAsync", interaction);
      AwaitStateReady();
    }

    public void ThrowExceptions()
    {
      if (_exceptions.TryTake(out var ex))
        throw ex;
    }


    public async Task<ClientLogicalForm> OpenForm(int pageId, string bookmark = null)
    {
      var interaction = new OpenFormInteraction
      {
        Page = $"{pageId}"
      };
      if (!string.IsNullOrEmpty(bookmark)) interaction.Bookmark = bookmark;
      return await InvokeInteractionAndCatchForm(interaction);
    }

    public async Task<ClientLogicalForm> InvokeInteractionAndCatchForm(ClientInteraction interaction)
    {
      _lastCaughtForm = null;
      await InvokeInteractionAsync(interaction);
      if (_lastCaughtForm == null) await CloseAllWarningForms();
      var caughForm = _lastCaughtForm;
      _lastCaughtForm = null;
      return caughForm;
    }

    public void CloseSession()
    {
      _session.MessageToShow -= ClientSessionOnMessageToShow;
      _session.CommunicationError -= ExceptionHandler;
      _session.UnhandledException -= ExceptionHandler;
      _session.InvalidCredentialsError -= ClientSessionOnInvalidCredentialsError;
      _session.UriToShow -= ClientSessionOnUriToShow;
      _session.LookupFormToShow -= ClientSessionOnLookupFormToShow;
      _session.DialogToShow -= ClientSessionOnDialogToShow;
      _session.FormToShow -= CatchFormHandler;

      try
      {
        _session.CloseSessionAsync();
        AwaitState(ClientSessionState.Closed);
      }
      catch
      {
        // ignore
      }

      _session.Dispose();
    }


    private void ExceptionHandler(object sender, ExceptionEventArgs e)
    {
      _exceptions.Add(e.Exception);
    }

    private static void ClientSessionOnMessageToShow(object sender, MessageToShowEventArgs e)
    {
      Console.WriteLine($"Message : {e.Message}");
    }

    private static void ClientSessionOnLookupFormToShow(object sender, ClientLookupFormToShowEventArgs e)
    {
      Console.WriteLine("Open lookup form");
    }

    private static void ClientSessionOnUriToShow(object sender, ClientUriToShowEventArgs e)
    {
      Console.WriteLine($"URI to show: '{e.UriToShow}'");
    }

    private void CatchFormHandler(object sender, ClientFormToShowEventArgs e)
    {
      _lastCaughtForm = e.FormToShow;
    }

    private void ClientSessionOnDialogToShow(object sender, ClientDialogToShowEventArgs e)
    {
      _ = ClientSessionOnDialogToShowAsync(e);
    }

    private async Task ClientSessionOnDialogToShowAsync(ClientDialogToShowEventArgs e)
    {
      var dialog = e.DialogToShow;
      if (dialog.ControlIdentifier == ErrorControlIdentifier)
      {
        // var message = e.DialogToShow.ContainedControls.OfType<ClientStaticStringControl>().FirstOrDefault()?.StringValue;
        // _callbackQueue.Enqueue(async () =>
        // {
        //   await Close(dialog);
        //   throw new Exception(message);
        // });
      }

      if (e.DialogToShow.ControlIdentifier == WarningControlIdentifier)
      {
        // _callbackQueue.Enqueue(() => Close(dialog));
        return;
      }

      if (e.DialogToShow.ControlIdentifier == "{000009ce-0000-0001-0c00-0000836bd2d2}" ||
          e.DialogToShow.ControlIdentifier == "{000009cd-0000-0001-0c00-0000836bd2d2}")
      {
        _lastCaughtForm = e.DialogToShow;
        return;
      }

      if (e.DialogToShow.ControlIdentifier == "8da61efd-0002-0003-0507-0b0d1113171d")
      {
        // _callbackQueue.Enqueue(() => Close(dialog));
        return;
      }

      var action = e.DialogToShow.GetActionByName("OK");
      if (action != null)
      {
        await Invoke(action);
      }
      else
      {
        await Close(e.DialogToShow);
      }
    }


    public async Task Invoke(ClientActionControl action)
    {
      await InvokeInteractionAsync(new InvokeActionInteraction(action));
    }

    public async Task SaveValue(ClientLogicalControl control, string newValue)
    {
      await InvokeInteractionAsync(new SaveValueInteraction(control, newValue));
    }

    public async Task SelectFirstRow(ClientLogicalControl control)
    {
      await InvokeInteractionAsync(new InvokeActionInteraction(control, SystemAction.SelectFirstRow));
    }

    public async Task Refresh(ClientLogicalControl control)
    {
      await InvokeInteractionAsync(new InvokeActionInteraction(control, SystemAction.Refresh));
    }

    public async Task ScrollRepeater(ClientRepeaterControl repeater, int delta)
    {
      await InvokeInteractionAsync(new ScrollRepeaterInteraction(repeater, delta));
    }


    public async Task Close(ClientLogicalForm form)
    {
      await InvokeInteractionAsync(new CloseFormInteraction(form));
    }

    public async Task CloseAllForms(string identifier = null)
    {
      foreach (var form in _session.ListForms(identifier))
      {
        await Close(form);
      }
    }

    public async Task CloseAllWarningForms()
    {
      await CloseAllForms(WarningControlIdentifier);
    }

    private void ClientSessionOnInvalidCredentialsError(object sender, MessageToShowEventArgs e)
    {
      _exceptions.Add(new Exception(e.Message));
    }


    public void Dispose()
    {
      CloseSession();
    }
  }
}