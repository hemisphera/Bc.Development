namespace Bc.Development.TestRunner
{
  /// <summary>
  /// The result of a test.
  /// </summary>
  public enum TestResult
  {
    /// <summary>
    /// Unknown result.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The test failed.
    /// </summary>
    Failure = 1,

    /// <summary>
    /// The test succeeded.
    /// </summary>
    Success = 2,

    /// <summary>
    /// The test was skipped.
    /// </summary>
    Skipped = 3
  }
}