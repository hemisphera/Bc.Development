using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Dynamics.Framework.UI.Client;

namespace Bc.Development.TestRunner
{
  internal class SessionContext
  {
    private static readonly List<SessionContext> Sessions = new List<SessionContext>();

    private readonly ClientSession _session;

    public ClientLogicalForm LastCaughtForm { get; set; }


    public static SessionContext Get(ClientSession session)
    {
      lock (Sessions)
      {
        var item = Sessions.FirstOrDefault(s => s._session.Info.SessionId == session.Info.SessionId);
        if (item == null)
        {
          item = new SessionContext(session);
          Sessions.Add(item);
        }

        return item;
      }
    }


    public ConcurrentBag<Exception> Exceptions { get; } = new ConcurrentBag<Exception>();


    private SessionContext(ClientSession session)
    {
      _session = session;
    }


    public void ThrowExceptions()
    {
      if (Exceptions.TryTake(out var ex))
        throw ex;
    }
  }
}