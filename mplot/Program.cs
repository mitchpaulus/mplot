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
        bool toGnuplotFile = false;
        bool printDependencies = false;
        List<string> stringArgs = new();

        while (i < args.Length)
        {
            if (args[i] == "-h" || args[i] == "--help")
            {
                Console.Write("mplot - plotting DSL\n\n");
                Console.Write("USAGE:\n");
                Console.Write("mplot [-g] FILE\n");
                Console.Write("mplot -h | --help\n");
                Console.Write("mplot -V | --version\n");
                Console.Write('\n');
                Console.Write("OPTIONS\n");
                Console.Write(" -c  Pipe directly to gnuplot to compile.\n");
                Console.Write(" -d  Print dependencies\n");
                Console.Write(" -g  Write directly to gnuplot file of same name.\n");
                Console.Write('\n');
                Console.Write('\n');
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
                Console.Write("0.2.0\n");
                return 0;
            }

            if (args[i] is "-c")
            {
                compile = true;
            }
            else if (args[i] is "-g")
            {
                toGnuplotFile = true;
            }
            else if (args[i] is "-d")
            {
                printDependencies = true;
            }
            else if (args[i] == "-s")
            {
                if (args.Length <= i + 1)
                {
                    Console.Error.Write("No file provided after -s\n");
                    return 1;
                }
                i++;
                stringArgs.Add(args[i]);
            }
            else
            {
                files.Add(args[i]);
            }

            i++;
        }

        // If no files are provided and standard input is still a terminal then print help
        if (!files.Any() && !Console.IsInputRedirected)
        {
            Console.Error.Write("No files found.\n");
            return 1;
        }

        string contents;
        Maybe<FileInfo> fileInfo;
        // FileInfo? fileInfo = null;

        if (files.Count == 0 || files[0] == "-") // Read from standard input
        {
            contents = Console.In.ReadToEnd();
            fileInfo = NoneMarker.None;
        }
        else
        {
            string filePath = files[0];
            fileInfo = new FileInfo(filePath);

            try
            {
                contents = File.ReadAllText(filePath, Encoding.UTF8);
            }
            catch (FileNotFoundException)
            {
                Console.Error.Write($"The file '{filePath}' was not found.\n");
                return 1;
            }
            catch (Exception e)
            {
                Console.Error.Write($"An error occurred while reading the file '{filePath}'.\n{e.Message}\n");
                return 1;
            }
        }

        AntlrInputStream inputStream = new AntlrInputStream(contents);
        MplotLexer lexer = new MplotLexer(inputStream);

        CommonTokenStream tokenStream = new CommonTokenStream(lexer);

        MplotParser parser = new MplotParser(tokenStream);

        var parsedFile = parser.mplot();

        ParseTreeWalker walker = new ParseTreeWalker();

        List<string> dependencies = new();

        foreach (var plot in parsedFile.plot())
        {
            MplotListener listener = new MplotListener(fileInfo, stringArgs);

            try
            {
                walker.Walk(listener, plot);

                var checks = listener.GnuplotCheck();
                if (checks.Any())
                {
                    foreach (var check in checks) Console.Write(check);
                    return 1;
                }

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
                else if (toGnuplotFile)
                {
                    string gnuplotFilename = fileInfo.Match(
                        info => info.FullName.EndsWith(".mplot")
                                    ? info.FullName[..^5] + "gnuplot"
                                    : info.FullName + ".gnuplot",
                        _ => "stdin.gnuplot"
                    );

                    Encoding e = new UTF8Encoding(false);
                    File.WriteAllText(gnuplotFilename, writeGnuplot, e);
                }
                else if (printDependencies)
                {
                    var fileSources = listener.FilePathArray.Elements().Where(source => source.IsFileSource).Select(source => source.AsT0).ToList();

                    foreach (var fs in fileSources)
                    {
                        if (!dependencies.Contains(fs.Filepath)) dependencies.Add(fs.Filepath);
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

        if (printDependencies)
        {
            dependencies.Sort();
            foreach (var d in dependencies)
            {
                Console.Write(d);
                Console.Write('\n');
            }
        }

        return 0;
    }
}
