using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;

namespace Arcweave.Interpreter
{
public class ErrorListener<S>: ConsoleErrorListener<S>
{
    public bool HasErrors = false;
    public List<string> Errors = new List<string>();
    
    public override void SyntaxError(TextWriter output, IRecognizer recognizer, S offendingSymbol, int line, int charPositionInLine,
        string msg, RecognitionException e)
    {
        HasErrors = true;
        Errors.Add($"line {line}:{charPositionInLine} {msg}");
        base.SyntaxError(output, recognizer, offendingSymbol, line, charPositionInLine, msg, e);
    }
}
}

