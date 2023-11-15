using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bc.Development.TestRunner.ResultsWriters
{
  public interface ITestResultWriter
  {
    Task BeginWrite();

    Task Write(IEnumerable<CommandLineTestToolCodeunit> codeunits);

    Task EndWrite();
  }
}