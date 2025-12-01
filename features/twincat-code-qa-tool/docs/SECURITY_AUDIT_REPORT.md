# TwinCatQA 프로젝트 보안 취약점 분석 보고서

**분석 일자**: 2025-11-26
**분석 대상**: D:\01. Vscode\Twincat\features\twincat-code-qa-tool
**분석자**: Security Engineer Agent
**프로젝트 버전**: 1.0.0

---

## 📊 보안 점수 및 요약

### 전체 보안 점수: **7.2 / 10**

**등급**: **양호 (Good)** - 일부 개선 필요

### 심각도별 취약점 요약

| 심각도 | 개수 | 설명 |
|--------|------|------|
| 🔴 **Critical** | 1 | 즉시 조치 필요 |
| 🟠 **High** | 3 | 우선 조치 권장 |
| 🟡 **Medium** | 4 | 단기 개선 권장 |
| 🔵 **Low** | 2 | 장기 개선 고려 |
| **총계** | **10** | |

---

## 🔴 Critical 취약점 (즉시 조치 필요)

### CRT-001: 경로 순회 (Path Traversal) 취약점

**파일**: `LibGit2Service.cs` (Line 395-446)
**심각도**: 🔴 **Critical**
**CVSS 점수**: 8.6 (High)

#### 취약점 설명
```csharp
public bool InstallPreCommitHook(string repoPath, bool blockOnCritical = true)
{
    var gitDir = FindGitDirectory(repoPath);
    var hooksDir = Path.Combine(gitDir, "hooks");

    if (!Directory.Exists(hooksDir))
    {
        Directory.CreateDirectory(hooksDir);  // ❌ 경로 검증 없음
    }

    string hookPath = Path.Combine(hooksDir, hookFileName);
    File.WriteAllText(hookPath, scriptContent);  // ❌ 임의 경로 쓰기 가능
}
```

**공격 시나리오**:
```csharp
// 악의적 입력
var service = new LibGit2Service(logger);
service.InstallPreCommitHook("../../../../etc/passwd", true);
// → 시스템 파일 덮어쓰기 가능
```

#### 영향도
- 임의 파일 시스템 위치에 파일 쓰기 가능
- 시스템 파일 덮어쓰기 위험
- 권한 상승 공격 가능성

#### 개선 권장사항 (우선순위: 1)

**방어 코드 예시**:
```csharp
public bool InstallPreCommitHook(string repoPath, bool blockOnCritical = true)
{
    // 1. 경로 정규화 및 검증
    repoPath = Path.GetFullPath(repoPath);

    // 2. 허용된 루트 디렉토리 내부인지 확인
    var allowedRoot = Path.GetFullPath(Environment.CurrentDirectory);
    if (!repoPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
    {
        throw new SecurityException($"경로가 허용된 범위를 벗어났습니다: {repoPath}");
    }

    // 3. Git 저장소 유효성 확인
    if (!IsGitRepository(repoPath))
    {
        throw new ArgumentException("유효한 Git 저장소가 아닙니다.");
    }

    var gitDir = FindGitDirectory(repoPath);
    var hooksDir = Path.Combine(gitDir, "hooks");

    // 4. 경로 순회 문자 검증
    if (hooksDir.Contains("..") || hooksDir.Contains("~"))
    {
        throw new SecurityException("경로에 허용되지 않은 문자가 포함되어 있습니다.");
    }

    // ... 나머지 로직
}
```

**추가 보안 조치**:
- `Path.GetFullPath()`로 경로 정규화
- 화이트리스트 기반 경로 검증
- 파일 쓰기 전 권한 확인

---

## 🟠 High 취약점 (우선 조치 권장)

### HGH-001: 외부 프로세스 명령어 주입 취약점

**파일**: `GraphvizVisualizationService.cs` (Line 99-108)
**심각도**: 🟠 **High**
**CVSS 점수**: 7.3 (High)

#### 취약점 설명
```csharp
var process = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = GRAPHVIZ_DOT_COMMAND,  // "dot"
        Arguments = $"-Tsvg \"{dotFilePath}\" -o \"{outputPath}\"",  // ❌ 입력 검증 없음
        UseShellExecute = false,
        CreateNoWindow = true
    }
};
```

**공격 시나리오**:
```csharp
// 악의적 입력
string maliciousPath = "test.svg\" && rm -rf / #";
await service.ConvertToSvgAsync(dotContent, maliciousPath);
// 실행: dot -Tsvg "..." -o "test.svg" && rm -rf / #"
```

#### 영향도
- 임의 명령어 실행 가능
- 시스템 파일 삭제/변조
- 데이터 유출 위험

#### 개선 권장사항 (우선순위: 2)

```csharp
public async Task<string?> ConvertToSvgAsync(
    string dotContent,
    string outputPath,
    CancellationToken cancellationToken = default)
{
    // 1. 입력 검증
    if (!IsValidFilePath(outputPath))
    {
        throw new ArgumentException("유효하지 않은 파일 경로입니다.", nameof(outputPath));
    }

    // 2. 확장자 검증
    var allowedExtensions = new[] { ".svg", ".png", ".pdf" };
    var extension = Path.GetExtension(outputPath).ToLowerInvariant();
    if (!allowedExtensions.Contains(extension))
    {
        throw new ArgumentException("지원하지 않는 파일 형식입니다.");
    }

    // 3. 경로 정규화
    dotFilePath = Path.GetFullPath(dotFilePath);
    outputPath = Path.GetFullPath(outputPath);

    // 4. 위험 문자 제거
    dotFilePath = SanitizeFilePath(dotFilePath);
    outputPath = SanitizeFilePath(outputPath);

    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = GRAPHVIZ_DOT_COMMAND,
            // 개별 인수로 전달 (문자열 보간 대신)
            ArgumentList =
            {
                "-Tsvg",
                dotFilePath,
                "-o",
                outputPath
            },
            UseShellExecute = false,
            CreateNoWindow = true
        }
    };
}

private static string SanitizeFilePath(string path)
{
    // 위험 문자 제거
    var dangerous = new[] { "|", "&", ";", "$", "`", "\n", "\r" };
    foreach (var ch in dangerous)
    {
        path = path.Replace(ch, "");
    }
    return path;
}

private static bool IsValidFilePath(string path)
{
    try
    {
        var fullPath = Path.GetFullPath(path);
        return !string.IsNullOrWhiteSpace(fullPath)
            && !path.Contains("..")
            && !path.Contains("~");
    }
    catch
    {
        return false;
    }
}
```

---

### HGH-002: XML 외부 엔티티 (XXE) 주입 취약점

**파일**: `AntlrParserService.cs` (Line 294)
**심각도**: 🟠 **High**
**CVSS 점수**: 7.1 (High)

#### 취약점 설명
```csharp
private string ExtractStructuredTextFromXml(string filePath)
{
    var doc = XDocument.Load(filePath);  // ❌ XXE 취약점
    // ...
}
```

**공격 시나리오**:
악의적인 TwinCAT XML 파일:
```xml
<?xml version="1.0"?>
<!DOCTYPE foo [
  <!ENTITY xxe SYSTEM "file:///etc/passwd">
]>
<TcPlcObject>
  <Declaration>&xxe;</Declaration>
</TcPlcObject>
```

#### 영향도
- 시스템 파일 읽기 (정보 유출)
- 서비스 거부 공격 (Billion Laughs)
- 네트워크 정찰 가능

#### 개선 권장사항 (우선순위: 3)

```csharp
private string ExtractStructuredTextFromXml(string filePath)
{
    // 안전한 XML 로드 설정
    var settings = new XmlReaderSettings
    {
        DtdProcessing = DtdProcessing.Prohibit,  // DTD 금지
        XmlResolver = null,  // 외부 참조 금지
        MaxCharactersFromEntities = 1024,
        MaxCharactersInDocument = 10_000_000  // 10MB 제한
    };

    using var reader = XmlReader.Create(filePath, settings);
    var doc = XDocument.Load(reader);

    // 또는 안전한 로드 옵션 사용
    // var doc = XDocument.Load(filePath, LoadOptions.None);

    // ... 나머지 로직
}
```

---

### HGH-003: 안전하지 않은 역직렬화 취약점

**파일**: `ConfigurationService.cs` (Line 74)
**심각도**: 🟠 **High**
**CVSS 점수**: 6.8 (Medium-High)

#### 취약점 설명
```csharp
public QualitySettings LoadSettings(string projectPath)
{
    var yamlContent = File.ReadAllText(settingsFilePath);
    var settings = _deserializer.Deserialize<QualitySettings>(yamlContent);  // ❌ 검증 없음
}
```

**공격 시나리오**:
악의적인 YAML 파일:
```yaml
global:
  logLevel: "{{ system('rm -rf /') }}"  # 템플릿 인젝션 시도
rules:
  configurations:
    malicious: !tag:clr:System.Diagnostics.Process,mscorlib
```

#### 영향도
- 임의 코드 실행 가능성 (YamlDotNet은 상대적으로 안전하나 주의 필요)
- 설정 값 조작
- 애플리케이션 동작 변경

#### 개선 권장사항 (우선순위: 4)

```csharp
public QualitySettings LoadSettings(string projectPath)
{
    // ... 기존 코드

    try
    {
        var yamlContent = File.ReadAllText(settingsFilePath);

        // 1. 파일 크기 제한
        if (yamlContent.Length > 1_000_000)  // 1MB
        {
            throw new ConfigurationException("설정 파일이 너무 큽니다.");
        }

        // 2. 안전한 역직렬화 설정
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .WithTypeConverter(new SafeStringConverter())  // 커스텀 검증
            .Build();

        var settings = deserializer.Deserialize<QualitySettings>(yamlContent);

        // 3. 역직렬화 후 검증
        ValidateSettings(settings);

        return MergeWithDefaults(settings);
    }
    catch (YamlDotNet.Core.YamlException ex)
    {
        _logger.LogError(ex, "YAML 파싱 오류");
        throw new ConfigurationException("설정 파일 형식이 올바르지 않습니다.", ex);
    }
}

private void ValidateSettings(QualitySettings settings)
{
    // 범위 검증
    if (settings.Global?.MaxDegreeOfParallelism < 1
        || settings.Global?.MaxDegreeOfParallelism > 64)
    {
        throw new ConfigurationException("MaxDegreeOfParallelism 값이 유효하지 않습니다.");
    }

    if (settings.Global?.TimeoutSeconds < 1
        || settings.Global?.TimeoutSeconds > 3600)
    {
        throw new ConfigurationException("TimeoutSeconds 값이 유효하지 않습니다.");
    }

    // 경로 검증
    if (!string.IsNullOrEmpty(settings.Reports?.OutputPath))
    {
        if (settings.Reports.OutputPath.Contains("..")
            || Path.IsPathRooted(settings.Reports.OutputPath))
        {
            throw new ConfigurationException("OutputPath에 절대 경로나 상대 경로 순회가 포함되어 있습니다.");
        }
    }
}
```

---

## 🟡 Medium 취약점 (단기 개선 권장)

### MED-001: 예외 정보 유출

**파일**: 다수 (`QaCommand.cs`, `CompareCommand.cs` 등)
**심각도**: 🟡 **Medium**
**CVSS 점수**: 5.3 (Medium)

#### 취약점 설명
```csharp
catch (Exception ex)
{
    Console.WriteLine($"오류 발생: {ex.Message}");  // ❌ 스택 트레이스 노출
    if (verbose)
    {
        Console.WriteLine(ex.StackTrace);  // ❌ 내부 구조 노출
    }
}
```

#### 영향도
- 내부 파일 경로 노출
- 시스템 구조 정보 유출
- 공격자에게 유용한 정보 제공

#### 개선 권장사항 (우선순위: 5)

```csharp
catch (Exception ex)
{
    // 사용자에게는 일반적인 메시지만
    Console.WriteLine("처리 중 오류가 발생했습니다. 관리자에게 문의하세요.");

    // 로그에는 상세 정보 기록
    _logger.LogError(ex, "분석 실패: {ProjectPath}", projectPath);

    // verbose 모드에서도 민감 정보 필터링
    if (verbose)
    {
        Console.WriteLine($"오류 유형: {ex.GetType().Name}");
        Console.WriteLine($"오류 코드: {GetErrorCode(ex)}");
        // 스택 트레이스는 로그 파일에만 기록
    }
}
```

---

### MED-002: 입력 검증 부족 (파일 경로)

**파일**: `FileScanner.cs` (Line 27-42), `QaCommand.cs` (Line 111-121)
**심각도**: 🟡 **Medium**
**CVSS 점수**: 5.8 (Medium)

#### 취약점 설명
```csharp
public static List<string> ScanTwinCATFiles(string projectPath)
{
    if (!Directory.Exists(projectPath))  // ❌ 경로 정규화 없음
    {
        throw new DirectoryNotFoundException($"프로젝트 경로가 존재하지 않습니다: {projectPath}");
    }

    var files = new List<string>();
    foreach (var extension in TwinCATExtensions)
    {
        var foundFiles = Directory.GetFiles(projectPath, $"*{extension}", SearchOption.AllDirectories);
        // ❌ 심볼릭 링크 공격, 경로 순회 가능
    }
}
```

#### 영향도
- 의도하지 않은 디렉토리 접근
- 심볼릭 링크를 통한 권한 우회
- 서비스 거부 공격 (대용량 디렉토리 스캔)

#### 개선 권장사항 (우선순위: 6)

```csharp
public static List<string> ScanTwinCATFiles(string projectPath)
{
    // 1. 경로 정규화
    projectPath = Path.GetFullPath(projectPath);

    // 2. 허용된 루트 확인
    var workingDir = Path.GetFullPath(Environment.CurrentDirectory);
    if (!projectPath.StartsWith(workingDir, StringComparison.OrdinalIgnoreCase))
    {
        throw new SecurityException("허용되지 않은 경로입니다.");
    }

    // 3. 디렉토리 존재 확인
    if (!Directory.Exists(projectPath))
    {
        throw new DirectoryNotFoundException($"프로젝트 경로가 존재하지 않습니다: {projectPath}");
    }

    // 4. 최대 파일 수 제한
    const int MaxFiles = 10000;
    var files = new List<string>();

    foreach (var extension in TwinCATExtensions)
    {
        var foundFiles = Directory.EnumerateFiles(projectPath, $"*{extension}",
            new EnumerationOptions
            {
                RecurseSubdirectories = true,
                MatchCasing = MatchCasing.CaseInsensitive,
                MaxRecursionDepth = 20,  // 재귀 깊이 제한
                AttributesToSkip = FileAttributes.System | FileAttributes.Hidden
            });

        foreach (var file in foundFiles)
        {
            if (files.Count >= MaxFiles)
            {
                throw new InvalidOperationException($"최대 파일 개수({MaxFiles})를 초과했습니다.");
            }

            // 심볼릭 링크 제외
            var fileInfo = new FileInfo(file);
            if ((fileInfo.Attributes & FileAttributes.ReparsePoint) == 0)
            {
                files.Add(file);
            }
        }
    }

    return files;
}
```

---

### MED-003: 민감 정보 로깅

**파일**: `LibGit2Service.cs`, `GraphvizVisualizationService.cs` 등
**심각도**: 🟡 **Medium**
**CVSS 점수**: 4.7 (Medium)

#### 취약점 설명
```csharp
_logger.LogInformation($"Graphviz 실행: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
// ❌ 파일 경로가 로그에 노출됨
```

#### 영향도
- 파일 시스템 구조 노출
- 사용자 경로 정보 유출
- 로그 파일을 통한 정보 수집

#### 개선 권장사항 (우선순위: 7)

```csharp
// 민감 정보 마스킹 유틸리티
public static class LogSanitizer
{
    public static string MaskFilePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        var fileName = Path.GetFileName(path);
        var extension = Path.GetExtension(path);
        return $"***/{fileName}";
    }

    public static string MaskArguments(string arguments)
    {
        // 경로처럼 보이는 패턴 마스킹
        return Regex.Replace(arguments, @"[A-Za-z]:\\[^\s""]+", "***");
    }
}

// 사용 예시
_logger.LogInformation($"Graphviz 실행: {process.StartInfo.FileName} {LogSanitizer.MaskArguments(process.StartInfo.Arguments)}");
```

---

### MED-004: 타임아웃 설정 부재

**파일**: `GraphvizVisualizationService.cs` (Line 112-117)
**심각도**: 🟡 **Medium**
**CVSS 점수**: 4.3 (Medium)

#### 취약점 설명
```csharp
process.Start();
await process.WaitForExitAsync(cancellationToken);  // ❌ 타임아웃 없음
```

#### 영향도
- 서비스 거부 공격 가능
- 리소스 고갈
- 애플리케이션 응답 없음

#### 개선 권장사항 (우선순위: 8)

```csharp
process.Start();

// 타임아웃 설정 (30초)
var timeout = TimeSpan.FromSeconds(30);
using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
cts.CancelAfter(timeout);

try
{
    await process.WaitForExitAsync(cts.Token);

    if (process.ExitCode != 0)
    {
        _logger.LogError($"Graphviz 변환 실패 (ExitCode: {process.ExitCode})");
        return null;
    }
}
catch (OperationCanceledException)
{
    if (!process.HasExited)
    {
        process.Kill(entireProcessTree: true);
        _logger.LogError("Graphviz 프로세스 타임아웃으로 종료됨");
    }
    throw new TimeoutException("Graphviz 변환이 시간 초과되었습니다.");
}
```

---

## 🔵 Low 취약점 (장기 개선 고려)

### LOW-001: 약한 난수 생성 (해당 없음 - 발견되지 않음)

**현재 상태**: 프로젝트에서 암호학적으로 안전한 난수가 필요한 경우가 발견되지 않음.

---

### LOW-002: 하드코딩된 설정 값

**파일**: `ConfigurationService.cs` (Line 166-194)
**심각도**: 🔵 **Low**
**CVSS 점수**: 2.1 (Low)

#### 취약점 설명
```csharp
MaxDegreeOfParallelism = 4,  // 하드코딩
TimeoutSeconds = 300,
```

#### 영향도
- 환경별 최적화 어려움
- 설정 변경 시 재컴파일 필요

#### 개선 권장사항 (우선순위: 9)

환경 변수 또는 외부 설정 파일 사용:
```csharp
MaxDegreeOfParallelism = Environment.GetEnvironmentVariable("TWINCAT_QA_MAX_PARALLEL")
    ?.TryParse<int>() ?? 4,
TimeoutSeconds = Environment.GetEnvironmentVariable("TWINCAT_QA_TIMEOUT")
    ?.TryParse<int>() ?? 300,
```

---

## ✅ 보안 강점 (Good Security Practices)

1. **의존성 관리**: NuGet 패키지에 알려진 취약점 없음 (dotnet list package --vulnerable 확인)
2. **최신 프레임워크**: .NET 8.0/9.0 사용 (보안 패치 적용)
3. **안전한 라이브러리 선택**:
   - LibGit2Sharp (0.27.0) - 안정적인 Git 라이브러리
   - YamlDotNet - 상대적으로 안전한 YAML 파서
   - ANTLR4 - 검증된 파서 생성기
4. **코드 구조**: 계층 아키텍처 (Domain, Infrastructure, Application 분리)
5. **로깅 사용**: Microsoft.Extensions.Logging 활용
6. **타입 안정성**: Nullable 활성화 (null 참조 예외 감소)
7. **비밀번호/토큰 하드코딩 없음**: 자격 증명 정보 미발견

---

## 📋 개선 권장사항 요약 (우선순위순)

| 우선순위 | 취약점 ID | 설명 | 심각도 | 예상 작업 시간 |
|---------|----------|------|--------|--------------|
| 1 | CRT-001 | 경로 순회 취약점 수정 | Critical | 4시간 |
| 2 | HGH-001 | 명령어 주입 방어 | High | 3시간 |
| 3 | HGH-002 | XXE 취약점 수정 | High | 2시간 |
| 4 | HGH-003 | 역직렬화 검증 추가 | High | 4시간 |
| 5 | MED-001 | 예외 정보 유출 방지 | Medium | 2시간 |
| 6 | MED-002 | 입력 검증 강화 | Medium | 3시간 |
| 7 | MED-003 | 로그 민감정보 마스킹 | Medium | 2시간 |
| 8 | MED-004 | 타임아웃 설정 추가 | Medium | 1시간 |
| 9 | LOW-002 | 설정 외부화 | Low | 2시간 |

**총 예상 작업 시간**: 23시간 (약 3일)

---

## 🛡️ 보안 개선 로드맵

### Phase 1: 긴급 (1주 이내)
- [x] 의존성 취약점 스캔 완료
- [ ] CRT-001: 경로 순회 취약점 수정
- [ ] HGH-001: 명령어 주입 방어 구현

### Phase 2: 단기 (2주 이내)
- [ ] HGH-002: XXE 취약점 수정
- [ ] HGH-003: 역직렬화 검증 추가
- [ ] MED-001, MED-002: 입력 검증 및 예외 처리 개선

### Phase 3: 중기 (1개월 이내)
- [ ] MED-003, MED-004: 로깅 및 타임아웃 개선
- [ ] 보안 코딩 가이드라인 문서화
- [ ] 정적 분석 도구 통합 (SonarQube, Checkmarx)

### Phase 4: 장기 (3개월 이내)
- [ ] 침투 테스트 수행
- [ ] 보안 회귀 테스트 자동화
- [ ] OWASP Top 10 재검증

---

## 📚 보안 코딩 가이드라인

### 파일 시스템 작업
```csharp
// ✅ 올바른 예시
public void SafeFileOperation(string userPath)
{
    // 1. 경로 정규화
    var fullPath = Path.GetFullPath(userPath);

    // 2. 허용 범위 확인
    var allowedRoot = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
    if (!fullPath.StartsWith(allowedRoot))
    {
        throw new SecurityException("허용되지 않은 경로입니다.");
    }

    // 3. 작업 수행
    File.WriteAllText(fullPath, content);
}

// ❌ 잘못된 예시
public void UnsafeFileOperation(string userPath)
{
    File.WriteAllText(userPath, content);  // 경로 검증 없음
}
```

### 외부 프로세스 실행
```csharp
// ✅ 올바른 예시
var psi = new ProcessStartInfo
{
    FileName = "tool.exe",
    ArgumentList = { arg1, arg2 },  // 개별 인수 사용
    UseShellExecute = false,
    CreateNoWindow = true
};

// ❌ 잘못된 예시
var psi = new ProcessStartInfo
{
    FileName = "tool.exe",
    Arguments = $"{arg1} {arg2}",  // 문자열 보간 위험
    UseShellExecute = true  // 셸 실행 위험
};
```

### XML 파싱
```csharp
// ✅ 올바른 예시
var settings = new XmlReaderSettings
{
    DtdProcessing = DtdProcessing.Prohibit,
    XmlResolver = null
};
using var reader = XmlReader.Create(path, settings);
var doc = XDocument.Load(reader);

// ❌ 잘못된 예시
var doc = XDocument.Load(path);  // XXE 취약
```

---

## 🔗 참고 자료

1. **OWASP Top 10 (2021)**
   - A01: Broken Access Control
   - A03: Injection
   - A05: Security Misconfiguration
   - https://owasp.org/Top10/

2. **CWE 참조**
   - CWE-22: Path Traversal
   - CWE-78: OS Command Injection
   - CWE-611: XXE (XML External Entities)
   - CWE-502: Deserialization of Untrusted Data

3. **.NET 보안 가이드**
   - https://learn.microsoft.com/en-us/dotnet/standard/security/
   - https://cheatsheetseries.owasp.org/cheatsheets/DotNet_Security_Cheat_Sheet.html

4. **보안 도구**
   - SonarQube: https://www.sonarqube.org/
   - Snyk: https://snyk.io/
   - OWASP Dependency-Check

---

## 📞 문의 및 보고

보안 취약점 발견 시:
- **이메일**: security@twincat-qa.local
- **이슈 트래커**: (비공개 보안 이슈)
- **책임 있는 공개 정책**: 90일 수정 기간

---

**보고서 생성일**: 2025-11-26
**다음 보안 감사 예정일**: 2025-12-26 (1개월 후)
**보고서 버전**: 1.0
