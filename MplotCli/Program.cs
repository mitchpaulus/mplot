using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using MplotLib;

namespace MplotCli;

internal static class Program
{
    private const string Version = "0.2.0";

    private static int Main(string[] args)
    {
        int i = 0;

        List<string> files = new();
        bool compile = false;
        bool toGnuplotFile = false;
        bool printDependencies = false;
        bool printHeaders = false;
        List<string> stringArgs = new();

        while (i < args.Length)
        {
            switch (args[i])
            {
                case "-h":
                case "--help":
                    PrintHelp();
                    return 0;
                case "-V":
                case "--version":
                    Console.WriteLine(Version);
                    return 0;
                case "-c":
                    compile = true;
                    break;
                case "-g":
                    toGnuplotFile = true;
                    break;
                case "-d":
                    printDependencies = true;
                    break;
                case "--headers":
                    printHeaders = true;
                    break;
                case "-s":
                    if (args.Length <= i + 1)
                    {
                        Console.Error.WriteLine("No file provided after -s");
                        return 1;
                    }

                    i++;
                    stringArgs.Add(args[i]);
                    break;
                default:
                    files.Add(args[i]);
                    break;
            }

            i++;
        }

        if (!files.Any() && !Console.IsInputRedirected)
        {
            Console.Error.WriteLine("No files found.");
            return 1;
        }

        string contents;
        Maybe<FileInfo> fileInfo;

        if (files.Count == 0 || files[0] == "-")
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
                Console.Error.WriteLine($"The file '{filePath}' was not found.");
                return 1;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"An error occurred while reading the file '{filePath}'.");
                Console.Error.WriteLine(e.Message);
                return 1;
            }
        }

        AntlrInputStream inputStream = new(contents);
        MplotLexer lexer = new(inputStream);
        ErrorListener errorListener = new();
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(errorListener);

        CommonTokenStream tokenStream = new(lexer);
        MplotParser parser = new(tokenStream);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(errorListener);
        var parsedFile = parser.mplot();

        if (errorListener.Messages.Count > 0)
        {
            foreach (var error in errorListener.Messages)
            {
                Console.Error.Write(error);
                if (!error.EndsWith('\n'))
                {
                    Console.Error.Write('\n');
                }
            }

            return 1;
        }

        ParseTreeWalker walker = new();

        List<string> dependencies = new();

        foreach (var plot in parsedFile.plot())
        {
            MplotListener listener = new(fileInfo, stringArgs);

            try
            {
                walker.Walk(listener, plot);

                var checks = listener.GnuplotCheck();
                if (checks.Any())
                {
                    foreach (var check in checks)
                    {
                        Console.Error.Write(check);
                    }

                    return 1;
                }

                if (compile)
                {
                    string writeGnuplot = listener.WriteGnuplot();
                    ProcessStartInfo startInfo = new()
                    {
                        FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "gnuplot.exe" : "gnuplot",
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using Process process = new() { StartInfo = startInfo };

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

                        if (!Console.IsOutputRedirected)
                        {
                            if (fileInfo.IsNone)
                            {
                                Console.Error.WriteLine("Not going to dump PNG contents to TTY. Please redirect to desired file.");
                                return 1;
                            }

                            if (fileInfo.AsValue.Name.EndsWith(".mplot", StringComparison.OrdinalIgnoreCase))
                            {
                                using FileStream fStream = new(fileInfo.AsValue.FullName[..^6] + ".png", FileMode.Create);
                                process.StandardOutput.BaseStream.CopyTo(fStream);
                            }
                            else
                            {
                                using FileStream fStream = new(fileInfo.AsValue.FullName + ".png", FileMode.Create);
                                process.StandardOutput.BaseStream.CopyTo(fStream);
                            }
                        }
                        else
                        {
                            Console.Write(process.StandardOutput.ReadToEnd());
                        }

                        process.WaitForExit();
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine($"There was an error compiling in gnuplot.\n{e.Message}");
                        return process.ExitCode;
                    }
                }
                else if (toGnuplotFile)
                {
                    string gnuplotFilename = fileInfo.Match(
                        info => info.FullName.EndsWith(".mplot", StringComparison.OrdinalIgnoreCase)
                            ? info.FullName[..^5] + "gnuplot"
                            : info.FullName + ".gnuplot",
                        _ => "stdin.gnuplot"
                    );

                    Encoding e = new UTF8Encoding(false);
                    string writeGnuplot = listener.WriteGnuplot();
                    File.WriteAllText(gnuplotFilename, writeGnuplot, e);
                }
                else if (printDependencies)
                {
                    var fileSources = listener.FilePathArray.Elements()
                        .Where(source => source.IsFileSource)
                        .Select(source => source.AsT0)
                        .ToList();

                    foreach (var fs in fileSources)
                    {
                        if (!dependencies.Contains(fs.Filepath)) dependencies.Add(fs.Filepath);
                    }
                }
                else if (printHeaders)
                {
                    var fileSources = listener.FilePathArray.Elements()
                        .Where(source => source.IsFileSource)
                        .Select(source => source.AsT0)
                        .ToList();

                    HashSet<string> headers = new(StringComparer.Ordinal);
                    foreach (var fs in fileSources)
                    {
                        try
                        {
                            if (fs.Filepath.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase))
                            {
                                var headerLine = File.ReadLines(fs.Filepath, Encoding.UTF8).Take(1).ToList();
                                if (headerLine.Count > 0)
                                {
                                    foreach (var head in headerLine[0].Split('\t')) headers.Add(head);
                                }
                            }
                            else if (fs.Filepath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                            {
                                var headerLine = File.ReadLines(fs.Filepath, Encoding.UTF8).Take(1).ToList();
                                CsvReader csvReader = new();
                                if (headerLine.Count > 0)
                                {
                                    var cells = csvReader.ParseCsvLine(headerLine[0]);
                                    foreach (var head in cells) headers.Add(head);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine(e.Message);
                            return 1;
                        }
                    }

                    foreach (var head in headers.OrderByNatural())
                    {
                        Console.Write(head);
                        Console.Write('\n');
                    }
                }
                else
                {
                    Console.Write(listener.WriteGnuplot());
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
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

    private static void PrintHelp()
    {
        Console.Write("mplot - plotting DSL\n\n");
        Console.Write("USAGE:\n");
        Console.Write("mplot [-g] [-s STRING]* FILE\n");
        Console.Write("mplot -d FILE\n");
        Console.Write("mplot -h | --help\n");
        Console.Write("mplot --headers FILE\n");
        Console.Write("mplot -V | --version\n");
        Console.Write('\n');
        Console.Write("OPTIONS\n");
        Console.Write(" -c        Pipe directly to gnuplot to compile.\n");
        Console.Write(" -d        Print dependencies\n");
        Console.Write(" -g        Write directly to gnuplot file of same name.\n");
        Console.Write(" -s STRING Add a string argument (used as sargXX in code)\n");
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
        Console.Write("red green blue black orange ccblue ccgray ccmaroon ccgreen ccyellow {- named -}\n");
    }
}
