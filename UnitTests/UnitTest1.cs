using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using mplot;
using NUnit.Framework;

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
        string filename = "/home/mitch/repos/mplot/tests/test1.mplot";


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
}