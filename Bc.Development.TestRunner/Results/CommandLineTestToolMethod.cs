using System;
using Newtonsoft.Json;

namespace Bc.Development.TestRunner
{
  /// <summary>
  /// The result of a test method in a test codeunit.
  /// </summary>
  [JsonObject(MemberSerialization.OptIn)]
  public class CommandLineTestToolMethod
  {
    /// <summary>
    /// The name of the method.
    /// </summary>
    [JsonProperty("Method")]
    public string MethodName { get; set; }

    /// <summary>
    /// The start time of the test.
    /// </summary>
    [JsonProperty]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// The finish time of the test.
    /// </summary>
    [JsonProperty]
    public DateTime FinishTime { get; set; }

    /// <summary>
    /// The result of the test.
    /// </summary>
    [JsonProperty]
    public TestResult Result { get; set; }

    /// <summary>
    /// The duration of the test.
    /// </summary>
    public TimeSpan Duration => FinishTime.Subtract(StartTime);

    /// <summary>
    /// The message of the test.
    /// </summary>
    [JsonProperty]
    public string Message { get; set; }

    /// <summary>
    /// If the test failed, this will contain the stack trace.
    /// </summary>
    [JsonProperty]
    public string StackTrace { get; set; }

    /// <summary>
    /// The stack trace lines.
    /// </summary>
    public string[] StackTraceLines => StackTrace?.Split(';') ?? Array.Empty<string>();


    /// <inheritdoc />
    public override string ToString()
    {
      return $"{Result}: {MethodName} ({Duration})";
    }
  }
}