using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.Dynamics.Framework.UI.Client;

namespace Bc.Development.TestRunner
{
  internal static class ClientSessionFactory
  {
    public static ClientSession CreateUserPassword(
      Uri serverUri, NetworkCredential credential,
      ClientSessionSettings settings = null)
    {
      var provider = ServiceAddressProvider.ServiceAddress(serverUri);
      var client = new JsonHttpClient(provider, credential, AuthenticationScheme.UserNamePassword);
      ConfigureUnderlyingHttpClient(client);
      var clientSession = new ClientSession(client, new NonDispatcher(), new TimerFactory<TaskTimer>());

      OpenSession(clientSession, settings ?? ClientSessionSettings.Default);
      return clientSession;
    }

    private static void ConfigureUnderlyingHttpClient(JsonHttpClient client)
    {
      var httpClientField = client.GetType().GetField("httpClient", BindingFlags.NonPublic | BindingFlags.Instance);
      if (!(httpClientField?.GetValue(client) is HttpClient httpClient)) return;

      httpClient.Timeout = TimeSpan.FromSeconds(10);
    }


    private static void OpenSession(ClientSession session, ClientSessionSettings settings)
    {
      var sessionParams = new ClientSessionParameters
      {
        CultureId = settings.Culture.Name,
        UICultureId = settings.Culture.Name,
        TimeZoneId = settings.TimeZone.Id,
        AdditionalSettings =
        {
          { "IncludeControlIdentifier", true }
        }
      };
      session.MessageToShow += ClientSessionOnMessageToShow;
      session.CommunicationError += ExceptionHandler;
      session.UnhandledException += ExceptionHandler;
      session.InvalidCredentialsError += ClientSessionOnInvalidCredentialsError;
      session.UriToShow += ClientSessionOnUriToShow;
      session.LookupFormToShow += ClientSessionOnLookupFormToShow;
      session.DialogToShow += ClientSessionOnDialogToShow;
      session.FormToShow += CatchFormHandler;
      session.OpenSessionAsync(sessionParams);
      session.AwaitStateReady();
    }

    private static void CatchFormHandler(object sender, ClientFormToShowEventArgs e)
    {
      var sc = SessionContext.Get(e.FormToShow.Session);
      sc.LastCaughtForm = e.FormToShow;
    }

    private static void ClientSessionOnDialogToShow(object sender, ClientDialogToShowEventArgs e)
    {
      var dialog = e.DialogToShow;
      if (dialog.ControlIdentifier == InternalExtensions.ErrorControlIdentifier)
      {
        var message = e.DialogToShow.ContainedControls.OfType<ClientStaticStringControl>().FirstOrDefault()?.StringValue;
        dialog.Close();
        throw new Exception(message);
      }

      if (e.DialogToShow.ControlIdentifier == InternalExtensions.WarningControlIdentifier)
      {
        dialog.Close();
        return;
      }

      if ((e.DialogToShow.ControlIdentifier == "{000009ce-0000-0001-0c00-0000836bd2d2}") ||
          (e.DialogToShow.ControlIdentifier == "{000009cd-0000-0001-0c00-0000836bd2d2}"))
      {
        var sessionContext = SessionContext.Get(e.ParentForm.Session);
        sessionContext.LastCaughtForm = e.DialogToShow;
        return;
      }

      if (e.DialogToShow.ControlIdentifier == "8da61efd-0002-0003-0507-0b0d1113171d")
      {
        //var message = e.DialogToShow.ContainedControls.OfType<ClientStaticStringControl>().FirstOrDefault()?.StringValue;
        dialog.Close();
        return;
      }

      var action = e.DialogToShow.GetActionByName("OK");

      if (action != null)
      {
        action.Invoke();
      }
      else
      {
        e.DialogToShow.Close();
      }
    }


    private static void ClientSessionOnLookupFormToShow(object sender, ClientLookupFormToShowEventArgs e)
    {
      Console.WriteLine($"Open lookup form");
    }

    private static void ClientSessionOnUriToShow(object sender, ClientUriToShowEventArgs e)
    {
      Console.WriteLine($"URI to show: '{e.UriToShow}'");
    }

    private static void ClientSessionOnInvalidCredentialsError(object sender, MessageToShowEventArgs e)
    {
      var sc = SessionContext.Get(sender as ClientSession);
      sc.Exceptions.Add(new Exception(e.Message));
    }

    private static void ExceptionHandler(object sender, ExceptionEventArgs e)
    {
      var sc = SessionContext.Get(sender as ClientSession);
      sc.Exceptions.Add(e.Exception);
    }

    private static void ClientSessionOnMessageToShow(object sender, MessageToShowEventArgs e)
    {
      Console.WriteLine($"Message : {e.Message}");
    }


    public static void Close(ClientSession session)
    {
      session.MessageToShow -= ClientSessionOnMessageToShow;
      session.CommunicationError -= ExceptionHandler;
      session.UnhandledException -= ExceptionHandler;
      session.InvalidCredentialsError -= ClientSessionOnInvalidCredentialsError;
      session.UriToShow -= ClientSessionOnUriToShow;
      session.LookupFormToShow -= ClientSessionOnLookupFormToShow;
      session.DialogToShow -= ClientSessionOnDialogToShow;
      session.FormToShow -= CatchFormHandler;

      SessionContext.Remove(session);

      try
      {
        session.CloseSessionAsync();
        session.AwaitState(ClientSessionState.Closed);
      }
      catch
      {
        // ignore
      }

      session.Dispose();
    }
  }
}