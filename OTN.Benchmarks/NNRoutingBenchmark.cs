using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using OTN.Core;
using OTN.Extensions;

namespace OTN.Benchmarks;

[MemoryDiagnoser]
public class NNRoutingBenchmark
{
    private Network _network = null!;
    private NetNode _source = null!;
    private NetNode _target = null!;

    [Params(35, 100)]
    public int TotalNodes { get; set; }

    [Params(5, 10)]
    public int BackboneNodes { get; set; }

    [Params(2, 3)]
    public int ConnectionAttemptUpperBound { get; set; }

    [Params(5)]
    public int MustAvoidNodes { get; set; }

    [Params(4)]
    public int MustPassNodes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        (_network, _source, _target) = BenchmarkNetworkInputs.Create(
            TotalNodes, BackboneNodes, ConnectionAttemptUpperBound, MustPassNodes, MustAvoidNodes, seed: 271);
    }

    [Benchmark]
    public List<Link> FindOpticPaths_Benchmark() 
        => _network.Optical.FindMustPassOpticPath(_source, _target);
}
