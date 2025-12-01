using System.Text.Json.Serialization;

namespace TwinCatQA.Infrastructure.Configuration;

/// <summary>
/// TwinCAT QA 도구의 전체 설정을 나타내는 루트 모델
/// .twincat-qa.json 파일의 구조를 정의
/// </summary>
public class QAConfiguration
{
    /// <summary>
    /// 설정 파일 버전 (예: "2.0")
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "2.0";

    /// <summary>
    /// 프로젝트 이름
    /// </summary>
    [JsonPropertyName("projectName")]
    public string? ProjectName { get; set; }

    /// <summary>
    /// 전역 제외 규칙 (파일 패턴, 규칙 ID 등)
    /// </summary>
    [JsonPropertyName("globalExclusions")]
    public GlobalExclusions GlobalExclusions { get; set; } = new();

    /// <summary>
    /// 규칙별 오버라이드 설정 (규칙 ID를 키로 사용)
    /// </summary>
    [JsonPropertyName("ruleOverrides")]
    public Dictionary<string, RuleOverride> RuleOverrides { get; set; } = new();

    /// <summary>
    /// 파일 패턴별 오버라이드 설정 (Glob 패턴을 키로 사용)
    /// </summary>
    [JsonPropertyName("fileOverrides")]
    public Dictionary<string, FileOverride> FileOverrides { get; set; } = new();

    /// <summary>
    /// 인라인 억제(suppression) 설정
    /// </summary>
    [JsonPropertyName("inlineSuppressions")]
    public InlineSuppressionConfig InlineSuppressions { get; set; } = new();
}

/// <summary>
/// 전역 제외 규칙 설정
/// </summary>
public class GlobalExclusions
{
    /// <summary>
    /// 제외할 파일 패턴 목록 (Glob 패턴 지원)
    /// 예: "**/Generated/**", "**/Test/**"
    /// </summary>
    [JsonPropertyName("files")]
    public List<string> Files { get; set; } = new();

    /// <summary>
    /// 전역적으로 비활성화할 규칙 ID 목록
    /// 예: ["SA0029", "SA0033"]
    /// </summary>
    [JsonPropertyName("rules")]
    public List<string> Rules { get; set; } = new();

    /// <summary>
    /// 제외할 디렉토리 패턴 목록
    /// 예: ["**/bin/**", "**/obj/**"]
    /// </summary>
    [JsonPropertyName("directories")]
    public List<string> Directories { get; set; } = new();
}

/// <summary>
/// 특정 규칙에 대한 오버라이드 설정
/// </summary>
public class RuleOverride
{
    /// <summary>
    /// 규칙 활성화 여부
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 심각도 수준 오버라이드
    /// Info, Warning, Error, Critical 중 하나
    /// </summary>
    [JsonPropertyName("severity")]
    public string? Severity { get; set; }

    /// <summary>
    /// 규칙별 커스텀 파라미터 (규칙에 따라 다름)
    /// 예: SA0049의 경우 maxMagicNumberValue, allowedValues 등
    /// </summary>
    [JsonPropertyName("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }

    /// <summary>
    /// 이 규칙이 적용될 파일 패턴 (null이면 모든 파일)
    /// </summary>
    [JsonPropertyName("filePatterns")]
    public List<string>? FilePatterns { get; set; }

    /// <summary>
    /// 이 규칙에서 제외할 파일 패턴
    /// </summary>
    [JsonPropertyName("excludePatterns")]
    public List<string>? ExcludePatterns { get; set; }
}

/// <summary>
/// 파일 패턴별 오버라이드 설정
/// </summary>
public class FileOverride
{
    /// <summary>
    /// 최소 심각도 수준 (이 수준 미만의 이슈는 무시)
    /// Info, Warning, Error, Critical 중 하나
    /// </summary>
    [JsonPropertyName("minSeverity")]
    public string? MinSeverity { get; set; }

    /// <summary>
    /// 엄격 모드 활성화 (모든 규칙을 더 엄격하게 적용)
    /// </summary>
    [JsonPropertyName("strictMode")]
    public bool StrictMode { get; set; } = false;

    /// <summary>
    /// 이 파일 패턴에서 활성화할 규칙 목록 (null이면 모든 규칙)
    /// </summary>
    [JsonPropertyName("enabledRules")]
    public List<string>? EnabledRules { get; set; }

    /// <summary>
    /// 이 파일 패턴에서 비활성화할 규칙 목록
    /// </summary>
    [JsonPropertyName("disabledRules")]
    public List<string>? DisabledRules { get; set; }

    /// <summary>
    /// 파일별 커스텀 파라미터
    /// </summary>
    [JsonPropertyName("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }
}

/// <summary>
/// 인라인 억제(suppression) 설정
/// </summary>
public class InlineSuppressionConfig
{
    /// <summary>
    /// 인라인 억제 기능 활성화 여부
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 인라인 억제에 사용할 주석 패턴 목록
    /// {ruleId}는 규칙 ID로 치환됨
    /// 예: "// qa-ignore: {ruleId}", "(* qa-ignore: {ruleId} *)"
    /// </summary>
    [JsonPropertyName("commentPatterns")]
    public List<string> CommentPatterns { get; set; } = new()
    {
        "// qa-ignore: {ruleId}",
        "(* qa-ignore: {ruleId} *)"
    };

    /// <summary>
    /// 블록 억제 시작 패턴 목록
    /// 예: "// qa-ignore-start: {ruleId}", "(* qa-ignore-start: {ruleId} *)"
    /// </summary>
    [JsonPropertyName("blockStartPatterns")]
    public List<string> BlockStartPatterns { get; set; } = new()
    {
        "// qa-ignore-start: {ruleId}",
        "(* qa-ignore-start: {ruleId} *)"
    };

    /// <summary>
    /// 블록 억제 종료 패턴 목록
    /// 예: "// qa-ignore-end", "(* qa-ignore-end *)"
    /// </summary>
    [JsonPropertyName("blockEndPatterns")]
    public List<string> BlockEndPatterns { get; set; } = new()
    {
        "// qa-ignore-end",
        "(* qa-ignore-end *)"
    };

    /// <summary>
    /// 전체 파일 억제 패턴
    /// 예: "// qa-ignore-file: {ruleId}", "(* qa-ignore-file: {ruleId} *)"
    /// </summary>
    [JsonPropertyName("fileSuppressionPatterns")]
    public List<string> FileSuppressionPatterns { get; set; } = new()
    {
        "// qa-ignore-file: {ruleId}",
        "(* qa-ignore-file: {ruleId} *)"
    };

    /// <summary>
    /// 억제 주석이 규칙을 참조하지 않을 때 경고 표시 여부
    /// </summary>
    [JsonPropertyName("warnOnUnusedSuppressions")]
    public bool WarnOnUnusedSuppressions { get; set; } = false;
}
