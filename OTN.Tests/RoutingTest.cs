/*
namespace OTN.Benchmarks;

[TestFixture]
public class RoutingTest
{
    [Test]
    public void KPathRoutingBenchmark()
    {
        var summary = BenchmarkRunner.Run<KPathRoutingBenchmark>();
        Assert.That(summary.Reports, Is.Not.Empty);
    }

    [Test]
    public void NNRoutingBenchmark()
    {
        var summary = BenchmarkRunner.Run<NNRoutingBenchmark>();
        Assert.That(summary.Reports, Is.Not.Empty);
    }
}

[MemoryDiagnoser]
public class KPathRoutingBenchmark
{
    private readonly Random _rnd = new Random();
    private Network _network = null!;
    private NetNode _source = null!;
    private NetNode _target = null!;

    [Params(35, 100)]
    public int TotalNodes { get; set; }
    [Params(5, 10)]
    public int BackboneNodes { get; set; }
    [Params(2, 3)]
    public int AverageEdgesPerNode { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _network = NetworkFactory.Create(TotalNodes, BackboneNodes, AverageEdgesPerNode);
        var n = _network.Optical.Vertices.ToList();
        _source = n[_rnd.Next(n.Count)];
        _target = n[_rnd.Next(n.Count)];
    }

    [Benchmark]
    public void FindOpticPaths_Benchmark()
    {
        _network.Optical.FindOpticPath(_source, _target);
    }
}

[MemoryDiagnoser]
public class NNRoutingBenchmark
{
    private readonly Random _rnd = new Random();
    private Network _network = null!;
    private NetNode _source = null!;
    private NetNode _target = null!;

    [Params(35, 100)]
    public int TotalNodes { get; set; }
    [Params(5, 10)]
    public int BackboneNodes { get; set; }
    [Params(2, 3)]
    public int AverageEdgesPerNode { get; set; }
    [Params(5)]
    public int MustAvoidNodes { get; set; }
    [Params(4)]
    public int MustPassNodes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _network = NetworkFactory.Create(TotalNodes, BackboneNodes, AverageEdgesPerNode, MustPassNodes, MustAvoidNodes);
        var n = _network.Optical.Vertices.ToList();
        _source = n[_rnd.Next(n.Count)];
        _target = n[_rnd.Next(n.Count)];
    }

    [Benchmark]
    public void FindOpticPaths_Benchmark()
    {
        _network.Optical.FindMustPassOpticPath(_source, _target);
    }
}
*/