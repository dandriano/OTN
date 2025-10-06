using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using OTN.Core;
using OTN.Extensions;
using OTN.Utils;

namespace OTN.Benchmarks;

[MemoryDiagnoser]
public class RoutingBenchmarks
{
    private readonly Random _rnd = new Random();
    private Network _network = null!;
    private NetNode _source = null!;
    private NetNode _target = null!;

    [Params(35, 100, 250)]
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
        _network.Optical.FindOpticPaths(_source, _target);
    }
}

public class Program
{
    static void Main() => BenchmarkRunner.Run<RoutingBenchmarks>();
}