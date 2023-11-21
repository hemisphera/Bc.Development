using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Bc.Development.TestRunner
{
  /// <summary>
  /// The results of a test codeunit.
  /// </summary>
  [JsonObject(MemberSerialization.OptIn)]
  public class CommandLineTestToolCodeunit
  {
    /// <summary>
    /// The name of the codeunit.
    /// </summary>
    [JsonProperty]
    public string Name { get; set; }

    /// <summary>
    /// The ID of the codeunit.
    /// </summary>
    [JsonProperty("Codeunit")]
    public int CodeunitId { get; set; }

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
    /// The duration of the test.
    /// </summary>
    public TimeSpan Duration => FinishTime.Subtract(StartTime);

    /// <summary>
    /// The single test method results.
    /// </summary>
    [JsonProperty("TestResults")]
    public List<CommandLineTestToolMethod> Methods { get; set; } = new List<CommandLineTestToolMethod>();


    /// <inheritdoc />
    public override string ToString()
    {
      var success = Methods?.Count(r => r.Result == TestResult.Success);
      var total = Methods?.Count;
      return $"{success.ToString() ?? "?"} / {total.ToString() ?? "?"} ({Duration})";
    }
  }
}