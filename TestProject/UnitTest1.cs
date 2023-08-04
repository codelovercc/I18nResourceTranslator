using System;
using System.IO;
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

    [Test]
    public void TestOpenCartRegex()
    {
        // 还有这种内容的
        // const string line = "$_['error_exception']               = 'Error Code(%s): %s in %s \"on\" line %s';    $_['2error_exception']               = \"'My Store &copy; 2009-' . date('Y') . ' All Rights Reserved.';\";";
        // "'My Store &copy; 2009-' . date('Y') . ' All Rights Reserved.';"
        // 要匹配这段内容的话，只能只匹配引号里的内容了，正则表达式中就不能匹配=号， 像@"(?<quote>['""])(?<value>[^\1]+)\1"这样的表达式就可以了
        const string line = "$_['error_exception']               = 'Error Code(%s): %s in %s \"on\" line %s';    $_['2error_exception']               = \"2Error Code(%s): %s in %s 'on' line %s\";";
        var matches = Regex.Matches(line, @"\s*=\s*(?<quote>['""])(?<value>[^\1]+)\1\s*;\s*");
        Assert.IsNotEmpty(matches);
        Console.WriteLine($"count: {matches.Count}");
        foreach (Match match in matches)
        {
            Console.WriteLine($"match index: {match.Index}");
            Console.WriteLine($"\tquote: {match.Groups["quote"].Value}");
            Console.WriteLine($"\tvalue: {match.Groups["value"].Value}");
        }
        // 接下来的断言如果改动了常量line，可能会不通过
        Assert.AreEqual(2, matches.Count);
        Assert.AreEqual("'", matches[0].Groups["quote"].Value);
        Assert.AreEqual("\"", matches[1].Groups["quote"].Value);
        Assert.AreEqual("Error Code(%s): %s in %s \"on\" line %s", matches[0].Groups["value"].Value);
        Assert.AreEqual("2Error Code(%s): %s in %s 'on' line %s", matches[1].Groups["value"].Value);
    }

    [Test]
    public void TestPath()
    {
        const string to = "zh-CN";
        const string sourcePath = "/opencart/upload/catalog/language/en-gb/";
        const string filePath = "/opencart/upload/catalog/language/en-gb/api/account/login.php";
        var repath = Path.Combine(".", to, filePath.Replace(sourcePath, ""));
        Console.WriteLine(repath);
        Console.WriteLine(Path.GetExtension(filePath));
        Console.WriteLine(Path.GetFileName(filePath));
        Console.WriteLine(Path.GetFileNameWithoutExtension(filePath));
    }

    [Test]
    public void TestGetAllFiles()
    {
        // 替换成自己的
        const string pathToFind = "/to/the/path";
        var filePaths =
            Directory.GetFiles(pathToFind,
                "*.php", SearchOption.AllDirectories);
        Console.WriteLine($"SearchOption.AllDirectories: {filePaths.Length}");
        foreach (var path in filePaths)
        {
            Console.WriteLine(path);
        }
        
        Console.WriteLine(new string('-', 30));
        
        var filePathsWithEnumerationOptions = 
            Directory.GetFiles(pathToFind,
                "*.php", new EnumerationOptions
                {
                    MaxRecursionDepth = 1024,
                    RecurseSubdirectories = true,
                });
        Assert.AreEqual(filePaths.Length, filePathsWithEnumerationOptions.Length);
        Console.WriteLine($"EnumerationOptionsWithRecurse: {filePathsWithEnumerationOptions.Length}");

        foreach (var path in filePathsWithEnumerationOptions)
        {
            Console.WriteLine(path);
        }
    }
}