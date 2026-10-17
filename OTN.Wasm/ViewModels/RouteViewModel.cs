using System.Collections.Generic;
using System.Linq;
using OTN.Core;

namespace OTN.Wasm.ViewModels;

public sealed record RouteViewModel(IReadOnlyList<Link> Links, double Length)
{
    public string Description => Links.Count == 0
        ? "No links"
        : string.Join(" → ", new[] { Links[0].Source }.Concat(Links.Select(link => link.Target)).Select(node => node.ToString()));
}
