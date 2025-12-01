using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.Analysis;

/// <summary>
/// 종합 안전성 분석기 구현
/// </summary>
public class ComprehensiveSafetyAnalyzer : IComprehensiveSafetyAnalyzer
{
    private readonly IDivisionSafetyChecker _divisionChecker;
    private readonly ITimerSafetyAnalyzer _timerAnalyzer;
    private readonly ITypeConversionChecker _typeConversionChecker;
    private readonly ILoopSafetyAnalyzer _loopAnalyzer;
    private readonly IFBInitializationChecker _fbInitChecker;
    private readonly IOutputAssignmentChecker _outputChecker;
    private readonly IIODirectionChecker _ioDirectionChecker;

    public ComprehensiveSafetyAnalyzer(
        IDivisionSafetyChecker divisionChecker,
        ITimerSafetyAnalyzer timerAnalyzer,
        ITypeConversionChecker typeConversionChecker,
        ILoopSafetyAnalyzer loopAnalyzer,
        IFBInitializationChecker fbInitChecker,
        IOutputAssignmentChecker outputChecker,
        IIODirectionChecker ioDirectionChecker)
    {
        _divisionChecker = divisionChecker;
        _timerAnalyzer = timerAnalyzer;
        _typeConversionChecker = typeConversionChecker;
        _loopAnalyzer = loopAnalyzer;
        _fbInitChecker = fbInitChecker;
        _outputChecker = outputChecker;
        _ioDirectionChecker = ioDirectionChecker;
    }

    public ComprehensiveSafetyAnalysis Analyze(ValidationSession session)
    {
        var safetyIssues = new List<SafetyIssue>();

        // 병렬 실행
        var tasks = new List<Task>
        {
            Task.Run(() => safetyIssues.AddRange(_loopAnalyzer.Analyze(session))),
            Task.Run(() => safetyIssues.AddRange(_ioDirectionChecker.Check(session))),
            Task.Run(() => safetyIssues.AddRange(CheckEmergencyStop(session))),
            Task.Run(() => safetyIssues.AddRange(CheckInterlock(session)))
        };

        var divisionTask = Task.Run(() => _divisionChecker.Check(session));
        var timerTask = Task.Run(() => _timerAnalyzer.Analyze(session));
        var typeConversionTask = Task.Run(() => _typeConversionChecker.Check(session));
        var fbInitTask = Task.Run(() => _fbInitChecker.Check(session));
        var outputTask = Task.Run(() => _outputChecker.Check(session));

        Task.WaitAll(tasks.Concat(new Task[] { divisionTask, timerTask, typeConversionTask, fbInitTask, outputTask }).ToArray());

        return new ComprehensiveSafetyAnalysis
        {
            SafetyIssues = safetyIssues,
            DivisionChecks = divisionTask.Result,
            TimerAnalysis = timerTask.Result,
            TypeConversions = typeConversionTask.Result,
            FBInitializations = fbInitTask.Result,
            OutputAssignments = outputTask.Result
        };
    }

    /// <summary>
    /// 비상정지 로직 검사
    /// </summary>
    private List<SafetyIssue> CheckEmergencyStop(ValidationSession session)
    {
        var issues = new List<SafetyIssue>();

        foreach (var file in session.Files)
        {
            // 출력(액추에이터) 관련 코드가 있는지 확인
            var hasActuator = Regex.IsMatch(file.Content, @"%Q[XBWDL]?\d+", RegexOptions.IgnoreCase);
            if (!hasActuator) continue;

            // 비상정지 관련 키워드 검색
            var hasEmergencyStop = Regex.IsMatch(file.Content,
                @"\b(emergency|estop|e_stop|비상|긴급|EmergencyStop)\b",
                RegexOptions.IgnoreCase);

            if (!hasEmergencyStop)
            {
                issues.Add(new SafetyIssue
                {
                    Type = SafetyIssueType.MissingEmergencyStop,
                    FilePath = file.FilePath,
                    Line = 1,
                    Severity = IssueSeverity.Critical,
                    Message = "출력(액추에이터)을 사용하지만 비상정지 로직이 없습니다",
                    Recommendation = "모든 출력 제어에 비상정지 조건을 추가하세요: IF NOT bEmergencyStop THEN ..."
                });
            }
        }

        return issues;
    }

    /// <summary>
    /// 인터락 로직 검사
    /// </summary>
    private List<SafetyIssue> CheckInterlock(ValidationSession session)
    {
        var issues = new List<SafetyIssue>();

        foreach (var file in session.Files)
        {
            var lines = file.Content.Split('\n');

            // 모터, 실린더, 밸브 등 액추에이터 패턴
            var actuatorPattern = new Regex(
                @"\b(motor|cylinder|valve|pump|heater|모터|실린더|밸브|펌프|히터)\w*\s*:=\s*TRUE",
                RegexOptions.IgnoreCase);

            for (int i = 0; i < lines.Length; i++)
            {
                var match = actuatorPattern.Match(lines[i]);
                if (!match.Success) continue;

                // 앞 5줄 내에 조건문(IF)이 있는지 확인
                bool hasCondition = false;
                int searchStart = Math.Max(0, i - 5);
                for (int j = searchStart; j <= i; j++)
                {
                    if (Regex.IsMatch(lines[j], @"\bIF\b", RegexOptions.IgnoreCase))
                    {
                        hasCondition = true;
                        break;
                    }
                }

                if (!hasCondition)
                {
                    issues.Add(new SafetyIssue
                    {
                        Type = SafetyIssueType.MissingInterlock,
                        FilePath = file.FilePath,
                        Line = i + 1,
                        CodeSnippet = lines[i].Trim(),
                        Severity = IssueSeverity.Critical,
                        Message = $"액추에이터 '{match.Groups[1].Value}' 제어에 인터락 조건이 없습니다",
                        Recommendation = "액추에이터 제어 전 안전 조건을 확인하세요: IF bSafetyOK AND NOT bFault THEN ...",
                        RelatedElement = match.Groups[1].Value
                    });
                }
            }
        }

        return issues;
    }
}

/// <summary>
/// 나눗셈 안전성 검사기 구현
/// </summary>
public class DivisionSafetyChecker : IDivisionSafetyChecker
{
    private static readonly Regex DivisionPattern = new(
        @"(\w+)\s*/\s*(\w+|\([^)]+\))",
        RegexOptions.Compiled);

    public List<DivisionCheckResult> Check(ValidationSession session)
    {
        var results = new List<DivisionCheckResult>();

        foreach (var file in session.Files)
        {
            var lines = file.Content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var matches = DivisionPattern.Matches(lines[i]);
                foreach (Match match in matches)
                {
                    var divisor = match.Groups[2].Value;

                    // 상수인 경우 건너뛰기
                    if (double.TryParse(divisor, out double val) && val != 0)
                        continue;

                    // 0 체크 여부 확인
                    bool hasZeroCheck = CheckForZeroValidation(lines, i, divisor);

                    results.Add(new DivisionCheckResult
                    {
                        FilePath = file.FilePath,
                        Line = i + 1,
                        Expression = match.Value,
                        Divisor = divisor,
                        HasZeroCheck = hasZeroCheck,
                        Severity = hasZeroCheck ? IssueSeverity.Info : IssueSeverity.Critical
                    });
                }
            }
        }

        return results.Where(r => !r.HasZeroCheck).ToList();
    }

    private bool CheckForZeroValidation(string[] lines, int currentLine, string divisor)
    {
        int start = Math.Max(0, currentLine - 10);
        for (int i = start; i <= currentLine; i++)
        {
            var line = lines[i];
            // 패턴: IF divisor <> 0, IF divisor > 0, IF divisor != 0
            if (Regex.IsMatch(line, $@"\b{Regex.Escape(divisor)}\s*(<>|!=|>)\s*0", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(line, $@"0\s*(<|<>|!=)\s*{Regex.Escape(divisor)}", RegexOptions.IgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}

/// <summary>
/// 타이머 안전성 분석기 구현
/// </summary>
public class TimerSafetyAnalyzer : ITimerSafetyAnalyzer
{
    private static readonly Regex TimerDeclPattern = new(
        @"(\w+)\s*:\s*(TON|TOF|TP)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TimerCallPattern = new(
        @"(\w+)\s*\(\s*IN\s*:=\s*(\w+).*?PT\s*:=\s*T#(\d+)(ms|s|m|h)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public List<TimerAnalysisResult> Analyze(ValidationSession session)
    {
        var results = new List<TimerAnalysisResult>();

        foreach (var file in session.Files)
        {
            var lines = file.Content.Split('\n');
            var timers = new Dictionary<string, (string Type, int Line)>();

            // 1단계: 타이머 선언 수집
            for (int i = 0; i < lines.Length; i++)
            {
                var match = TimerDeclPattern.Match(lines[i]);
                if (match.Success)
                {
                    timers[match.Groups[1].Value] = (match.Groups[2].Value, i + 1);
                }
            }

            // 2단계: 타이머 호출 분석
            for (int i = 0; i < lines.Length; i++)
            {
                var match = TimerCallPattern.Match(lines[i]);
                if (match.Success)
                {
                    var timerName = match.Groups[1].Value;
                    var ptValue = int.Parse(match.Groups[3].Value);
                    var ptUnit = match.Groups[4].Value.ToLower();

                    // ms로 변환
                    long ptMs = ptUnit switch
                    {
                        "s" => ptValue * 1000L,
                        "m" => ptValue * 60000L,
                        "h" => ptValue * 3600000L,
                        _ => ptValue
                    };

                    var issues = new List<string>();

                    // 너무 긴 타이머 경고
                    if (ptMs > 3600000) // 1시간 초과
                    {
                        issues.Add($"타이머 시간이 너무 깁니다 ({ptMs / 60000}분)");
                    }

                    // 너무 짧은 타이머 경고 (PLC 사이클보다 짧을 수 있음)
                    if (ptMs < 10)
                    {
                        issues.Add($"타이머 시간이 너무 짧습니다 ({ptMs}ms) - PLC 사이클 시간 확인 필요");
                    }

                    if (issues.Any())
                    {
                        results.Add(new TimerAnalysisResult
                        {
                            TimerName = timerName,
                            TimerType = timers.TryGetValue(timerName, out var t) ? t.Type : "Unknown",
                            FilePath = file.FilePath,
                            Line = i + 1,
                            PresetTimeMs = ptMs,
                            Issues = issues,
                            Severity = ptMs < 10 ? IssueSeverity.Warning : IssueSeverity.Info
                        });
                    }
                }
            }
        }

        return results;
    }
}

/// <summary>
/// 형변환 안전성 검사기 구현
/// </summary>
public class TypeConversionChecker : ITypeConversionChecker
{
    // 타입별 비트 크기
    private static readonly Dictionary<string, int> TypeSizes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BOOL"] = 1, ["BYTE"] = 8, ["WORD"] = 16, ["DWORD"] = 32, ["LWORD"] = 64,
        ["SINT"] = 8, ["INT"] = 16, ["DINT"] = 32, ["LINT"] = 64,
        ["USINT"] = 8, ["UINT"] = 16, ["UDINT"] = 32, ["ULINT"] = 64,
        ["REAL"] = 32, ["LREAL"] = 64
    };

    // 부호 있는 타입
    private static readonly HashSet<string> SignedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "SINT", "INT", "DINT", "LINT", "REAL", "LREAL"
    };

    private static readonly Regex ConversionPattern = new(
        @"(\w+)_TO_(\w+)\s*\(\s*(\w+)\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ImplicitAssignPattern = new(
        @"(\w+)\s*:\s*(\w+).*?:=\s*(\w+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public List<TypeConversionResult> Check(ValidationSession session)
    {
        var results = new List<TypeConversionResult>();

        foreach (var file in session.Files)
        {
            var lines = file.Content.Split('\n');
            var variables = CollectVariableTypes(lines);

            for (int i = 0; i < lines.Length; i++)
            {
                // 명시적 변환 검사
                var convMatches = ConversionPattern.Matches(lines[i]);
                foreach (Match match in convMatches)
                {
                    var sourceType = match.Groups[1].Value.ToUpper();
                    var targetType = match.Groups[2].Value.ToUpper();

                    var (hasLoss, message) = CheckConversionLoss(sourceType, targetType);
                    if (hasLoss)
                    {
                        results.Add(new TypeConversionResult
                        {
                            FilePath = file.FilePath,
                            Line = i + 1,
                            SourceType = sourceType,
                            TargetType = targetType,
                            PotentialDataLoss = true,
                            CodeSnippet = match.Value,
                            Severity = IssueSeverity.Warning,
                            Message = message
                        });
                    }
                }
            }
        }

        return results;
    }

    private Dictionary<string, string> CollectVariableTypes(string[] lines)
    {
        var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pattern = new Regex(@"(\w+)\s*:\s*(\w+)\s*(?:;|:=)", RegexOptions.IgnoreCase);

        foreach (var line in lines)
        {
            var match = pattern.Match(line);
            if (match.Success)
            {
                vars[match.Groups[1].Value] = match.Groups[2].Value;
            }
        }

        return vars;
    }

    private (bool HasLoss, string Message) CheckConversionLoss(string source, string target)
    {
        if (!TypeSizes.TryGetValue(source, out int sourceSize) ||
            !TypeSizes.TryGetValue(target, out int targetSize))
        {
            return (false, "");
        }

        // 크기 감소
        if (sourceSize > targetSize)
        {
            return (true, $"{source}({sourceSize}비트)에서 {target}({targetSize}비트)로 변환 시 데이터 손실 가능");
        }

        // 부호 있음 -> 부호 없음
        if (SignedTypes.Contains(source) && !SignedTypes.Contains(target))
        {
            return (true, $"{source}(부호 있음)에서 {target}(부호 없음)로 변환 시 음수 값 손실 가능");
        }

        // 실수 -> 정수
        if ((source == "REAL" || source == "LREAL") &&
            !source.Equals(target, StringComparison.OrdinalIgnoreCase) &&
            !target.Contains("REAL"))
        {
            return (true, $"{source}(실수)에서 {target}(정수)로 변환 시 소수점 이하 손실");
        }

        return (false, "");
    }
}

/// <summary>
/// 루프 안전성 분석기 구현
/// </summary>
public class LoopSafetyAnalyzer : ILoopSafetyAnalyzer
{
    public List<SafetyIssue> Analyze(ValidationSession session)
    {
        var issues = new List<SafetyIssue>();

        foreach (var file in session.Files)
        {
            var lines = file.Content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                // WHILE 루프 검사
                if (Regex.IsMatch(lines[i], @"\bWHILE\b", RegexOptions.IgnoreCase))
                {
                    var issue = AnalyzeWhileLoop(file.FilePath, lines, i);
                    if (issue != null) issues.Add(issue);
                }

                // REPEAT 루프 검사
                if (Regex.IsMatch(lines[i], @"\bREPEAT\b", RegexOptions.IgnoreCase))
                {
                    var issue = AnalyzeRepeatLoop(file.FilePath, lines, i);
                    if (issue != null) issues.Add(issue);
                }
            }
        }

        return issues;
    }

    private SafetyIssue? AnalyzeWhileLoop(string filePath, string[] lines, int startLine)
    {
        // WHILE 조건 추출
        var conditionMatch = Regex.Match(lines[startLine], @"\bWHILE\s+(.+?)\s+DO", RegexOptions.IgnoreCase);
        if (!conditionMatch.Success) return null;

        var condition = conditionMatch.Groups[1].Value;

        // 항상 참인 조건 검사
        if (Regex.IsMatch(condition, @"^\s*(TRUE|1)\s*$", RegexOptions.IgnoreCase))
        {
            return new SafetyIssue
            {
                Type = SafetyIssueType.PotentialInfiniteLoop,
                FilePath = filePath,
                Line = startLine + 1,
                CodeSnippet = lines[startLine].Trim(),
                Severity = IssueSeverity.Critical,
                Message = "WHILE TRUE는 무한 루프입니다 - EXIT 조건이 있는지 확인하세요",
                Recommendation = "명확한 종료 조건을 추가하거나 EXIT 문을 사용하세요"
            };
        }

        // 루프 내에서 조건 변수가 변경되는지 확인
        var conditionVars = Regex.Matches(condition, @"\b([a-zA-Z_]\w*)\b")
            .Cast<Match>()
            .Select(m => m.Value)
            .Where(v => !IsKeyword(v))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // END_WHILE까지 검사
        bool foundModification = false;
        bool foundExit = false;
        for (int i = startLine + 1; i < lines.Length; i++)
        {
            if (Regex.IsMatch(lines[i], @"\bEND_WHILE\b", RegexOptions.IgnoreCase))
                break;

            if (Regex.IsMatch(lines[i], @"\bEXIT\b", RegexOptions.IgnoreCase))
                foundExit = true;

            foreach (var v in conditionVars)
            {
                if (Regex.IsMatch(lines[i], $@"\b{v}\s*:=", RegexOptions.IgnoreCase))
                {
                    foundModification = true;
                    break;
                }
            }
        }

        if (!foundModification && !foundExit)
        {
            return new SafetyIssue
            {
                Type = SafetyIssueType.PotentialInfiniteLoop,
                FilePath = filePath,
                Line = startLine + 1,
                CodeSnippet = lines[startLine].Trim(),
                Severity = IssueSeverity.Critical,
                Message = $"WHILE 루프 내에서 조건 변수({string.Join(", ", conditionVars)})가 변경되지 않음 - 무한 루프 가능성",
                Recommendation = "루프 내에서 조건 변수를 변경하거나 EXIT 문을 추가하세요"
            };
        }

        return null;
    }

    private SafetyIssue? AnalyzeRepeatLoop(string filePath, string[] lines, int startLine)
    {
        // UNTIL 조건 찾기
        for (int i = startLine + 1; i < lines.Length; i++)
        {
            var untilMatch = Regex.Match(lines[i], @"\bUNTIL\s+(.+?)\s*;", RegexOptions.IgnoreCase);
            if (untilMatch.Success)
            {
                var condition = untilMatch.Groups[1].Value;

                // 항상 거짓인 조건 검사
                if (Regex.IsMatch(condition, @"^\s*(FALSE|0)\s*$", RegexOptions.IgnoreCase))
                {
                    return new SafetyIssue
                    {
                        Type = SafetyIssueType.PotentialInfiniteLoop,
                        FilePath = filePath,
                        Line = i + 1,
                        CodeSnippet = lines[i].Trim(),
                        Severity = IssueSeverity.Critical,
                        Message = "UNTIL FALSE는 무한 루프입니다",
                        Recommendation = "종료 조건을 TRUE가 될 수 있는 조건으로 변경하세요"
                    };
                }
                break;
            }

            if (Regex.IsMatch(lines[i], @"\bEND_REPEAT\b", RegexOptions.IgnoreCase))
                break;
        }

        return null;
    }

    private bool IsKeyword(string word)
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AND", "OR", "NOT", "XOR", "TRUE", "FALSE", "MOD"
        };
        return keywords.Contains(word);
    }
}

/// <summary>
/// FB 초기화 검사기 구현
/// </summary>
public class FBInitializationChecker : IFBInitializationChecker
{
    private static readonly Regex FBDeclPattern = new(
        @"(\w+)\s*:\s*(FB_\w+|TON|TOF|TP|CTU|CTD|R_TRIG|F_TRIG)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public List<FBInitializationResult> Check(ValidationSession session)
    {
        var results = new List<FBInitializationResult>();

        foreach (var file in session.Files)
        {
            var lines = file.Content.Split('\n');
            var fbInstances = new Dictionary<string, (string Type, int Line)>();

            // FB 인스턴스 선언 수집
            for (int i = 0; i < lines.Length; i++)
            {
                var matches = FBDeclPattern.Matches(lines[i]);
                foreach (Match match in matches)
                {
                    fbInstances[match.Groups[1].Value] = (match.Groups[2].Value, i + 1);
                }
            }

            // 호출 여부 확인
            foreach (var (name, info) in fbInstances)
            {
                bool isCalled = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    // FB 호출 패턴: fbInstance( 또는 fbInstance.Method(
                    if (Regex.IsMatch(lines[i], $@"\b{Regex.Escape(name)}\s*\(", RegexOptions.IgnoreCase))
                    {
                        isCalled = true;
                        break;
                    }
                }

                if (!isCalled)
                {
                    results.Add(new FBInitializationResult
                    {
                        InstanceName = name,
                        FBType = info.Type,
                        FilePath = file.FilePath,
                        DeclarationLine = info.Line,
                        IsInitialized = false,
                        IsCalled = false,
                        Severity = IssueSeverity.Warning,
                        Message = $"FB 인스턴스 '{name}'({info.Type})가 선언되었지만 호출되지 않음"
                    });
                }
            }
        }

        return results;
    }
}

/// <summary>
/// 출력 할당 검사기 구현
/// </summary>
public class OutputAssignmentChecker : IOutputAssignmentChecker
{
    public List<OutputAssignmentResult> Check(ValidationSession session)
    {
        var results = new List<OutputAssignmentResult>();

        foreach (var file in session.Files)
        {
            var lines = file.Content.Split('\n');

            // VAR_OUTPUT 변수 수집
            bool inOutputBlock = false;
            var outputVars = new Dictionary<string, int>();

            for (int i = 0; i < lines.Length; i++)
            {
                if (Regex.IsMatch(lines[i], @"\bVAR_OUTPUT\b", RegexOptions.IgnoreCase))
                    inOutputBlock = true;

                if (Regex.IsMatch(lines[i], @"\bEND_VAR\b", RegexOptions.IgnoreCase))
                    inOutputBlock = false;

                if (inOutputBlock)
                {
                    var match = Regex.Match(lines[i], @"(\w+)\s*:", RegexOptions.IgnoreCase);
                    if (match.Success && !match.Groups[1].Value.Equals("VAR_OUTPUT", StringComparison.OrdinalIgnoreCase))
                    {
                        outputVars[match.Groups[1].Value] = i + 1;
                    }
                }
            }

            // 할당 여부 확인
            foreach (var (varName, declLine) in outputVars)
            {
                bool isAssigned = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (Regex.IsMatch(lines[i], $@"\b{Regex.Escape(varName)}\s*:=", RegexOptions.IgnoreCase))
                    {
                        isAssigned = true;
                        break;
                    }
                }

                if (!isAssigned)
                {
                    results.Add(new OutputAssignmentResult
                    {
                        VariableName = varName,
                        FilePath = file.FilePath,
                        Line = declLine,
                        IsAssigned = false,
                        IsAssignedInAllPaths = false,
                        Severity = IssueSeverity.Warning,
                        Message = $"출력 변수 '{varName}'에 값이 할당되지 않음"
                    });
                }
            }
        }

        return results;
    }
}

/// <summary>
/// I/O 방향성 검사기 구현
/// </summary>
public class IODirectionChecker : IIODirectionChecker
{
    public List<SafetyIssue> Check(ValidationSession session)
    {
        var issues = new List<SafetyIssue>();

        foreach (var file in session.Files)
        {
            var lines = file.Content.Split('\n');

            // 입력 변수 수집 (%I로 시작)
            var inputVars = new Dictionary<string, int>();
            var inputPattern = new Regex(@"(\w+)\s+AT\s+%I[XBWDL]?\d+", RegexOptions.IgnoreCase);

            for (int i = 0; i < lines.Length; i++)
            {
                var match = inputPattern.Match(lines[i]);
                if (match.Success)
                {
                    inputVars[match.Groups[1].Value] = i + 1;
                }
            }

            // 입력 변수에 값 할당 시도 검출
            foreach (var (varName, declLine) in inputVars)
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    if (Regex.IsMatch(lines[i], $@"\b{Regex.Escape(varName)}\s*:=", RegexOptions.IgnoreCase))
                    {
                        issues.Add(new SafetyIssue
                        {
                            Type = SafetyIssueType.InputUsedAsOutput,
                            FilePath = file.FilePath,
                            Line = i + 1,
                            CodeSnippet = lines[i].Trim(),
                            Severity = IssueSeverity.Critical,
                            Message = $"입력 변수 '{varName}'에 값을 할당하려고 합니다 - 입력은 읽기 전용입니다",
                            Recommendation = "입력 변수를 로컬 변수에 복사한 후 사용하세요",
                            RelatedElement = varName
                        });
                    }
                }
            }
        }

        return issues;
    }
}
