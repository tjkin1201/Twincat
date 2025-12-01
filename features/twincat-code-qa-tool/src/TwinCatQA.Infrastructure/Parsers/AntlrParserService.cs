using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Antlr4.Runtime;
using TwinCatQA.Domain.Contracts;
using TwinCatQA.Domain.Models;
using ASTSyntaxTree = TwinCatQA.Domain.Models.AST.SyntaxTree;
using ASTParsingError = TwinCatQA.Domain.Models.AST.ParsingError;

namespace TwinCatQA.Infrastructure.Parsers
{
    /// <summary>
    /// ANTLR4 기반 파서 서비스
    ///
    /// TwinCAT XML 파일을 파싱하고 ST 코드를 추출하여 ANTLR4로 구문 분석합니다.
    /// IEC 61131-3 Structured Text 언어를 지원합니다.
    /// </summary>
    public class AntlrParserService : IParserService
    {
        // ANTLR4 생성 파일은 수동으로 컴파일 후 추가
        // java -jar antlr-4.11.1-complete.jar -Dlanguage=CSharp StructuredText.g4
        // 생성 파일: StructuredTextLexer.cs, StructuredTextParser.cs, StructuredTextVisitor.cs

        private readonly Regex _koreanPattern = new Regex(@"[\uAC00-\uD7A3]", RegexOptions.Compiled);
        private const int MinKoreanCharacters = 10; // 한글 주석 최소 글자 수

        /// <summary>
        /// TwinCAT 파일을 파싱하여 구문 트리를 생성합니다.
        /// </summary>
        /// <param name="filePath">파일 절대 경로 (.TcPOU, .TcDUT, .TcGVL)</param>
        /// <returns>구문 트리 (AST)</returns>
        /// <exception cref="FileNotFoundException">파일이 존재하지 않을 때</exception>
        /// <exception cref="InvalidOperationException">파싱 오류 발생 시</exception>
        public SyntaxTree ParseFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"파일을 찾을 수 없습니다: {filePath}");
            }

            var syntaxTree = new SyntaxTree
            {
                FilePath = filePath,
                Errors = new List<ParsingError>(),
                SourceCode = ""
            };

            try
            {
                // TwinCAT XML 파일에서 ST 코드 추출
                string sourceCode = ExtractStructuredTextFromXml(filePath);
                syntaxTree.SourceCode = sourceCode;

                if (string.IsNullOrWhiteSpace(sourceCode))
                {
                    syntaxTree.Errors.Add(new ParsingError
                    {
                        Line = 0,
                        Column = 0,
                        Message = "파일에서 ST 코드를 찾을 수 없습니다.",
                        OffendingSymbol = ""
                    });
                    return syntaxTree;
                }

                // ANTLR4 파서로 파싱
                var inputStream = new AntlrInputStream(sourceCode);
                var lexer = new StructuredTextLexer(inputStream);
                var tokenStream = new CommonTokenStream(lexer);
                var parser = new StructuredTextParser(tokenStream);

                // 오류 리스너 등록
                parser.RemoveErrorListeners();
                parser.AddErrorListener(new ParsingErrorListener(syntaxTree.Errors));

                // 파싱 실행 (Parse Tree 생성)
                var programContext = parser.program();
                syntaxTree.RootNode = programContext;

                // AST 생성 (선택적 - 향후 고급 분석용)
                var astSyntaxTree = new ASTSyntaxTree
                {
                    FilePath = filePath,
                    SourceCode = sourceCode,
                    Errors = new List<ASTParsingError>()
                };

                var astBuilder = new ASTBuilderVisitor(filePath);
                astSyntaxTree.RootNodes = programContext.programUnit()
                    .Select(pu => astBuilder.Visit(pu))
                    .Where(node => node != null)
                    .ToList();

                // 의미 분석
                var semanticAnalyzer = new SemanticAnalyzer();
                semanticAnalyzer.Analyze(astSyntaxTree);

                // 의미 오류를 파싱 오류에 추가
                foreach (var semanticError in semanticAnalyzer.Errors)
                {
                    syntaxTree.Errors.Add(new ParsingError
                    {
                        Line = semanticError.Line,
                        Column = semanticError.Column,
                        Message = $"[의미 오류] {semanticError.Message}",
                        OffendingSymbol = ""
                    });
                }
            }
            catch (Exception ex)
            {
                syntaxTree.Errors.Add(new ParsingError
                {
                    Line = 0,
                    Column = 0,
                    Message = $"파싱 중 오류 발생: {ex.Message}",
                    OffendingSymbol = ""
                });
            }

            return syntaxTree;
        }

        /// <summary>
        /// AST에서 Function Block 목록을 추출합니다.
        /// </summary>
        /// <param name="ast">구문 트리</param>
        /// <returns>Function Block 목록</returns>
        public List<FunctionBlock> ExtractFunctionBlocks(SyntaxTree ast)
        {
            var functionBlocks = new List<FunctionBlock>();

            // TODO: ANTLR4 Visitor 패턴으로 구현
            // ANTLR 생성 파일 추가 후:
            // var visitor = new FunctionBlockExtractorVisitor();
            // visitor.Visit(ast.RootNode);
            // return visitor.FunctionBlocks;

            // 현재는 스켈레톤: 빈 리스트 반환
            return functionBlocks;
        }

        /// <summary>
        /// AST에서 변수 목록을 추출합니다.
        /// </summary>
        /// <param name="ast">구문 트리</param>
        /// <param name="scope">필터링할 변수 스코프 (선택적)</param>
        /// <returns>변수 목록</returns>
        public List<Variable> ExtractVariables(SyntaxTree ast, VariableScope? scope = null)
        {
            var variables = new List<Variable>();

            // TODO: ANTLR4 Visitor 패턴으로 구현
            // var visitor = new VariableExtractorVisitor(scope);
            // visitor.Visit(ast.RootNode);
            // return visitor.Variables;

            return variables;
        }

        /// <summary>
        /// AST에서 데이터 타입 목록을 추출합니다.
        /// </summary>
        /// <param name="ast">구문 트리</param>
        /// <returns>데이터 타입 목록</returns>
        public List<DataType> ExtractDataTypes(SyntaxTree ast)
        {
            var dataTypes = new List<DataType>();

            // TODO: ANTLR4 Visitor 패턴으로 구현
            // var visitor = new DataTypeExtractorVisitor();
            // visitor.Visit(ast.RootNode);
            // return visitor.DataTypes;

            return dataTypes;
        }

        /// <summary>
        /// Function Block의 사이클로매틱 복잡도를 계산합니다.
        /// </summary>
        /// <param name="fb">Function Block</param>
        /// <returns>복잡도 값 (기본값 1부터 시작)</returns>
        public int CalculateCyclomaticComplexity(FunctionBlock fb)
        {
            // TODO: CyclomaticComplexityVisitor 사용
            // 구현 완료 후:
            // var visitor = new CyclomaticComplexityVisitor();
            // return visitor.Calculate(fb.AstNode);

            // 현재는 기본값 반환
            return 1;
        }

        /// <summary>
        /// 코드에서 주석을 추출합니다.
        /// </summary>
        /// <param name="ast">구문 트리</param>
        /// <returns>주석 맵 (라인 번호 → 주석 내용)</returns>
        public Dictionary<int, string> ExtractComments(SyntaxTree ast)
        {
            var comments = new Dictionary<int, string>();

            if (string.IsNullOrEmpty(ast.SourceCode))
            {
                return comments;
            }

            // 정규식으로 주석 추출 (임시 구현)
            // ANTLR4 토큰 스트림에서 HIDDEN 채널의 COMMENT 토큰을 가져오는 것이 더 정확함

            var lines = ast.SourceCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                int lineNumber = i + 1;

                // 라인 주석 (//) 추출
                int lineCommentIndex = line.IndexOf("//");
                if (lineCommentIndex >= 0)
                {
                    string comment = line.Substring(lineCommentIndex + 2).Trim();
                    if (!string.IsNullOrWhiteSpace(comment))
                    {
                        comments[lineNumber] = comment;
                    }
                }
            }

            // 블록 주석 (* ... *) 추출 (간단한 구현)
            var blockCommentPattern = @"\(\*(.*?)\*\)";
            var blockMatches = Regex.Matches(ast.SourceCode, blockCommentPattern, RegexOptions.Singleline);

            foreach (Match match in blockMatches)
            {
                // 블록 주석의 첫 번째 라인 번호 찾기
                int startIndex = match.Index;
                int lineNumber = ast.SourceCode.Substring(0, startIndex).Split('\n').Length;

                string comment = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(comment))
                {
                    comments[lineNumber] = comment;
                }
            }

            return comments;
        }

        /// <summary>
        /// 주석이 한글인지 검증합니다.
        /// </summary>
        /// <param name="comment">주석 내용</param>
        /// <returns>한글이 충분히 포함되어 있으면 true</returns>
        public bool IsKoreanComment(string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
            {
                return false;
            }

            // 한글 문자 개수 카운트
            var matches = _koreanPattern.Matches(comment);
            return matches.Count >= MinKoreanCharacters;
        }

        /// <summary>
        /// 파싱 오류 목록을 반환합니다.
        /// </summary>
        /// <param name="ast">구문 트리</param>
        /// <returns>오류 목록</returns>
        public List<ParsingError> GetParsingErrors(SyntaxTree ast)
        {
            return ast.Errors ?? new List<ParsingError>();
        }

        // ----------------------------------------------------------------------------
        // Private 헬퍼 메서드
        // ----------------------------------------------------------------------------

        /// <summary>
        /// TwinCAT XML 파일에서 Structured Text 코드를 추출합니다.
        /// Declaration(선언부)과 Implementation/ST(구현부)를 모두 추출하여 결합합니다.
        /// </summary>
        /// <param name="filePath">TwinCAT 파일 경로</param>
        /// <returns>ST 소스 코드 (Declaration + Implementation)</returns>
        private string ExtractStructuredTextFromXml(string filePath)
        {
            try
            {
                var doc = XDocument.Load(filePath);
                var extractedParts = new List<string>();

                // TwinCAT POU 파일 구조:
                // <TcPlcObject>
                //   <POU Name="FB_Example">
                //     <Declaration><![CDATA[ FUNCTION_BLOCK FB_Example VAR ... END_VAR ]]></Declaration>
                //     <Implementation>
                //       <ST><![CDATA[ ... ST 코드 ... ]]></ST>
                //     </Implementation>
                //   </POU>
                // </TcPlcObject>

                // 1. Declaration 섹션 추출 (PROGRAM, FUNCTION_BLOCK, VAR 선언 등)
                var declarationElements = doc.Descendants("Declaration").ToList();
                if (declarationElements.Any())
                {
                    var declarations = declarationElements
                        .Select(e => e.Value)
                        .Where(v => !string.IsNullOrWhiteSpace(v));
                    extractedParts.AddRange(declarations);
                }

                // 2. Implementation/ST 섹션 추출 (실제 로직 코드)
                var stElements = doc.Descendants("ST").ToList();
                if (stElements.Any())
                {
                    var stCodes = stElements
                        .Select(e => e.Value)
                        .Where(v => !string.IsNullOrWhiteSpace(v));
                    extractedParts.AddRange(stCodes);
                }

                // 추출된 내용이 있으면 결합하여 반환
                if (extractedParts.Any())
                {
                    return string.Join("\n\n", extractedParts);
                }

                // GVL 파일 등은 Declaration만 있을 수 있음
                return string.Empty;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"XML 파일에서 ST 코드 추출 실패: {ex.Message}", ex);
            }
        }
    }

    // TODO: ANTLR4 오류 리스너 구현 (ANTLR 생성 파일 추가 후 활성화)
    /*
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
    */
}
