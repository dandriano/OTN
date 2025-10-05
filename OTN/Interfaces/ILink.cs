using QuikGraph;
using System;

namespace OTN.Interfaces;

public interface ILink : IEdge<INetNode>, ITagged<double>
{
    Guid Id { get; }
}