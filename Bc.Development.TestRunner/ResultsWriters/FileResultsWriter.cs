using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Bc.Development.TestRunner.ResultsWriters
{
  /// <summary>
  /// A general purpos test result writer that writes results to a file.
  /// </summary>
  public abstract class FileResultsWriter : ITestResultWriter
  {
    /// <summary>
    /// The full path to the file.
    /// </summary>
    public string Filename { get; protected set; }

    /// <summary>
    /// The file stream.
    /// </summary>
    protected Stream Stream { get; private set; }


    /// <inheritdoc />
    public async Task BeginWrite()
    {
      Stream = File.Create(Filename);
      await Task.CompletedTask;
    }

    /// <inheritdoc />
    public abstract Task Write(IEnumerable<CommandLineTestToolCodeunit> codeunits);

    /// <inheritdoc />
    public async Task EndWrite()
    {
      try
      {
        await Stream.FlushAsync();
        Stream.Close();
      }
      catch (ObjectDisposedException)
      {
        // ignore failure caused by already closed/disposed streams
      }

      Stream = null;
    }
  }
}