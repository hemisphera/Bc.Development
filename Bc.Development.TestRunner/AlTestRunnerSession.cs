using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
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

    private readonly Queue<Action> _callbackQueue = new Queue<Action>();


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
      _session.OpenSessionAsync(sessionParams);
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

    public void ThrowExceptions()
    {
      if (_exceptions.TryTake(out var ex))
        throw ex;
    }


    public ClientLogicalForm OpenForm(int pageId, string bookmark = null)
    {
      var interaction = new OpenFormInteraction
      {
        Page = $"{pageId}"
      };
      if (!String.IsNullOrEmpty(bookmark)) interaction.Bookmark = bookmark;
      return InvokeInteractionAndCatchForm(interaction);
    }

    public ClientLogicalForm InvokeInteractionAndCatchForm(ClientInteraction interaction)
    {
      _lastCaughtForm = null;
      _session.InvokeInteractionAsync(interaction);
      AwaitStateReady();
      if (_lastCaughtForm == null) CloseAllWarningForms();
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
      Console.WriteLine($"Open lookup form");
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
      var dialog = e.DialogToShow;
      if (dialog.ControlIdentifier == ErrorControlIdentifier)
      {
        var message = e.DialogToShow.ContainedControls.OfType<ClientStaticStringControl>().FirstOrDefault()?.StringValue;
        _callbackQueue.Enqueue(() =>
        {
          Close(dialog);
          throw new Exception(message);
        });
      }

      if (e.DialogToShow.ControlIdentifier == WarningControlIdentifier)
      {
        _callbackQueue.Enqueue(() => Close(dialog));
        return;
      }

      if ((e.DialogToShow.ControlIdentifier == "{000009ce-0000-0001-0c00-0000836bd2d2}") ||
          (e.DialogToShow.ControlIdentifier == "{000009cd-0000-0001-0c00-0000836bd2d2}"))
      {
        _lastCaughtForm = e.DialogToShow;
        return;
      }

      if (e.DialogToShow.ControlIdentifier == "8da61efd-0002-0003-0507-0b0d1113171d")
      {
        _callbackQueue.Enqueue(() => Close(dialog));
        return;
      }

      var action = e.DialogToShow.GetActionByName("OK");
      if (action != null)
      {
        Invoke(action);
      }
      else
      {
        Close(e.DialogToShow);
      }
    }


    public void Invoke(ClientActionControl action)
    {
      _session.InvokeInteractionAsync(new InvokeActionInteraction(action));
      AwaitStateReady();
    }

    public void SaveValue(ClientLogicalControl control, string newValue)
    {
      _session.InvokeInteractionAsync(new SaveValueInteraction(control, newValue));
      AwaitStateReady();
    }

    public void SelectFirstRow(ClientLogicalControl control)
    {
      _session.InvokeInteractionAsync(new InvokeActionInteraction(control, SystemAction.SelectFirstRow));
      AwaitStateReady();
    }

    public void SelectLastRow(ClientLogicalControl control)
    {
      _session.InvokeInteractionAsync(new InvokeActionInteraction(control, SystemAction.SelectLastRow));
      AwaitStateReady();
    }

    public void Refresh(ClientLogicalControl control)
    {
      _session.InvokeInteractionAsync(new InvokeActionInteraction(control, SystemAction.Refresh));
      AwaitStateReady();
    }

    public void ScrollRepeater(ClientRepeaterControl repeater, int delta)
    {
      _session.InvokeInteractionAsync(new ScrollRepeaterInteraction(repeater, delta));
      AwaitStateReady();
    }

    public void ActivateControl(ClientRepeaterControl control)
    {
      _session.InvokeInteractionAsync(new ActivateControlInteraction(control));
      AwaitStateReady();
    }


    public string GetErrorFromErrorForm()
    {
      return _session.OpenedForms
        .FirstOrDefault(f => f.ControlIdentifier == ErrorControlIdentifier)?
        .ContainedControls.OfType<ClientStaticStringControl>()
        .LastOrDefault()?.StringValue;
    }

    public string GetWarningFromWarningForm()
    {
      return _session.OpenedForms
        .FirstOrDefault(f => f.ControlIdentifier == WarningControlIdentifier)?
        .ContainedControls.OfType<ClientStaticStringControl>()
        .LastOrDefault()?.StringValue;
    }


    public void Close(ClientLogicalForm form)
    {
      _session.InvokeInteractionAsync(new CloseFormInteraction(form));
      AwaitStateReady();
    }

    public void CloseAllForms(string identifier = null)
    {
      foreach (var form in _session.ListForms(identifier))
      {
        Close(form);
      }
    }

    public void CloseAllErrorForms()
    {
      CloseAllForms(ErrorControlIdentifier);
    }

    public void CloseAllWarningForms()
    {
      CloseAllForms(WarningControlIdentifier);
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