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
        _clientFactory.Add(() => new Signal(Guid.NewGuid(), "Ethernet 1G", 1.0));       // 1 Gbps approx
        _clientFactory.Add(() => new Signal(Guid.NewGuid(), "Ethernet 10G", 10.0));     // 10 Gbps approx 

        // Some SDH clients
        _clientFactory.Add(() => new Signal(Guid.NewGuid(), "SDH STM-1", 0.15552));     // 155.52 Mbps
        _clientFactory.Add(() => new Signal(Guid.NewGuid(), "SDH STM-4", 0.62208));     // 622.08 Mbps
        _clientFactory.Add(() => new Signal(Guid.NewGuid(), "SDH STM-16", 2.48832));    // 2.488 Gbps
    }

    [Test]
    public void AggregationRuleTest()
    {
        var baikalNode = new OtnNode(_baikalRuleSet);
        var fullNode = new OtnNode(_fullRuleSet);

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
        var rnd = new Random();
        var capacity = 5;
        var clients = Enumerable.Range(0, capacity)
                                .Select(i => _clientFactory[rnd.Next(_clientFactory.Count - 1)]())
                                .ToList();

        var baikalNode = new OtnNode(_baikalRuleSet);
        foreach (var client in clients)
        {
            var otn = client.ToOtnSignal();
            if (otn.OduLevel > OtnLevel.ODU1)
            {
                Assert.That(!baikalNode.TryAggregate(otn));
            }
            else
            {
                Assert.That(baikalNode.TryAggregate(otn));
            }
        }

        Assert.That(baikalNode.Signals, Is.Not.Empty);
        Assert.That(baikalNode.Signals, Has.Count.LessThan(capacity));
    }
}