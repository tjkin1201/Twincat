using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.Analysis;

/// <summary>
/// 심화 안전성 분석 통합 오케스트레이터
/// </summary>
public class ExtendedSafetyAnalyzer : IExtendedSafetyAnalyzer
{
    private readonly IConcurrencyAnalyzer _concurrencyAnalyzer;
    private readonly IStateMachineAnalyzer _stateMachineAnalyzer;
    private readonly ICommunicationAnalyzer _communicationAnalyzer;
    private readonly IResourceAnalyzer _resourceAnalyzer;
    private readonly ISensorAnalyzer _sensorAnalyzer;
    private readonly IErrorRecoveryAnalyzer _errorRecoveryAnalyzer;
    private readonly ILoggingAnalyzer _loggingAnalyzer;

    public ExtendedSafetyAnalyzer(
        IConcurrencyAnalyzer concurrencyAnalyzer,
        IStateMachineAnalyzer stateMachineAnalyzer,
        ICommunicationAnalyzer communicationAnalyzer,
        IResourceAnalyzer resourceAnalyzer,
        ISensorAnalyzer sensorAnalyzer,
        IErrorRecoveryAnalyzer errorRecoveryAnalyzer,
        ILoggingAnalyzer loggingAnalyzer)
    {
        _concurrencyAnalyzer = concurrencyAnalyzer;
        _stateMachineAnalyzer = stateMachineAnalyzer;
        _communicationAnalyzer = communicationAnalyzer;
        _resourceAnalyzer = resourceAnalyzer;
        _sensorAnalyzer = sensorAnalyzer;
        _errorRecoveryAnalyzer = errorRecoveryAnalyzer;
        _loggingAnalyzer = loggingAnalyzer;
    }

    public ExtendedSafetyAnalysis Analyze(ValidationSession session)
    {
        // 병렬 실행
        var concurrencyTask = Task.Run(() => _concurrencyAnalyzer.Analyze(session));
        var stateMachineTask = Task.Run(() => _stateMachineAnalyzer.Analyze(session));
        var communicationTask = Task.Run(() => _communicationAnalyzer.Analyze(session));
        var resourceTask = Task.Run(() => _resourceAnalyzer.Analyze(session));
        var sensorTask = Task.Run(() => _sensorAnalyzer.Analyze(session));
        var errorRecoveryTask = Task.Run(() => _errorRecoveryAnalyzer.Analyze(session));
        var loggingTask = Task.Run(() => _loggingAnalyzer.Analyze(session));

        Task.WaitAll(concurrencyTask, stateMachineTask, communicationTask, resourceTask, sensorTask, errorRecoveryTask, loggingTask);

        return new ExtendedSafetyAnalysis
        {
            ConcurrencyIssues = concurrencyTask.Result,
            StateMachineIssues = stateMachineTask.Result,
            CommunicationIssues = communicationTask.Result,
            ResourceIssues = resourceTask.Result,
            SensorIssues = sensorTask.Result,
            ErrorRecoveryIssues = errorRecoveryTask.Result,
            LoggingIssues = loggingTask.Result
        };
    }
}

/// <summary>
/// 동시성 분석기 구현
/// </summary>
public class ConcurrencyAnalyzer : IConcurrencyAnalyzer
{
    // 전역 변수 패턴
    private static readonly Regex GlobalVarPattern = new(
        @"VAR_GLOBAL.*?END_VAR",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    // 태스크 설정 패턴
    private static readonly Regex TaskPattern = new(
        @"TASK\s+(\w+).*?PRIORITY\s*:=\s*(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    public List<ConcurrencyIssue> Analyze(ValidationSession session)
    {
        var issues = new List<ConcurrencyIssue>();

        // 전역 변수 수집
        var globalVars = CollectGlobalVariables(session);

        // 각 파일에서 전역 변수 사용 검사
        var varUsage = new Dictionary<string, List<(string File, int Line, bool IsWrite)>>();

        foreach (var file in session.Files)
        {
            var lines = file.Content.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                foreach (var gVar in globalVars)
                {
                    // 쓰기 접근
                    if (Regex.IsMatch(lines[i], $@"\b{Regex.Escape(gVar)}\s*:=", RegexOptions.IgnoreCase))
                    {
                        if (!varUsage.ContainsKey(gVar))
                            varUsage[gVar] = new List<(string, int, bool)>();
                        varUsage[gVar].Add((file.FilePath, i + 1, true));
                    }
                    // 읽기 접근 (쓰기가 아닌 경우)
                    else if (Regex.IsMatch(lines[i], $@"\b{Regex.Escape(gVar)}\b", RegexOptions.IgnoreCase))
                    {
                        if (!varUsage.ContainsKey(gVar))
                            varUsage[gVar] = new List<(string, int, bool)>();
                        varUsage[gVar].Add((file.FilePath, i + 1, false));
                    }
                }
            }
        }

        // 여러 파일에서 쓰기가 발생하는 변수 검출
        foreach (var (varName, usages) in varUsage)
        {
            var writeLocations = usages.Where(u => u.IsWrite).ToList();
            var uniqueFiles = writeLocations.Select(w => w.File).Distinct().ToList();

            if (uniqueFiles.Count > 1)
            {
                issues.Add(new ConcurrencyIssue
                {
                    Type = ConcurrencyIssueType.RaceCondition,
                    FilePath = writeLocations.First().File,
                    Line = writeLocations.First().Line,
                    VariableName = varName,
                    AccessingTasks = uniqueFiles,
                    Severity = IssueSeverity.Critical,
                    Message = $"전역 변수 '{varName}'이 여러 파일에서 쓰기 접근됨 - 경쟁 조건 가능",
                    Recommendation = "세마포어 또는 CRITICAL SECTION을 사용하거나 변수를 태스크별로 분리하세요"
                });
            }
        }

        return issues;
    }

    private HashSet<string> CollectGlobalVariables(ValidationSession session)
    {
        var globals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in session.Files)
        {
            var matches = GlobalVarPattern.Matches(file.Content);
            foreach (Match match in matches)
            {
                var varMatches = Regex.Matches(match.Value, @"(\w+)\s*:", RegexOptions.IgnoreCase);
                foreach (Match varMatch in varMatches)
                {
                    var varName = varMatch.Groups[1].Value;
                    if (!varName.Equals("VAR_GLOBAL", StringComparison.OrdinalIgnoreCase))
                    {
                        globals.Add(varName);
                    }
                }
            }
        }

        return globals;
    }
}

/// <summary>
/// 상태머신 분석기 구현
/// </summary>
public class StateMachineAnalyzer : IStateMachineAnalyzer
{
    public List<StateMachineIssue> Analyze(ValidationSession session)
    {
        var issues = new List<StateMachineIssue>();

        foreach (var file in session.Files)
        {
            var lines = file.Content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                // CASE 문 찾기
                var caseMatch = Regex.Match(lines[i], @"\bCASE\s+(\w+)\s+OF", RegexOptions.IgnoreCase);
                if (!caseMatch.Success) continue;

                var stateVar = caseMatch.Groups[1].Value;
                var caseStartLine = i;
                var definedStates = new List<string>();
                var stateTransitions = new Dictionary<string, List<string>>();
                bool hasElse = false;

                // CASE 블록 분석
                for (int j = i + 1; j < lines.Length; j++)
                {
                    if (Regex.IsMatch(lines[j], @"\bEND_CASE\b", RegexOptions.IgnoreCase))
                        break;

                    // 상태 정의 찾기
                    var stateMatch = Regex.Match(lines[j], @"^\s*(\d+|E_\w+\.\w+|\w+)\s*:", RegexOptions.IgnoreCase);
                    if (stateMatch.Success)
                    {
                        var state = stateMatch.Groups[1].Value;
                        definedStates.Add(state);
                        stateTransitions[state] = new List<string>();
                    }

                    // ELSE 확인
                    if (Regex.IsMatch(lines[j], @"^\s*ELSE\s*:", RegexOptions.IgnoreCase))
                    {
                        hasElse = true;
                    }

                    // 상태 전이 찾기
                    var transitionMatch = Regex.Match(lines[j], $@"{Regex.Escape(stateVar)}\s*:=\s*(\w+)", RegexOptions.IgnoreCase);
                    if (transitionMatch.Success && definedStates.Any())
                    {
                        var currentState = definedStates.Last();
                        var nextState = transitionMatch.Groups[1].Value;
                        if (stateTransitions.ContainsKey(currentState))
                        {
                            stateTransitions[currentState].Add(nextState);
                        }
                    }
                }

                // ELSE 분기 없음 검사
                if (!hasElse && definedStates.Count > 0)
                {
                    issues.Add(new StateMachineIssue
                    {
                        Type = StateMachineIssueType.MissingElseBranch,
                        FilePath = file.FilePath,
                        Line = caseStartLine + 1,
                        StateMachineName = stateVar,
                        DefinedStates = definedStates,
                        Severity = IssueSeverity.Warning,
                        Message = $"CASE 문에 ELSE 분기가 없음 - 정의되지 않은 상태값 처리 불가",
                        Recommendation = "ELSE: 분기를 추가하여 예외 상태를 처리하세요"
                    });
                }

                // 탈출 불가 상태 검사 (전이가 없는 상태)
                var deadEndStates = stateTransitions
                    .Where(kv => !kv.Value.Any())
                    .Select(kv => kv.Key)
                    .ToList();

                // 에러/완료 상태 제외
                deadEndStates = deadEndStates
                    .Where(s => !Regex.IsMatch(s, @"(error|fault|done|complete|finish|idle)", RegexOptions.IgnoreCase))
                    .ToList();

                if (deadEndStates.Any())
                {
                    issues.Add(new StateMachineIssue
                    {
                        Type = StateMachineIssueType.DeadEndState,
                        FilePath = file.FilePath,
                        Line = caseStartLine + 1,
                        StateMachineName = stateVar,
                        DefinedStates = definedStates,
                        ProblematicStates = deadEndStates,
                        Severity = IssueSeverity.High,
                        Message = $"탈출 불가능한 상태 발견: {string.Join(", ", deadEndStates)}",
                        Recommendation = "상태 전이 로직을 추가하거나 의도적인 최종 상태라면 명명 규칙을 따르세요 (예: STATE_DONE)"
                    });
                }

                // 초기 상태(0) 확인
                if (!definedStates.Any(s => s == "0" || Regex.IsMatch(s, @"(init|idle|start)", RegexOptions.IgnoreCase)))
                {
                    issues.Add(new StateMachineIssue
                    {
                        Type = StateMachineIssueType.MissingDefaultState,
                        FilePath = file.FilePath,
                        Line = caseStartLine + 1,
                        StateMachineName = stateVar,
                        DefinedStates = definedStates,
                        Severity = IssueSeverity.Warning,
                        Message = "초기/기본 상태(0 또는 INIT)가 정의되지 않음",
                        Recommendation = "상태 0 또는 명시적인 초기 상태를 정의하세요"
                    });
                }
            }
        }

        return issues;
    }
}

/// <summary>
/// 통신 안전성 분석기 구현
/// </summary>
public class CommunicationAnalyzer : ICommunicationAnalyzer
{
    // 통신 FB 패턴
    private static readonly string[] CommFBTypes = {
        "ADSREAD", "ADSWRITE", "ADSRDWRT",
        "FB_MBReadCoils", "FB_MBReadInputs", "FB_MBReadRegs", "FB_MBWriteCoils", "FB_MBWriteRegs",
        "FB_ClientServerConnection", "FB_SocketConnect", "FB_SocketSend", "FB_SocketReceive",
        "SendString", "ReceiveString"
    };

    public List<CommunicationIssue> Analyze(ValidationSession session)
    {
        var issues = new List<CommunicationIssue>();

        foreach (var file in session.Files)
        {
            var lines = file.Content.Split('\n');

            // 통신 FB 인스턴스 수집
            var commInstances = new Dictionary<string, (string Type, int Line)>();

            for (int i = 0; i < lines.Length; i++)
            {
                foreach (var fbType in CommFBTypes)
                {
                    var match = Regex.Match(lines[i], $@"(\w+)\s*:\s*{Regex.Escape(fbType)}\b", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        commInstances[match.Groups[1].Value] = (fbType, i + 1);
                    }
                }
            }

            // 각 통신 인스턴스 사용 분석
            foreach (var (instanceName, info) in commInstances)
            {
                bool hasTimeoutCheck = false;
                bool hasErrorCheck = false;
                bool hasDataValidation = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];

                    // 타임아웃 체크 패턴
                    if (Regex.IsMatch(line, $@"{Regex.Escape(instanceName)}\.(TIMEOUT|tTimeout|bTimeout)", RegexOptions.IgnoreCase) ||
                        Regex.IsMatch(line, $@"TON.*{Regex.Escape(instanceName)}", RegexOptions.IgnoreCase))
                    {
                        hasTimeoutCheck = true;
                    }

                    // 에러 체크 패턴
                    if (Regex.IsMatch(line, $@"{Regex.Escape(instanceName)}\.(bError|Error|ERR|bBusy)", RegexOptions.IgnoreCase))
                    {
                        hasErrorCheck = true;
                    }

                    // 데이터 검증 패턴 (수신 후 범위 체크)
                    if (Regex.IsMatch(line, $@"IF.*{Regex.Escape(instanceName)}.*(<|>|>=|<=|=)", RegexOptions.IgnoreCase))
                    {
                        hasDataValidation = true;
                    }
                }

                if (!hasTimeoutCheck)
                {
                    issues.Add(new CommunicationIssue
                    {
                        Type = CommunicationIssueType.MissingTimeout,
                        FilePath = file.FilePath,
                        Line = info.Line,
                        CommunicationType = GetCommType(info.Type),
                        FunctionBlockName = instanceName,
                        Severity = IssueSeverity.High,
                        Message = $"통신 FB '{instanceName}'({info.Type})에 타임아웃 처리가 없음",
                        Recommendation = "타임아웃 타이머를 추가하여 통신 지연/실패를 감지하세요"
                    });
                }

                if (!hasErrorCheck)
                {
                    issues.Add(new CommunicationIssue
                    {
                        Type = CommunicationIssueType.MissingErrorHandling,
                        FilePath = file.FilePath,
                        Line = info.Line,
                        CommunicationType = GetCommType(info.Type),
                        FunctionBlockName = instanceName,
                        Severity = IssueSeverity.High,
                        Message = $"통신 FB '{instanceName}'({info.Type})에 에러 처리가 없음",
                        Recommendation = "bError 출력을 확인하여 통신 실패를 처리하세요"
                    });
                }

                if (!hasDataValidation && IsReceiveFB(info.Type))
                {
                    issues.Add(new CommunicationIssue
                    {
                        Type = CommunicationIssueType.UnvalidatedData,
                        FilePath = file.FilePath,
                        Line = info.Line,
                        CommunicationType = GetCommType(info.Type),
                        FunctionBlockName = instanceName,
                        Severity = IssueSeverity.Critical,
                        Message = $"수신 FB '{instanceName}'({info.Type})의 데이터 검증이 없음",
                        Recommendation = "수신된 데이터의 범위와 유효성을 검증하세요"
                    });
                }
            }
        }

        return issues;
    }

    private string GetCommType(string fbType)
    {
        if (fbType.Contains("ADS")) return "ADS";
        if (fbType.Contains("MB") || fbType.Contains("Modbus")) return "Modbus";
        if (fbType.Contains("Socket") || fbType.Contains("TCP")) return "TCP/IP";
        return "Serial/Other";
    }

    private bool IsReceiveFB(string fbType)
    {
        return fbType.Contains("Read") || fbType.Contains("Receive") ||
               fbType.Contains("ADSREAD") || fbType.Contains("ADSRDWRT");
    }
}

/// <summary>
/// 리소스 안전성 분석기 구현
/// </summary>
public class ResourceAnalyzer : IResourceAnalyzer
{
    public List<ResourceIssue> Analyze(ValidationSession session)
    {
        var issues = new List<ResourceIssue>();

        foreach (var file in session.Files)
        {
            var lines = file.Content.Split('\n');

            issues.AddRange(CheckStringOverflow(file.FilePath, lines));
            issues.AddRange(CheckPointerUsage(file.FilePath, lines));
        }

        return issues;
    }

    private List<ResourceIssue> CheckStringOverflow(string filePath, string[] lines)
    {
        var issues = new List<ResourceIssue>();
        var stringVars = new Dictionary<string, (int Size, int Line)>();

        // STRING 변수 수집
        for (int i = 0; i < lines.Length; i++)
        {
            // STRING(80), STRING 등
            var match = Regex.Match(lines[i], @"(\w+)\s*:\s*STRING(?:\((\d+)\))?", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var varName = match.Groups[1].Value;
                var size = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 80; // 기본 80자
                stringVars[varName] = (size, i + 1);
            }
        }

        // CONCAT, 문자열 할당 검사
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // CONCAT 사용 시 결과 문자열 길이 체크
            if (Regex.IsMatch(line, @"\bCONCAT\s*\(", RegexOptions.IgnoreCase))
            {
                // 결과를 저장하는 변수 찾기
                var assignMatch = Regex.Match(line, @"(\w+)\s*:=\s*CONCAT", RegexOptions.IgnoreCase);
                if (assignMatch.Success)
                {
                    var targetVar = assignMatch.Groups[1].Value;
                    if (stringVars.ContainsKey(targetVar))
                    {
                        // 길이 체크가 있는지 확인
                        bool hasLenCheck = false;
                        for (int j = Math.Max(0, i - 5); j <= i; j++)
                        {
                            if (Regex.IsMatch(lines[j], @"\bLEN\s*\(", RegexOptions.IgnoreCase))
                            {
                                hasLenCheck = true;
                                break;
                            }
                        }

                        if (!hasLenCheck)
                        {
                            issues.Add(new ResourceIssue
                            {
                                Type = ResourceIssueType.StringBufferOverflow,
                                FilePath = filePath,
                                Line = i + 1,
                                VariableName = targetVar,
                                DataType = $"STRING({stringVars[targetVar].Size})",
                                Severity = IssueSeverity.High,
                                Message = $"CONCAT 결과를 '{targetVar}'에 저장하지만 길이 체크 없음 - 버퍼 오버플로우 가능",
                                Recommendation = "CONCAT 전 LEN() 함수로 결과 길이를 확인하세요"
                            });
                        }
                    }
                }
            }
        }

        return issues;
    }

    private List<ResourceIssue> CheckPointerUsage(string filePath, string[] lines)
    {
        var issues = new List<ResourceIssue>();
        var pointerVars = new Dictionary<string, int>();

        // POINTER 변수 수집
        for (int i = 0; i < lines.Length; i++)
        {
            var match = Regex.Match(lines[i], @"(\w+)\s*:\s*POINTER\s+TO", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                pointerVars[match.Groups[1].Value] = i + 1;
            }
        }

        // 포인터 역참조 시 널 체크 확인
        for (int i = 0; i < lines.Length; i++)
        {
            foreach (var (ptrName, declLine) in pointerVars)
            {
                // 포인터 역참조 패턴: ptr^
                if (Regex.IsMatch(lines[i], $@"\b{Regex.Escape(ptrName)}\^", RegexOptions.IgnoreCase))
                {
                    // 앞 5줄 내에 널 체크가 있는지 확인
                    bool hasNullCheck = false;
                    for (int j = Math.Max(0, i - 5); j <= i; j++)
                    {
                        if (Regex.IsMatch(lines[j], $@"\b{Regex.Escape(ptrName)}\s*(<>|!=)\s*0", RegexOptions.IgnoreCase) ||
                            Regex.IsMatch(lines[j], $@"0\s*(<>|!=)\s*{Regex.Escape(ptrName)}", RegexOptions.IgnoreCase))
                        {
                            hasNullCheck = true;
                            break;
                        }
                    }

                    if (!hasNullCheck)
                    {
                        issues.Add(new ResourceIssue
                        {
                            Type = ResourceIssueType.UncheckedPointer,
                            FilePath = filePath,
                            Line = i + 1,
                            VariableName = ptrName,
                            DataType = "POINTER",
                            Severity = IssueSeverity.Critical,
                            Message = $"포인터 '{ptrName}' 역참조 시 널 체크 없음",
                            Recommendation = "IF ptr <> 0 THEN ptr^ ... END_IF 로 널 체크하세요"
                        });
                        break; // 같은 포인터에 대해 한 번만 보고
                    }
                }
            }
        }

        return issues;
    }
}

/// <summary>
/// 센서 안전성 분석기 구현
/// </summary>
public class SensorAnalyzer : ISensorAnalyzer
{
    public List<SensorIssue> Analyze(ValidationSession session)
    {
        var issues = new List<SensorIssue>();

        foreach (var file in session.Files)
        {
            var lines = file.Content.Split('\n');

            issues.AddRange(CheckAnalogSensors(file.FilePath, lines));
            issues.AddRange(CheckDigitalSensors(file.FilePath, lines));
        }

        return issues;
    }

    private List<SensorIssue> CheckAnalogSensors(string filePath, string[] lines)
    {
        var issues = new List<SensorIssue>();
        var analogInputs = new Dictionary<string, (string Address, int Line, string SensorKind)>();

        // 아날로그 입력 변수 수집 (%IW, %ID - 워드/더블워드 입력)
        for (int i = 0; i < lines.Length; i++)
        {
            var match = Regex.Match(lines[i], @"(\w+)\s+AT\s+(%I[WD]\d+)\s*:\s*(INT|UINT|WORD|DINT|REAL)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var varName = match.Groups[1].Value;
                var sensorKind = DetectSensorKind(varName);
                analogInputs[varName] = (match.Groups[2].Value, i + 1, sensorKind);
            }
        }

        foreach (var (sensorName, info) in analogInputs)
        {
            bool hasRangeCheck = false;
            bool hasFaultDetection = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // 범위 체크 패턴
                if (Regex.IsMatch(line, $@"\b{Regex.Escape(sensorName)}\s*(>|<|>=|<=)", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(line, $@"(>|<|>=|<=)\s*{Regex.Escape(sensorName)}", RegexOptions.IgnoreCase))
                {
                    hasRangeCheck = true;
                }

                // 고장 감지 패턴 (0 또는 최대값 체크)
                if (Regex.IsMatch(line, $@"\b{Regex.Escape(sensorName)}\s*(=|<>)\s*(0|32767|65535)", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(line, $@"(0|32767|65535)\s*(=|<>)\s*{Regex.Escape(sensorName)}", RegexOptions.IgnoreCase))
                {
                    hasFaultDetection = true;
                }
            }

            // 센서 종류에 따른 심각도 결정 (압력/진공 센서는 더 중요)
            var severity = info.SensorKind switch
            {
                "Pressure" or "Vacuum" or "DiffPressure" => IssueSeverity.Critical,
                _ => IssueSeverity.High
            };

            if (!hasRangeCheck)
            {
                issues.Add(new SensorIssue
                {
                    Type = SensorIssueType.MissingRangeCheck,
                    FilePath = filePath,
                    Line = info.Line,
                    SensorName = sensorName,
                    SensorType = $"Analog/{info.SensorKind}",
                    IOAddress = info.Address,
                    Severity = severity,
                    Message = $"{info.SensorKind} 센서 '{sensorName}'({info.Address})의 값 범위 체크 없음",
                    Recommendation = GetRangeCheckRecommendation(info.SensorKind)
                });
            }

            if (!hasFaultDetection)
            {
                issues.Add(new SensorIssue
                {
                    Type = SensorIssueType.MissingFaultDetection,
                    FilePath = filePath,
                    Line = info.Line,
                    SensorName = sensorName,
                    SensorType = $"Analog/{info.SensorKind}",
                    IOAddress = info.Address,
                    Severity = severity,
                    Message = $"{info.SensorKind} 센서 '{sensorName}'({info.Address})의 고장 감지 로직 없음",
                    Recommendation = GetFaultDetectionRecommendation(info.SensorKind)
                });
            }
        }

        return issues;
    }

    /// <summary>
    /// 센서 종류 감지 (변수명 기반)
    /// </summary>
    private string DetectSensorKind(string varName)
    {
        var name = varName.ToLower();

        // 차압 센서
        if (name.Contains("diff") && name.Contains("press") || name.Contains("dp") || name.Contains("차압"))
            return "DiffPressure";

        // 진공 센서
        if (name.Contains("vacuum") || name.Contains("vac") || name.Contains("진공"))
            return "Vacuum";

        // 압력 센서/게이지
        if (name.Contains("press") || name.Contains("gauge") || name.Contains("압력"))
            return "Pressure";

        // 온도 센서
        if (name.Contains("temp") || name.Contains("온도"))
            return "Temperature";

        // 유량 센서
        if (name.Contains("flow") || name.Contains("유량"))
            return "Flow";

        // 레벨 센서
        if (name.Contains("level") || name.Contains("레벨"))
            return "Level";

        return "General";
    }

    /// <summary>
    /// 센서 종류별 범위 체크 권장사항
    /// </summary>
    private string GetRangeCheckRecommendation(string sensorKind)
    {
        return sensorKind switch
        {
            "Pressure" => "압력 센서의 유효 범위(예: 0~10bar)를 확인하고, 과압/저압 알람을 설정하세요",
            "Vacuum" => "진공도 유효 범위를 확인하고, 진공 파괴/누설 알람을 설정하세요",
            "DiffPressure" => "차압 유효 범위를 확인하고, 필터 막힘/시스템 이상 알람을 설정하세요",
            "Temperature" => "온도 유효 범위를 확인하고, 과열/저온 알람을 설정하세요",
            "Flow" => "유량 유효 범위를 확인하고, 유량 이상 알람을 설정하세요",
            "Level" => "레벨 유효 범위를 확인하고, 과충/저충 알람을 설정하세요",
            _ => "센서 값이 유효 범위 내인지 확인하세요"
        };
    }

    /// <summary>
    /// 센서 종류별 고장 감지 권장사항
    /// </summary>
    private string GetFaultDetectionRecommendation(string sensorKind)
    {
        return sensorKind switch
        {
            "Pressure" or "Vacuum" or "DiffPressure" =>
                "센서 단선(0) 또는 단락(MAX), 급격한 변화율을 감지하는 로직을 추가하세요. 압력 센서 고장은 시스템 안전에 직결됩니다",
            "Temperature" => "센서 단선(0 또는 MAX), 온도 급변 감지 로직을 추가하세요",
            _ => "센서 단선(0) 또는 단락(MAX) 상태를 감지하는 로직을 추가하세요"
        };
    }

    private List<SensorIssue> CheckDigitalSensors(string filePath, string[] lines)
    {
        var issues = new List<SensorIssue>();
        var digitalInputs = new Dictionary<string, (string Address, int Line)>();

        // 디지털 입력 변수 수집 (%IX - 비트 입력)
        for (int i = 0; i < lines.Length; i++)
        {
            // sensor, proximity, photo 등 센서 관련 이름 패턴
            var match = Regex.Match(lines[i],
                @"(\w*(?:sensor|prox|photo|limit|switch|detect)\w*)\s+AT\s+(%IX\d+\.\d+)\s*:\s*BOOL",
                RegexOptions.IgnoreCase);
            if (match.Success)
            {
                digitalInputs[match.Groups[1].Value] = (match.Groups[2].Value, i + 1);
            }
        }

        foreach (var (sensorName, info) in digitalInputs)
        {
            bool hasChatteringFilter = false;
            bool hasTimeoutCheck = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // 채터링 필터 패턴 (TON/TOF 사용, R_TRIG/F_TRIG 사용)
                if (Regex.IsMatch(line, $@"(TON|TOF|R_TRIG|F_TRIG).*{Regex.Escape(sensorName)}", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(line, $@"{Regex.Escape(sensorName)}.*\.(Q|IN)", RegexOptions.IgnoreCase))
                {
                    hasChatteringFilter = true;
                }

                // 타임아웃 체크 패턴 (센서 응답 대기)
                if (Regex.IsMatch(line, $@"(TIMEOUT|tTimeout).*{Regex.Escape(sensorName)}", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(line, $@"TON.*{Regex.Escape(sensorName)}", RegexOptions.IgnoreCase))
                {
                    hasTimeoutCheck = true;
                }
            }

            if (!hasChatteringFilter)
            {
                issues.Add(new SensorIssue
                {
                    Type = SensorIssueType.MissingChatteringFilter,
                    FilePath = filePath,
                    Line = info.Line,
                    SensorName = sensorName,
                    SensorType = "Digital",
                    IOAddress = info.Address,
                    Severity = IssueSeverity.Warning,
                    Message = $"디지털 센서 '{sensorName}'({info.Address})에 채터링 필터 없음",
                    Recommendation = "TON/TOF 타이머 또는 R_TRIG/F_TRIG를 사용하여 신호 떨림을 필터링하세요"
                });
            }
        }

        return issues;
    }
}
