using OTN.Extensions;
using OTN.Interfaces;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using QuikGraph;
using System.Linq;
using System.Threading.Tasks;
using System;
using OTN.Utils;
using OTN.Core;

namespace OTN.Benchmarks;

[MemoryDiagnoser]
public class RoutingBenchmarks
{
    private Network _network = null!;
    private NetNode _source = null!;
    private NetNode _target = null!;

    [Params(1000)]
    public int TotalNodes { get; set; } = 1000;

    [Params(50)]
    public int BackboneNodes { get; set; } = 50;

    [Params(3)]
    public int AverageEdgesPerNode { get; set; } = 3;

    [GlobalSetup]
    public void Setup()
    {
        _network = NetworkFactory.Create(TotalNodes, BackboneNodes, AverageEdgesPerNode);
        _source = _network.Optical.Vertices.First();
        _target = _network.Optical.Vertices.Last();
    }

    [Benchmark]
    public async Task FindOpticPathsAsync_Benchmark()
    {
        var paths = await _network.FindOpticPathsAsync(_source, _target, k: 3);
        if (paths == null || paths.Count == 0)
            throw new InvalidOperationException("No paths found");
    }
}

public class Program
{
    static void Main() => BenchmarkRunner.Run<RoutingBenchmarks>();
}