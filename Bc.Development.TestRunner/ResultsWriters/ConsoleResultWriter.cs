using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bc.Development.TestRunner.ResultsWriters
{
  /// <summary>
  /// Writes the test results to the console.
  /// </summary>
  public class ConsoleResultWriter : ITestResultWriter
  {
    /// <inheritdoc />
    public Task BeginWrite()
    {
      return Task.CompletedTask;
    }


    /// <summary>
    /// The action that writes the string. This defaults to Console.WriteLine.
    /// </summary>
    public Action<string> WriteAction { get; set; } = Console.WriteLine;


    /// <inheritdoc />
    public Task Write(IEnumerable<CommandLineTestToolCodeunit> codeunits)
    {
      var l1Spacer = "".PadLeft(2);
      var l2Spacer = "".PadLeft(9);
      var l3Spacer = "".PadLeft(11);

      foreach (var codeunit in codeunits)
      {
        WriteAction($"Codeunit {codeunit.CodeunitId} {codeunit.Name} ({codeunit.Duration.TotalSeconds}s)");
        foreach (var method in codeunit.Methods)
        {
          WriteAction(l1Spacer + FormatResult(method.Result) + " Method " + method.MethodName + " (" + method.Duration.TotalSeconds + "s)");
          if (!string.IsNullOrEmpty(method.Message))
            WriteAction(l2Spacer + method.Message);
          if (!string.IsNullOrEmpty(method.StackTrace))
          {
            var lines = method.StackTrace.Split(';');
            foreach (var line in lines)
            {
              WriteAction(l3Spacer + line.Trim('\r'));
            }
          }
        }
      }

      return Task.CompletedTask;
    }

    private static string FormatResult(TestResult methodResult)
    {
      switch (methodResult)
      {
        case TestResult.Failure:
          return "[FAIL]";
        case TestResult.Success:
          return "[PASS]";
        case TestResult.Skipped:
          return "[SKIP]";
        default:
          return "[ ?? ]";
      }
    }

    /// <inheritdoc />
    public Task EndWrite()
    {
      return Task.CompletedTask;
    }
  }
}