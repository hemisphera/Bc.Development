using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bc.Development.TestRunner.ResultsWriters
{
  /// <summary>
  ///  Interface for writing test results.
  /// </summary>
  public interface ITestResultWriter
  {
    /// <summary>
    /// Starts the write operation.
    /// </summary>
    Task BeginWrite();

    /// <summary>
    /// Writes the test results.
    /// </summary>
    /// <param name="codeunits">The codeunits to write.</param>
    Task Write(IEnumerable<CommandLineTestToolCodeunit> codeunits);

    /// <summary>
    /// Ends the write operation.
    /// </summary>
    Task EndWrite();
  }
}