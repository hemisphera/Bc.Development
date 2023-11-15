using System;
using System.Linq;

namespace Bc.Development.TestRunner
{
  public class CommandLineTestToolCodeunit
  {
    public string Name { get; set; }

    public int Codeunit { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime FinishTime { get; set; }

    public TimeSpan Duration => FinishTime.Subtract(StartTime);

    public CommandLineTestToolMethod[] TestResults { get; set; }

    public override string ToString()
    {
      var success = TestResults?.Count(r => r.Result == TestResult.Success);
      var total = TestResults?.Length;
      return $"{success.ToString() ?? "?"} / {total.ToString() ?? "?"} ({Duration})";
    }
  }
}