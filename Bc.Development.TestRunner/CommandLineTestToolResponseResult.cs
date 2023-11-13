using System;

namespace Bc.Development.TestRunner
{
  public class CommandLineTestToolResponseResult
  {
    public string Method { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime FinishTime { get; set; }

    public TestResult Result { get; set; }

    public TimeSpan Duration => FinishTime.Subtract(StartTime);


    public override string ToString()
    {
      return $"{Result}: {Method} ({Duration})";
    }
  }
}