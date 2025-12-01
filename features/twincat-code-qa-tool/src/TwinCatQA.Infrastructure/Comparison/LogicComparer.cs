using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.Comparison;

/// <summary>
/// 로직 비교 분석기 구현
/// </summary>
public class LogicComparer : ILogicComparer
{
    // FB/PROGRAM/FUNCTION 블록 패턴 (END 키워드 포함 버전)
    private static readonly Regex FunctionBlockPatternFull = new(
        @"(FUNCTION_BLOCK|PROGRAM|FUNCTION)\s+(\w+)(.*?)(END_FUNCTION_BLOCK|END_PROGRAM|END_FUNCTION)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    // FB/PROGRAM/FUNCTION 선언만 찾기 (Declaration 섹션에서 이름 추출용)
    private static readonly Regex FunctionBlockDeclarationPattern = new(
        @"(FUNCTION_BLOCK|PROGRAM|FUNCTION)\s+(\w+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // VAR 블록 제거용 패턴 (본문만 비교)
    private static readonly Regex VarBlockPattern = new(
        @"VAR(?:_GLOBAL|_INPUT|_OUTPUT|_IN_OUT|_TEMP|_STAT|_CONSTANT)?\s*.*?END_VAR",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    #region 검토 의견 생성용 패턴

    // 안전 관련 키워드
    private static readonly Regex SafetyKeywordPattern = new(
        @"\b(Emergency|EStop|Safety|Fault|Error|Alarm|Interlock|Limit|Guard)\w*\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 타이머/카운터 패턴
    private static readonly Regex TimerCounterPattern = new(
        @"\b(TON|TOF|TP|CTU|CTD|CTUD|R_TRIG|F_TRIG)\s*\(",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 조건문 패턴
    private static readonly Regex ConditionPattern = new(
        @"\b(IF|ELSIF|CASE|WHILE|FOR|REPEAT)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 출력/액추에이터 패턴
    private static readonly Regex OutputPattern = new(
        @"\b(Motor|Valve|Cylinder|Actuator|Output|Drive|Servo)\w*\s*:=",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // I/O 직접 접근 패턴
    private static readonly Regex DirectIOPattern = new(
        @"%[IQM][XBWD]?\d+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 상태 머신 패턴
    private static readonly Regex StateMachinePattern = new(
        @"\b(State|Step|Phase|Mode|Status)\s*:=\s*\d+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // 파라미터/설정값 패턴
    private static readonly Regex ParameterPattern = new(
        @"\b(Speed|Velocity|Position|Timeout|Delay|Limit|Max|Min|Threshold)\w*\s*:=",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    #endregion

    public List<LogicChange> Compare(List<CodeFile> oldFiles, List<CodeFile> newFiles)
    {
        var changes = new List<LogicChange>();

        // 모든 로직 블록 수집
        var oldBlocks = CollectLogicBlocks(oldFiles);
        var newBlocks = CollectLogicBlocks(newFiles);

        // 추가된 블록 찾기
        foreach (var (key, newBlock) in newBlocks)
        {
            if (!oldBlocks.ContainsKey(key))
            {
                var reviewComments = GenerateReviewCommentsForAdded(newBlock);

                changes.Add(new LogicChange
                {
                    ChangeType = ChangeType.Added,
                    ElementName = newBlock.Name,
                    FilePath = newBlock.FilePath,
                    StartLine = newBlock.StartLine,
                    EndLine = newBlock.EndLine,
                    OldCode = "(없음 - 새로 추가됨)",
                    NewCode = FormatCodeForDisplay(newBlock.Body),
                    ChangedLineCount = newBlock.Body.Split('\n').Length,
                    Summary = $"새 {newBlock.Type} 추가됨",
                    ReviewComments = reviewComments
                });
            }
        }

        // 삭제된 블록 찾기
        foreach (var (key, oldBlock) in oldBlocks)
        {
            if (!newBlocks.ContainsKey(key))
            {
                var reviewComments = GenerateReviewCommentsForRemoved(oldBlock);

                changes.Add(new LogicChange
                {
                    ChangeType = ChangeType.Removed,
                    ElementName = oldBlock.Name,
                    FilePath = oldBlock.FilePath,
                    StartLine = oldBlock.StartLine,
                    EndLine = oldBlock.EndLine,
                    OldCode = FormatCodeForDisplay(oldBlock.Body),
                    NewCode = "(삭제됨)",
                    ChangedLineCount = oldBlock.Body.Split('\n').Length,
                    Summary = $"{oldBlock.Type} 삭제됨",
                    ReviewComments = reviewComments
                });
            }
        }

        // 변경된 블록 찾기
        foreach (var (key, oldBlock) in oldBlocks)
        {
            if (newBlocks.TryGetValue(key, out var newBlock))
            {
                // VAR 블록 제거 후 본문만 비교
                var oldBody = NormalizeBody(oldBlock.Body);
                var newBody = NormalizeBody(newBlock.Body);

                if (!string.Equals(oldBody, newBody, StringComparison.Ordinal))
                {
                    var diffResult = ComputeDiff(oldBody, newBody);
                    var reviewComments = GenerateReviewCommentsForModified(oldBlock, newBlock);

                    // 원본 코드 전체를 보존 (UI에서 근거 표시용)
                    changes.Add(new LogicChange
                    {
                        ChangeType = ChangeType.Modified,
                        ElementName = newBlock.Name,
                        FilePath = newBlock.FilePath,
                        StartLine = newBlock.StartLine,
                        EndLine = newBlock.EndLine,
                        OldCode = FormatCodeForDisplay(oldBlock.Body),
                        NewCode = FormatCodeForDisplay(newBlock.Body),
                        ChangedLineCount = diffResult.ChangedLines,
                        Summary = diffResult.Summary,
                        ReviewComments = reviewComments
                    });
                }
            }
        }

        return changes;
    }

    private Dictionary<string, LogicBlockInfo> CollectLogicBlocks(List<CodeFile> files)
    {
        var blocks = new Dictionary<string, LogicBlockInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (string.IsNullOrEmpty(file.Content)) continue;

            // 방법 1: 전체 블록 패턴 (END 키워드 포함)
            var fullMatches = FunctionBlockPatternFull.Matches(file.Content);
            foreach (Match match in fullMatches)
            {
                var blockType = match.Groups[1].Value;
                var blockName = match.Groups[2].Value;
                var blockBody = match.Groups[3].Value;

                var startIndex = match.Index;
                var endIndex = match.Index + match.Length;
                var startLine = file.Content[..startIndex].Count(c => c == '\n') + 1;
                var endLine = file.Content[..endIndex].Count(c => c == '\n') + 1;

                blocks[blockName] = new LogicBlockInfo
                {
                    Name = blockName,
                    Type = blockType,
                    Body = blockBody,
                    FilePath = file.FilePath,
                    StartLine = startLine,
                    EndLine = endLine
                };
            }

            // 방법 2: Declaration + ST 분리 구조 처리 (TwinCAT XML에서 추출된 경우)
            // Declaration 섹션에서 FB/PROGRAM 이름 추출, 전체 코드를 Body로 사용
            if (fullMatches.Count == 0)
            {
                var declMatch = FunctionBlockDeclarationPattern.Match(file.Content);
                if (declMatch.Success)
                {
                    var blockType = declMatch.Groups[1].Value;
                    var blockName = declMatch.Groups[2].Value;

                    // 전체 코드를 Body로 사용 (Declaration + Implementation 모두 포함)
                    blocks[blockName] = new LogicBlockInfo
                    {
                        Name = blockName,
                        Type = blockType,
                        Body = file.Content,  // 전체 내용 사용
                        FilePath = file.FilePath,
                        StartLine = 1,
                        EndLine = file.Content.Count(c => c == '\n') + 1
                    };
                }
            }
        }

        return blocks;
    }

    /// <summary>
    /// 본문 정규화 (VAR 블록 제거, 공백 정리)
    /// </summary>
    private string NormalizeBody(string body)
    {
        // VAR 블록 제거
        var normalized = VarBlockPattern.Replace(body, "");

        // 주석 제거
        normalized = Regex.Replace(normalized, @"//.*?$", "", RegexOptions.Multiline);
        normalized = Regex.Replace(normalized, @"\(\*.*?\*\)", "", RegexOptions.Singleline);

        // 공백 정규화
        normalized = Regex.Replace(normalized, @"\s+", " ");

        return normalized.Trim();
    }

    /// <summary>
    /// 간단한 차이 분석
    /// </summary>
    private DiffResult ComputeDiff(string oldBody, string newBody)
    {
        var oldLines = oldBody.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var newLines = newBody.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var oldSet = new HashSet<string>(oldLines.Select(l => l.Trim()));
        var newSet = new HashSet<string>(newLines.Select(l => l.Trim()));

        var added = newSet.Except(oldSet).Count();
        var removed = oldSet.Except(newSet).Count();
        var changed = added + removed;

        var summary = new List<string>();
        if (added > 0) summary.Add($"+{added}줄 추가");
        if (removed > 0) summary.Add($"-{removed}줄 삭제");

        return new DiffResult
        {
            ChangedLines = changed,
            Summary = summary.Count > 0 ? string.Join(", ", summary) : "내용 변경됨"
        };
    }

    /// <summary>
    /// 코드를 일정 길이로 자름
    /// </summary>
    private string TruncateCode(string code, int maxLength = 500)
    {
        if (string.IsNullOrEmpty(code)) return string.Empty;

        if (code.Length <= maxLength) return code;

        return code[..maxLength] + "...";
    }

    /// <summary>
    /// UI 표시용 코드 포맷팅 (원본 유지, 너무 길면 자름)
    /// </summary>
    private string FormatCodeForDisplay(string code, int maxLength = 3000)
    {
        if (string.IsNullOrEmpty(code)) return "(코드 없음)";

        // 앞뒤 공백만 제거하고 원본 유지
        var formatted = code.Trim();

        if (formatted.Length > maxLength)
        {
            return formatted[..maxLength] + "\n\n... (코드가 너무 길어 일부만 표시)";
        }

        return formatted;
    }

    private class LogicBlockInfo
    {
        public string Name { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public string Body { get; init; } = string.Empty;
        public string FilePath { get; init; } = string.Empty;
        public int StartLine { get; init; }
        public int EndLine { get; init; }
    }

    private class DiffResult
    {
        public int ChangedLines { get; init; }
        public string Summary { get; init; } = string.Empty;
    }

    #region 검토 의견 생성

    /// <summary>
    /// 새로 추가된 블록에 대한 검토 의견 생성
    /// </summary>
    private List<ReviewComment> GenerateReviewCommentsForAdded(LogicBlockInfo block)
    {
        var comments = new List<ReviewComment>();
        var code = block.Body;

        // 새 블록 추가 기본 안내
        comments.Add(new ReviewComment
        {
            Severity = ReviewSeverity.Info,
            Category = ReviewCategory.General,
            Title = $"새로운 {block.Type} 추가됨",
            Description = $"'{block.Name}'이(가) 새로 추가되었습니다.",
            Impact = "기존 시스템에 새로운 기능이 추가됩니다.",
            Recommendation = "새 블록의 동작을 충분히 테스트하고, 기존 로직과의 상호작용을 확인하세요."
        });

        // 안전 관련 코드 검사
        if (SafetyKeywordPattern.IsMatch(code))
        {
            var matches = SafetyKeywordPattern.Matches(code);
            var keywords = string.Join(", ", matches.Select(m => m.Value).Distinct().Take(5));

            comments.Add(new ReviewComment
            {
                Severity = ReviewSeverity.Critical,
                Category = ReviewCategory.Safety,
                Title = "안전 관련 코드 포함",
                Description = $"새 블록에 안전 관련 키워드가 포함되어 있습니다: {keywords}",
                Impact = "안전 로직이 올바르게 구현되지 않으면 장비 손상이나 인명 피해가 발생할 수 있습니다.",
                Recommendation = "안전 로직의 정확성을 반드시 검증하세요. 가능하면 시뮬레이션 테스트를 수행하세요."
            });
        }

        // 출력/액추에이터 제어 검사
        if (OutputPattern.IsMatch(code))
        {
            comments.Add(new ReviewComment
            {
                Severity = ReviewSeverity.Warning,
                Category = ReviewCategory.IO,
                Title = "출력/액추에이터 제어 코드 포함",
                Description = "모터, 밸브, 실린더 등의 출력 제어 코드가 포함되어 있습니다.",
                Impact = "출력 제어 로직 오류는 장비 오동작으로 이어질 수 있습니다.",
                Recommendation = "출력 제어 조건과 인터락 로직을 꼼꼼히 검토하세요."
            });
        }

        // 타이머/카운터 사용 검사
        if (TimerCounterPattern.IsMatch(code))
        {
            comments.Add(new ReviewComment
            {
                Severity = ReviewSeverity.Info,
                Category = ReviewCategory.Timing,
                Title = "타이머/카운터 사용",
                Description = "TON, TOF, CTU 등의 타이머/카운터가 사용됩니다.",
                Impact = "타이밍 값이 공정 요구사항에 맞는지 확인이 필요합니다.",
                Recommendation = "타이머 설정값이 적절한지, 타임아웃 처리가 올바른지 확인하세요."
            });
        }

        return comments;
    }

    /// <summary>
    /// 삭제된 블록에 대한 검토 의견 생성
    /// </summary>
    private List<ReviewComment> GenerateReviewCommentsForRemoved(LogicBlockInfo block)
    {
        var comments = new List<ReviewComment>();
        var code = block.Body;

        // 삭제 기본 경고
        comments.Add(new ReviewComment
        {
            Severity = ReviewSeverity.Warning,
            Category = ReviewCategory.General,
            Title = $"{block.Type} 삭제됨",
            Description = $"'{block.Name}'이(가) 삭제되었습니다.",
            Impact = "이 블록을 참조하는 다른 코드에서 오류가 발생할 수 있습니다.",
            Recommendation = "삭제된 블록을 호출하는 코드가 없는지 확인하세요."
        });

        // 안전 관련 코드 삭제 경고
        if (SafetyKeywordPattern.IsMatch(code))
        {
            comments.Add(new ReviewComment
            {
                Severity = ReviewSeverity.Critical,
                Category = ReviewCategory.Safety,
                Title = "⚠️ 안전 관련 코드 삭제됨",
                Description = "삭제된 블록에 안전 관련 로직이 포함되어 있었습니다.",
                Impact = "안전 로직 삭제는 심각한 안전 문제를 초래할 수 있습니다.",
                Recommendation = "안전 기능이 다른 곳에서 대체되었는지 반드시 확인하세요. 안전 담당자의 검토가 필요합니다."
            });
        }

        // 출력 제어 코드 삭제 경고
        if (OutputPattern.IsMatch(code))
        {
            comments.Add(new ReviewComment
            {
                Severity = ReviewSeverity.Warning,
                Category = ReviewCategory.IO,
                Title = "출력 제어 코드 삭제됨",
                Description = "삭제된 블록에 출력 제어 로직이 포함되어 있었습니다.",
                Impact = "관련 출력이 더 이상 제어되지 않을 수 있습니다.",
                Recommendation = "해당 출력의 제어가 다른 곳에서 수행되는지 확인하세요."
            });
        }

        return comments;
    }

    /// <summary>
    /// 수정된 블록에 대한 검토 의견 생성
    /// </summary>
    private List<ReviewComment> GenerateReviewCommentsForModified(LogicBlockInfo oldBlock, LogicBlockInfo newBlock)
    {
        var comments = new List<ReviewComment>();
        var oldCode = oldBlock.Body;
        var newCode = newBlock.Body;

        // 라인별 변경 분석
        var lineChanges = AnalyzeLineChanges(oldCode, newCode);

        // 기본 변경 정보 - 구체적인 변경 내역 포함
        var changeDetails = BuildChangeDetails(lineChanges);
        comments.Add(new ReviewComment
        {
            Severity = ReviewSeverity.Info,
            Category = ReviewCategory.Logic,
            Title = $"로직 변경: {newBlock.Name}",
            Description = $"총 {lineChanges.AddedLines.Count}줄 추가, {lineChanges.RemovedLines.Count}줄 삭제",
            Impact = "기존 동작이 변경됩니다. 영향받는 공정을 확인하세요.",
            Recommendation = "변경된 로직이 의도한 대로 동작하는지 테스트하세요.",
            CodeSnippet = changeDetails
        });

        // 안전 로직 변경 검사 - 구체적인 변경 내용 포함
        var oldSafetyLines = ExtractMatchingLinesWithContext(oldCode, SafetyKeywordPattern);
        var newSafetyLines = ExtractMatchingLinesWithContext(newCode, SafetyKeywordPattern);

        if (oldSafetyLines.Any() || newSafetyLines.Any())
        {
            var safetyChanges = CompareLinesDetailed(oldSafetyLines, newSafetyLines);
            if (safetyChanges.HasChanges)
            {
                comments.Add(new ReviewComment
                {
                    Severity = ReviewSeverity.Critical,
                    Category = ReviewCategory.Safety,
                    Title = "⚠️ 안전 로직 변경됨",
                    Description = BuildSafetyChangeDescription(safetyChanges),
                    Impact = "안전 로직 변경은 장비와 인명의 안전에 직접적인 영향을 미칩니다.",
                    Recommendation = "안전 담당자의 검토와 승인이 필요합니다. 변경 전후 안전 기능 테스트를 수행하세요.",
                    CodeSnippet = BuildDetailedCodeComparison(safetyChanges, "안전 로직")
                });
            }
        }

        // 조건문 변경 검사 - 구체적인 조건 변경 내용 포함
        var oldConditionLines = ExtractMatchingLinesWithContext(oldCode, ConditionPattern);
        var newConditionLines = ExtractMatchingLinesWithContext(newCode, ConditionPattern);
        var conditionChanges = CompareLinesDetailed(oldConditionLines, newConditionLines);

        if (conditionChanges.HasChanges)
        {
            comments.Add(new ReviewComment
            {
                Severity = ReviewSeverity.Warning,
                Category = ReviewCategory.ControlFlow,
                Title = "제어 흐름 구조 변경",
                Description = $"조건문/반복문 변경: {conditionChanges.AddedCount}개 추가, {conditionChanges.RemovedCount}개 삭제, {conditionChanges.ModifiedCount}개 수정",
                Impact = "프로그램의 분기 구조가 변경되어 예상치 못한 동작이 발생할 수 있습니다.",
                Recommendation = "모든 분기 경로가 의도한 대로 동작하는지 테스트하세요.",
                CodeSnippet = BuildDetailedCodeComparison(conditionChanges, "조건문")
            });
        }

        // 타이머 설정 변경 검사 - 구체적인 타이머 변경 내용 포함
        var oldTimerLines = ExtractMatchingLinesWithContext(oldCode, TimerCounterPattern);
        var newTimerLines = ExtractMatchingLinesWithContext(newCode, TimerCounterPattern);
        var timerChanges = CompareLinesDetailed(oldTimerLines, newTimerLines);

        if (timerChanges.HasChanges)
        {
            comments.Add(new ReviewComment
            {
                Severity = ReviewSeverity.Warning,
                Category = ReviewCategory.Timing,
                Title = "타이머/카운터 변경",
                Description = $"타이머/카운터 변경: {timerChanges.AddedCount}개 추가, {timerChanges.RemovedCount}개 삭제, {timerChanges.ModifiedCount}개 수정",
                Impact = "타이밍 변경은 공정 사이클 타임과 동기화에 영향을 줄 수 있습니다.",
                Recommendation = "변경된 타이밍이 공정 요구사항을 만족하는지 확인하세요.",
                CodeSnippet = BuildDetailedCodeComparison(timerChanges, "타이머/카운터")
            });
        }

        // 출력 제어 변경 검사 - 구체적인 출력 변경 내용 포함
        var oldOutputLines = ExtractMatchingLinesWithContext(oldCode, OutputPattern);
        var newOutputLines = ExtractMatchingLinesWithContext(newCode, OutputPattern);
        var outputChanges = CompareLinesDetailed(oldOutputLines, newOutputLines);

        if (outputChanges.HasChanges)
        {
            comments.Add(new ReviewComment
            {
                Severity = ReviewSeverity.Warning,
                Category = ReviewCategory.IO,
                Title = "출력 제어 로직 변경",
                Description = $"출력 제어 변경: {outputChanges.AddedCount}개 추가, {outputChanges.RemovedCount}개 삭제, {outputChanges.ModifiedCount}개 수정",
                Impact = "출력 동작 조건이나 타이밍이 변경될 수 있습니다.",
                Recommendation = "출력 제어 조건과 인터락이 올바르게 동작하는지 확인하세요.",
                CodeSnippet = BuildDetailedCodeComparison(outputChanges, "출력 제어")
            });
        }

        // 상태 머신 변경 검사
        var oldStateLines = ExtractMatchingLinesWithContext(oldCode, StateMachinePattern);
        var newStateLines = ExtractMatchingLinesWithContext(newCode, StateMachinePattern);
        var stateChanges = CompareLinesDetailed(oldStateLines, newStateLines);

        if (stateChanges.HasChanges)
        {
            comments.Add(new ReviewComment
            {
                Severity = ReviewSeverity.Warning,
                Category = ReviewCategory.ControlFlow,
                Title = "상태 머신 구조 변경",
                Description = $"상태 전이 변경: {stateChanges.AddedCount}개 추가, {stateChanges.RemovedCount}개 삭제",
                Impact = "시퀀스 동작 순서나 조건이 변경됩니다.",
                Recommendation = "모든 상태 전이 경로를 테스트하고, 시퀀스가 정상적으로 완료되는지 확인하세요.",
                CodeSnippet = BuildDetailedCodeComparison(stateChanges, "상태 전이")
            });
        }

        // 파라미터/설정값 변경 검사
        var oldParamLines = ExtractMatchingLinesWithContext(oldCode, ParameterPattern);
        var newParamLines = ExtractMatchingLinesWithContext(newCode, ParameterPattern);
        var paramChanges = CompareLinesDetailed(oldParamLines, newParamLines);

        if (paramChanges.HasChanges)
        {
            comments.Add(new ReviewComment
            {
                Severity = ReviewSeverity.Info,
                Category = ReviewCategory.Variable,
                Title = "파라미터/설정값 변경",
                Description = $"설정값 변경: {paramChanges.AddedCount}개 추가, {paramChanges.RemovedCount}개 삭제, {paramChanges.ModifiedCount}개 수정",
                Impact = "공정 동작 특성이 변경됩니다.",
                Recommendation = "변경된 설정값이 공정 사양에 맞는지 확인하세요.",
                CodeSnippet = BuildDetailedCodeComparison(paramChanges, "파라미터")
            });
        }

        return comments;
    }

    /// <summary>
    /// 라인별 변경 분석
    /// </summary>
    private LineChangeAnalysis AnalyzeLineChanges(string oldCode, string newCode)
    {
        var oldLines = oldCode.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();
        var newLines = newCode.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();

        var oldSet = new HashSet<string>(oldLines);
        var newSet = new HashSet<string>(newLines);

        return new LineChangeAnalysis
        {
            AddedLines = newLines.Where(l => !oldSet.Contains(l)).ToList(),
            RemovedLines = oldLines.Where(l => !newSet.Contains(l)).ToList()
        };
    }

    /// <summary>
    /// 변경 상세 내역 생성
    /// </summary>
    private string BuildChangeDetails(LineChangeAnalysis changes)
    {
        var sb = new System.Text.StringBuilder();

        if (changes.RemovedLines.Any())
        {
            sb.AppendLine("━━━ 삭제된 코드 ━━━");
            foreach (var line in changes.RemovedLines.Take(10))
            {
                sb.AppendLine($"  - {line}");
            }
            if (changes.RemovedLines.Count > 10)
                sb.AppendLine($"  ... 외 {changes.RemovedLines.Count - 10}줄");
            sb.AppendLine();
        }

        if (changes.AddedLines.Any())
        {
            sb.AppendLine("━━━ 추가된 코드 ━━━");
            foreach (var line in changes.AddedLines.Take(10))
            {
                sb.AppendLine($"  + {line}");
            }
            if (changes.AddedLines.Count > 10)
                sb.AppendLine($"  ... 외 {changes.AddedLines.Count - 10}줄");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// 패턴에 매칭되는 라인과 주변 컨텍스트 추출
    /// </summary>
    private List<string> ExtractMatchingLinesWithContext(string code, Regex pattern)
    {
        var lines = code.Split('\n');
        var result = new List<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            if (pattern.IsMatch(lines[i]))
            {
                result.Add(lines[i].Trim());
            }
        }

        return result;
    }

    /// <summary>
    /// 두 라인 집합을 상세 비교
    /// </summary>
    private DetailedComparison CompareLinesDetailed(List<string> oldLines, List<string> newLines)
    {
        var oldSet = new HashSet<string>(oldLines);
        var newSet = new HashSet<string>(newLines);

        var added = newLines.Where(l => !oldSet.Contains(l)).ToList();
        var removed = oldLines.Where(l => !newSet.Contains(l)).ToList();

        // 유사한 라인 찾기 (수정된 것으로 판단)
        var modified = new List<(string Old, string New)>();
        var usedNew = new HashSet<string>();

        foreach (var oldLine in removed.ToList())
        {
            var bestMatch = added
                .Where(n => !usedNew.Contains(n))
                .Select(n => new { Line = n, Similarity = CalculateSimilarity(oldLine, n) })
                .Where(x => x.Similarity > 0.5)
                .OrderByDescending(x => x.Similarity)
                .FirstOrDefault();

            if (bestMatch != null)
            {
                modified.Add((oldLine, bestMatch.Line));
                removed.Remove(oldLine);
                added.Remove(bestMatch.Line);
                usedNew.Add(bestMatch.Line);
            }
        }

        return new DetailedComparison
        {
            Added = added,
            Removed = removed,
            Modified = modified
        };
    }

    /// <summary>
    /// 두 문자열의 유사도 계산 (0~1)
    /// </summary>
    private double CalculateSimilarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;

        var aWords = a.Split(new[] { ' ', '\t', ':', ';', '(', ')', '=' }, StringSplitOptions.RemoveEmptyEntries);
        var bWords = b.Split(new[] { ' ', '\t', ':', ';', '(', ')', '=' }, StringSplitOptions.RemoveEmptyEntries);

        var commonWords = aWords.Intersect(bWords, StringComparer.OrdinalIgnoreCase).Count();
        var totalWords = Math.Max(aWords.Length, bWords.Length);

        return totalWords > 0 ? (double)commonWords / totalWords : 0;
    }

    /// <summary>
    /// 안전 변경 설명 생성
    /// </summary>
    private string BuildSafetyChangeDescription(DetailedComparison changes)
    {
        var parts = new List<string>();

        if (changes.Removed.Any())
        {
            var keywords = changes.Removed
                .SelectMany(l => SafetyKeywordPattern.Matches(l).Select(m => m.Value))
                .Distinct()
                .Take(3);
            parts.Add($"안전 키워드 제거됨: {string.Join(", ", keywords)}");
        }

        if (changes.Added.Any())
        {
            var keywords = changes.Added
                .SelectMany(l => SafetyKeywordPattern.Matches(l).Select(m => m.Value))
                .Distinct()
                .Take(3);
            parts.Add($"안전 키워드 추가됨: {string.Join(", ", keywords)}");
        }

        if (changes.Modified.Any())
        {
            parts.Add($"안전 로직 {changes.Modified.Count}개 수정됨");
        }

        return string.Join(". ", parts);
    }

    /// <summary>
    /// 상세 코드 비교 결과 생성
    /// </summary>
    private string BuildDetailedCodeComparison(DetailedComparison changes, string category)
    {
        var sb = new System.Text.StringBuilder();

        if (changes.Removed.Any())
        {
            sb.AppendLine($"━━━ 삭제된 {category} ━━━");
            foreach (var line in changes.Removed.Take(5))
            {
                sb.AppendLine($"  [-] {line}");
            }
            if (changes.Removed.Count > 5)
                sb.AppendLine($"  ... 외 {changes.Removed.Count - 5}개");
            sb.AppendLine();
        }

        if (changes.Added.Any())
        {
            sb.AppendLine($"━━━ 추가된 {category} ━━━");
            foreach (var line in changes.Added.Take(5))
            {
                sb.AppendLine($"  [+] {line}");
            }
            if (changes.Added.Count > 5)
                sb.AppendLine($"  ... 외 {changes.Added.Count - 5}개");
            sb.AppendLine();
        }

        if (changes.Modified.Any())
        {
            sb.AppendLine($"━━━ 수정된 {category} ━━━");
            foreach (var (old, @new) in changes.Modified.Take(5))
            {
                sb.AppendLine($"  [이전] {old}");
                sb.AppendLine($"  [이후] {@new}");
                sb.AppendLine();
            }
            if (changes.Modified.Count > 5)
                sb.AppendLine($"  ... 외 {changes.Modified.Count - 5}개");
        }

        return sb.ToString().TrimEnd();
    }

    private class LineChangeAnalysis
    {
        public List<string> AddedLines { get; init; } = new();
        public List<string> RemovedLines { get; init; } = new();
    }

    private class DetailedComparison
    {
        public List<string> Added { get; init; } = new();
        public List<string> Removed { get; init; } = new();
        public List<(string Old, string New)> Modified { get; init; } = new();

        public bool HasChanges => Added.Any() || Removed.Any() || Modified.Any();
        public int AddedCount => Added.Count;
        public int RemovedCount => Removed.Count;
        public int ModifiedCount => Modified.Count;
    }

    /// <summary>
    /// 변경 사항 분석
    /// </summary>
    private ChangeAnalysis AnalyzeChanges(string oldCode, string newCode)
    {
        var oldLines = oldCode.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToHashSet();
        var newLines = newCode.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToHashSet();

        var added = newLines.Except(oldLines).Count();
        var removed = oldLines.Except(newLines).Count();
        var unchanged = oldLines.Intersect(newLines).Count();

        var descriptions = new List<string>();
        if (added > 0) descriptions.Add($"{added}줄 추가");
        if (removed > 0) descriptions.Add($"{removed}줄 삭제");
        if (unchanged > 0) descriptions.Add($"{unchanged}줄 유지");

        return new ChangeAnalysis
        {
            AddedLines = added,
            RemovedLines = removed,
            UnchangedLines = unchanged,
            ChangeDescription = string.Join(", ", descriptions)
        };
    }

    /// <summary>
    /// 패턴에 매칭되는 라인 추출
    /// </summary>
    private string ExtractMatchingLines(string code, Regex pattern)
    {
        var lines = code.Split('\n')
            .Where(line => pattern.IsMatch(line))
            .Select(line => line.Trim())
            .ToList();

        return string.Join("\n", lines);
    }

    /// <summary>
    /// 타이머 값 추출
    /// </summary>
    private HashSet<string> ExtractTimerValues(string code)
    {
        var values = new HashSet<string>();
        var matches = TimerCounterPattern.Matches(code);

        foreach (Match match in matches)
        {
            values.Add(match.Value);
        }

        return values;
    }

    private class ChangeAnalysis
    {
        public int AddedLines { get; init; }
        public int RemovedLines { get; init; }
        public int UnchangedLines { get; init; }
        public string ChangeDescription { get; init; } = string.Empty;
    }

    #endregion
}
