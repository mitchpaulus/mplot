using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using mplot;
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
    public void Test1()
    {
        // string filename = "/home/mitch/repos/mplot/tests/test1.mplot";
        string filename = "C:\\Users\\mpaulus\\source\\repos\\mplot\\tests\\expressions.mplot";

        string contents = File.ReadAllText(filename, Encoding.UTF8);

        AntlrInputStream inputStream = new AntlrInputStream(contents);
        MplotLexer lexer = new MplotLexer(inputStream);

        CommonTokenStream tokenStream = new CommonTokenStream(lexer);

        MplotParser parser = new MplotParser(tokenStream);

        var parsedFile = parser.mplot();

        MplotListener listener = new MplotListener();

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
        ExpressionListener expListener = new ExpressionListener();
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
}