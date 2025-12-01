using System.Text;
using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.QA.Rules;

/// <summary>
/// CASE ELSE 누락 감지 규칙 (Warning)
/// CASE 문에 ELSE 절이 없어 예상치 못한 값 처리 불가능한 경우 감지
/// </summary>
public class MissingCaseElseRule : IQARuleChecker
{
    public string RuleId => "QA011";
    public string RuleName => "CASE ELSE 누락 감지";
    public string Description => "CASE 문에 ELSE 절이 없어 예외 상황 처리가 불가능한 경우 감지";
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
        var caseStatements = FindCaseStatements(code);

        foreach (var caseInfo in caseStatements)
        {
            if (!caseInfo.HasElse)
            {
                issues.Add(new QAIssue
                {
                    Severity = Severity.Warning,
                    Category = "안전성",
                    Title = "CASE 문에 ELSE 절 누락",
                    Description = $"{change.ElementName}: CASE 문에 기본 처리(ELSE) 없음",
                    Location = $"{change.FilePath}:{caseInfo.LineNumber}",
                    FilePath = change.FilePath,
                    Line = caseInfo.LineNumber,
                    WhyDangerous = @"
CASE ELSE 누락은 다음 위험을 야기합니다:
- 예상치 못한 값 처리 불가
- 시스템 상태 불명확
- 디버깅 어려움 (어느 분기도 실행 안됨)
- 안전 관련 동작 누락 가능
- 런타임 오류 발생 가능
",
                    Recommendation = $@"
✅ 권장 해결 방법:

1. ELSE 절 추가 (에러 처리)
   CASE state OF
       0: // 초기 상태
       1: // 실행 상태
       2: // 완료 상태
   ELSE
       // 예상치 못한 값
       errorFlag := TRUE;
       errorCode := 9999;
       state := 0;  // 안전 상태로 복귀
   END_CASE

2. 모든 가능한 값 명시 (열거형 사용)
   TYPE E_State : (INIT, RUNNING, COMPLETE);
   END_TYPE

   CASE state OF
       E_State.INIT: ...
       E_State.RUNNING: ...
       E_State.COMPLETE: ...
   ELSE
       // 방어 코드
   END_CASE

3. 로그 기록
   ELSE
       LogError('알 수 없는 상태: %d', state);
       state := E_State.INIT;
   END_CASE
",
                    Examples = new List<string>
                    {
                        "❌ 위험한 코드:",
                        "   CASE mode OF",
                        "       1: StartMotor();",
                        "       2: StopMotor();",
                        "   END_CASE  // mode=3이면?",
                        "",
                        "✅ 안전한 코드:",
                        "   CASE mode OF",
                        "       1: StartMotor();",
                        "       2: StopMotor();",
                        "   ELSE",
                        "       errorFlag := TRUE;",
                        "       EmergencyStop();",
                        "   END_CASE"
                    },
                    RuleId = RuleId,
                    OldCodeSnippet = change.OldCode ?? string.Empty,
                    NewCodeSnippet = caseInfo.CaseBlock
                });
            }
        }

        return issues;
    }

    public List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        // 데이터 타입 변경에서는 검사하지 않음
        return new List<QAIssue>();
    }

    /// <summary>
    /// CASE 문 정보
    /// </summary>
    private class CaseStatementInfo
    {
        public bool HasElse { get; set; }
        public int LineNumber { get; set; }
        public string CaseBlock { get; set; } = string.Empty;
    }

    /// <summary>
    /// 코드에서 CASE 문 찾기
    /// </summary>
    private List<CaseStatementInfo> FindCaseStatements(string code)
    {
        var caseStatements = new List<CaseStatementInfo>();
        var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        int currentLine = 0;
        bool inCaseBlock = false;
        int caseStartLine = 0;
        bool hasElse = false;
        var caseBlock = new StringBuilder();

        foreach (var line in lines)
        {
            currentLine++;
            var trimmed = line.Trim().ToUpper();

            // CASE 시작
            if (Regex.IsMatch(trimmed, @"^CASE\s+\w+\s+OF"))
            {
                inCaseBlock = true;
                caseStartLine = currentLine;
                hasElse = false;
                caseBlock.Clear();
                caseBlock.AppendLine(line);
            }
            else if (inCaseBlock)
            {
                caseBlock.AppendLine(line);

                // ELSE 확인
                if (trimmed == "ELSE")
                {
                    hasElse = true;
                }

                // CASE 종료
                if (trimmed == "END_CASE")
                {
                    caseStatements.Add(new CaseStatementInfo
                    {
                        HasElse = hasElse,
                        LineNumber = caseStartLine,
                        CaseBlock = caseBlock.ToString()
                    });
                    inCaseBlock = false;
                }
            }
        }

        return caseStatements;
    }
}
