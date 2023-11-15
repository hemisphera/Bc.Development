using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Bc.Development.TestRunner.ResultsWriters
{
  public abstract class FileResultsWriter : ITestResultWriter
  {
    public string Filename { get; protected set; }

    protected Stream Stream { get; private set; }


    public async Task BeginWrite()
    {
      Stream = File.Create(Filename);
      await Task.CompletedTask;
    }

    public abstract Task Write(IEnumerable<CommandLineTestToolCodeunit> codeunits);

    public async Task EndWrite()
    {
      try
      {
        await Stream.FlushAsync();
        Stream.Close();
      }
      catch (ObjectDisposedException disp)
      {
        // ignore
      }

      Stream = null;
    }
  }
}