using System;
using System.Collections.Generic;
using System.Linq;
using OTN.Core;
using OTN.Enums;
using OTN.Extensions;

namespace OTN.Tests;

public sealed class OtnNodeFixture
{
    public List<AggregationRule> FullRuleSet { get; } = new List<AggregationRule>();
    public List<AggregationRule> BaikalRuleSet { get; } = new List<AggregationRule> ();
    public List<Func<OtnNode, OtnNode, Signal>> ClientFactory { get; } = new List<Func<OtnNode, OtnNode, Signal>>();

    public OtnNodeFixture()
    {
        // OTNv3 rule set (miss flex and xN, not yet supported)
        FullRuleSet.Add(new AggregationRule(OtnLevel.ODU0, OtnLevel.ODU4));
        FullRuleSet.Add(new AggregationRule(OtnLevel.ODU1, OtnLevel.ODU4));
        FullRuleSet.Add(new AggregationRule(OtnLevel.ODU2, OtnLevel.ODU4));
        FullRuleSet.Add(new AggregationRule(OtnLevel.ODU3, OtnLevel.ODU4));
        FullRuleSet.Add(new AggregationRule(OtnLevel.ODU0, OtnLevel.ODU3));
        FullRuleSet.Add(new AggregationRule(OtnLevel.ODU1, OtnLevel.ODU3));
        FullRuleSet.Add(new AggregationRule(OtnLevel.ODU2, OtnLevel.ODU3));
        FullRuleSet.Add(new AggregationRule(OtnLevel.ODU0, OtnLevel.ODU2));
        FullRuleSet.Add(new AggregationRule(OtnLevel.ODU1, OtnLevel.ODU2));

        // Ancient baikal rule set
        BaikalRuleSet.Add(new AggregationRule(OtnLevel.ODU0, OtnLevel.ODU2));
        BaikalRuleSet.Add(new AggregationRule(OtnLevel.ODU1, OtnLevel.ODU2));

        // Some ethernet clients
        ClientFactory.Add((s, t) => new Signal(s, t, 1.0));        // Ethernet 1G : 1 Gbps approx
        ClientFactory.Add((s, t) => new Signal(s, t, 10.0));       // Ethernet 10G : 10 Gbps approx

        // Some SDH clients
        ClientFactory.Add((s, t) => new Signal(s, t, 0.15552));    // SDH STM-1 : 155.52 Mbps
        ClientFactory.Add((s, t) => new Signal(s, t, 0.62208));    // SDH STM-4 : 1622.08 Mbps
        ClientFactory.Add((s, t) => new Signal(s, t, 2.48832));    // SDH STM-16 : 12.488 Gbps
    }
}

public class OtnNodeTest : IClassFixture<OtnNodeFixture>
{
    private readonly List<AggregationRule> _fullRuleSet;
    private readonly List<AggregationRule> _baikalRuleSet;
    private readonly List<Func<OtnNode, OtnNode, Signal>> _clientFactory;

    public OtnNodeTest(OtnNodeFixture fixture)
    {
        _fullRuleSet = fixture.FullRuleSet;
        _baikalRuleSet = fixture.BaikalRuleSet;
        _clientFactory = fixture.ClientFactory;
    }

    [Fact]
    public void Constructor_AssertRules()
    {
        // Two "head" rule
        var invalidRules = new List<AggregationRule>()
        {
            new AggregationRule(OtnLevel.ODU0, OtnLevel.ODU2),
            new AggregationRule(OtnLevel.ODU0, OtnLevel.ODU4)
        };

        Assert.Throws<InvalidOperationException>(() =>
        {
            var otn = new OtnNode(new NetNode(NetNodeType.Terminal), invalidRules);
        });

        // Stupid, but...
        var stupidButOkRule = new List<AggregationRule>()
        {
            new AggregationRule(OtnLevel.ODU0, OtnLevel.ODU2),
            new AggregationRule(OtnLevel.ODU0, OtnLevel.ODU4),
            new AggregationRule(OtnLevel.ODU2, OtnLevel.ODU4),
        };

        _ = new OtnNode(new NetNode(NetNodeType.Terminal), stupidButOkRule);
    }

    [Fact]
    public void IsAggregationSupported_AssertExpectedResultsForDifferentRuleSets()
    {
        var baikalNode = new OtnNode(new NetNode(NetNodeType.Terminal), _baikalRuleSet);
        var fullNode = new OtnNode(new NetNode(NetNodeType.Terminal), _fullRuleSet);

        // Some simple direct checks
        Assert.Multiple(() =>
        {
            Assert.True(baikalNode.IsAggregationSupported(OtnLevel.ODU0, OtnLevel.ODU2));
            Assert.False(baikalNode.IsAggregationSupported(OtnLevel.ODU0, OtnLevel.ODU4));
            Assert.True(fullNode.IsAggregationSupported(OtnLevel.ODU0, OtnLevel.ODU4));
        });
    }

    [Fact]
    public void TryAggregate_AssertHandlesAggregation()
    {
        var fullNode1 = new OtnNode(new NetNode(NetNodeType.Terminal), _fullRuleSet);
        var fullNode2 = new OtnNode(new NetNode(NetNodeType.Terminal), _fullRuleSet);
        var s = _clientFactory[0](fullNode1, fullNode2).ToOtnSignal();

        // Check for direct aggregation
        Assert.True(fullNode1.TryAggregate(s, out var aggregated1));
        Assert.True(fullNode2.TryAggregate(aggregated1!, out var aggregated2));

        Assert.Equal(aggregated2!.Id, aggregated1!.Id);

        Assert.Equal(OtnLevel.ODU4, aggregated1.OduLevel);
        Assert.Equal(OtnLevel.ODU0, aggregated1.Signals.Single().OduLevel);

        // Check for random agregation
        var rnd = new Random();
        var baikalNode1 = new OtnNode(new NetNode(NetNodeType.Terminal), _baikalRuleSet);
        var baikalNode2 = new OtnNode(new NetNode(NetNodeType.Terminal), _baikalRuleSet);
        var clients = Enumerable.Range(0, 5)
                                .Select(i => _clientFactory[rnd.Next(_clientFactory.Count - 1)](baikalNode1, baikalNode2))
                                .ToList();


        foreach (var client in clients)
        {
            var otn = client.ToOtnSignal();
            if (otn.OduLevel > OtnLevel.ODU1)
            {
                Assert.False(baikalNode1.TryAggregate(otn, out _));
            }
            else
            {
                Assert.True(baikalNode1.TryAggregate(otn, out var aggregated3));
                Assert.True(baikalNode2.TryAggregate(aggregated3!, out var aggregated4));
                Assert.Equal(aggregated4!.Id, aggregated3!.Id);
            }
        }

        Assert.Single(baikalNode1.Signals);  // in OTN Node capacity by default

        // Let's "fill" a remainning space of the OTN signal with aggregation
        // of STM-1
        for (; ; )
        {
            var signal = _clientFactory[2](baikalNode1, baikalNode2).ToOtnSignal();
            if (baikalNode1.TryAggregate(signal, out var aggregated5)
                && baikalNode2.TryAggregate(aggregated5!, out _))
            {
                continue;
            }
            break;
        }

        // No more
        var newSignal = _clientFactory[rnd.Next(_clientFactory.Count - 1)](baikalNode1, baikalNode2).ToOtnSignal();
        Assert.Single(baikalNode1.Signals);  // in OTN Node capacity by default
        Assert.False(baikalNode1.TryAggregate(newSignal, out _));

        // Ok, let's assume that baikal could aggregate up to ODU4/100G
        var nonExistentBaikalRuleSet = _baikalRuleSet.ToList();
        nonExistentBaikalRuleSet.Add(new AggregationRule(OtnLevel.ODU2, OtnLevel.ODU4));

        // Check transitive aggregation, where's no direct path
        var nonExistentBaikal1 = new OtnNode(new NetNode(NetNodeType.Terminal), nonExistentBaikalRuleSet);
        var nonExistentBaikal2 = new OtnNode(new NetNode(NetNodeType.Terminal), nonExistentBaikalRuleSet);

        var anotherNewSignal = _clientFactory[0](nonExistentBaikal1, nonExistentBaikal2).ToOtnSignal();
        Assert.True(nonExistentBaikal1.TryAggregate(anotherNewSignal, out var aggregated));
        Assert.True(nonExistentBaikal2.TryAggregate(aggregated!, out _));
        Assert.Equal(OtnLevel.ODU4, aggregated!.OduLevel);
        Assert.Equal(OtnLevel.ODU2, aggregated.Signals.Single().OduLevel);
        Assert.Equal(OtnLevel.ODU0, aggregated.Signals.Single().Signals.Single().OduLevel);

        // Ok, new empty node
        var emotyNode = new OtnNode(new NetNode(NetNodeType.Terminal), _baikalRuleSet);
        // but no more
        Assert.False(emotyNode.TryAggregate(newSignal, out var _ ,dontCreateAggregated: true));
    }

    [Fact]
    public void TryDeAggregate_AssertHandlesDeAggregation()
    {
        // Line 1xODU2 OTN Node
        var assertId = Guid.Empty;
        var baikalNode1 = new OtnNode(new NetNode(NetNodeType.Terminal), _baikalRuleSet);
        var baikalNode2 = new OtnNode(new NetNode(NetNodeType.Terminal), _baikalRuleSet);
        var aggregation = new Queue<OtnSignal>();

        // 4xGE + 4xSTM-1
        for (int i = 0; i < 8; i++)
        {
            // GE or STM-1
            var id = i % 2 == 0 ? 0 : 2;
            var signal = _clientFactory[id](baikalNode1, baikalNode2).ToOtnSignal();
            Assert.True(baikalNode1.TryAggregate(signal, out var aggregated1));
            Assert.True(baikalNode2.TryAggregate(aggregated1!, out var aggregated2));
            Assert.Equal(aggregated2!.Id, aggregated1!.Id);

            if (i == 0)
                assertId = aggregated1.Id;
            else
                Assert.Equal(aggregated1.Id, assertId);

            aggregation.Enqueue(signal);
        }

        // No more
        var newSignal = _clientFactory[0](baikalNode1, baikalNode2).ToOtnSignal();
        Assert.False(baikalNode1.TryAggregate(newSignal, out _));

        // Drain aggregation
        while (aggregation.TryDequeue(out var loAgg))
        {
            Assert.True(baikalNode2.TryDeAggregate(loAgg, out var hoAgg));
            if (aggregation.Count == 0)
            {
                Assert.Equal(0, hoAgg!.SignalCount);
                Assert.True(baikalNode1.TryDeAggregate(hoAgg, out _));
            }
        }

        // No less
        Assert.Empty(baikalNode1.Signals);
        Assert.Empty(baikalNode2.Signals);
    }

    [Fact]
    public void TryAggregate_TopLevelContainerMustTerminateAtNode()
    {
        var fullRuleSet = _fullRuleSet;

        var nodeA = new OtnNode(new NetNode(NetNodeType.Terminal), fullRuleSet);
        var nodeB = new OtnNode(new NetNode(NetNodeType.Terminal), fullRuleSet);
        var nodeC = new OtnNode(new NetNode(NetNodeType.Terminal), fullRuleSet);

        // Build a top-level aggregated container between A and B.
        // We intentionally aggregate from B so that B gets the container stored.
        var client = _clientFactory[0](nodeA, nodeB).ToOtnSignal();
        Assert.True(nodeB.TryAggregate(client, out var hoBetweenAandB));
        Assert.Equal(OtnLevel.ODU4, hoBetweenAandB!.OduLevel);
        Assert.Equal(1, hoBetweenAandB.SignalCount);

        // Node C should not accept a container that doesn't terminate at C.
        Assert.False(nodeC.TryAggregate(hoBetweenAandB!, out _));
    }

    [Fact]
    public void TryAggregate_DontCreateAggregatedBlocksTopLevelStore()
    {
        var fullRuleSet = _fullRuleSet;

        var nodeA = new OtnNode(new NetNode(NetNodeType.Terminal), fullRuleSet);
        var nodeB = new OtnNode(new NetNode(NetNodeType.Terminal), fullRuleSet);

        // Build a top-level aggregated container between A and B.
        // We aggregate from B so that A stays empty.
        var client = _clientFactory[0](nodeA, nodeB).ToOtnSignal();
        Assert.True(nodeB.TryAggregate(client, out var hoBetweenAandB));
        Assert.Equal(OtnLevel.ODU4, hoBetweenAandB!.OduLevel);

        // A offered a prebuilt top-level container, but must not store/create it.
        Assert.False(nodeA.TryAggregate(hoBetweenAandB!, out _, dontCreateAggregated: true));

        // Without the restriction, A should accept it.
        Assert.True(nodeA.TryAggregate(hoBetweenAandB!, out var stored));
        Assert.Equal(hoBetweenAandB!.Id, stored!.Id);
    }
}
