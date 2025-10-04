using System;
using QuikGraph;

namespace OTN.Interfaces;

public interface ILink : IEdge<INetNode>
{
    Guid Id { get; }
}