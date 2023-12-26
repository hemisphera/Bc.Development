using System;

namespace Bc.Development.Util
{
  internal class ProcessNotFoundException : Exception
  {
    public string PidString { get; }

    public ProcessNotFoundException(string pidText)
      : base($"Process holding lock not found. ({pidText})")
    {
      PidString = pidText;
    }
  }
}