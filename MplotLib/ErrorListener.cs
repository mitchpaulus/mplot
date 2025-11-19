using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;

namespace MplotLib;

public class ErrorListener : IAntlrErrorListener<IToken>, IAntlrErrorListener<int>
{
    public List<string> Messages { get; } = new();

    public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        Messages.Add($"{line}:{charPositionInLine}: {msg}");
    }

    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        Messages.Add($"{line}:{charPositionInLine}: {msg}");
    }
}
