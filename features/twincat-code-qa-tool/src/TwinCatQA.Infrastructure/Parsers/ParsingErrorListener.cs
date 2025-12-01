using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;
using TwinCatQA.Domain.Contracts;

namespace TwinCatQA.Infrastructure.Parsers;

/// <summary>
/// ANTLR4 파싱 오류를 수집하는 리스너
/// </summary>
internal class ParsingErrorListener : BaseErrorListener
{
    private readonly List<ParsingError> _errors;

    public ParsingErrorListener(List<ParsingError> errors)
    {
        _errors = errors;
    }

    public override void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        _errors.Add(new ParsingError
        {
            Line = line,
            Column = charPositionInLine,
            Message = msg,
            OffendingSymbol = offendingSymbol?.Text ?? ""
        });
    }
}
