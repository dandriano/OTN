using OTN.Core;

namespace OTN.Wasm.ViewModels;

public sealed class LinkViewModel(Link link)
{
    public Link Link { get; internal set; } = link;
}
