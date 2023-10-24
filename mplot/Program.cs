using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace mplot;

class Program
{
    public static int Main(string[] args)
    {
        int i = 0;

        List<string> files = new();
        bool compile = false;
        while (i < args.Length)
        {
            if (args[i] == "-h" || args[i] == "--help")
            {
                Console.Write("mplot - plotting DSL\n\n");
                Console.Write("USAGE:\n");
                Console.Write("mplot FILE\n");
                Console.Write("mplot -h | --help\n");
                Console.Write("mplot -V | --version\n");

                Console.Write("\n");
                Console.Write("Example syntax:\n");
                Console.Write("f \"file.tsv\"\n");
                Console.Write("font \"Arial\"\n");
                Console.Write("w 6\n");
                Console.Write("h 3-1/2 {- Rational numbers are used -}\n");
                Console.Write("fs 8 {- fontsize -}\n");
                Console.Write("dpi 300\n");
                Console.Write("out \"plot.png\"\n");
                Console.Write("\n");
                Console.Write("{- data formats -}\n");
                Console.Write("sep \",\" {- or -}\n");
                Console.Write("tab\n");
                Console.Write(",\n");
                Console.Write("\n");
                Console.Write("ct \"Chart Title\"\n");
                Console.Write("\n");
                Console.Write("xl \"X Axis Label\"\n");
                Console.Write("yl \"Y Axis Label\"\n");
                Console.Write("\n");
                Console.Write("x 1 {- x column -}\n");
                Console.Write("y 2 {- y column -}\n");
                Console.Write("{- Can also do it by name -}\n");
                Console.Write("\n");
                Console.Write("g {- grid -}\n");
                Console.Write("\n");
                Console.Write("xr 0:100 {- x range -}\n");
                Console.Write("xr :100\n");
                Console.Write("xr 0:\n");
                Console.Write("yr 0 to 10\n");
                Console.Write("\n");
                Console.Write("{- marker types -}\n");
                Console.Write("o + -\n");
                Console.Write("\n");
                Console.Write("s \"Series 1\" \"Series 2\"\n");
                Console.Write("\n");
                Console.Write("lw 2 {- line width -}\n");
                Console.Write("\n");
                Console.Write("{- time axis settings -}\n");
                Console.Write("time\n");
                Console.Write("tf \"%Y-%m-%d\"\n");
                Console.Write("isodate {- for \"%Y-%m-%d\" }\n");
                Console.Write("\n");
                Console.Write("{- legend locations -}\n");
                Console.Write("top right ul upperleft ur upperright\n");
                Console.Write("\n");
                Console.Write("{- colors -}\n");
                Console.Write("#000000 {- hex -}\n");
                Console.Write("rgb 123 124 125 {- rgb -}\n");
                Console.Write("red green blue ccblue ccgray ccmaroon ccgreen ccyellow {- named -}\n");

                return 0;
            }

            if (args[i] is "-V" or "--version")
            {
                Console.Write("0.1.0\n");
                return 0;
            }

            if (args[i] is "-c")
            {
                compile = true;
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

        foreach (var plot in parsedFile.plot())
        {
            MplotListener listener = new MplotListener();
            ParseTreeWalker walker = new ParseTreeWalker();

            try
            {
                walker.Walk(listener, plot);
                string writeGnuplot = listener.WriteGnuplot();

                if (compile)
                {
                    // Attempt to pipe string into gnuplot
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "gnuplot.exe" : "gnuplot";
                    startInfo.RedirectStandardInput = true;
                    startInfo.UseShellExecute = false;
                    startInfo.CreateNoWindow = true;

                    using Process process = new Process();
                    process.StartInfo = startInfo;

                    try
                    {
                        process.Start();

                        using (StreamWriter w = process.StandardInput)
                        {
                            if (w.BaseStream.CanWrite)
                            {
                                w.Write(writeGnuplot);
                            }
                        }

                        process.WaitForExit();
                    }
                    catch (Exception e)
                    {
                        Console.Error.Write($"There was an error compiling in gnuplot.\n{e.Message}\n");
                        return process.ExitCode;
                    }
                }
                else
                {
                    Console.Write(writeGnuplot);
                }
            }
            catch (Exception e)
            {
                Console.Error.Write(e.Message + "\n");
                return 1;
            }
        }

        return 0;
    }
}
