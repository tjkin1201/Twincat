using System.Collections.Generic;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Domain.Contracts
{
    /// <summary>
    /// 파서 서비스 인터페이스
    ///
    /// TwinCAT 파일을 파싱하고 AST를 생성합니다.
    /// ANTLR4 기반 ST 파서와 LINQ to XML 기반 TwinCAT XML 파서를 통합합니다.
    /// </summary>
    public interface IParserService
    {
        /// <summary>
        /// TwinCAT 파일을 파싱하여 AST를 생성합니다.
        /// </summary>
        /// <param name="filePath">파일 절대 경로</param>
        /// <returns>파싱된 구문 트리</returns>
        SyntaxTree ParseFile(string filePath);

        /// <summary>
        /// AST에서 Function Block 목록을 추출합니다.
        /// </summary>
        /// <param name="ast">구문 트리</param>
        /// <returns>Function Block 목록</returns>
        List<FunctionBlock> ExtractFunctionBlocks(SyntaxTree ast);

        /// <summary>
        /// AST에서 변수 목록을 추출합니다.
        /// </summary>
        /// <param name="ast">구문 트리</param>
        /// <param name="scope">변수 스코프 (선택적)</param>
        /// <returns>변수 목록</returns>
        List<Variable> ExtractVariables(SyntaxTree ast, VariableScope? scope = null);

        /// <summary>
        /// AST에서 데이터 타입 목록을 추출합니다.
        /// </summary>
        /// <param name="ast">구문 트리</param>
        /// <returns>데이터 타입 목록</returns>
        List<DataType> ExtractDataTypes(SyntaxTree ast);

        /// <summary>
        /// Function Block의 사이클로매틱 복잡도를 계산합니다.
        /// </summary>
        /// <param name="fb">Function Block</param>
        /// <returns>복잡도 값</returns>
        int CalculateCyclomaticComplexity(FunctionBlock fb);

        /// <summary>
        /// 코드에서 주석을 추출합니다.
        /// </summary>
        /// <param name="ast">구문 트리</param>
        /// <returns>주석 목록 (라인 번호, 내용)</returns>
        Dictionary<int, string> ExtractComments(SyntaxTree ast);

        /// <summary>
        /// 주석이 한글인지 검증합니다.
        /// </summary>
        /// <param name="comment">주석 내용</param>
        /// <returns>한글 포함 여부</returns>
        bool IsKoreanComment(string comment);

        /// <summary>
        /// 파싱 오류가 발생했는지 확인합니다.
        /// </summary>
        /// <param name="ast">구문 트리</param>
        /// <returns>오류 목록</returns>
        List<ParsingError> GetParsingErrors(SyntaxTree ast);
    }

    /// <summary>
    /// 구문 트리 (Abstract Syntax Tree)
    /// </summary>
    public class SyntaxTree
    {
        public string SourceCode { get; set; }
        public object RootNode { get; set; }  // ANTLR4 ParserRuleContext
        public string FilePath { get; set; }
        public List<ParsingError> Errors { get; set; } = new List<ParsingError>();
    }

    /// <summary>
    /// 파싱 오류
    /// </summary>
    public class ParsingError
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public string Message { get; set; }
        public string OffendingSymbol { get; set; }
    }
}
