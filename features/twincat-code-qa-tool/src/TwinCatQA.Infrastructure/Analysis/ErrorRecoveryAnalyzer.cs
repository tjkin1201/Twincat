using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.Analysis;

/// <summary>
/// 에러 복구 분석기 구현
/// </summary>
public class ErrorRecoveryAnalyzer : IErrorRecoveryAnalyzer
{
    public List<ErrorRecoveryIssue> Analyze(ValidationSession session)
    {
        var issues = new List<ErrorRecoveryIssue>();

        foreach (var file in session.Files)
        {
            var lines = file.Content.Split('\n');

            issues.AddRange(CheckErrorResetLogic(file.FilePath, lines, file.Content));
            issues.AddRange(CheckWatchdog(file.FilePath, lines, file.Content));
            issues.AddRange(CheckSafeStateTransition(file.FilePath, lines, file.Content));
        }

        return issues;
    }

    /// <summary>
    /// 에러 리셋 로직 검사
    /// </summary>
    private List<ErrorRecoveryIssue> CheckErrorResetLogic(string filePath, string[] lines, string content)
    {
        var issues = new List<ErrorRecoveryIssue>();

        // 에러 변수 검출 패턴
        var errorVarPattern = new Regex(
            @"(b\w*Error|b\w*Fault|b\w*Alarm|\w+Error|\w+Fault)\s*:\s*BOOL",
            RegexOptions.IgnoreCase);

        var errorVars = new List<(string Name, int Line)>();

        // 에러 변수 수집
        for (int i = 0; i < lines.Length; i++)
        {
            var matches = errorVarPattern.Matches(lines[i]);
            foreach (Match match in matches)
            {
                errorVars.Add((match.Groups[1].Value, i + 1));
            }
        }

        // 각 에러 변수에 대해 리셋 로직 확인
        foreach (var (errorVar, declLine) in errorVars)
        {
            // 에러 변수가 TRUE로 설정되는 곳 찾기
            bool hasErrorSet = Regex.IsMatch(content, $@"\b{Regex.Escape(errorVar)}\s*:=\s*TRUE", RegexOptions.IgnoreCase);

            if (!hasErrorSet) continue;

            // 리셋 로직 패턴 확인
            bool hasReset = false;

            // 패턴 1: bErrorReset이나 bReset 버튼으로 리셋
            if (Regex.IsMatch(content, $@"IF\s+\w*(Reset|Clear|Ack)\w*\s+THEN[\s\S]*?{Regex.Escape(errorVar)}\s*:=\s*FALSE", RegexOptions.IgnoreCase))
                hasReset = true;

            // 패턴 2: 직접 FALSE 할당 (조건부)
            if (Regex.IsMatch(content, $@"IF\s+.*THEN[\s\S]*?{Regex.Escape(errorVar)}\s*:=\s*FALSE", RegexOptions.IgnoreCase))
                hasReset = true;

            // 패턴 3: R_TRIG로 에러 리셋
            if (Regex.IsMatch(content, $@"R_TRIG.*Reset[\s\S]*?{Regex.Escape(errorVar)}", RegexOptions.IgnoreCase))
                hasReset = true;

            if (!hasReset)
            {
                issues.Add(new ErrorRecoveryIssue
                {
                    Type = ErrorRecoveryIssueType.MissingErrorReset,
                    FilePath = filePath,
                    Line = declLine,
                    RelatedElement = errorVar,
                    Severity = IssueSeverity.High,
                    Message = $"에러 변수 '{errorVar}'에 리셋 로직이 없음 - 에러 발생 후 복구 불가",
                    Recommendation = "에러 리셋 버튼/조건을 추가하세요: IF bErrorReset THEN bError := FALSE; END_IF"
                });
            }
        }

        return issues;
    }

    /// <summary>
    /// 워치독 타이머 검사
    /// </summary>
    private List<ErrorRecoveryIssue> CheckWatchdog(string filePath, string[] lines, string content)
    {
        var issues = new List<ErrorRecoveryIssue>();

        // 주요 실행 로직이 있는 파일인지 확인
        bool hasMainLogic = Regex.IsMatch(content, @"\bPROGRAM\s+MAIN\b", RegexOptions.IgnoreCase) ||
                           Regex.IsMatch(content, @"\bMAIN\s*\(", RegexOptions.IgnoreCase);

        if (!hasMainLogic) return issues;

        // 워치독 패턴 검색
        bool hasWatchdog = Regex.IsMatch(content, @"\b(watchdog|wdog|heartbeat|alive|health)\b", RegexOptions.IgnoreCase) ||
                          Regex.IsMatch(content, @"FB_\w*Watch\w*", RegexOptions.IgnoreCase);

        if (!hasWatchdog)
        {
            issues.Add(new ErrorRecoveryIssue
            {
                Type = ErrorRecoveryIssueType.MissingWatchdog,
                FilePath = filePath,
                Line = 1,
                RelatedElement = "MAIN",
                Severity = IssueSeverity.High,
                Message = "메인 프로그램에 워치독 타이머가 없음 - 프로그램 행(Hang) 감지 불가",
                Recommendation = "주기적으로 토글되는 워치독 비트를 추가하고 외부에서 모니터링하세요"
            });
        }

        return issues;
    }

    /// <summary>
    /// 에러 발생 시 안전 상태 전환 검사
    /// </summary>
    private List<ErrorRecoveryIssue> CheckSafeStateTransition(string filePath, string[] lines, string content)
    {
        var issues = new List<ErrorRecoveryIssue>();

        // 에러 조건문 찾기
        var errorIfPattern = new Regex(
            @"IF\s+(\w*(?:Error|Fault|Alarm)\w*)\s+THEN",
            RegexOptions.IgnoreCase);

        for (int i = 0; i < lines.Length; i++)
        {
            var match = errorIfPattern.Match(lines[i]);
            if (!match.Success) continue;

            // IF 블록 내용 검사 (END_IF까지)
            var blockContent = new System.Text.StringBuilder();
            int nestLevel = 1;
            for (int j = i + 1; j < lines.Length && nestLevel > 0; j++)
            {
                if (Regex.IsMatch(lines[j], @"\bIF\b", RegexOptions.IgnoreCase))
                    nestLevel++;
                if (Regex.IsMatch(lines[j], @"\bEND_IF\b", RegexOptions.IgnoreCase))
                    nestLevel--;
                blockContent.AppendLine(lines[j]);
            }

            var block = blockContent.ToString();

            // 안전 상태 전환 패턴 확인
            bool hasSafeAction = false;

            // 출력 OFF
            if (Regex.IsMatch(block, @":=\s*FALSE", RegexOptions.IgnoreCase))
                hasSafeAction = true;

            // 상태 전환
            if (Regex.IsMatch(block, @"(nState|eState|State)\s*:=", RegexOptions.IgnoreCase))
                hasSafeAction = true;

            // 정지 명령
            if (Regex.IsMatch(block, @"(Stop|Halt|Disable|Off)\s*:=\s*TRUE", RegexOptions.IgnoreCase))
                hasSafeAction = true;

            if (!hasSafeAction)
            {
                issues.Add(new ErrorRecoveryIssue
                {
                    Type = ErrorRecoveryIssueType.MissingSafeState,
                    FilePath = filePath,
                    Line = i + 1,
                    RelatedElement = match.Groups[1].Value,
                    Severity = IssueSeverity.High,
                    Message = $"에러 조건 '{match.Groups[1].Value}' 발생 시 안전 상태로 전환하는 로직이 없음",
                    Recommendation = "에러 발생 시 출력을 OFF하거나 안전 상태로 전환하세요"
                });
            }
        }

        return issues;
    }
}

/// <summary>
/// 로깅 분석기 구현
/// </summary>
public class LoggingAnalyzer : ILoggingAnalyzer
{
    public List<LoggingIssue> Analyze(ValidationSession session)
    {
        var issues = new List<LoggingIssue>();

        foreach (var file in session.Files)
        {
            var lines = file.Content.Split('\n');

            issues.AddRange(CheckErrorLogging(file.FilePath, lines, file.Content));
            issues.AddRange(CheckStateChangeLogging(file.FilePath, lines, file.Content));
            issues.AddRange(CheckOperationLogging(file.FilePath, lines, file.Content));
        }

        return issues;
    }

    /// <summary>
    /// 에러/알람 로깅 검사
    /// </summary>
    private List<LoggingIssue> CheckErrorLogging(string filePath, string[] lines, string content)
    {
        var issues = new List<LoggingIssue>();

        // 에러 설정 패턴 찾기
        var errorSetPattern = new Regex(
            @"(\w*(?:Error|Fault|Alarm)\w*)\s*:=\s*TRUE",
            RegexOptions.IgnoreCase);

        for (int i = 0; i < lines.Length; i++)
        {
            var match = errorSetPattern.Match(lines[i]);
            if (!match.Success) continue;

            // 주변 5줄 내에 로깅 코드가 있는지 확인
            bool hasLogging = false;
            int start = Math.Max(0, i - 2);
            int end = Math.Min(lines.Length - 1, i + 5);

            for (int j = start; j <= end; j++)
            {
                if (IsLoggingCode(lines[j]))
                {
                    hasLogging = true;
                    break;
                }
            }

            if (!hasLogging)
            {
                issues.Add(new LoggingIssue
                {
                    Type = LoggingIssueType.MissingErrorLogging,
                    FilePath = filePath,
                    Line = i + 1,
                    RelatedElement = match.Groups[1].Value,
                    Severity = IssueSeverity.Medium,
                    Message = $"에러 '{match.Groups[1].Value}' 발생 시 로깅이 없음 - 문제 추적 어려움",
                    Recommendation = "ADSLOGSTR 또는 이벤트 로깅 FB를 사용하여 에러를 기록하세요"
                });
            }
        }

        return issues;
    }

    /// <summary>
    /// 상태 변경 로깅 검사
    /// </summary>
    private List<LoggingIssue> CheckStateChangeLogging(string filePath, string[] lines, string content)
    {
        var issues = new List<LoggingIssue>();

        // 상태 변경 패턴
        var stateChangePattern = new Regex(
            @"(nState|eState|State)\s*:=\s*(\d+|\w+)",
            RegexOptions.IgnoreCase);

        // CASE 문 내 상태 변경 수집
        bool inCase = false;
        int caseStartLine = 0;
        var stateChanges = new List<int>();

        for (int i = 0; i < lines.Length; i++)
        {
            if (Regex.IsMatch(lines[i], @"\bCASE\s+\w+\s+OF", RegexOptions.IgnoreCase))
            {
                inCase = true;
                caseStartLine = i + 1;
                stateChanges.Clear();
            }

            if (inCase && stateChangePattern.IsMatch(lines[i]))
            {
                stateChanges.Add(i + 1);
            }

            if (Regex.IsMatch(lines[i], @"\bEND_CASE\b", RegexOptions.IgnoreCase))
            {
                // 상태 변경이 3개 이상이고 로깅이 없으면 경고
                if (stateChanges.Count >= 3)
                {
                    bool hasAnyLogging = false;
                    for (int j = caseStartLine - 1; j <= i; j++)
                    {
                        if (IsLoggingCode(lines[j]))
                        {
                            hasAnyLogging = true;
                            break;
                        }
                    }

                    if (!hasAnyLogging)
                    {
                        issues.Add(new LoggingIssue
                        {
                            Type = LoggingIssueType.MissingStateChangeLogging,
                            FilePath = filePath,
                            Line = caseStartLine,
                            RelatedElement = "StateMachine",
                            Severity = IssueSeverity.Low,
                            Message = $"상태머신에 {stateChanges.Count}개 상태 전이가 있지만 로깅 없음 - 디버깅 어려움",
                            Recommendation = "상태 전이 시 이전/현재 상태를 로깅하세요"
                        });
                    }
                }

                inCase = false;
            }
        }

        return issues;
    }

    /// <summary>
    /// 중요 동작 로깅 검사
    /// </summary>
    private List<LoggingIssue> CheckOperationLogging(string filePath, string[] lines, string content)
    {
        var issues = new List<LoggingIssue>();

        // 중요 동작 패턴 (모터 시작, 밸브 열기 등)
        var criticalOpPattern = new Regex(
            @"(b\w*(?:Motor|Pump|Valve|Heater|Cylinder)\w*)\s*:=\s*TRUE",
            RegexOptions.IgnoreCase);

        var criticalOps = new List<(string Name, int Line)>();

        for (int i = 0; i < lines.Length; i++)
        {
            var match = criticalOpPattern.Match(lines[i]);
            if (match.Success)
            {
                criticalOps.Add((match.Groups[1].Value, i + 1));
            }
        }

        // 5개 이상 중요 동작이 있는데 로깅이 전혀 없으면 경고
        if (criticalOps.Count >= 5)
        {
            bool hasAnyLogging = lines.Any(line => IsLoggingCode(line));

            if (!hasAnyLogging)
            {
                issues.Add(new LoggingIssue
                {
                    Type = LoggingIssueType.MissingOperationLogging,
                    FilePath = filePath,
                    Line = criticalOps.First().Line,
                    RelatedElement = $"{criticalOps.Count}개 액추에이터",
                    Severity = IssueSeverity.Medium,
                    Message = $"{criticalOps.Count}개 중요 액추에이터 동작이 있지만 로깅 없음",
                    Recommendation = "주요 동작(모터 시작/정지, 밸브 개폐 등)을 로깅하세요"
                });
            }
        }

        return issues;
    }

    /// <summary>
    /// 로깅 코드 여부 확인
    /// </summary>
    private bool IsLoggingCode(string line)
    {
        // TwinCAT 로깅 패턴
        return Regex.IsMatch(line, @"\b(ADSLOGSTR|ADSLOGDINT|FB_\w*Log\w*|WriteLog|AddEvent|LogMessage|EventLog)\b", RegexOptions.IgnoreCase) ||
               Regex.IsMatch(line, @"\b(FB_EventLogger|FB_AlarmManager|FB_Message)\b", RegexOptions.IgnoreCase);
    }
}
