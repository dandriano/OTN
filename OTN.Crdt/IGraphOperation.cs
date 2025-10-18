using System;

namespace OTN.Crdt;

public interface IGraphOperation : IComparable<IGraphOperation>
{
    long LamportTimestamp { get; }
    string ReplicaId { get; }
    string OperationId { get; }

    new int CompareTo(IGraphOperation? other)
    {
        if (other == null)
            return 1;

        int cmp = LamportTimestamp.CompareTo(other.LamportTimestamp);
        if (cmp != 0)
            return cmp;

        // If timestamps are equal, compare ReplicaId
        return string.Compare(ReplicaId, other.ReplicaId, StringComparison.Ordinal);
    }
}