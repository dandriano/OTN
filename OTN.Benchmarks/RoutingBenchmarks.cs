using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using OTN.Core;
using OTN.Extensions;
using OTN.Utils;

namespace OTN.Benchmarks;

[MemoryDiagnoser]
public class RoutingBenchmarks
{
    private Network _network = null!;
    private NetNode _source = null!;
    private NetNode _target = null!;

    [Params(1000)]
    public int TotalNodes { get; set; }

    [Params(25)]
    public int BackboneNodes { get; set; }

    [Params(2, 3)]
    public int AverageEdgesPerNode { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _network = NetworkFactory.Create(TotalNodes, BackboneNodes, AverageEdgesPerNode);
        _source = _network.Optical.Vertices.First();
        _target = _network.Optical.Vertices.Last();
    }

    [Benchmark]
    public Task FindOpticPathsAsync_Benchmark()
    {
        return _network.FindOpticPathsAsync(_source, _target, k: 10);
    }
}

public class Program
{
    static void Main() => BenchmarkRunner.Run<RoutingBenchmarks>();
}