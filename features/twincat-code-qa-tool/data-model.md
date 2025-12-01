# 데이터 모델 설계: TwinCAT 코드 품질 검증 도구

**작성일**: 2025-11-20
**버전**: 1.0.0
**목적**: 도메인 엔티티, 관계, 검증 규칙 정의

---

## 1. 핵심 엔티티 (Core Entities)

### 1.1 CodeFile (코드 파일)

**설명**: TwinCAT 프로젝트의 개별 코드 파일을 나타내는 엔티티

**속성**:
```csharp
public class CodeFile
{
    // 식별자
    public Guid Id { get; set; }
    public string FilePath { get; set; }          // 절대 경로

    // 메타데이터
    public FileType Type { get; set; }            // POU, DUT, GVL
    public ProgrammingLanguage Language { get; set; } // ST, LD, FBD, SFC
    public int LineCount { get; set; }
    public DateTime LastModified { get; set; }
    public string FileHash { get; set; }          // SHA256 (변경 감지용)

    // 파싱 결과
    public SyntaxTree Ast { get; set; }           // ANTLR4 AST
    public List<FunctionBlock> FunctionBlocks { get; set; }
    public List<DataType> DataTypes { get; set; }
    public List<GlobalVariable> GlobalVariables { get; set; }

    // 검증 결과
    public List<Violation> Violations { get; set; }
    public double QualityScore { get; set; }      // 0-100
}

public enum FileType
{
    POU,        // Program Organization Unit (FB, Function, Program)
    DUT,        // Data Unit Type (Struct, Enum, Union)
    GVL,        // Global Variable List
    Unknown
}

public enum ProgrammingLanguage
{
    ST,         // Structured Text
    LD,         // Ladder Diagram
    FBD,        // Function Block Diagram
    SFC,        // Sequential Function Chart
    IL,         // Instruction List (legacy)
    Unknown
}
```

**관계**:
- 1:N → `Violation` (한 파일은 여러 위반 사항 보유)
- 1:N → `FunctionBlock` (한 파일은 여러 Function Block 포함)
- N:1 → `ValidationSession` (여러 파일이 한 검증 세션에 속함)

**검증 규칙**:
- `FilePath`는 절대 경로여야 하며 존재하는 파일을 가리켜야 함
- `LineCount`는 1 이상
- `QualityScore`는 0-100 범위
- `FileHash`는 SHA256 해시 (64자 16진수 문자열)

---

### 1.2 FunctionBlock (함수 블록)

**설명**: IEC 61131-3 Function Block 또는 Function을 나타내는 엔티티

**속성**:
```csharp
public class FunctionBlock
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public FunctionBlockType Type { get; set; }   // FB, Function, Program

    // 위치 정보
    public string FilePath { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }

    // 코드 메트릭
    public int CyclomaticComplexity { get; set; }
    public int LineCount { get; set; }
    public int CommentLineCount { get; set; }
    public double CommentRatio { get; set; }      // 주석 비율 (0-1)

    // 변수 및 의존성
    public List<Variable> InputVariables { get; set; }
    public List<Variable> OutputVariables { get; set; }
    public List<Variable> LocalVariables { get; set; }
    public List<string> Dependencies { get; set; }  // 호출하는 다른 FB 이름

    // 상태 머신 정보
    public bool HasStateMachine { get; set; }
    public string StateEnumType { get; set; }     // 상태 ENUM 타입명
    public List<string> States { get; set; }      // 정의된 모든 상태

    // 주석 및 문서화
    public string Description { get; set; }       // FB 목적 설명 주석
    public bool HasKoreanComment { get; set; }    // 한글 주석 존재 여부
}

public enum FunctionBlockType
{
    FunctionBlock,  // FUNCTION_BLOCK
    Function,       // FUNCTION
    Program,        // PROGRAM
    Method          // METHOD (클래스 메서드)
}
```

**관계**:
- N:1 → `CodeFile` (여러 FB가 한 파일에 속함)
- 1:N → `Variable` (한 FB는 여러 변수 보유)
- 1:N → `Violation` (한 FB는 여러 위반 발생 가능)

**검증 규칙**:
- `Name`은 `FB_` 접두사로 시작해야 함 (헌장 원칙 5)
- `CyclomaticComplexity >= 10`: Medium 경고
- `CyclomaticComplexity >= 15`: High 경고
- `CommentRatio < 0.1`: 주석 부족 경고
- `HasKoreanComment == false`: 한글 주석 부재 경고 (헌장 원칙 1)

---

### 1.3 Variable (변수)

**설명**: PLC 변수 (입력, 출력, 로컬, 전역)

**속성**:
```csharp
public class Variable
{
    public string Name { get; set; }
    public VariableScope Scope { get; set; }
    public string DataType { get; set; }          // INT, REAL, T_MyStruct 등
    public string InitialValue { get; set; }      // 초기값 (선택적)
    public bool IsConstant { get; set; }

    // 위치 정보
    public int DeclarationLine { get; set; }

    // 검증 플래그
    public bool IsUsed { get; set; }              // 사용되지 않는 변수 감지
    public bool FollowsNamingConvention { get; set; }
}

public enum VariableScope
{
    Input,      // VAR_INPUT
    Output,     // VAR_OUTPUT
    InOut,      // VAR_IN_OUT
    Local,      // VAR (로컬)
    Global,     // GVL에 선언된 전역 변수
    Constant    // VAR CONSTANT
}
```

**검증 규칙**:
- `Scope == Global` → `Name`은 `g` 접두사로 시작 (헌장 원칙 5)
- `Scope == Constant` → `Name`은 UPPER_CASE (헌장 원칙 5)
- `Scope == Local` → `Name`은 camelCase (헌장 원칙 5)
- `IsUsed == false` → 미사용 변수 경고

---

### 1.4 DataType (데이터 타입)

**설명**: 사용자 정의 데이터 타입 (구조체, 열거형 등)

**속성**:
```csharp
public class DataType
{
    public string Name { get; set; }
    public DataTypeKind Kind { get; set; }

    // 구조체 필드 (Kind == Struct)
    public List<StructField> Fields { get; set; }

    // 열거형 값 (Kind == Enum)
    public List<EnumValue> EnumValues { get; set; }

    // 위치 정보
    public string FilePath { get; set; }
    public int DeclarationLine { get; set; }
}

public enum DataTypeKind
{
    Struct,     // TYPE T_MyStruct : STRUCT ... END_STRUCT END_TYPE
    Enum,       // TYPE E_State : (Idle, Running, Error) END_TYPE
    Union,      // TYPE T_Union : UNION ... END_UNION END_TYPE
    Alias       // TYPE T_MyInt : INT END_TYPE
}

public class StructField
{
    public string Name { get; set; }
    public string DataType { get; set; }
    public string Comment { get; set; }           // 필드 설명 주석
}

public class EnumValue
{
    public string Name { get; set; }
    public int Value { get; set; }                // 명시적 값 (선택적)
}
```

**검증 규칙**:
- `Kind == Struct` → `Name`은 `T_` 접두사 (헌장 원칙 5)
- `Kind == Enum` → `Name`은 `E_` 접두사 (헌장 원칙 5)
- 레시피 관련 구조체 → 버전 필드 존재 확인 (FR-4)

---

### 1.5 Violation (위반 사항)

**설명**: 검증 규칙 위반 사항

**속성**:
```csharp
public class Violation
{
    public Guid Id { get; set; }

    // 규칙 정보
    public string RuleId { get; set; }            // 예: "FR-1-COMPLEXITY"
    public string RuleName { get; set; }          // 예: "사이클로매틱 복잡도 초과"
    public ConstitutionPrinciple RelatedPrinciple { get; set; }
    public ViolationSeverity Severity { get; set; }

    // 위치 정보
    public string FilePath { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public string CodeSnippet { get; set; }       // 위반 코드 5줄 컨텍스트

    // 설명 및 제안
    public string Message { get; set; }           // 한글 설명
    public string Suggestion { get; set; }        // 수정 제안 (선택적)
    public string DocumentationUrl { get; set; }  // 헌장 또는 문서 링크

    // 메타데이터
    public DateTime DetectedAt { get; set; }
    public bool IsSuppressed { get; set; }        // 사용자가 억제한 경우
}

public enum ConstitutionPrinciple
{
    None = 0,
    KoreanFirst = 1,              // 원칙 1: 한글 우선
    RealTimeSafety = 2,           // 원칙 2: 실시간 안전성
    Modularity = 3,               // 원칙 3: 모듈화 및 재사용성
    StateMachineDesign = 4,       // 원칙 4: 상태 기반 설계
    NamingConvention = 5,         // 원칙 5: 명명 규칙
    Documentation = 6,            // 원칙 6: 문서화 의무
    VersionControl = 7,           // 원칙 7: 버전 관리
    TestingSimulation = 8         // 원칙 8: 테스트 및 시뮬레이션
}

public enum ViolationSeverity
{
    Low,        // 권장 사항
    Medium,     // 주의 필요
    High,       // 수정 권장
    Critical    // 커밋 차단 (pre-commit hook)
}
```

**관계**:
- N:1 → `ValidationSession`
- N:1 → `CodeFile`
- N:1 → `FunctionBlock` (선택적)

**검증 규칙**:
- `Severity == Critical` → Git pre-commit hook에서 커밋 차단
- `Line > 0`
- `Column >= 0`

---

### 1.6 ValidationSession (검증 세션)

**설명**: 한 번의 검증 실행을 나타내는 엔티티

**속성**:
```csharp
public class ValidationSession
{
    public Guid SessionId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }

    // 프로젝트 정보
    public string ProjectPath { get; set; }       // .tsproj 절대 경로
    public string ProjectName { get; set; }

    // 검증 모드
    public ValidationMode Mode { get; set; }
    public string GitCommitHash { get; set; }     // 증분 검증 시 기준 커밋

    // 통계
    public int ScannedFilesCount { get; set; }
    public int TotalLinesOfCode { get; set; }
    public int ViolationsCount { get; set; }
    public Dictionary<ViolationSeverity, int> ViolationsBySeverity { get; set; }

    // 품질 점수
    public double OverallQualityScore { get; set; } // 0-100
    public Dictionary<ConstitutionPrinciple, double> ConstitutionCompliance { get; set; }

    // 결과
    public List<CodeFile> ScannedFiles { get; set; }
    public List<Violation> Violations { get; set; }
    public string ReportHtmlPath { get; set; }    // 생성된 리포트 경로
    public string ReportPdfPath { get; set; }

    // 사용자 정보
    public string ExecutedBy { get; set; }        // 실행한 사용자 (Windows 계정)
}

public enum ValidationMode
{
    Full,           // 프로젝트 전체 검증
    Incremental,    // Git diff 기반 증분 검증
    FileSpecific    // 특정 파일만 검증
}
```

**관계**:
- 1:N → `CodeFile`
- 1:N → `Violation`
- 1:1 → `QualityReport` (생성된 리포트)

**검증 규칙**:
- `EndTime >= StartTime`
- `OverallQualityScore`는 0-100 범위
- `ConstitutionCompliance`의 각 값은 0-1 범위

---

### 1.7 QualityReport (품질 리포트)

**설명**: 생성된 품질 리포트 (HTML/PDF)

**속성**:
```csharp
public class QualityReport
{
    public Guid ReportId { get; set; }
    public Guid SessionId { get; set; }           // 관련 검증 세션

    // 리포트 메타데이터
    public DateTime GeneratedAt { get; set; }
    public ReportFormat Format { get; set; }
    public string FilePath { get; set; }

    // 콘텐츠
    public string Title { get; set; }             // 예: "TwinCAT QA 리포트 - MyProject"
    public ReportSummary Summary { get; set; }
    public List<ViolationGroup> ViolationGroups { get; set; }
    public List<ChartData> Charts { get; set; }

    // 옵션
    public bool IncludeCodeSnippets { get; set; }
    public bool IncludeCharts { get; set; }
}

public enum ReportFormat
{
    HTML,
    PDF
}

public class ReportSummary
{
    public string ProjectName { get; set; }
    public DateTime ValidationDate { get; set; }
    public int TotalFiles { get; set; }
    public int TotalLines { get; set; }
    public double QualityScore { get; set; }
    public Dictionary<ViolationSeverity, int> ViolationCounts { get; set; }
}

public class ViolationGroup
{
    public string GroupName { get; set; }         // 예: "명명 규칙 위반"
    public ViolationSeverity Severity { get; set; }
    public List<Violation> Violations { get; set; }
}

public class ChartData
{
    public string ChartTitle { get; set; }
    public ChartType Type { get; set; }
    public Dictionary<string, double> Data { get; set; }
}

public enum ChartType
{
    Bar,            // 막대 그래프
    Pie,            // 원형 그래프
    Line,           // 선 그래프 (추세)
    Radar           // 레이더 차트 (헌장 준수율)
}
```

**관계**:
- 1:1 → `ValidationSession`

---

### 1.8 ConfigurationProfile (설정 프로파일)

**설명**: 검증 규칙 및 임계값 설정

**속성**:
```csharp
public class ConfigurationProfile
{
    public string ProfileName { get; set; }       // 예: "default", "strict"
    public bool IsDefault { get; set; }

    // 규칙 설정
    public Dictionary<string, RuleConfiguration> Rules { get; set; }

    // 전역 설정
    public List<ReportFormat> ReportFormats { get; set; }
    public bool AutoOpenReport { get; set; }
    public int RetentionMonths { get; set; }      // 데이터 보존 기간

    // Git 훅 설정
    public bool GitPreCommitEnabled { get; set; }
    public bool BlockOnCritical { get; set; }
    public bool IncrementalValidation { get; set; }
}

public class RuleConfiguration
{
    public string RuleId { get; set; }
    public bool Enabled { get; set; }
    public ViolationSeverity Severity { get; set; }
    public Dictionary<string, object> Parameters { get; set; } // 규칙별 파라미터
}
```

**예시 (YAML)**:
```yaml
rules:
  cyclomatic_complexity:
    enabled: true
    severity: medium
    parameters:
      medium_threshold: 10
      high_threshold: 15
```

---

## 2. 엔티티 관계도 (ERD)

```
┌─────────────────────┐
│  ValidationSession  │
│  - SessionId        │
│  - ProjectPath      │
│  - Mode             │
│  - QualityScore     │
└─────────────────────┘
          │ 1
          │
          │ N
┌─────────────────────┐       N  ┌─────────────────────┐
│      CodeFile       │─────────→│     Violation       │
│  - FilePath         │          │  - RuleId           │
│  - Language         │          │  - Severity         │
│  - QualityScore     │          │  - Message          │
└─────────────────────┘          └─────────────────────┘
          │ 1
          │
          │ N
┌─────────────────────┐
│   FunctionBlock     │
│  - Name             │
│  - Complexity       │
│  - HasStateMachine  │
└─────────────────────┘
          │ 1
          │
          │ N
┌─────────────────────┐
│      Variable       │
│  - Name             │
│  - Scope            │
│  - DataType         │
└─────────────────────┘


┌─────────────────────┐       1  ┌─────────────────────┐
│  ValidationSession  │─────────→│   QualityReport     │
│                     │          │  - Format           │
│                     │          │  - FilePath         │
└─────────────────────┘          └─────────────────────┘
```

---

## 3. 상태 전이 다이어그램

### 3.1 ValidationSession 상태

```
[생성됨]
   │
   ├─→ [파일 스캔 중]
   │        │
   │        ├─→ [파싱 중]
   │        │        │
   │        │        ├─→ [검증 중]
   │        │        │        │
   │        │        │        ├─→ [리포트 생성 중]
   │        │        │        │        │
   │        │        │        │        └─→ [완료]
   │        │        │        │
   │        │        │        └─→ [오류 발생]
   │        │        │
   │        │        └─→ [파싱 실패]
   │        │
   │        └─→ [파일 없음]
   │
   └─→ [취소됨]
```

---

## 4. 도메인 서비스 (Domain Services)

### 4.1 ValidationEngine (검증 엔진)

**책임**:
- ValidationSession 생성 및 관리
- 규칙 실행 오케스트레이션
- 위반 사항 집계

**메서드**:
```csharp
public interface IValidationEngine
{
    ValidationSession StartSession(string projectPath, ValidationMode mode);
    void ScanFiles(ValidationSession session);
    void RunValidation(ValidationSession session);
    void GenerateReport(ValidationSession session);
    void CompleteSession(ValidationSession session);
}
```

### 4.2 ParserService (파서 서비스)

**책임**:
- TwinCAT 파일 파싱 (XML → ST 추출)
- ANTLR4 기반 ST 파싱
- AST 생성

**메서드**:
```csharp
public interface IParserService
{
    SyntaxTree ParseFile(string filePath);
    List<FunctionBlock> ExtractFunctionBlocks(SyntaxTree ast);
    List<Variable> ExtractVariables(SyntaxTree ast);
    int CalculateCyclomaticComplexity(FunctionBlock fb);
}
```

### 4.3 ReportGenerator (리포트 생성기)

**책임**:
- HTML/PDF 리포트 생성
- 템플릿 렌더링
- 차트 생성

**메서드**:
```csharp
public interface IReportGenerator
{
    string GenerateHtmlReport(ValidationSession session);
    string GeneratePdfReport(ValidationSession session);
    ChartData CreateQualityTrendChart(List<ValidationSession> sessions);
}
```

### 4.4 GitService (Git 서비스)

**책임**:
- Git diff 분석
- 변경된 파일 추출
- Pre-commit hook 관리

**메서드**:
```csharp
public interface IGitService
{
    List<string> GetChangedFiles(string repoPath);
    List<LineChange> GetChangedLines(string repoPath, string filePath);
    void InstallPreCommitHook(string repoPath);
    bool IsRepositoryClean(string repoPath);
}

public class LineChange
{
    public int LineNumber { get; set; }
    public ChangeType Type { get; set; }  // Added, Modified, Deleted
    public string Content { get; set; }
}
```

---

## 5. 값 객체 (Value Objects)

### 5.1 LineRange (라인 범위)

```csharp
public class LineRange : IEquatable<LineRange>
{
    public int Start { get; }
    public int End { get; }

    public LineRange(int start, int end)
    {
        if (start < 1 || end < start)
            throw new ArgumentException("Invalid line range");

        Start = start;
        End = end;
    }

    public bool Contains(int line) => line >= Start && line <= End;

    public int LineCount => End - Start + 1;

    public bool Equals(LineRange other) =>
        other != null && Start == other.Start && End == other.End;
}
```

### 5.2 QualityScore (품질 점수)

```csharp
public class QualityScore : IComparable<QualityScore>
{
    public double Value { get; }

    public QualityScore(double value)
    {
        if (value < 0 || value > 100)
            throw new ArgumentException("Quality score must be between 0 and 100");

        Value = value;
    }

    public QualityGrade Grade
    {
        get
        {
            if (Value >= 90) return QualityGrade.Excellent;
            if (Value >= 75) return QualityGrade.Good;
            if (Value >= 50) return QualityGrade.Fair;
            return QualityGrade.Poor;
        }
    }

    public int CompareTo(QualityScore other) => Value.CompareTo(other.Value);
}

public enum QualityGrade
{
    Poor,       // < 50
    Fair,       // 50-74
    Good,       // 75-89
    Excellent   // 90-100
}
```

---

## 6. 집계 (Aggregates)

### 6.1 ValidationSession Aggregate

**집계 루트 (Aggregate Root)**: `ValidationSession`

**구성 엔티티**:
- `CodeFile[]`
- `Violation[]`
- `QualityReport`

**불변 조건 (Invariants)**:
- 모든 `Violation`은 `ScannedFiles`에 속한 파일을 참조해야 함
- `ViolationsBySeverity`의 합은 `ViolationsCount`와 일치해야 함
- 세션이 완료되면 `EndTime`이 설정되어야 함

---

## 7. 리포지토리 인터페이스 (Repositories)

### 7.1 ValidationSessionRepository

```csharp
public interface IValidationSessionRepository
{
    void Save(ValidationSession session);
    ValidationSession GetById(Guid sessionId);
    List<ValidationSession> GetByProject(string projectPath, int limit = 10);
    List<ValidationSession> GetRecentSessions(int days = 30);
    void Delete(Guid sessionId);
    void ArchiveOldSessions(int months = 12);
}
```

### 7.2 ViolationRepository

```csharp
public interface IViolationRepository
{
    List<Violation> GetBySession(Guid sessionId);
    List<Violation> GetByFile(string filePath);
    List<Violation> GetBySeverity(ViolationSeverity severity);
    Dictionary<string, int> GetViolationTrendByRule(string projectPath, int days = 30);
}
```

### 7.3 ConfigurationRepository

```csharp
public interface IConfigurationRepository
{
    ConfigurationProfile GetDefaultProfile();
    ConfigurationProfile GetByName(string profileName);
    void Save(ConfigurationProfile profile);
    List<string> GetAllProfileNames();
}
```

---

## 8. 데이터 저장 형식 (JSON 스키마)

### 8.1 ValidationSession JSON

**파일명**: `.twincat-qa/sessions/2025-11-20_103045.json`

```json
{
  "sessionId": "a3f2e1d4-5b6c-7a8b-9c0d-1e2f3a4b5c6d",
  "startTime": "2025-11-20T10:30:45Z",
  "endTime": "2025-11-20T10:31:12Z",
  "duration": "00:00:27",
  "projectPath": "D:\\Projects\\TwinCAT\\MyProject.tsproj",
  "projectName": "MyProject",
  "mode": "Full",
  "scannedFilesCount": 25,
  "totalLinesOfCode": 3542,
  "violationsCount": 27,
  "violationsBySeverity": {
    "Critical": 2,
    "High": 5,
    "Medium": 12,
    "Low": 8
  },
  "overallQualityScore": 87.5,
  "constitutionCompliance": {
    "KoreanFirst": 0.96,
    "RealTimeSafety": 1.0,
    "Modularity": 0.89,
    "StateMachineDesign": 0.92,
    "NamingConvention": 0.95,
    "Documentation": 0.78,
    "VersionControl": 1.0,
    "TestingSimulation": 0.85
  },
  "reportHtmlPath": ".twincat-qa/reports/2025-11-20_103045.html",
  "reportPdfPath": ".twincat-qa/reports/2025-11-20_103045.pdf",
  "executedBy": "DOMAIN\\tjkim1"
}
```

### 8.2 Violations JSON

**파일명**: `.twincat-qa/sessions/2025-11-20_103045_violations.json`

```json
{
  "sessionId": "a3f2e1d4-5b6c-7a8b-9c0d-1e2f3a4b5c6d",
  "violations": [
    {
      "id": "v-001",
      "ruleId": "FR-1-COMPLEXITY",
      "ruleName": "사이클로매틱 복잡도 초과",
      "relatedPrinciple": "StateMachineDesign",
      "severity": "High",
      "filePath": "POUs/FB_TemperatureController.TcPOU",
      "line": 145,
      "column": 5,
      "codeSnippet": "IF temp > maxTemp THEN\n    alarm := TRUE;\n    ...",
      "message": "Function Block 'FB_TemperatureController'의 복잡도가 18로 임계값(15)을 초과합니다.",
      "suggestion": "로직을 여러 Function으로 분리하여 복잡도를 낮추세요.",
      "documentationUrl": "file:///.../constitution.md#원칙-4",
      "detectedAt": "2025-11-20T10:30:58Z",
      "isSuppressed": false
    }
  ]
}
```

---

## 9. 다음 단계

1. **contracts/ 생성**: 내부 인터페이스 및 API 계약 문서화
2. **quickstart.md** 작성: 개발 환경 설정 가이드
3. **plan.md** 작성: 최종 구현 계획서 통합

---

**데이터 모델 설계 완료**: 2025-11-20
**다음 문서**: contracts/IValidationRule.cs
