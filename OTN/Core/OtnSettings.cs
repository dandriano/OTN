using OTN.Interfaces;

namespace OTN.Core;

public class DefaultOtnSettings : IOtnSettings
{
    // No overrides, default implementations from interface will be used
}

public static class OtnSettings
{
    public static IOtnSettings Default { get; } = new DefaultOtnSettings();
}
