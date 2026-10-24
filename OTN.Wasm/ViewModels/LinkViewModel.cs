using OTN.Core;

namespace OTN.Wasm.ViewModels;

public sealed class LinkViewModel(Link link)
{
    public string Id { get; } = link.Id.ToString()[..5];
    public Link Link { get; internal set; } = link;
}
