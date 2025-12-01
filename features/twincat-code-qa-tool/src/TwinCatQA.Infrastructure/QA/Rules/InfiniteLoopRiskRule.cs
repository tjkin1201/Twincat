using System.Text;
using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.QA.Rules;

/// <summary>
/// 무한 루프 위험 감지 규칙 (Warning)
/// WHILE/REPEAT 루프에 타임아웃이나 최대 반복 횟수가 없는 경우 감지
/// </summary>
public class InfiniteLoopRiskRule : IQARuleChecker
{
    public string RuleId => "QA015";
    public string RuleName => "무한 루프 위험 감지";
    public string Description => "WHILE/REPEAT 루프의 무한 실행 위험을 감지하여 안전성 향상";
    public Severity Severity => Severity.Warning;

    public List<QAIssue> CheckVariableChange(VariableChange change)
    {
        // 변수 변경에서는 검사하지 않음
        return new List<QAIssue>();
    }

    public List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        // 추가되거나 수정된 로직만 검사
        if (change.ChangeType != ChangeType.Added && change.ChangeType != ChangeType.Modified)
        {
            return issues;
        }

        var code = change.NewCode ?? string.Empty;
        var riskyLoops = FindRiskyLoops(code);

        foreach (var loop in riskyLoops)
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Warning,
                Category = "안전성",
                Title = "무한 루프 위험",
                Description = $"{change.ElementName}: {loop.Type} 루프에 안전장치 없음",
                Location = $"{change.FilePath}:{loop.LineNumber}",
                FilePath = change.FilePath,
                Line = loop.LineNumber,
                WhyDangerous = @"
타임아웃/카운터 없는 루프는 다음 위험을 야기합니다:
- PLC 사이클 타임 초과 (Watchdog 오류)
- 시스템 응답 정지
- 실시간 제어 실패
- 전체 시스템 다운
- 안전 기능 미동작
",
                Recommendation = $@"
✅ 권장 해결 방법:

1. 최대 반복 횟수 제한
   VAR
       loopCounter : INT := 0;
       MAX_ITERATIONS : INT := 1000;
   END_VAR

   WHILE condition AND (loopCounter < MAX_ITERATIONS) DO
       // 작업
       loopCounter := loopCounter + 1;
   END_WHILE

   IF loopCounter >= MAX_ITERATIONS THEN
       // 에러 처리
       errorFlag := TRUE;
   END_IF

2. 타임아웃 사용
   VAR
       startTime : TIME;
       TIMEOUT : TIME := T#1s;
   END_VAR

   startTime := TIME();
   WHILE condition AND ((TIME() - startTime) < TIMEOUT) DO
       // 작업
   END_WHILE

   IF (TIME() - startTime) >= TIMEOUT THEN
       // 타임아웃 처리
       timeoutError := TRUE;
   END_IF

3. FOR 루프로 변경 (가능한 경우)
   FOR i := 1 TO 100 DO
       // 명확한 반복 횟수
   END_FOR

4. 상태 머신으로 변경
   CASE state OF
       0: // 초기화
       1: // 작업 (1 사이클에 1번만)
       2: // 완료
   END_CASE

PLC 실시간 원칙:
- 한 사이클에 한 단계씩 처리
- WHILE보다 상태 머신 권장
- 꼭 필요한 경우만 루프 사용
",
                Examples = new List<string>
                {
                    "❌ 위험한 코드:",
                    "   WHILE dataReady DO",
                    "       ProcessData();",
                    "   END_WHILE  // dataReady가 FALSE 안되면?",
                    "",
                    "   REPEAT",
                    "       ReadSensor();",
                    "   UNTIL sensorValid  // 센서 고장이면?",
                    "   END_REPEAT",
                    "",
                    "✅ 안전한 코드:",
                    "   counter := 0;",
                    "   WHILE dataReady AND (counter < 100) DO",
                    "       ProcessData();",
                    "       counter := counter + 1;",
                    "   END_WHILE",
                    "   ",
                    "   IF counter >= 100 THEN",
                    "       errorCode := ERR_TIMEOUT;",
                    "   END_IF"
                },
                RuleId = RuleId,
                OldCodeSnippet = change.OldCode ?? string.Empty,
                NewCodeSnippet = loop.CodeSnippet
            });
        }

        return issues;
    }

    public List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        // 데이터 타입 변경에서는 검사하지 않음
        return new List<QAIssue>();
    }

    /// <summary>
    /// 루프 정보
    /// </summary>
    private class LoopInfo
    {
        public string Type { get; set; } = string.Empty;  // WHILE, REPEAT
        public int LineNumber { get; set; }
        public string CodeSnippet { get; set; } = string.Empty;
        public bool HasSafetyMechanism { get; set; }
    }

    /// <summary>
    /// 위험한 루프 찾기
    /// </summary>
    private List<LoopInfo> FindRiskyLoops(string code)
    {
        var riskyLoops = new List<LoopInfo>();
        var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        bool inLoop = false;
        string loopType = string.Empty;
        int loopStartLine = 0;
        var loopCode = new StringBuilder();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim().ToUpper();

            // WHILE 또는 REPEAT 시작
            if (Regex.IsMatch(trimmed, @"^WHILE\b"))
            {
                inLoop = true;
                loopType = "WHILE";
                loopStartLine = i + 1;
                loopCode.Clear();
                loopCode.AppendLine(line);
            }
            else if (Regex.IsMatch(trimmed, @"^REPEAT\b"))
            {
                inLoop = true;
                loopType = "REPEAT";
                loopStartLine = i + 1;
                loopCode.Clear();
                loopCode.AppendLine(line);
            }
            else if (inLoop)
            {
                loopCode.AppendLine(line);

                // 루프 종료
                if ((loopType == "WHILE" && trimmed == "END_WHILE") ||
                    (loopType == "REPEAT" && Regex.IsMatch(trimmed, @"^UNTIL\b")))
                {
                    var fullLoopCode = loopCode.ToString();

                    // 안전장치 확인
                    if (!HasSafetyMechanism(fullLoopCode))
                    {
                        riskyLoops.Add(new LoopInfo
                        {
                            Type = loopType,
                            LineNumber = loopStartLine,
                            CodeSnippet = fullLoopCode,
                            HasSafetyMechanism = false
                        });
                    }

                    inLoop = false;
                }
            }
        }

        return riskyLoops;
    }

    /// <summary>
    /// 안전장치 존재 여부 확인
    /// </summary>
    private bool HasSafetyMechanism(string loopCode)
    {
        var code = loopCode.ToUpper();

        // 카운터 패턴
        if (Regex.IsMatch(code, @"\b(COUNT|COUNTER|ITERATION|INDEX)\s*<\s*\d+"))
        {
            return true;
        }

        // 타임아웃 패턴
        if (Regex.IsMatch(code, @"\b(TIMEOUT|TIME|ELAPSED)\b"))
        {
            return true;
        }

        // MAX_ 상수 사용
        if (Regex.IsMatch(code, @"\bMAX_\w+"))
        {
            return true;
        }

        return false;
    }
}
