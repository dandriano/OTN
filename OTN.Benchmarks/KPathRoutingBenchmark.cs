using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using OTN.Core;
using OTN.Extensions;

namespace OTN.Benchmarks;

[MemoryDiagnoser]
public class KPathRoutingBenchmark
{
    private Network _network = null!;
    private NetNode _source = null!;
    private NetNode _target = null!;
    private RouteConstraints _constraints = null!;

    [Params(35, 100)]
    public int TotalNodes { get; set; }

    [Params(5, 10)]
    public int BackboneNodes { get; set; }

    [Params(2, 4)]
    public int ConnectionAttemptUpperBound { get; set; }

    [Params(1, 5)]
    public int PathCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        (_network, _source, _target, _constraints) = BenchmarkNetworkInputs.Create(
            TotalNodes, BackboneNodes, ConnectionAttemptUpperBound, requiredNodeCount: 0, avoidedNodeCount: 0, seed: 137);
    }

    [Benchmark]
    public List<List<Link>> FindOpticPaths_Benchmark() 
        => _network.Optical.FindOpticPath(_source, _target, _constraints, PathCount);
}
