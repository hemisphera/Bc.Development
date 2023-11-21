using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Dynamics.Framework.UI.Client;
using Microsoft.Dynamics.Framework.UI.Client.Interactions;

namespace Bc.Development.TestRunner
{
  internal static class InternalExtensions
  {
    private static readonly TimeSpan AwaitStateTimeout = TimeSpan.FromSeconds(20);

    public const string ErrorControlIdentifier = "00000000-0000-0000-0800-0000836bd2d2";

    public const string WarningControlIdentifier = "00000000-0000-0000-0300-0000836bd2d2";

    public static void AwaitStateReady(this ClientSession session)
    {
      session.AwaitState(ClientSessionState.Ready);
    }

    public static void AwaitState(this ClientSession session, ClientSessionState targetState)
    {
      var sessionContext = SessionContext.Get(session);
      var sw = Stopwatch.StartNew();
      while (session.State != targetState)
      {
        sessionContext?.ThrowExceptions();
        Thread.Sleep(TimeSpan.FromMilliseconds(100));
        switch (session.State)
        {
          case ClientSessionState.InError:
            throw new Exception("Session error.");
          case ClientSessionState.TimedOut:
            throw new Exception("Session timed out.");
        }

        if (sw.Elapsed > AwaitStateTimeout)
          throw new TimeoutException($"Timeout waiting for state '{targetState}'");
      }
    }


    public static ClientLogicalForm OpenForm(this ClientSession session, int pageId, string bookmark = null)
    {
      var interaction = new OpenFormInteraction
      {
        Page = $"{pageId}"
      };
      if (!String.IsNullOrEmpty(bookmark)) interaction.Bookmark = bookmark;
      return session.InvokeInteractionAndCatchForm(interaction);
    }

    public static IEnumerable<ClientLogicalForm> ListForms(this ClientSession session, string identifier = null)
    {
      foreach (var attempt in Enumerable.Range(0, 3))
      {
        try
        {
          var all = session.OpenedForms;
          if (!String.IsNullOrEmpty(identifier))
            all = all.Where(f => f.ControlIdentifier == identifier);
          return all.ToArray();
        }
        catch
        {
          if (attempt == 2) throw;
          Thread.Sleep(TimeSpan.FromSeconds(1));
        }
      }

      return Array.Empty<ClientLogicalForm>();
    }


    public static ClientLogicalControl GetControlByCaption(this ClientLogicalControl control, string caption)
    {
      return control.ContainedControls
        .FirstOrDefault(c => c.Caption.Replace("&", "").Equals(caption, StringComparison.OrdinalIgnoreCase));
    }

    public static ClientLogicalControl GetControlByName(this ClientLogicalControl control, string name)
    {
      var result = control.ContainedControls
        .FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
      if (result == null)
        result = control.GetControlByCaption(name);
      return result;
    }

    public static ClientActionControl GetActionByCaption(this ClientLogicalControl control, string caption)
    {
      return control.ContainedControls
        .OfType<ClientActionControl>()
        .FirstOrDefault(c => c.Caption.Replace("&", "").Equals(caption, StringComparison.OrdinalIgnoreCase));
    }

    public static ClientActionControl GetActionByName(this ClientLogicalControl control, string name)
    {
      var result = control.ContainedControls
        .OfType<ClientActionControl>()
        .FirstOrDefault(c => c.Name.Replace("&", "").Equals(name, StringComparison.OrdinalIgnoreCase));
      if (result == null)
        result = control.GetActionByCaption(name);

      return result;
    }

    public static T GetControlByType<T>(this ClientLogicalControl control)
    {
      return control.ContainedControls.OfType<T>().FirstOrDefault();
    }


    public static string GetErrorFromErrorForm(this ClientSession clientSession)
    {
      return clientSession.OpenedForms
        .FirstOrDefault(f => f.ControlIdentifier == ErrorControlIdentifier)?
        .ContainedControls.OfType<ClientStaticStringControl>()
        .LastOrDefault()?.StringValue;
    }

    public static string GetWarningFromWarningForm(this ClientSession clientSession)
    {
      return clientSession.OpenedForms
        .FirstOrDefault(f => f.ControlIdentifier == WarningControlIdentifier)?
        .ContainedControls.OfType<ClientStaticStringControl>()
        .LastOrDefault()?.StringValue;
    }

    public static void Close(this ClientLogicalForm form)
    {
      form.Session.InvokeInteractionAsync(new CloseFormInteraction(form));
      form.Session.AwaitStateReady();
    }


    public static void CloseAllForms(this ClientSession session, string identifier = null)
    {
      foreach (var form in session.ListForms(identifier))
      {
        form.Close();
      }
    }


    public static void CloseAllErrorForms(this ClientSession session)
    {
      session.CloseAllForms(ErrorControlIdentifier);
    }

    public static void CloseAllWarningForms(this ClientSession session)
    {
      session.CloseAllForms(WarningControlIdentifier);
    }


    public static ClientLogicalForm InvokeInteractionAndCatchForm(this ClientSession session, ClientInteraction interaction)
    {
      var sc = SessionContext.Get(session);
      sc.LastCaughtForm = null;
      session.InvokeInteractionAsync(interaction);
      session.AwaitStateReady();
      if (sc.LastCaughtForm == null) session.CloseAllWarningForms();
      var caughForm = sc.LastCaughtForm;
      sc.LastCaughtForm = null;
      return caughForm;
    }

    public static ClientActionControl Invoke(this ClientActionControl action)
    {
      action.ParentForm.Session.InvokeInteractionAsync(new InvokeActionInteraction(action));
      action.ParentForm.Session.AwaitStateReady();
      return action;
    }


    public static ClientLogicalControl SaveValue(this ClientLogicalControl control, string newValue)
    {
      control.ParentForm.Session.InvokeInteractionAsync(new SaveValueInteraction(control, newValue));
      control.ParentForm.Session.AwaitStateReady();
      return control;
    }

    public static ClientLogicalControl SelectFirstRow(this ClientLogicalControl control)
    {
      control.ParentForm.Session.InvokeInteractionAsync(new InvokeActionInteraction(control, SystemAction.SelectFirstRow));
      control.ParentForm.Session.AwaitStateReady();
      return control;
    }

    public static ClientLogicalControl SelectLastRow(ClientLogicalControl control)
    {
      control.ParentForm.Session.InvokeInteractionAsync(new InvokeActionInteraction(control, SystemAction.SelectLastRow));
      control.ParentForm.Session.AwaitStateReady();
      return control;
    }

    public static ClientLogicalControl Refresh(ClientLogicalControl control)
    {
      control.ParentForm.Session.InvokeInteractionAsync(new InvokeActionInteraction(control, SystemAction.Refresh));
      control.ParentForm.Session.AwaitStateReady();
      return control;
    }


    public static ClientRepeaterControl ScrollRepeater(ClientRepeaterControl repeater, int delta)
    {
      repeater.ParentForm.Session.InvokeInteractionAsync(new ScrollRepeaterInteraction(repeater, delta));
      repeater.ParentForm.Session.AwaitStateReady();
      return repeater;
    }

    public static ClientRepeaterControl ActivateControl(ClientRepeaterControl control)
    {
      control.ParentForm.Session.InvokeInteractionAsync(new ActivateControlInteraction(control));
      control.ParentForm.Session.AwaitStateReady();
      return control;
    }
  }
}