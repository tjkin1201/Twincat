using System.Collections.Generic;
using System.Threading.Tasks;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Domain.Contracts
{
    /// <summary>
    /// 파서 서비스 인터페이스
    ///
    /// TwinCAT 파일을 파싱하고 AST를 생성합니다.
    /// ANTLR4 기반 ST 파서와 LINQ to XML 기반 TwinCAT XML 파서를 통합합니다.
    /// 인터페이스 분리 원칙(ISP)에 따라 파싱과 분석 기능만 제공합니다.
    /// </summary>
    public interface IParserService
    {
        /// <summary>
        /// TwinCAT 파일을 파싱하여 AST를 생성합니다.
        /// 파일 확장자에 따라 적절한 파서를 자동으로 선택합니다.
        /// </summary>
        /// <param name="filePath">파일 절대 경로 (.TcPOU, .TcDUT, .TcGVL 등)</param>
        /// <returns>파싱된 구문 트리. 파싱 오류 발생 시에도 부분 트리를 반환합니다.</returns>
        /// <exception cref="System.IO.FileNotFoundException">파일이 존재하지 않을 때</exception>
        SyntaxTree ParseFile(string filePath);

        /// <summary>
        /// AST에서 Function Block 목록을 추출합니다.
        /// FUNCTION_BLOCK 키워드로 정의된 모든 블록을 찾아 반환합니다.
        /// </summary>
        /// <param name="ast">구문 트리</param>
        /// <returns>Function Block 목록. 없으면 빈 리스트를 반환합니다.</returns>
        List<FunctionBlock> ExtractFunctionBlocks(SyntaxTree ast);

        /// <summary>
        /// AST에서 변수 목록을 추출합니다.
        /// VAR, VAR_INPUT, VAR_OUTPUT 등 모든 변수 선언을 파싱합니다.
        /// </summary>
        /// <param name="ast">구문 트리</param>
        /// <param name="scope">
        /// 변수 스코프 (선택적).
        /// null이면 모든 스코프의 변수를 반환합니다.
        /// </param>
        /// <returns>변수 목록</returns>
        List<Variable> ExtractVariables(SyntaxTree ast, VariableScope? scope = null);

        /// <summary>
        /// AST에서 데이터 타입 목록을 추출합니다.
        /// TYPE 키워드로 정의된 STRUCT, ENUM, UNION 등을 파싱합니다.
        /// </summary>
        /// <param name="ast">구문 트리</param>
        /// <returns>데이터 타입 목록</returns>
        List<DataType> ExtractDataTypes(SyntaxTree ast);

        /// <summary>
        /// Function Block의 사이클로매틱 복잡도를 계산합니다.
        /// IF, CASE, FOR, WHILE, REPEAT 등 제어 흐름 분기점을 기준으로 계산합니다.
        /// </summary>
        /// <param name="fb">Function Block</param>
        /// <returns>복잡도 값 (최소 1). 값이 높을수록 코드가 복잡합니다.</returns>
        int CalculateCyclomaticComplexity(FunctionBlock fb);

        /// <summary>
        /// 코드에서 주석을 추출합니다.
        /// // 라인 주석과 (* *) 블록 주석을 모두 파싱합니다.
        /// </summary>
        /// <param name="ast">구문 트리</param>
        /// <returns>
        /// 주석 딕셔너리.
        /// Key: 라인 번호, Value: 주석 내용 (주석 기호 제외)
        /// </returns>
        Dictionary<int, string> ExtractComments(SyntaxTree ast);

        /// <summary>
        /// 주석이 한글을 포함하는지 검증합니다.
        /// UTF-8 한글 범위(U+AC00 ~ U+D7A3)를 검사합니다.
        /// </summary>
        /// <param name="comment">주석 내용</param>
        /// <returns>
        /// 한글 포함 여부.
        /// 최소 1개 이상의 한글 글자가 있으면 true를 반환합니다.
        /// </returns>
        bool IsKoreanComment(string comment);

        /// <summary>
        /// 파싱 오류가 발생했는지 확인합니다.
        /// 구문 오류, 의미 오류 등 모든 파싱 문제를 포함합니다.
        /// </summary>
        /// <param name="ast">구문 트리</param>
        /// <returns>
        /// 오류 목록.
        /// 파싱이 성공했으면 빈 리스트를 반환합니다.
        /// </returns>
        List<ParsingError> GetParsingErrors(SyntaxTree ast);
    }

    /// <summary>
    /// 비동기 파서 서비스 인터페이스
    ///
    /// 대용량 파일이나 다수의 파일을 비동기적으로 파싱합니다.
    /// I/O 바운드 작업에 최적화되어 있습니다.
    /// </summary>
    public interface IAsyncParserService
    {
        /// <summary>
        /// 비동기적으로 파일을 파싱합니다.
        /// </summary>
        /// <param name="filePath">파일 절대 경로</param>
        /// <returns>파싱된 구문 트리</returns>
        Task<SyntaxTree> ParseFileAsync(string filePath);

        /// <summary>
        /// 여러 파일을 병렬로 파싱합니다.
        /// 멀티코어 환경에서 성능을 최적화합니다.
        /// </summary>
        /// <param name="filePaths">파일 경로 목록</param>
        /// <returns>파싱된 구문 트리 목록</returns>
        Task<List<SyntaxTree>> ParseFilesAsync(IEnumerable<string> filePaths);
    }

    /// <summary>
    /// 구문 트리 (Abstract Syntax Tree)
    ///
    /// 파싱된 코드의 구조를 나타내는 트리 구조입니다.
    /// ANTLR4 ParserRuleContext를 래핑하여 도메인 계층에서 사용합니다.
    /// </summary>
    public class SyntaxTree
    {
        /// <summary>
        /// 원본 소스 코드를 가져오거나 설정합니다.
        /// </summary>
        public string SourceCode { get; set; } = string.Empty;

        /// <summary>
        /// AST 루트 노드를 가져오거나 설정합니다.
        /// ANTLR4 ParserRuleContext 타입이지만 도메인 계층에서는 object로 처리합니다.
        /// </summary>
        public object RootNode { get; set; } = new object();

        /// <summary>
        /// 원본 파일 경로를 가져오거나 설정합니다.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 파싱 중 발생한 오류 목록을 가져오거나 설정합니다.
        /// </summary>
        public List<ParsingError> Errors { get; set; } = new List<ParsingError>();

        /// <summary>
        /// 파싱이 성공했는지 여부를 가져옵니다.
        /// </summary>
        public bool IsValid => Errors == null || Errors.Count == 0;
    }

    /// <summary>
    /// 파싱 오류
    ///
    /// 구문 분석 중 발생한 오류 정보를 담습니다.
    /// </summary>
    public class ParsingError
    {
        /// <summary>
        /// 오류가 발생한 라인 번호를 가져오거나 설정합니다 (1부터 시작).
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// 오류가 발생한 컬럼 번호를 가져오거나 설정합니다 (0부터 시작).
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// 오류 메시지를 가져오거나 설정합니다.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 오류를 일으킨 심볼을 가져오거나 설정합니다.
        /// </summary>
        public string OffendingSymbol { get; set; } = string.Empty;

        /// <summary>
        /// 오류 설명을 문자열로 반환합니다.
        /// </summary>
        public override string ToString()
        {
            return $"라인 {Line}, 컬럼 {Column}: {Message} (심볼: '{OffendingSymbol}')";
        }
    }
}
