using System.Security.Cryptography;
using System.Text;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.QA.Rules;

/// <summary>
/// 중복 코드 감지 규칙 (Warning)
/// 동일하거나 유사한 코드 블록이 반복되는 경우 감지
/// </summary>
public class DuplicateCodeRule : IQARuleChecker
{
    public string RuleId => "QA010";
    public string RuleName => "중복 코드 감지";
    public string Description => "동일한 로직이 반복되는 중복 코드를 감지하여 재사용성 향상 권장";
    public Severity Severity => Severity.Warning;

    private const int MIN_DUPLICATE_LINES = 5;

    // 로직 변경 이력 저장 (간단한 중복 검사용)
    private static readonly Dictionary<string, List<LogicChange>> _logicHistory = new();

    public List<QAIssue> CheckVariableChange(VariableChange change)
    {
        // 변수 변경에서는 검사하지 않음
        return new List<QAIssue>();
    }

    public List<QAIssue> CheckLogicChange(LogicChange change)
    {
        var issues = new List<QAIssue>();

        // 추가된 로직만 검사
        if (change.ChangeType != ChangeType.Added)
        {
            return issues;
        }

        var code = change.NewCode ?? string.Empty;
        var normalizedCode = NormalizeCode(code);

        // 최소 라인 수 확인
        if (code.Split('\n').Length < MIN_DUPLICATE_LINES)
        {
            return issues;
        }

        // 코드 해시 계산
        var codeHash = CalculateHash(normalizedCode);

        // 이력에 추가
        if (!_logicHistory.ContainsKey(codeHash))
        {
            _logicHistory[codeHash] = new List<LogicChange>();
        }

        // 중복 검사
        var duplicates = _logicHistory[codeHash];
        if (duplicates.Any())
        {
            var firstOccurrence = duplicates.First();
            issues.Add(new QAIssue
            {
                Severity = Severity.Warning,
                Category = "코드 품질",
                Title = "중복 코드 감지",
                Description = $"{change.ElementName}: '{firstOccurrence.ElementName}'와 유사한 코드 중복",
                Location = $"{change.FilePath}:{change.StartLine}",
                FilePath = change.FilePath,
                Line = change.StartLine,
                WhyDangerous = @"
중복 코드는 다음 문제를 야기합니다:
- 유지보수 비용 증가 (여러 곳 수정 필요)
- 버그 수정 누락 위험
- 코드 크기 증가 (PLC 메모리 낭비)
- 일관성 유지 어려움
- 리팩토링 어려움
",
                Recommendation = $@"
✅ 권장 해결 방법:

1. Function Block으로 공통 로직 추출
   FUNCTION_BLOCK FB_CommonLogic
       VAR_INPUT
           param1 : INT;
           param2 : BOOL;
       END_VAR
       VAR_OUTPUT
           result : INT;
       END_VAR

       // 공통 로직
   END_FUNCTION_BLOCK

   // 사용
   commonLogic(param1 := value1, param2 := flag);

2. Function으로 재사용 가능한 코드 작성
   FUNCTION F_ProcessData : BOOL
       VAR_INPUT
           data : ARRAY[1..10] OF INT;
       END_VAR
       // 처리 로직
   END_FUNCTION

3. 상속 사용 (확장 가능한 구조)
   FUNCTION_BLOCK FB_Base
       // 기본 로직
   END_FUNCTION_BLOCK

   FUNCTION_BLOCK FB_Extended EXTENDS FB_Base
       // 확장 로직
   END_FUNCTION_BLOCK

중복 위치:
- 현재: {change.FilePath}:{change.StartLine}
- 원본: {firstOccurrence.FilePath}:{firstOccurrence.StartLine}
",
                Examples = new List<string>
                {
                    "중복된 코드 패턴 발견:",
                    $"  1. {firstOccurrence.ElementName} ({firstOccurrence.FilePath})",
                    $"  2. {change.ElementName} ({change.FilePath})",
                    "",
                    "리팩토링 권장:",
                    "  - 공통 Function Block 생성",
                    "  - 파라미터로 차이점 처리",
                    "  - 재사용성 향상"
                },
                RuleId = RuleId,
                OldCodeSnippet = $"First: {firstOccurrence.ElementName}",
                NewCodeSnippet = $"Duplicate: {change.ElementName}"
            });
        }

        // 이력에 추가
        duplicates.Add(change);

        return issues;
    }

    public List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        // 데이터 타입 변경에서는 검사하지 않음
        return new List<QAIssue>();
    }

    /// <summary>
    /// 코드 정규화 (공백, 주석 제거)
    /// </summary>
    private string NormalizeCode(string code)
    {
        // 주석 제거
        var withoutComments = System.Text.RegularExpressions.Regex.Replace(
            code, @"//.*$|/\*.*?\*/", "",
            System.Text.RegularExpressions.RegexOptions.Multiline);

        // 공백 정규화
        var normalized = System.Text.RegularExpressions.Regex.Replace(
            withoutComments, @"\s+", " ");

        return normalized.Trim().ToUpper();
    }

    /// <summary>
    /// 코드 해시 계산
    /// </summary>
    private string CalculateHash(string code)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(code);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
