using OTN.Core;

namespace OTN.Editor;

public sealed class EditorLink(Link link)
{
    public Link Link { get; internal set; } = link;
}
