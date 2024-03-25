using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Dynamics.Framework.UI.Client;

namespace Bc.Development.TestRunner
{
  internal static class InternalExtensions
  {
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

    public static void ThrowIfAny(this IEnumerable<ClientValidationResultItem> items)
    {
      var clientValidationResultItems = items as ClientValidationResultItem[] ?? items.ToArray();
      if (clientValidationResultItems?.Any() != true) return;
      var sb = new StringBuilder();

      foreach (var item in clientValidationResultItems)
      {
        sb.AppendLine(item.Description);
      }

      throw new Exception(sb.ToString());
    }
  }
}