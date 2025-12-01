namespace TwinCatQA.Infrastructure.Git;

/// <summary>
/// 변경된 라인의 코드 컨텍스트를 분석하는 클래스
/// </summary>
public class ContextAnalyzer
{
    private const int DefaultContextRange = 10;

    /// <summary>
    /// 라인이 속한 FunctionBlock 찾기
    /// </summary>
    /// <param name="file">코드 파일 객체 (동적 타입)</param>
    /// <param name="line">라인 번호</param>
    /// <returns>FB 정보, 없으면 null</returns>
    public (int startLine, int endLine, string name)? FindContainingFunctionBlock(dynamic file, int line)
    {
        try
        {
            // CodeFile 객체에서 FunctionBlocks 컬렉션 접근
            if (file?.FunctionBlocks == null)
            {
                return null;
            }

            foreach (var fb in file.FunctionBlocks)
            {
                // FB의 시작/종료 라인 체크
                if (fb.StartLine <= line && line <= fb.EndLine)
                {
                    return (fb.StartLine, fb.EndLine, fb.Name ?? "Unknown");
                }
            }
        }
        catch
        {
            // 타입 불일치 등 오류 발생 시 null 반환
        }

        return null;
    }

    /// <summary>
    /// 라인이 속한 제어 구조 찾기 (CASE/FOR/IF/WHILE)
    /// </summary>
    /// <param name="ast">구문 트리 객체</param>
    /// <param name="line">라인 번호</param>
    /// <returns>제어 구조 정보, 없으면 null</returns>
    public (int startLine, int endLine, string type)? FindContainingControlStructure(dynamic ast, int line)
    {
        try
        {
            // AST에서 제어 구조 노드 탐색
            if (ast?.Statements == null)
            {
                return null;
            }

            return SearchControlStructure(ast.Statements, line);
        }
        catch
        {
            // 타입 불일치 등 오류 발생 시 null 반환
        }

        return null;
    }

    /// <summary>
    /// 재귀적으로 제어 구조 탐색
    /// </summary>
    private (int startLine, int endLine, string type)? SearchControlStructure(dynamic statements, int line)
    {
        try
        {
            foreach (var stmt in statements)
            {
                // 제어 구조인지 확인
                string nodeType = stmt.GetType().Name;

                if (IsControlStructure(nodeType))
                {
                    int startLine = stmt.StartLine ?? 0;
                    int endLine = stmt.EndLine ?? 0;

                    if (startLine <= line && line <= endLine)
                    {
                        // 중첩된 구조가 있는지 재귀 탐색
                        if (stmt.Body != null)
                        {
                            var nested = SearchControlStructure(stmt.Body, line);
                            if (nested.HasValue)
                            {
                                return nested; // 더 구체적인 컨텍스트 반환
                            }
                        }

                        return (startLine, endLine, SimplifyNodeType(nodeType));
                    }
                }

                // 중첩된 문장들도 탐색
                if (stmt.Statements != null)
                {
                    var nested = SearchControlStructure(stmt.Statements, line);
                    if (nested.HasValue)
                    {
                        return nested;
                    }
                }
            }
        }
        catch
        {
            // 탐색 중 오류 발생 시 무시
        }

        return null;
    }

    /// <summary>
    /// 제어 구조 노드 타입인지 확인
    /// </summary>
    private bool IsControlStructure(string nodeType)
    {
        return nodeType.Contains("Case", StringComparison.OrdinalIgnoreCase) ||
               nodeType.Contains("For", StringComparison.OrdinalIgnoreCase) ||
               nodeType.Contains("If", StringComparison.OrdinalIgnoreCase) ||
               nodeType.Contains("While", StringComparison.OrdinalIgnoreCase) ||
               nodeType.Contains("Repeat", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 노드 타입을 간단한 이름으로 변환
    /// </summary>
    private string SimplifyNodeType(string nodeType)
    {
        if (nodeType.Contains("Case", StringComparison.OrdinalIgnoreCase))
            return "CASE";
        if (nodeType.Contains("For", StringComparison.OrdinalIgnoreCase))
            return "FOR";
        if (nodeType.Contains("If", StringComparison.OrdinalIgnoreCase))
            return "IF";
        if (nodeType.Contains("While", StringComparison.OrdinalIgnoreCase))
            return "WHILE";
        if (nodeType.Contains("Repeat", StringComparison.OrdinalIgnoreCase))
            return "REPEAT";

        return nodeType;
    }

    /// <summary>
    /// 주변 라인 범위 가져오기
    /// </summary>
    /// <param name="file">코드 파일 객체</param>
    /// <param name="line">중심 라인 번호</param>
    /// <param name="range">범위 (기본 10줄)</param>
    /// <returns>시작/종료 라인</returns>
    public (int startLine, int endLine) GetSurroundingLines(dynamic file, int line, int range = DefaultContextRange)
    {
        try
        {
            // 파일의 총 라인 수 확인
            int totalLines = file?.TotalLines ?? 0;

            if (totalLines == 0)
            {
                // 라인 수를 알 수 없으면 범위만 계산
                int startLine = Math.Max(1, line - range);
                int endLine = line + range;
                return (startLine, endLine);
            }

            // 파일 경계 내에서 범위 계산
            int start = Math.Max(1, line - range);
            int end = Math.Min(totalLines, line + range);

            return (start, end);
        }
        catch
        {
            // 오류 발생 시 기본 범위 반환
            int startLine = Math.Max(1, line - range);
            int endLine = line + range;
            return (startLine, endLine);
        }
    }

    /// <summary>
    /// 변경 라인의 최적 컨텍스트 결정
    /// </summary>
    /// <param name="file">코드 파일 객체</param>
    /// <param name="ast">구문 트리 객체</param>
    /// <param name="changedLine">변경된 라인 번호</param>
    /// <returns>코드 컨텍스트 정보</returns>
    public CodeContext DetermineContext(dynamic file, dynamic ast, int changedLine)
    {
        // 1순위: FunctionBlock 내부인지 확인
        var fbContext = FindContainingFunctionBlock(file, changedLine);
        if (fbContext.HasValue)
        {
            return new CodeContext
            {
                StartLine = fbContext.Value.startLine,
                EndLine = fbContext.Value.endLine,
                ContextType = "FunctionBlock",
                ContextName = fbContext.Value.name
            };
        }

        // 2순위: 제어 구조 내부인지 확인
        var controlContext = FindContainingControlStructure(ast, changedLine);
        if (controlContext.HasValue)
        {
            return new CodeContext
            {
                StartLine = controlContext.Value.startLine,
                EndLine = controlContext.Value.endLine,
                ContextType = "ControlStructure",
                ContextName = controlContext.Value.type
            };
        }

        // 3순위: 주변 라인 범위 반환
        var surroundingLines = GetSurroundingLines(file, changedLine);
        return new CodeContext
        {
            StartLine = surroundingLines.startLine,
            EndLine = surroundingLines.endLine,
            ContextType = "Surrounding",
            ContextName = $"Line {changedLine} ±{DefaultContextRange}"
        };
    }
}
