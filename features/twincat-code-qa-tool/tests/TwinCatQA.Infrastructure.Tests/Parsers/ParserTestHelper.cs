using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace TwinCatQA.Infrastructure.Tests.Parsers
{
    /// <summary>
    /// 파서 테스트를 위한 헬퍼 클래스
    /// </summary>
    public static class ParserTestHelper
    {
        /// <summary>
        /// ST 코드를 파싱하여 파스 트리 반환
        /// </summary>
        public static IParseTree ParseCode(string code)
        {
            var inputStream = new AntlrInputStream(code);
            var lexer = new StructuredTextLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new StructuredTextParser(tokenStream);

            // 오류 리스너 제거 (테스트에서는 예외 발생)
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ThrowingErrorListener());

            return parser.program();
        }

        /// <summary>
        /// 단일 문장을 파싱
        /// </summary>
        public static StructuredTextParser.StatementContext ParseStatement(string code)
        {
            // 문장만 파싱하기 위해 PROGRAM으로 감싸기
            var wrappedCode = $@"
PROGRAM Test
{code}
END_PROGRAM
";
            var inputStream = new AntlrInputStream(wrappedCode);
            var lexer = new StructuredTextLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new StructuredTextParser(tokenStream);

            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ThrowingErrorListener());

            var program = parser.program();
            var programDecl = program.programUnit(0).programDeclaration();
            return programDecl.statement(0);
        }

        /// <summary>
        /// 표현식을 파싱
        /// </summary>
        public static StructuredTextParser.ExpressionContext ParseExpression(string code)
        {
            // 표현식을 할당문으로 감싸서 파싱
            var statement = ParseStatement($"temp := {code};");

            // StatementContext에서 assignmentStatement() 메서드로 접근
            var assignStmt = statement.assignmentStatement();
            if (assignStmt != null)
            {
                return assignStmt.expression();
            }

            throw new InvalidOperationException("표현식 파싱 실패");
        }

        /// <summary>
        /// 변수 선언을 파싱
        /// </summary>
        public static StructuredTextParser.VarDeclarationContext ParseVarDeclaration(string code)
        {
            var wrappedCode = $@"
PROGRAM Test
{code}
END_PROGRAM
";
            var inputStream = new AntlrInputStream(wrappedCode);
            var lexer = new StructuredTextLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new StructuredTextParser(tokenStream);

            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ThrowingErrorListener());

            var program = parser.program();
            var programDecl = program.programUnit(0).programDeclaration();
            return programDecl.varDeclaration(0);
        }

        /// <summary>
        /// 파싱 오류가 발생하는지 검증
        /// </summary>
        public static bool HasParsingError(string code)
        {
            try
            {
                ParseCode(code);
                return false;
            }
            catch (ParseCanceledException)
            {
                return true;
            }
            catch (Exception)
            {
                return true;
            }
        }

        /// <summary>
        /// 리터럴 값을 추출
        /// </summary>
        public static object GetLiteralValue(StructuredTextParser.ExpressionContext expr)
        {
            if (expr.literal() != null)
            {
                var literal = expr.literal();

                if (literal.INTEGER_LITERAL() != null)
                {
                    return int.Parse(literal.INTEGER_LITERAL().GetText());
                }

                if (literal.REAL_LITERAL() != null)
                {
                    return double.Parse(literal.REAL_LITERAL().GetText());
                }

                if (literal.STRING_LITERAL() != null)
                {
                    return literal.STRING_LITERAL().GetText().Trim('\'');
                }

                if (literal.BOOLEAN_LITERAL() != null)
                {
                    return literal.BOOLEAN_LITERAL().GetText().ToUpper() == "TRUE";
                }
            }

            throw new InvalidOperationException("리터럴 값이 아닙니다");
        }

        /// <summary>
        /// 이진 연산자 추출
        /// </summary>
        public static string GetBinaryOperator(StructuredTextParser.ExpressionContext expr)
        {
            if (expr.ChildCount >= 3 && expr.GetChild(1) is ITerminalNode opNode)
            {
                return opNode.GetText();
            }

            // AND, OR, XOR, MOD 같은 키워드 연산자
            for (int i = 0; i < expr.ChildCount; i++)
            {
                var child = expr.GetChild(i);
                if (child is ITerminalNode terminal)
                {
                    var text = terminal.GetText().ToUpper();
                    if (text == "AND" || text == "OR" || text == "XOR" ||
                        text == "MOD" || text == "NOT")
                    {
                        return text;
                    }
                }
            }

            throw new InvalidOperationException("이진 연산자를 찾을 수 없습니다");
        }

        /// <summary>
        /// 함수/FB 이름 추출
        /// </summary>
        public static string GetFunctionBlockName(IParseTree tree)
        {
            if (tree is StructuredTextParser.ProgramContext program)
            {
                foreach (var unit in program.programUnit())
                {
                    if (unit.functionBlockDeclaration() != null)
                    {
                        return unit.functionBlockDeclaration().IDENTIFIER().GetText();
                    }
                    if (unit.programDeclaration() != null)
                    {
                        return unit.programDeclaration().IDENTIFIER().GetText();
                    }
                    if (unit.functionDeclaration() != null)
                    {
                        return unit.functionDeclaration().IDENTIFIER().GetText();
                    }
                }
            }

            throw new InvalidOperationException("함수/FB 이름을 찾을 수 없습니다");
        }

        /// <summary>
        /// 모든 문장 개수 카운트
        /// </summary>
        public static int CountStatements(StructuredTextParser.ProgramDeclarationContext program)
        {
            return program.statement()?.Length ?? 0;
        }

        /// <summary>
        /// 변수 선언 개수 카운트
        /// </summary>
        public static int CountVariableDeclarations(StructuredTextParser.FunctionBlockDeclarationContext fb)
        {
            return fb.varDeclaration()?.Length ?? 0;
        }
    }

    /// <summary>
    /// 파싱 오류 시 예외를 던지는 리스너
    /// </summary>
    internal class ThrowingErrorListener : BaseErrorListener
    {
        public override void SyntaxError(
            System.IO.TextWriter output,
            IRecognizer recognizer,
            IToken offendingSymbol,
            int line,
            int charPositionInLine,
            string msg,
            RecognitionException e)
        {
            throw new ParseCanceledException(
                $"라인 {line}:{charPositionInLine} - {msg}");
        }
    }
}
