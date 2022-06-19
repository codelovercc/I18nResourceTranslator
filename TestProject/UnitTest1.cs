using System.Text.RegularExpressions;
using NUnit.Framework;

namespace TestProject;

public class Tests
{
    private static readonly Regex tokensRegex = new Regex(@"\{[\w\-_\+\d]+\}");

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }

    [Test]
    public void TestToken()
    {
        const string tokenStr = "{days1}我{parent_id}的上级:  {days}  {day+-s1}";
        var matches = tokensRegex.Matches(tokenStr);
        Assert.AreEqual(4, matches.Count);
        Assert.AreEqual("{days1}", matches[0].Value);
        Assert.AreEqual("{parent_id}", matches[1].Value);
        Assert.AreEqual("{days}", matches[2].Value);
        Assert.AreEqual("{day+-s1}", matches[3].Value);
    }
}