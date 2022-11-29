using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace mplot;

class Program
{
    public static int Main(string[] args)
    {
        int i = 0;

        List<string> files = new List<string>();
        while (i < args.Length)
        {
            if (args[i] == "-h" || args[i] == "--help")
            {
                Console.Write("mplot - plotting DSL\n\n");
                Console.Write("USAGE:\n");
                Console.Write("mplot file...\n");
                return 0;
            }
            else
            {
                files.Add(args[i]);
            }

            i++;
        }

        if (!files.Any())
        {
            Console.Error.Write("No files found.\n");
            return 1;
        }


        string filePath = files[0];

        FileInfo fileInfo = new FileInfo(filePath);

        if (!fileInfo.Exists)
        {
            Console.Error.Write($"The file '{fileInfo.FullName}' does not exist.\n");
            return 1;
        }

        string contents = File.ReadAllText(fileInfo.FullName, Encoding.UTF8);

        AntlrInputStream inputStream = new AntlrInputStream(contents);
        MplotLexer lexer = new MplotLexer(inputStream);

        CommonTokenStream tokenStream = new CommonTokenStream(lexer);

        MplotParser parser = new MplotParser(tokenStream);

        var parsedFile = parser.mplot();

        MplotListener listener = new MplotListener();

        ParseTreeWalker walker = new ParseTreeWalker();

        try
        {
            walker.Walk(listener, parsedFile);
            string writeGnuplot = listener.WriteGnuplot();
            Console.Write(writeGnuplot);
        }
        catch (Exception e)
        {
            Console.Error.Write(e.Message + "\n");
            return 1;
        }


        return 0;
    }
}
