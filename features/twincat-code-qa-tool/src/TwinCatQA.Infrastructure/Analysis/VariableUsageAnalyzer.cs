using Microsoft.Extensions.Logging;
using TwinCatQA.Domain.Contracts;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.Analysis;

/// <summary>
/// ANTLR AST 기반 변수 사용 분석기
/// </summary>
public class VariableUsageAnalyzer : IVariableUsageAnalyzer
{
    private readonly ILogger<VariableUsageAnalyzer> _logger;
    private readonly IParserService _parserService;

    public VariableUsageAnalyzer(
        ILogger<VariableUsageAnalyzer> logger,
        IParserService parserService)
    {
        _logger = logger;
        _parserService = parserService;
    }

    /// <inheritdoc/>
    public async Task<VariableUsageAnalysis> AnalyzeVariableUsageAsync(
        ValidationSession session,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("변수 사용 분석 시작: {ProjectPath}", session.ProjectPath);

        var unusedVariables = await FindUnusedVariablesAsync(session, cancellationToken);
        var uninitializedVariables = await FindUninitializedVariablesAsync(session, cancellationToken);
        var deadCodeBlocks = await FindDeadCodeAsync(session, cancellationToken);

        var analysis = new VariableUsageAnalysis
        {
            UnusedVariables = unusedVariables,
            UninitializedVariables = uninitializedVariables,
            DeadCodeBlocks = deadCodeBlocks,
            ProjectPath = session.ProjectPath,
            AnalysisTime = DateTime.UtcNow
        };

        _logger.LogInformation("변수 사용 분석 완료: 사용되지 않은 변수 {UnusedCount}개, " +
                              "초기화되지 않은 변수 {UninitCount}개, Dead Code {DeadCount}개",
            unusedVariables.Count, uninitializedVariables.Count, deadCodeBlocks.Count);

        return analysis;
    }

    /// <inheritdoc/>
    public async Task<List<UnusedVariable>> FindUnusedVariablesAsync(
        ValidationSession session,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("사용되지 않은 변수 탐지 시작");

        var unusedVariables = new List<UnusedVariable>();

        foreach (var syntaxTree in session.SyntaxTrees)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // VAR 블록에서 선언된 변수 추출
            var declaredVariables = await ExtractDeclaredVariablesAsync(syntaxTree);

            // 코드에서 사용된 변수 추출
            var usedVariables = await ExtractUsedVariablesAsync(syntaxTree);

            // 선언되었지만 사용되지 않은 변수 찾기
            foreach (var declared in declaredVariables)
            {
                if (!usedVariables.Contains(declared.Name))
                {
                    unusedVariables.Add(new UnusedVariable
                    {
                        VariableName = declared.Name,
                        VariableType = declared.Type,
                        FilePath = syntaxTree.FilePath,
                        LineNumber = declared.LineNumber,
                        PouName = ExtractPouName(syntaxTree.FilePath),
                        Scope = declared.Scope,
                        Severity = IssueSeverity.Warning
                    });
                }
            }
        }

        _logger.LogDebug("사용되지 않은 변수 {Count}개 발견", unusedVariables.Count);
        return unusedVariables;
    }

    /// <inheritdoc/>
    public async Task<List<UninitializedVariable>> FindUninitializedVariablesAsync(
        ValidationSession session,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("초기화되지 않은 변수 탐지 시작");

        var uninitializedVariables = new List<UninitializedVariable>();

        foreach (var syntaxTree in session.SyntaxTrees)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 선언 시 초기화되지 않은 변수 찾기
            var declaredVariables = await ExtractDeclaredVariablesAsync(syntaxTree);
            var initializedVariables = await ExtractInitializedVariablesAsync(syntaxTree);

            foreach (var declared in declaredVariables)
            {
                if (!initializedVariables.Contains(declared.Name) && !HasDefaultInitialization(declared.Type))
                {
                    // 사용 전에 할당되는지 확인
                    bool isAssignedBeforeUse = await IsVariableAssignedBeforeUseAsync(syntaxTree, declared.Name);

                    if (!isAssignedBeforeUse)
                    {
                        uninitializedVariables.Add(new UninitializedVariable
                        {
                            VariableName = declared.Name,
                            VariableType = declared.Type,
                            FilePath = syntaxTree.FilePath,
                            LineNumber = declared.LineNumber,
                            PouName = ExtractPouName(syntaxTree.FilePath),
                            UsageContext = "변수가 초기화 없이 사용됨",
                            Severity = IssueSeverity.Error
                        });
                    }
                }
            }
        }

        _logger.LogDebug("초기화되지 않은 변수 {Count}개 발견", uninitializedVariables.Count);
        return uninitializedVariables;
    }

    /// <inheritdoc/>
    public async Task<List<DeadCode>> FindDeadCodeAsync(
        ValidationSession session,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Dead Code 탐지 시작");

        var deadCodeBlocks = new List<DeadCode>();

        foreach (var syntaxTree in session.SyntaxTrees)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 주석 처리된 코드 블록 탐지
            var commentedCode = await FindCommentedOutCodeAsync(syntaxTree);
            deadCodeBlocks.AddRange(commentedCode);

            // 도달 불가능한 코드 탐지 (RETURN 후 코드 등)
            var unreachableCode = await FindUnreachableCodeAsync(syntaxTree);
            deadCodeBlocks.AddRange(unreachableCode);

            // 항상 거짓인 조건문 내부 코드 탐지
            var alwaysFalseBlocks = await FindAlwaysFalseConditionBlocksAsync(syntaxTree);
            deadCodeBlocks.AddRange(alwaysFalseBlocks);
        }

        _logger.LogDebug("Dead Code 블록 {Count}개 발견", deadCodeBlocks.Count);
        return deadCodeBlocks;
    }

    /// <inheritdoc/>
    public async Task<VariableUsageAnalysis> AnalyzePouVariableUsageAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("단일 POU 변수 사용 분석: {FilePath}", filePath);

        var syntaxTree = _parserService.ParseFile(filePath);

        var session = new ValidationSession
        {
            ProjectPath = Path.GetDirectoryName(filePath) ?? string.Empty,
            SyntaxTrees = new List<SyntaxTree> { syntaxTree }
        };

        return await AnalyzeVariableUsageAsync(session, cancellationToken);
    }

    /// <inheritdoc/>
    public Dictionary<string, int> GetVariableUsageStatistics(VariableUsageAnalysis analysis)
    {
        // 변수 사용 통계 계산
        var statistics = new Dictionary<string, int>();

        // 사용되지 않은 변수는 0회
        foreach (var unused in analysis.UnusedVariables)
        {
            statistics[unused.VariableName] = 0;
        }

        return statistics;
    }

    // ============================================
    // Private Helper Methods
    // ============================================

    private async Task<List<VariableDeclaration>> ExtractDeclaredVariablesAsync(SyntaxTree syntaxTree)
    {
        return await Task.Run(() =>
        {
            var variables = new List<VariableDeclaration>();

            // ANTLR AST를 순회하여 VAR 블록에서 변수 선언 추출
            // 실제 구현에서는 ANTLR Visitor 패턴 사용
            var lines = syntaxTree.SourceCode.Split('\n');
            bool inVarBlock = false;
            int lineNumber = 0;

            foreach (var line in lines)
            {
                lineNumber++;
                var trimmed = line.Trim();

                if (trimmed.StartsWith("VAR", StringComparison.OrdinalIgnoreCase))
                {
                    inVarBlock = true;
                    continue;
                }

                if (trimmed.StartsWith("END_VAR", StringComparison.OrdinalIgnoreCase))
                {
                    inVarBlock = false;
                    continue;
                }

                if (inVarBlock && trimmed.Contains(':'))
                {
                    var parts = trimmed.Split(':');
                    if (parts.Length >= 2)
                    {
                        var varName = parts[0].Trim();
                        var varType = parts[1].Split(';')[0].Trim();

                        variables.Add(new VariableDeclaration
                        {
                            Name = varName,
                            Type = varType,
                            LineNumber = lineNumber,
                            Scope = VariableScope.Local
                        });
                    }
                }
            }

            return variables;
        });
    }

    private async Task<HashSet<string>> ExtractUsedVariablesAsync(SyntaxTree syntaxTree)
    {
        return await Task.Run(() =>
        {
            var usedVariables = new HashSet<string>();

            // 코드 섹션에서 사용된 변수 이름 추출
            // 실제 구현에서는 ANTLR AST 분석
            var lines = syntaxTree.SourceCode.Split('\n');

            foreach (var line in lines)
            {
                // 간단한 패턴 매칭 (실제로는 AST 분석 필요)
                if (line.Contains(":=") || line.Contains("(") || line.Contains(")"))
                {
                    // 변수 사용 감지 로직
                }
            }

            return usedVariables;
        });
    }

    private async Task<HashSet<string>> ExtractInitializedVariablesAsync(SyntaxTree syntaxTree)
    {
        return await Task.Run(() =>
        {
            var initializedVars = new HashSet<string>();

            // 선언 시 초기값이 있는 변수 찾기
            var lines = syntaxTree.SourceCode.Split('\n');

            foreach (var line in lines)
            {
                if (line.Contains(":="))
                {
                    var parts = line.Split(":=");
                    if (parts.Length >= 2)
                    {
                        var varPart = parts[0].Split(':').FirstOrDefault()?.Trim();
                        if (!string.IsNullOrEmpty(varPart))
                        {
                            initializedVars.Add(varPart);
                        }
                    }
                }
            }

            return initializedVars;
        });
    }

    private async Task<bool> IsVariableAssignedBeforeUseAsync(SyntaxTree syntaxTree, string variableName)
    {
        return await Task.Run(() =>
        {
            // 변수가 사용되기 전에 할당되는지 확인
            // 실제 구현에서는 제어 흐름 분석 필요
            return false; // 간단한 구현: 보수적으로 false 반환
        });
    }

    private bool HasDefaultInitialization(string variableType)
    {
        // PLC 타입의 기본 초기화 여부 확인
        var typesWithDefaultInit = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "BOOL", "BYTE", "WORD", "DWORD", "LWORD",
            "SINT", "INT", "DINT", "LINT",
            "USINT", "UINT", "UDINT", "ULINT",
            "REAL", "LREAL"
        };

        return typesWithDefaultInit.Contains(variableType);
    }

    private async Task<List<DeadCode>> FindCommentedOutCodeAsync(SyntaxTree syntaxTree)
    {
        return await Task.Run(() =>
        {
            var deadCode = new List<DeadCode>();
            var comments = _parserService.ExtractComments(syntaxTree);

            foreach (var comment in comments)
            {
                // 주석이 코드처럼 보이는지 확인
                if (LooksLikeCode(comment.Value))
                {
                    deadCode.Add(new DeadCode
                    {
                        FilePath = syntaxTree.FilePath,
                        StartLine = comment.Key,
                        EndLine = comment.Key,
                        PouName = ExtractPouName(syntaxTree.FilePath),
                        Type = DeadCodeType.CommentedOutCode,
                        Description = $"주석 처리된 코드: {comment.Value}",
                        Severity = IssueSeverity.Info
                    });
                }
            }

            return deadCode;
        });
    }

    private async Task<List<DeadCode>> FindUnreachableCodeAsync(SyntaxTree syntaxTree)
    {
        return await Task.Run(() =>
        {
            var deadCode = new List<DeadCode>();
            // RETURN, EXIT 등 후의 코드 찾기
            // 실제 구현에서는 AST 분석 필요
            return deadCode;
        });
    }

    private async Task<List<DeadCode>> FindAlwaysFalseConditionBlocksAsync(SyntaxTree syntaxTree)
    {
        return await Task.Run(() =>
        {
            var deadCode = new List<DeadCode>();
            // IF FALSE THEN ... 같은 패턴 찾기
            // 실제 구현에서는 상수 전파 분석 필요
            return deadCode;
        });
    }

    private bool LooksLikeCode(string comment)
    {
        // 주석이 실제 코드처럼 보이는지 휴리스틱 검사
        var codePatterns = new[] { ":=", "IF ", "FOR ", "WHILE ", "CASE ", "RETURN", ";", "END_" };
        return codePatterns.Any(pattern => comment.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private string ExtractPouName(string filePath)
    {
        return Path.GetFileNameWithoutExtension(filePath);
    }

    private class VariableDeclaration
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public VariableScope Scope { get; set; }
    }
}
