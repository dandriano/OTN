using OTN.Core;
using OTN.Enums;
using OTN.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OTN.Tests;

[TestFixture]
public class OtnNodeTest
{
    private List<AggregationRule> _fullRuleSet = new List<AggregationRule>();
    private List<AggregationRule> _baikalRuleSet = new List<AggregationRule>();
    private List<Func<Signal>> _clientFactory = new List<Func<Signal>>();

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
        _clientFactory.Add(() => new Signal("Ethernet 1G", 1.0));       // 1 Gbps approx
        _clientFactory.Add(() => new Signal("Ethernet 10G", 10.0));     // 10 Gbps approx 

        // Some SDH clients
        _clientFactory.Add(() => new Signal("SDH STM-1", 0.15552));     // 155.52 Mbps
        _clientFactory.Add(() => new Signal("SDH STM-4", 0.62208));     // 622.08 Mbps
        _clientFactory.Add(() => new Signal("SDH STM-16", 2.48832));    // 2.488 Gbps
    }

    [Test]
    public void AggregationRuleTest()
    {
        var baikalNode = new OtnNode(_baikalRuleSet);
        var fullNode = new OtnNode(_fullRuleSet);

        // Some simple direct checks
        Assert.Multiple(() =>
        {
            Assert.That(baikalNode.IsAggregationSupported(OtnLevel.ODU0, OtnLevel.ODU2));
            Assert.That(!baikalNode.IsAggregationSupported(OtnLevel.ODU0, OtnLevel.ODU4));
            Assert.That(fullNode.IsAggregationSupported(OtnLevel.ODU0, OtnLevel.ODU4));
        });
    }

    [Test]
    public void ClientAggregationTest()
    {
        var fullNode = new OtnNode(_fullRuleSet);
        // Check for direct aggregation
        Assert.Multiple(() =>
        {
            Assert.That(fullNode.TryAggregate(_clientFactory[0]().ToOtnSignal()));
            Assert.That(fullNode.Signals.Single().OduLevel, Is.EqualTo(OtnLevel.ODU4));
            Assert.That(fullNode.Signals.Single().Aggregation.Single().OduLevel, Is.EqualTo(OtnLevel.ODU0));
        });

        // Check for random agregation
        var rnd = new Random();
        var clients = Enumerable.Range(0, 5)
                                .Select(i => _clientFactory[rnd.Next(_clientFactory.Count - 1)]())
                                .ToList();

        var baikalNode = new OtnNode(_baikalRuleSet);
        foreach (var client in clients)
        {
            var otn = client.ToOtnSignal();
            if (otn.OduLevel > OtnLevel.ODU1)
                Assert.That(!baikalNode.TryAggregate(otn));
            else
                Assert.That(baikalNode.TryAggregate(otn));
        }

        Assert.That(baikalNode.Signals, Is.Not.Empty);
        Assert.That(baikalNode.Signals, Has.Count.EqualTo(1));  // in OTN Node capacity by default

        // Let's "fill" a remainning space of the OTN signal with aggregation
        // of STM-1
        while (baikalNode.TryAggregate(_clientFactory[2]().ToOtnSignal()))
            continue;

        // No more
        Assert.Multiple(() =>
        {
            Assert.That(baikalNode.Signals, Has.Count.EqualTo(1));  // in OTN Node capacity by default
            Assert.That(!baikalNode.TryAggregate(_clientFactory[rnd.Next(_clientFactory.Count - 1)]().ToOtnSignal()));
        });

        // Ok, let's assume that baikal could aggregate up to ODU4/100G
        var nonExistentBaikalRuleSet = _baikalRuleSet.ToList();
        nonExistentBaikalRuleSet.Add(new AggregationRule(OtnLevel.ODU2, OtnLevel.ODU4));

        // Check transitive aggregation, where's no direct path
        var nonExistentBaikal = new OtnNode(nonExistentBaikalRuleSet);
        Assert.Multiple(() =>
        {
            Assert.That(nonExistentBaikal.TryAggregate(_clientFactory[0]().ToOtnSignal()));
            Assert.That(nonExistentBaikal.Signals.Single().OduLevel, Is.EqualTo(OtnLevel.ODU4));
            Assert.That(nonExistentBaikal.Signals.Single().Aggregation.Single().OduLevel, Is.EqualTo(OtnLevel.ODU2));
            Assert.That(nonExistentBaikal.Signals.Single().Aggregation.Single().Aggregation.Single().OduLevel, Is.EqualTo(OtnLevel.ODU0));
        });
    }

    [Test]
    public void CreateInvalidOtnNodeTest()
    {
        // Two "head" rule
        var invalidRules = new List<AggregationRule>()
        {
            new AggregationRule(OtnLevel.ODU0, OtnLevel.ODU2),
            new AggregationRule(OtnLevel.ODU0, OtnLevel.ODU4)
        };

        Assert.Throws<InvalidOperationException>(() =>
        {
            var otn = new OtnNode(invalidRules);
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
            var otn = new OtnNode(stupidButOkRule);
        });
    }

    [Test]
    public void CpacityOtnNodeTest()
    {
        // Line 1xODU2 OTN Node
        var baikalNode = new OtnNode(_baikalRuleSet);

        // 4xGE + 4xSTM-1
        for (int i = 0; i < 8; i++)
        {
            // GE or STM-1
            var id = i % 2 == 0 ? 0 : 2;
            Assert.That(baikalNode.TryAggregate(_clientFactory[id]().ToOtnSignal()));
        }

        // No more
        Assert.That(!baikalNode.TryAggregate(_clientFactory[0]().ToOtnSignal()));
    }
}