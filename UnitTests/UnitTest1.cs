using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using MplotLib;
using NUnit.Framework;
using OneOf;

namespace UnitTests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    [Ignore("One off")]
    public void Test1()
    {
        // string filename = "/home/mitch/repos/mplot/tests/test1.mplot";
        // string filename = "C:\\Users\\mpaulus\\source\\repos\\mplot\\tests\\and_or.mplot";
        // string filename = @"C:\Users\mpaulus\source\repos\23-CEGDC-001-GSA-ESPC-ROB-Building-Model\data\wc.mplot";
        string filename =
            "C:\\Users\\mpaulus\\Command Commissioning\\Jobs - 23-FIDTX-003 WLK TDW Economizer Tune-up Ph 1\\3 Cx\\assets\\co2.mplot";

        string contents = File.ReadAllText(filename, Encoding.UTF8);

        AntlrInputStream inputStream = new AntlrInputStream(contents);
        MplotLexer lexer = new MplotLexer(inputStream);

        CommonTokenStream tokenStream = new CommonTokenStream(lexer);

        MplotParser parser = new MplotParser(tokenStream);

        var parsedFile = parser.mplot();

        MplotListener listener = new MplotListener(NoneMarker.None, new List<string>());

        ParseTreeWalker walker = new ParseTreeWalker();

        walker.Walk(listener, parsedFile);
        string writeGnuplot = listener.WriteGnuplot();
        Console.Write(writeGnuplot);
    }

    [Test]
    public void EvaluationTest()
    {
        TestDataSource testDataSource = new TestDataSource();

        var colSpec1 = new Atom( new ExpressionColSpec( new ColNumSpec(1)));
        var colSpec2 = new Atom( new ExpressionColSpec( new ColNumSpec(2)));

        List<Atom> atoms = new List<Atom>()
        {
            colSpec1,
            colSpec2,
            new(OneOf<Operator, ExpressionColSpec>.FromT0(Operator.Add))
        };

        ExpressionArray result = Evaluator.Evaluate(atoms, testDataSource);

        if (result is DoubleArray dArray)
        {
            foreach (var d in dArray.Array) Console.Write(d + "\n");
        }
    }

    [Test]
    public void EvaluationTest2()
    {
        TestDataSource testDataSource = new TestDataSource();

        string expression = "$1 $2 +";
        AntlrInputStream inputStream = new AntlrInputStream(expression);
        MplotLexer lexer = new(inputStream);
        CommonTokenStream tokenStream = new CommonTokenStream(lexer);
        MplotParser parser = new MplotParser(tokenStream);
        var expressionTree = parser.expression();
        ExpressionListener expListener = new ExpressionListener(new());
        ParseTreeWalker walker = new ParseTreeWalker();
        walker.Walk(expListener, expressionTree);

        foreach (var token in expListener.Tokens)
        {
            Console.Write($"{token.TokenType}: {token.Contents}\n");
        }

        var result = Evaluator.Evaluate(expListener.Tokens, testDataSource);
        if (result is DoubleArray dArray)
        {
            foreach (var d in dArray.Array) Console.Write(d + "\n");
        }
    }

    [Test]
    [Ignore("One off")]
    public void PathTests()
    {
        string path1 = "C:\\Users\\mpaulus\\test.txt";
        string otherDir = "D:\\sample";

        string fullPath = Path.GetFullPath(path1, otherDir);
        Console.WriteLine(fullPath);
    }

    [Test]
    public void CustomDash_InvalidCharacter_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CustomDash("abc"));
    }

    [Test]
    public void CustomDash_ToGnuplot_WritesPattern()
    {
        CustomDash dash = new(".- _");
        Assert.That(dash.ToGnuplot(), Is.EqualTo("dashtype \".- _\""));
    }

    [Test]
    public void TextLabel_WritesSetLabel()
    {
        const string script = @"start
1 2
end
s ""Series 1""
x 1
y 2
text ""My Label"" 10 20
text ""Second"" 30 40
";
        var listener = ParseScript(script);
        string gnuplot = listener.WriteGnuplot();
        Assert.That(gnuplot, Does.Contain("set label 1 \"My Label\" at 10,20"));
        Assert.That(gnuplot, Does.Contain("set label 2 \"Second\" at 30,40"));
    }

    [Test]
    public void TextLabel_AllowsStringAfterCoordinates()
    {
        const string script = @"start
1 2
end
s ""Series 1""
x 1
y 2
text 15 25 ""Later""
";
        var listener = ParseScript(script);
        string gnuplot = listener.WriteGnuplot();
        Assert.That(gnuplot, Does.Contain("set label 1 \"Later\" at 15,25"));
    }

    private static MplotListener ParseScript(string script)
    {
        AntlrInputStream inputStream = new(script);
        MplotLexer lexer = new(inputStream);
        CommonTokenStream tokenStream = new(lexer);
        MplotParser parser = new(tokenStream);
        var parsedFile = parser.mplot();
        MplotListener listener = new(NoneMarker.None, new List<string>());
        ParseTreeWalker walker = new();
        walker.Walk(listener, parsedFile);
        return listener;
    }
}
