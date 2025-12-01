using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace TwinCatQA.Application.Services;

/// <summary>
/// 단일 TwinCAT 프로젝트 QA 분석기
/// </summary>
public class SingleProjectAnalyzer
{
    // QA 규칙 정의
    private static readonly Dictionary<string, QARule> _rules = new()
    {
        ["QA001"] = new("QA001", "Critical", "Safety", "초기화되지 않은 중요 변수 (REAL/LREAL/포인터)"),
        ["QA002"] = new("QA002", "Critical", "Safety", "위험한 타입 변환 (정밀도 손실 가능)"),
        ["QA003"] = new("QA003", "Warning", "Performance", "큰 배열/구조체 반복 복사"),
        ["QA004"] = new("QA004", "Warning", "Safety", "배열 인덱스 범위 초과 가능성"),
        ["QA005"] = new("QA005", "Critical", "Safety", "실수형 직접 등호 비교"),
        ["QA006"] = new("QA006", "Critical", "Safety", "0으로 나누기 가능성"),
        ["QA007"] = new("QA007", "Warning", "Maintainability", "매직 넘버 사용"),
        ["QA008"] = new("QA008", "Warning", "Maintainability", "CASE 문에 ELSE 없음"),
        ["QA009"] = new("QA009", "Warning", "Maintainability", "깊은 중첩 (depth > 4)"),
        ["QA010"] = new("QA010", "Warning", "Maintainability", "긴 함수 (100줄 초과)"),
        ["QA011"] = new("QA011", "Info", "Maintainability", "사용되지 않는 변수"),
        ["QA012"] = new("QA012", "Warning", "Safety", "하드코딩된 I/O 주소"),
        ["QA013"] = new("QA013", "Info", "Maintainability", "주석 부족"),
        ["QA014"] = new("QA014", "Warning", "Maintainability", "높은 순환 복잡도"),
        ["QA015"] = new("QA015", "Info", "Maintainability", "중복 코드"),
        ["QA016"] = new("QA016", "Info", "Style", "명명 규칙 위반"),
        ["QA017"] = new("QA017", "Warning", "Maintainability", "너무 많은 매개변수"),
        ["QA018"] = new("QA018", "Warning", "Safety", "NULL 체크 누락"),
        ["QA019"] = new("QA019", "Critical", "Safety", "무한 루프 위험"),
        ["QA020"] = new("QA020", "Info", "Style", "전역 변수 과다 사용"),
    };

    /// <summary>
    /// 프로젝트 분석 실행
    /// </summary>
    public SingleProjectAnalysisResult Analyze(string projectPath)
    {
        var result = new SingleProjectAnalysisResult
        {
            ProjectPath = projectPath,
            Timestamp = DateTime.Now
        };

        // TwinCAT 파일 수집
        var files = CollectTwinCatFiles(projectPath);

        foreach (var file in files)
        {
            var fileInfo = AnalyzeFile(file);
            result.Files.Add(fileInfo);
            result.TotalLines += fileInfo.LineCount;

            // 파일별 이슈 추가
            foreach (var issue in fileInfo.Issues)
            {
                issue.FileName = fileInfo.FileName;
                result.Issues.Add(issue);
            }
        }

        result.TotalFiles = result.Files.Count;
        return result;
    }

    /// <summary>
    /// TwinCAT 파일 수집
    /// </summary>
    private List<string> CollectTwinCatFiles(string projectPath)
    {
        var files = new List<string>();
        var extensions = new[] { "*.TcPOU", "*.TcGVL", "*.TcDUT" };

        foreach (var ext in extensions)
        {
            files.AddRange(Directory.GetFiles(projectPath, ext, SearchOption.AllDirectories));
        }

        return files;
    }

    /// <summary>
    /// 파일 분석
    /// </summary>
    private FileAnalysisResult AnalyzeFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var fileType = Path.GetExtension(filePath).TrimStart('.').ToUpper();

        var result = new FileAnalysisResult
        {
            FileName = fileName,
            FilePath = filePath,
            FileType = fileType
        };

        try
        {
            var content = File.ReadAllText(filePath);

            // XML 파싱 및 코드 추출
            var (pouType, pouName, code, declarations) = ExtractCodeFromXml(content, fileType);
            result.PouType = pouType;
            result.PouName = pouName;

            // 라인 수 계산
            if (!string.IsNullOrEmpty(code))
            {
                result.LineCount = code.Split('\n').Length;
            }

            // 복잡도 계산
            result.Complexity = CalculateComplexity(code);

            // QA 규칙 검사
            result.Issues = CheckQARules(code, declarations, filePath);
            result.IssueCount = result.Issues.Count;
        }
        catch (Exception ex)
        {
            result.Issues.Add(new SingleProjectIssue
            {
                RuleId = "ERROR",
                Severity = "Critical",
                Category = "System",
                Message = $"파일 분석 오류: {ex.Message}",
                LineNumber = 0
            });
        }

        return result;
    }

    /// <summary>
    /// XML에서 코드 추출 (네임스페이스 없이 Descendants 사용)
    /// </summary>
    private (string pouType, string pouName, string code, string declarations) ExtractCodeFromXml(string content, string fileType)
    {
        try
        {
            var doc = XDocument.Parse(content);

            string pouType = "";
            string pouName = "";
            string code = "";
            string declarations = "";

            if (fileType == "TCPOU")
            {
                // 네임스페이스 없이 검색 (더 범용적)
                var pou = doc.Descendants("POU").FirstOrDefault();
                if (pou != null)
                {
                    pouName = pou.Attribute("Name")?.Value ?? "";
                    declarations = pou.Descendants("Declaration").FirstOrDefault()?.Value ?? "";
                    code = pou.Descendants("ST").FirstOrDefault()?.Value ?? "";

                    // 타입 결정
                    pouType = declarations.Contains("FUNCTION_BLOCK", StringComparison.OrdinalIgnoreCase) ? "FUNCTION_BLOCK" :
                              declarations.Contains("FUNCTION", StringComparison.OrdinalIgnoreCase) ? "FUNCTION" :
                              declarations.Contains("PROGRAM", StringComparison.OrdinalIgnoreCase) ? "PROGRAM" : "POU";
                }
            }
            else if (fileType == "TCGVL")
            {
                var gvl = doc.Descendants("GVL").FirstOrDefault();
                if (gvl != null)
                {
                    pouName = gvl.Attribute("Name")?.Value ?? "";
                    pouType = "GVL";
                    declarations = gvl.Descendants("Declaration").FirstOrDefault()?.Value ?? "";
                }
            }
            else if (fileType == "TCDUT")
            {
                var dut = doc.Descendants("DUT").FirstOrDefault();
                if (dut != null)
                {
                    pouName = dut.Attribute("Name")?.Value ?? "";
                    pouType = "DUT";
                    declarations = dut.Descendants("Declaration").FirstOrDefault()?.Value ?? "";
                }
            }

            return (pouType, pouName, code, declarations);
        }
        catch
        {
            return ("", "", "", "");
        }
    }

    /// <summary>
    /// 복잡도 계산
    /// </summary>
    private int CalculateComplexity(string code)
    {
        if (string.IsNullOrEmpty(code)) return 0;

        int complexity = 1;

        // 분기문 카운트
        complexity += Regex.Matches(code, @"\bIF\b", RegexOptions.IgnoreCase).Count;
        complexity += Regex.Matches(code, @"\bELSIF\b", RegexOptions.IgnoreCase).Count;
        complexity += Regex.Matches(code, @"\bCASE\b", RegexOptions.IgnoreCase).Count;
        complexity += Regex.Matches(code, @"\bWHILE\b", RegexOptions.IgnoreCase).Count;
        complexity += Regex.Matches(code, @"\bFOR\b", RegexOptions.IgnoreCase).Count;
        complexity += Regex.Matches(code, @"\bREPEAT\b", RegexOptions.IgnoreCase).Count;
        complexity += Regex.Matches(code, @"\bAND\b", RegexOptions.IgnoreCase).Count;
        complexity += Regex.Matches(code, @"\bOR\b", RegexOptions.IgnoreCase).Count;

        return complexity;
    }

    /// <summary>
    /// QA 규칙 검사
    /// </summary>
    private List<SingleProjectIssue> CheckQARules(string code, string declarations, string filePath)
    {
        var issues = new List<SingleProjectIssue>();
        var lines = (declarations + "\n" + code).Split('\n');

        // QA001: 초기화되지 않은 REAL/LREAL/POINTER 변수
        CheckUninitializedVariables(declarations, issues);

        // QA002: 위험한 타입 변환
        CheckDangerousTypeConversions(code, issues);

        // QA005: 실수형 직접 등호 비교
        CheckFloatingPointComparison(code, issues);

        // QA006: 0으로 나누기 가능성
        CheckDivisionByZero(code, issues);

        // QA007: 매직 넘버 사용
        CheckMagicNumbers(code, issues);

        // QA008: CASE 문에 ELSE 없음
        CheckMissingCaseElse(code, issues);

        // QA009: 깊은 중첩
        CheckDeepNesting(code, issues);

        // QA010: 긴 함수
        CheckLongFunction(code, issues);

        // QA013: 주석 부족
        CheckInsufficientComments(code, issues);

        // QA014: 높은 순환 복잡도
        var complexity = CalculateComplexity(code);
        if (complexity > 15)
        {
            issues.Add(CreateIssue("QA014", 1, $"순환 복잡도가 높음: {complexity}", code.Split('\n').FirstOrDefault() ?? ""));
        }

        // QA016: 명명 규칙 위반
        CheckNamingConventions(declarations, issues);

        return issues;
    }

    private void CheckUninitializedVariables(string declarations, List<SingleProjectIssue> issues)
    {
        var lines = declarations.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // REAL/LREAL/POINTER 타입이지만 초기값이 없는 경우
            if (Regex.IsMatch(line, @":\s*(REAL|LREAL|POINTER\s+TO)", RegexOptions.IgnoreCase) &&
                !line.Contains(":=") &&
                !line.StartsWith("//") &&
                !line.StartsWith("(*"))
            {
                issues.Add(CreateIssue("QA001", i + 1, _rules["QA001"].Description, line));
            }
        }
    }

    private void CheckDangerousTypeConversions(string code, List<SingleProjectIssue> issues)
    {
        var patterns = new[]
        {
            (@"REAL_TO_INT\b", "REAL→INT"),
            (@"REAL_TO_DINT\b", "REAL→DINT"),
            (@"LREAL_TO_REAL\b", "LREAL→REAL"),
            (@"LREAL_TO_INT\b", "LREAL→INT"),
            (@"LREAL_TO_DINT\b", "LREAL→DINT"),
        };

        var lines = code.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            foreach (var (pattern, conversion) in patterns)
            {
                if (Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase))
                {
                    issues.Add(CreateIssue("QA002", i + 1, $"위험한 타입 변환: {conversion}", line.Trim()));
                }
            }
        }
    }

    private void CheckFloatingPointComparison(string code, List<SingleProjectIssue> issues)
    {
        var lines = code.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            // 실수형 변수를 직접 = 로 비교하는 패턴 (근사 비교가 아닌 경우)
            if (Regex.IsMatch(line, @"\br[A-Za-z_]\w*\s*=\s*\d+\.\d+", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(line, @"\b\d+\.\d+\s*=\s*r[A-Za-z_]\w*", RegexOptions.IgnoreCase))
            {
                issues.Add(CreateIssue("QA005", i + 1, _rules["QA005"].Description, line.Trim()));
            }
        }
    }

    private void CheckDivisionByZero(string code, List<SingleProjectIssue> issues)
    {
        var lines = code.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            // 나눗셈이 있고, 분모에 대한 체크가 없는 경우
            if (Regex.IsMatch(line, @"/\s*[a-zA-Z_]\w*", RegexOptions.IgnoreCase) &&
                !line.Contains("// ") && !line.Contains("(*"))
            {
                // 간단한 휴리스틱: 변수로 나누는 경우
                var match = Regex.Match(line, @"/\s*([a-zA-Z_]\w*)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var divisor = match.Groups[1].Value;
                    // 상수가 아닌 경우에만
                    if (!Regex.IsMatch(divisor, @"^c[A-Z]", RegexOptions.None)) // 상수 접두사가 아닌 경우
                    {
                        issues.Add(CreateIssue("QA006", i + 1, _rules["QA006"].Description, line.Trim()));
                    }
                }
            }
        }
    }

    private void CheckMagicNumbers(string code, List<SingleProjectIssue> issues)
    {
        var lines = code.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            // 숫자 리터럴 (0, 1, -1, TRUE, FALSE 제외)
            var matches = Regex.Matches(line, @"(?<![a-zA-Z_\d])\d{2,}(?!\d)");
            foreach (Match match in matches)
            {
                var number = match.Value;
                if (int.TryParse(number, out int val) && val > 1 && val != 10 && val != 100 && val != 1000)
                {
                    // 배열 인덱스나 선언부가 아닌 경우
                    if (!line.Contains("[") && !line.Contains("ARRAY") && !line.Contains(":="))
                    {
                        issues.Add(CreateIssue("QA007", i + 1, $"매직 넘버 사용: {number}", line.Trim()));
                    }
                }
            }
        }
    }

    private void CheckMissingCaseElse(string code, List<SingleProjectIssue> issues)
    {
        // CASE 문이 있지만 ELSE가 없는 경우
        var caseMatches = Regex.Matches(code, @"\bCASE\b.*?\bEND_CASE\b", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        foreach (Match match in caseMatches)
        {
            if (!match.Value.Contains("ELSE", StringComparison.OrdinalIgnoreCase))
            {
                var lineNumber = code.Substring(0, match.Index).Split('\n').Length;
                issues.Add(CreateIssue("QA008", lineNumber, _rules["QA008"].Description, "CASE ... END_CASE"));
            }
        }
    }

    private void CheckDeepNesting(string code, List<SingleProjectIssue> issues)
    {
        var lines = code.Split('\n');
        int depth = 0;
        int maxDepth = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].ToUpper();

            // 중첩 증가
            if (Regex.IsMatch(line, @"\bIF\b|\bFOR\b|\bWHILE\b|\bCASE\b|\bREPEAT\b"))
            {
                depth++;
                if (depth > maxDepth) maxDepth = depth;

                if (depth > 4)
                {
                    issues.Add(CreateIssue("QA009", i + 1, $"깊은 중첩 감지 (depth: {depth})", lines[i].Trim()));
                }
            }

            // 중첩 감소
            if (Regex.IsMatch(line, @"\bEND_IF\b|\bEND_FOR\b|\bEND_WHILE\b|\bEND_CASE\b|\bUNTIL\b"))
            {
                depth = Math.Max(0, depth - 1);
            }
        }
    }

    private void CheckLongFunction(string code, List<SingleProjectIssue> issues)
    {
        var lines = code.Split('\n').Length;
        if (lines > 100)
        {
            issues.Add(CreateIssue("QA010", 1, $"긴 함수: {lines}줄 (권장: 100줄 이하)", ""));
        }
    }

    private void CheckInsufficientComments(string code, List<SingleProjectIssue> issues)
    {
        var lines = code.Split('\n');
        var codeLines = lines.Count(l => !string.IsNullOrWhiteSpace(l) && !l.Trim().StartsWith("//") && !l.Trim().StartsWith("(*"));
        var commentLines = lines.Count(l => l.Contains("//") || l.Contains("(*") || l.Contains("*)"));

        if (codeLines > 20 && commentLines < codeLines * 0.1)
        {
            issues.Add(CreateIssue("QA013", 1, $"주석 부족: {commentLines}개 주석 / {codeLines}줄 코드 ({(double)commentLines / codeLines * 100:F1}%)", ""));
        }
    }

    private void CheckNamingConventions(string declarations, List<SingleProjectIssue> issues)
    {
        var lines = declarations.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // 변수 선언 패턴
            var match = Regex.Match(line, @"^\s*([a-zA-Z_]\w*)\s*:", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var varName = match.Groups[1].Value;

                // Hungarian notation 체크 (선택적)
                if (varName.Length > 1 && char.IsLower(varName[0]) && char.IsUpper(varName[1]))
                {
                    // 헝가리안 표기법 사용 중 - OK
                }
                else if (varName.Length > 0 && !char.IsLower(varName[0]) && !varName.StartsWith("_"))
                {
                    // 대문자로 시작하거나 접두사 없음
                    // issues.Add(CreateIssue("QA016", i + 1, $"명명 규칙 위반: {varName}", line));
                }
            }
        }
    }

    private SingleProjectIssue CreateIssue(string ruleId, int lineNumber, string message, string codeSnippet)
    {
        var rule = _rules.GetValueOrDefault(ruleId) ?? new QARule(ruleId, "Info", "Unknown", message);

        return new SingleProjectIssue
        {
            RuleId = ruleId,
            Severity = rule.Severity,
            Category = rule.Category,
            Message = message,
            LineNumber = lineNumber,
            CodeSnippet = codeSnippet
        };
    }
}

#region Models

/// <summary>
/// QA 규칙 정의
/// </summary>
public record QARule(string RuleId, string Severity, string Category, string Description);

/// <summary>
/// 단일 프로젝트 분석 결과
/// </summary>
public class SingleProjectAnalysisResult
{
    public string ProjectPath { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int TotalFiles { get; set; }
    public int TotalLines { get; set; }
    public List<FileAnalysisResult> Files { get; set; } = new();
    public List<SingleProjectIssue> Issues { get; set; } = new();
}

/// <summary>
/// 파일 분석 결과
/// </summary>
public class FileAnalysisResult
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string PouType { get; set; } = string.Empty;
    public string PouName { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public int Complexity { get; set; }
    public int IssueCount { get; set; }
    public List<SingleProjectIssue> Issues { get; set; } = new();
}

/// <summary>
/// 단일 프로젝트 이슈
/// </summary>
public class SingleProjectIssue
{
    public string FileName { get; set; } = string.Empty;
    public string RuleId { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string CodeSnippet { get; set; } = string.Empty;
}

#endregion
