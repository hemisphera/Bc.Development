namespace Bc.Development.TestRunner
{
  /// <summary>
  /// A test item on the server
  /// </summary>
  public class ServerTestItem
  {
    /// <summary>
    /// The ID of the codeunit.
    /// </summary>
    public int CodeunitId { get; set; }

    /// <summary>
    /// The codeunit name. 
    /// </summary>
    public string CodeunitName { get; set; }

    /// <summary>
    /// The method name. 
    /// </summary>
    public string MethodName { get; set; }

    /// <summary>
    /// Indicates whether the test should be run.
    /// </summary>
    public bool Run { get; set; }


    /// <inheritdoc />
    public override string ToString()
    {
      return $"{CodeunitId} {CodeunitName} - {MethodName}";
    }

    /// <summary>
    /// Converts the item to a playlist entry.
    /// </summary>
    /// <returns>The playlist entry.</returns>
    public TestPlaylistEntry ToPlaylistEntry()
    {
      return new TestPlaylistEntry(CodeunitId, MethodName);
    }
  }
}