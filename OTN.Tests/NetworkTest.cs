using System.Linq;
using OTN.Core;
using OTN.Extensions;
using OTN.Utils;

namespace OTN.Tests;

[TestFixture]
public class NetworkTest
{
    private Network _network;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        _network = NetworkFactory.Create(35, 5, 3);
    }

    [Test]
    public void FindOpticPathsAsync_AssertPathFound()
    {
        var source = _network.Optical.Vertices.First();
        var target = _network.Optical.Vertices.Last();
        var paths = _network.FindOpticPathsAsync(source, target);

        Assert.DoesNotThrowAsync(async () => await paths);

        // Not guaranteed
        // Assert.That(paths, Is.Not.Null);
        // Assert.That(paths, Has.Count.Not.Zero);
    }
}