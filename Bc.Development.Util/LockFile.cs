using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Bc.Development.Util
{
  public class LockFile : IDisposable
  {
    private readonly string _filePath;

    private static readonly TimeSpan ProcessCheckFrequency = TimeSpan.FromSeconds(5);


    public LockFile(string path)
    {
      _filePath = path;
    }


    public async Task Wait()
    {
      Stopwatch sw = null;
      while (File.Exists(_filePath))
      {
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        if (sw == null || sw.Elapsed > ProcessCheckFrequency)
        {
          sw?.Stop();
          if (!await DeleteFileIfOrphaned())
            sw = Stopwatch.StartNew();
        }
      }

      CreateFile();
    }

    public async Task Release()
    {
      DeleteFile();
      await Task.CompletedTask;
    }


    private void DeleteFile()
    {
      if (File.Exists(_filePath)) File.Delete(_filePath);
    }

    public async Task<Process> GetLockedByProcess()
    {
      if (!File.Exists(_filePath)) return null;

      Process process = null;
      var pidText = String.Empty;
      try
      {
        int pid;
        using (var fs = File.OpenText(_filePath))
        {
          pidText = await fs.ReadLineAsync();
          pid = int.Parse(pidText);
        }

        process = Process.GetProcesses().FirstOrDefault(p => p.Id == pid);
      }
      catch
      {
        // ignore
      }

      if (process == null)
      {
        throw new ProcessNotFoundException(pidText);
      }

      return process;
    }

    private async Task<bool> DeleteFileIfOrphaned()
    {
      try
      {
        await GetLockedByProcess();
        return false;
      }
      catch (ProcessNotFoundException)
      {
        DeleteFile();
        return true;
      }
    }

    private void CreateFile()
    {
      var directoryPath = Path.GetDirectoryName(_filePath);
      if (!string.IsNullOrEmpty(directoryPath))
      {
        Directory.CreateDirectory(directoryPath);
      }

      using (var fs = File.CreateText(_filePath))
      {
        fs.WriteLine(Process.GetCurrentProcess().Id);
        fs.Close();
      }
    }

    public void Dispose()
    {
      DeleteFile();
    }
  }
}