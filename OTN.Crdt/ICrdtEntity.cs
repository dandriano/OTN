using System;
using System.Collections.Generic;

namespace OTN.Crdt;

public interface ICrdtEntity
{
    string Id { get; }
    Dictionary<string, object> Attributes { get; }
    bool IsTombstone { get; set; }
    long LastTimestamp { get; set; }

    void ApplyOperation(IGraphOperation op)
    {
        ArgumentNullException.ThrowIfNull(op);
        if (op.LamportTimestamp <= LastTimestamp)
            return;
        switch (op)
        {
            case AddEntityOp addOp when addOp.Node.Id == Id:
                if (!IsTombstone) 
                    LastTimestamp = addOp.LamportTimestamp;
                break;
            case UpdateEntityAttrOp attrOp when attrOp.NodeId == Id:
                Attributes[attrOp.Key] = attrOp.Value;
                LastTimestamp = attrOp.LamportTimestamp;
                break;
            case RemoveEntityOp removeOp when MatchesId(removeOp.ElementId):
                IsTombstone = true;
                LastTimestamp = op.LamportTimestamp;
                break;
            default:
                break;
        }
    }

    void Merge(ICrdtEntity other)
    {
        ArgumentNullException.ThrowIfNull(other);
        
        if (other.LastTimestamp > LastTimestamp)
        {
            IsTombstone = other.IsTombstone;
            LastTimestamp = other.LastTimestamp;
        }
    }

    bool MatchesId(string? elementId);
}

public interface ICrdtEdge<TVertex, TWeight> : ICrdtEntity
    where TVertex : ICrdtEntity
    where TWeight : struct
{
    ICrdtEntity Source { get; }
    ICrdtEntity Target { get; }
    TWeight Tag { get; }
}

public class AddEntityOp : IGraphOperation
{
    public long LamportTimestamp { get; }
    public string ReplicaId { get; }
    public string OperationId { get; }
    public ICrdtEntity Node { get; }

    public AddEntityOp(long timestamp, string replicaId, ICrdtEntity entity)
    {
        LamportTimestamp = timestamp;
        ReplicaId = replicaId 
            ?? throw new ArgumentNullException(nameof(replicaId));
        OperationId = $"{timestamp}:{replicaId}:{GetType().Name}";
        Node = entity 
            ?? throw new ArgumentNullException(nameof(entity));
    }

    public int CompareTo(IGraphOperation? other)
    {
        throw new NotImplementedException();
    }
}

public class RemoveEntityOp : IGraphOperation
{
    public long LamportTimestamp { get; }
    public string ReplicaId { get; }
    public string OperationId { get; }
    public string ElementId { get; }

    public RemoveEntityOp(long timestamp, string replicaId, string elementId)
    {
        LamportTimestamp = timestamp;
        ReplicaId = replicaId 
            ?? throw new ArgumentNullException(nameof(replicaId));
        OperationId = $"{timestamp}:{replicaId}:{GetType().Name}";
        ElementId = elementId 
            ?? throw new ArgumentNullException(nameof(elementId));
    }

    public int CompareTo(IGraphOperation? other)
    {
        throw new NotImplementedException();
    }
}

public class UpdateEntityAttrOp : IGraphOperation
{
    public long LamportTimestamp { get; }
    public string ReplicaId { get; }
    public string OperationId { get; }
    public string NodeId { get; }
    public string Key { get; }
    public object Value { get; }

    public UpdateEntityAttrOp(long timestamp, string replicaId, string nodeId, string key, object value)
    {
        ArgumentNullException.ThrowIfNull(replicaId);
        ArgumentNullException.ThrowIfNull(nodeId);
        ArgumentNullException.ThrowIfNull(key);

        LamportTimestamp = timestamp;
        ReplicaId = replicaId;
        OperationId = $"{timestamp}:{replicaId}:{GetType().Name}";
        NodeId = nodeId;
        Key = key;
        Value = value; // Value can be null (e.g., to clear an attribute)
    }

    public int CompareTo(IGraphOperation? other)
    {
        throw new NotImplementedException();
    }
}