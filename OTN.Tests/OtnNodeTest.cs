using System;
using System.Collections.Generic;
using System.Linq;
using OTN.Core;
using OTN.Enums;
using OTN.Extensions;

namespace OTN.Tests;

[TestFixture]
public class OtnNodeTest
{
    private List<AggregationRule> _fullRuleSet = new List<AggregationRule>();
    private List<AggregationRule> _baikalRuleSet = new List<AggregationRule>();
    private List<Func<OtnNode, OtnNode, Signal>> _clientFactory = new List<Func<OtnNode, OtnNode, Signal>>();

    [OneTimeSetUp]
    public void SetUp()
    {
        // OTNv3 rule set (miss flex and xN, not yet supported)
        _fullRuleSet.Add(new AggregationRule(OtnLevel.ODU0, OtnLevel.ODU4));
        _fullRuleSet.Add(new AggregationRule(OtnLevel.ODU1, OtnLevel.ODU4));
        _fullRuleSet.Add(new AggregationRule(OtnLevel.ODU2, OtnLevel.ODU4));
        _fullRuleSet.Add(new AggregationRule(OtnLevel.ODU3, OtnLevel.ODU4));
        _fullRuleSet.Add(new AggregationRule(OtnLevel.ODU0, OtnLevel.ODU3));
        _fullRuleSet.Add(new AggregationRule(OtnLevel.ODU1, OtnLevel.ODU3));
        _fullRuleSet.Add(new AggregationRule(OtnLevel.ODU2, OtnLevel.ODU3));
        _fullRuleSet.Add(new AggregationRule(OtnLevel.ODU0, OtnLevel.ODU2));
        _fullRuleSet.Add(new AggregationRule(OtnLevel.ODU1, OtnLevel.ODU2));

        // Ancient baikal rule set
        _baikalRuleSet.Add(new AggregationRule(OtnLevel.ODU0, OtnLevel.ODU2));
        _baikalRuleSet.Add(new AggregationRule(OtnLevel.ODU1, OtnLevel.ODU2));

        // Some ethernet clients
        _clientFactory.Add((s, t) => new Signal(s, t, 1.0));        // Ethernet 1G : 1 Gbps approx
        _clientFactory.Add((s, t) => new Signal(s, t, 10.0));       // Ethernet 10G : 10 Gbps approx 

        // Some SDH clients
        _clientFactory.Add((s, t) => new Signal(s, t, 0.15552));    // SDH STM-1 : 155.52 Mbps
        _clientFactory.Add((s, t) => new Signal(s, t, 0.62208));    // SDH STM-4 : 1622.08 Mbps
        _clientFactory.Add((s, t) => new Signal(s, t, 2.48832));    // SDH STM-16 : 12.488 Gbps
    }

    [Test]
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

        Assert.DoesNotThrow(() =>
        {
            var otn = new OtnNode(new NetNode(NetNodeType.Terminal), stupidButOkRule);
        });
    }

    [Test]
    public void IsAggregationSupported_AssertExpectedResultsForDifferentRuleSets()
    {
        var baikalNode = new OtnNode(new NetNode(NetNodeType.Terminal), _baikalRuleSet);
        var fullNode = new OtnNode(new NetNode(NetNodeType.Terminal), _fullRuleSet);

        // Some simple direct checks
        Assert.Multiple(() =>
        {
            Assert.That(baikalNode.IsAggregationSupported(OtnLevel.ODU0, OtnLevel.ODU2));
            Assert.That(!baikalNode.IsAggregationSupported(OtnLevel.ODU0, OtnLevel.ODU4));
            Assert.That(fullNode.IsAggregationSupported(OtnLevel.ODU0, OtnLevel.ODU4));
        });
    }

    [Test]
    public void TryAggregate_AssertHandlesAggregation()
    {
        var fullNode1 = new OtnNode(new NetNode(NetNodeType.Terminal), _fullRuleSet);
        var fullNode2 = new OtnNode(new NetNode(NetNodeType.Terminal), _fullRuleSet);
        var s = _clientFactory[0](fullNode1, fullNode2).ToOtnSignal();

        // Check for direct aggregation
        Assert.That(fullNode1.TryAggregate(s, out var aggregated1));
        Assert.That(fullNode2.TryAggregate(aggregated1!, out var aggregated2));

        Assert.That(aggregated1!.Id, Is.EqualTo(aggregated2!.Id));

        Assert.That(aggregated1.OduLevel, Is.EqualTo(OtnLevel.ODU4));
        Assert.That(aggregated1.Signals.Single().OduLevel, Is.EqualTo(OtnLevel.ODU0));

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
                Assert.That(!baikalNode1.TryAggregate(otn, out _));
            }
            else
            {
                Assert.That(baikalNode1.TryAggregate(otn, out var aggregated3));
                Assert.That(baikalNode2.TryAggregate(aggregated3!, out var aggregated4));
                Assert.That(aggregated3!.Id, Is.EqualTo(aggregated4!.Id));
            }
        }

        Assert.That(baikalNode1.SignalCount, Is.EqualTo(1));  // in OTN Node capacity by default

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
        Assert.That(baikalNode1.SignalCount, Is.EqualTo(1));  // in OTN Node capacity by default
        Assert.That(!baikalNode1.TryAggregate(newSignal, out _));

        // Ok, let's assume that baikal could aggregate up to ODU4/100G
        var nonExistentBaikalRuleSet = _baikalRuleSet.ToList();
        nonExistentBaikalRuleSet.Add(new AggregationRule(OtnLevel.ODU2, OtnLevel.ODU4));

        // Check transitive aggregation, where's no direct path
        var nonExistentBaikal1 = new OtnNode(new NetNode(NetNodeType.Terminal), nonExistentBaikalRuleSet);
        var nonExistentBaikal2 = new OtnNode(new NetNode(NetNodeType.Terminal), nonExistentBaikalRuleSet);

        var anotherNewSignal = _clientFactory[0](nonExistentBaikal1, nonExistentBaikal2).ToOtnSignal();
        Assert.That(nonExistentBaikal1.TryAggregate(anotherNewSignal, out var aggregated));
        Assert.That(nonExistentBaikal2.TryAggregate(aggregated!, out _));
        Assert.That(aggregated!.OduLevel, Is.EqualTo(OtnLevel.ODU4));
        Assert.That(aggregated.Signals.Single().OduLevel, Is.EqualTo(OtnLevel.ODU2));
        Assert.That(aggregated.Signals.Single().Signals.Single().OduLevel, Is.EqualTo(OtnLevel.ODU0));
    }

    [Test]
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
            Assert.That(baikalNode1.TryAggregate(signal, out var aggregated1));
            Assert.That(baikalNode2.TryAggregate(aggregated1!, out var aggregated2));
            Assert.That(aggregated1!.Id, Is.EqualTo(aggregated2!.Id));

            if (i == 0)
                assertId = aggregated1.Id;
            else
                Assert.That(assertId, Is.EqualTo(aggregated1.Id));

            aggregation.Enqueue(signal);
        }

        // No more
        var newSignal = _clientFactory[0](baikalNode1, baikalNode2).ToOtnSignal();
        Assert.That(!baikalNode1.TryAggregate(newSignal, out _));

        // Drain aggregation
        while (aggregation.TryDequeue(out var loAgg))
        {
            Assert.That(baikalNode2.TryDeAggregate(loAgg, out var hoAgg));
            if (aggregation.Count == 0)
            {
                Assert.That(hoAgg!.SignalCount, Is.Zero);
                Assert.That(baikalNode1.TryDeAggregate(hoAgg, out _));
            }
        }

        // No less
        Assert.That(baikalNode1.SignalCount, Is.Zero);
        Assert.That(baikalNode2.SignalCount, Is.Zero);
    }
}