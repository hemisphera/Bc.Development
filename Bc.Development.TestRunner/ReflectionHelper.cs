using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Dynamics.Framework.UI.Client;

namespace Bc.Development.TestRunner
{
  internal static class ReflectionHelper
  {
    public static async Task RunMethod(this object item, string name, params object[] args)
    {
      var method =
        item.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
          .FirstOrDefault(m => m.Name == name && m.GetParameters().Length == args.Length)
        ?? throw new MissingMethodException();
      var result = method.Invoke(item, args);
      if (result is Task task) await task;
    }
  }
}