using System;

namespace OTN.Crdt;

public class LamportClock
{
    private long _counter = 0;
    public string ReplicaId { get; }

    public LamportClock(string replicaId)
    {
        ReplicaId = replicaId 
            ?? throw new ArgumentNullException(nameof(replicaId));
    }

    public long Tick() 
        => ++_counter;

    public void Update(long remoteTimestamp)
        => _counter = Math.Max(_counter, remoteTimestamp);
}