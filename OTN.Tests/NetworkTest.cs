using OTN.Core;
using OTN.Extensions;
using OTN.Utils;
using System.Linq;
using System.Threading.Tasks;

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
    public async Task FindOpticPathsAsync_AssertPathFound()
    {
        var source = _network.Optical.Vertices.First();
        var target = _network.Optical.Vertices.Last();
        var paths = await _network.FindOpticPathsAsync(source, target);

        Assert.That(paths, Is.Not.Null);
        Assert.That(paths, Has.Count.Not.Zero);
    }
}