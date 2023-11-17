using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bc.Development.TestRunner.ResultsWriters
{
  public class ConsoleResultWriter : ITestResultWriter
  {
    public Task BeginWrite()
    {
      return Task.CompletedTask;
    }

    public Task Write(IEnumerable<CommandLineTestToolCodeunit> codeunits)
    {
      var l1Spacer = "".PadLeft(2);
      var l2Spacer = "".PadLeft(9);
      var l3Spacer = "".PadLeft(11);

      foreach (var codeunit in codeunits)
      {
        Console.WriteLine($"Codeunit {codeunit.Codeunit} {codeunit.Name} ({codeunit.Duration.TotalSeconds}s)");
        foreach (var method in codeunit.TestResults)
        {
          Console.WriteLine(l1Spacer + FormatResult(method.Result) + " Method " + method.Method + " (" + method.Duration.TotalSeconds + "s)");
          if (!String.IsNullOrEmpty(method.Message))
            Console.WriteLine(l2Spacer + method.Message);
          if (!String.IsNullOrEmpty(method.StackTrace))
          {
            var lines = method.StackTrace.Split(';');
            foreach (var line in lines)
            {
              Console.WriteLine(l3Spacer + line.Trim('\r'));
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

    public Task EndWrite()
    {
      return Task.CompletedTask;
    }
  }
}