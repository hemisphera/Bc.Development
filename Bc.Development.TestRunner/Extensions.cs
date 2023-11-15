using System.Collections.Generic;
using System.Threading.Tasks;
using Bc.Development.TestRunner.ResultsWriters;

namespace Bc.Development.TestRunner
{
  public static class Extensions
  {
    public static Task Write(this IEnumerable<CommandLineTestToolCodeunit> results, ITestResultWriter writer)
    {
      return writer.Write(results);
    }
  }
}