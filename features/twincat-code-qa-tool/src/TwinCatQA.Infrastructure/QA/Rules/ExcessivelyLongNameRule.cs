using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.QA.Rules;

/// <summary>
/// 긴 변수명 감지 규칙 (Info)
/// 변수명이 50자 이상인 경우 간결한 명명 권장
/// </summary>
public class ExcessivelyLongNameRule : IQARuleChecker
{
    public string RuleId => "QA019";
    public string RuleName => "과도하게 긴 변수명 감지";
    public string Description => "변수명 간결화 권장 (50자 이상)";
    public Severity Severity => Severity.Info;

    private const int MaxNameLengthThreshold = 50;

    public List<QAIssue> CheckVariableChange(VariableChange change)
    {
        var issues = new List<QAIssue>();

        // 추가되거나 수정된 변수만 검사
        if (change.ChangeType != ChangeType.Added &&
            change.ChangeType != ChangeType.Modified)
        {
            return issues;
        }

        var variableName = change.VariableName;

        if (variableName.Length >= MaxNameLengthThreshold)
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Info,
                Category = "코드 품질",
                Title = "과도하게 긴 변수명으로 인한 가독성 저하",
                Description = $"{variableName}: 변수명 길이 {variableName.Length}자 (권장: {MaxNameLengthThreshold}자 미만)",
                Location = $"{change.FilePath}:{change.Line}",
                FilePath = change.FilePath,
                Line = change.Line,
                WhyDangerous = $@"
변수명이 {variableName.Length}자로 매우 길어 다음 문제가 발생합니다:

1. 가독성 저하: 코드 라인이 불필요하게 길어짐
2. 타이핑 오류: 긴 이름은 입력 실수 가능성 증가
3. 코드 래핑: 80-120자 코딩 규칙 초과로 줄바꿈 발생
4. 인지 부하: 변수명을 읽고 이해하는 데 시간 소요

좋은 변수명은 간결하면서도 의미를 명확히 전달해야 합니다.
일반적으로 20-30자 이내가 적절하며, 최대 50자를 넘지 않아야 합니다.
",
                Recommendation = $@"
✅ 권장 해결 방법:

1. 약어 사용 (일관된 규칙 적용)
   // 너무 긴 이름
   conveyorBeltMotorSpeedSetpointValue → conveyorMotorSpeed
   isEmergencyStopButtonPressedState → isEmergencyStop

2. 문맥상 불필요한 단어 제거
   // 'Variable', 'Value', 'Data' 등 제거
   temperatureSensorValueData → temperature
   motorSpeedValueVariable → motorSpeed

3. 계층 구조 활용 (구조체/FB 멤버)
   // FB 내부에서는 짧은 이름 사용
   FB_ConveyorControl.speed  // (전체: conveyorSpeed)
   FB_ConveyorControl.status // (전체: conveyorStatus)

4. 공통 접두사를 타입/구조체로 그룹화
   TYPE ST_ConveyorData :
   STRUCT
       speed : REAL;           // 개별 필드는 짧게
       position : LREAL;
       status : INT;
   END_STRUCT
   END_TYPE

5. 약어 사전 정의 및 문서화
   // 프로젝트 공통 약어
   Temp → Temperature
   Pos → Position
   Ctrl → Control
   Cfg → Configuration
",
                Examples = new List<string>
                {
                    "// ❌ 과도하게 긴 변수명",
                    "VAR",
                    "    mainConveyorBeltDriveMotorCurrentSpeedValue : REAL;",
                    "    isEmergencyStopButtonCurrentlyPressedStatus : BOOL;",
                    "END_VAR",
                    "",
                    "// ✅ 간결하고 명확한 변수명",
                    "VAR",
                    "    conveyorSpeed : REAL;         // 맥락상 'main', 'current' 불필요",
                    "    isEmergencyStop : BOOL;       // 'Currently', 'Status' 제거",
                    "END_VAR",
                    "",
                    "// ✅ 구조체로 그룹화",
                    "TYPE ST_ConveyorMotor :",
                    "STRUCT",
                    "    speed : REAL;          // FB_Conveyor.motor.speed",
                    "    current : REAL;        // FB_Conveyor.motor.current",
                    "    temperature : REAL;    // FB_Conveyor.motor.temperature",
                    "END_STRUCT",
                    "END_TYPE"
                },
                RuleId = RuleId,
                OldCodeSnippet = change.OldDataType != null ?
                    $"{change.VariableName} : {change.OldDataType}" : string.Empty,
                NewCodeSnippet = change.NewDataType != null ?
                    $"{change.VariableName} : {change.NewDataType}" : string.Empty
            });
        }

        return issues;
    }

    public List<QAIssue> CheckLogicChange(LogicChange change)
    {
        // 로직 변경에서는 검사하지 않음 (변수명은 VariableChange에서 검사)
        return new List<QAIssue>();
    }

    public List<QAIssue> CheckDataTypeChange(DataTypeChange change)
    {
        var issues = new List<QAIssue>();

        // 추가되거나 수정된 타입만 검사
        if (change.ChangeType != ChangeType.Added &&
            change.ChangeType != ChangeType.Modified)
        {
            return issues;
        }

        // 타입명 길이 검사
        var typeName = change.TypeName;
        if (typeName.Length >= MaxNameLengthThreshold)
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Info,
                Category = "코드 품질",
                Title = "과도하게 긴 타입명으로 인한 가독성 저하",
                Description = $"{typeName}: 타입명 길이 {typeName.Length}자 (권장: {MaxNameLengthThreshold}자 미만)",
                Location = $"{change.FilePath}:{change.Line}",
                FilePath = change.FilePath,
                Line = change.Line,
                WhyDangerous = $@"
타입명이 {typeName.Length}자로 매우 길어 코드 가독성이 저하됩니다.
타입명은 자주 사용되므로 간결해야 합니다.
",
                Recommendation = "타입명을 간결하게 변경하거나 약어를 사용하세요.",
                RuleId = RuleId
            });
        }

        // 구조체 필드명 길이 검사
        foreach (var field in change.FieldChanges)
        {
            if (field.FieldName.Length >= MaxNameLengthThreshold)
            {
                issues.Add(new QAIssue
                {
                    Severity = Severity.Info,
                    Category = "코드 품질",
                    Title = "과도하게 긴 필드명으로 인한 가독성 저하",
                    Description = $"{typeName}.{field.FieldName}: 필드명 길이 {field.FieldName.Length}자",
                    Location = $"{change.FilePath}:{change.Line}",
                    FilePath = change.FilePath,
                    Line = change.Line,
                    WhyDangerous = "구조체 필드명이 너무 길면 코드 가독성이 저하됩니다.",
                    Recommendation = "필드명을 간결하게 변경하세요. 구조체명과 중복되는 부분은 제거할 수 있습니다.",
                    RuleId = RuleId
                });
            }
        }

        return issues;
    }
}
