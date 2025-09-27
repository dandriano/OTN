using System.Collections.Generic;

namespace OTN.Tests;

[TestFixture]
public class OtnNodeTest
{
    private List<AggregationRule> _fullRuleSet = [];
    private List<AggregationRule> _baikalRuleSet = [];

    [OneTimeSetUp]
    public void SetUp()
    {
        _fullRuleSet.Add(new AggregationRule(OtnLevel.ODU0, OtnLevel.ODU4));
        _fullRuleSet.Add(new AggregationRule(OtnLevel.ODU1, OtnLevel.ODU4));
        _fullRuleSet.Add(new AggregationRule(OtnLevel.ODU2, OtnLevel.ODU4));
        _fullRuleSet.Add(new AggregationRule(OtnLevel.ODU3, OtnLevel.ODU4));
        _fullRuleSet.Add(new AggregationRule(OtnLevel.ODU0, OtnLevel.ODU3));
        _fullRuleSet.Add(new AggregationRule(OtnLevel.ODU1, OtnLevel.ODU3));
        _fullRuleSet.Add(new AggregationRule(OtnLevel.ODU2, OtnLevel.ODU3));
        _fullRuleSet.Add(new AggregationRule(OtnLevel.ODU0, OtnLevel.ODU2));
        _fullRuleSet.Add(new AggregationRule(OtnLevel.ODU1, OtnLevel.ODU2));

        _baikalRuleSet.Add(new AggregationRule(OtnLevel.ODU0, OtnLevel.ODU2));
        _baikalRuleSet.Add(new AggregationRule(OtnLevel.ODU1, OtnLevel.ODU2));
    }
    
    [Test]
    public void DirectAggregationRuleTest()
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
}