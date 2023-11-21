using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Bc.Development.TestRunner
{
  /// <summary>
  /// An entry in a test execution playlist.
  /// </summary>
  public class TestPlaylistEntry
  {
    /// <summary>
    /// Loads a playlist from a file
    /// </summary>
    /// <param name="filename">The full path to the playlist file.</param>
    /// <returns>The list of playlist entries.</returns>
    public static TestPlaylistEntry[] FromFile(string filename)
    {
      return JsonConvert.DeserializeObject<TestPlaylistEntry[]>(File.ReadAllText(filename, Encoding.UTF8));
    }

    /// <summary>
    /// Writes a test playlist to a file.
    /// </summary>
    /// <param name="filename">The full path to the playlist file.</param>
    /// <param name="entries">The playlist entries.</param>
    public static void ToFile(string filename, IEnumerable<TestPlaylistEntry> entries)
    {
      var content = JsonConvert.SerializeObject(entries.ToArray());
      var fi = new FileInfo(filename);
      if (fi.Directory?.Exists == false) fi.Directory.Create();
      File.WriteAllText(filename, content, Encoding.UTF8);
    }


    public TestPlaylistEntry()
    {
    }

    public TestPlaylistEntry(int codenitId, string methodName = null)
      : this()
    {
      CodeunitId = codenitId;
      MethodName = methodName;
    }

    /// <summary>
    /// The ID of the codeunit.
    /// </summary>
    public int CodeunitId { get; set; }

    /// <summary>
    /// The name of the method. This is optional, if not specified all methods of the codeunit will be used.
    /// </summary>
    public string MethodName { get; set; }
  }
}