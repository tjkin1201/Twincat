# TwinCatQA 플러그인 시스템 및 사용자 정의 규칙 설계

## 목차
1. [개요](#1-개요)
2. [플러그인 아키텍처](#2-플러그인-아키텍처)
3. [규칙 정의 인터페이스 확장](#3-규칙-정의-인터페이스-확장)
4. [설정 파일 스키마](#4-설정-파일-스키마)
5. [규칙 패키지 배포 방식](#5-규칙-패키지-배포-방식)
6. [API 설계](#6-api-설계)
7. [구현 로드맵](#7-구현-로드맵)

---

## 1. 개요

### 1.1 현재 상황
- **18개의 하드코딩된 QA 규칙** (Infrastructure 레이어)
- `IValidationRule` 인터페이스 존재
- Clean Architecture 기반 (Domain → Application → Infrastructure)
- Configuration 시스템 부분적 구현 (`QualitySettings.cs`)

### 1.2 설계 목표
1. **확장성**: 코드 재컴파일 없이 새 규칙 추가
2. **플러그 앤 플레이**: 동적 로딩을 통한 규칙 플러그인 지원
3. **커스터마이징**: 조직별/프로젝트별 규칙 파라미터 설정
4. **배포 용이성**: NuGet 패키지 기반 규칙 배포
5. **API 통합**: 외부 도구(CI/CD, IDE 플러그인) 연동

### 1.3 주요 기능
- 동적 규칙 로딩 (Reflection 기반)
- 규칙 메타데이터 검증
- 설정 기반 규칙 활성화/비활성화
- 규칙 버전 관리
- 외부 API를 통한 원격 규칙 실행

---

## 2. 플러그인 아키텍처

### 2.1 아키텍처 계층 구조

```
┌─────────────────────────────────────────────────────────┐
│                     TwinCatQA.CLI                        │
│                  (사용자 인터페이스)                     │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│              TwinCatQA.Application                       │
│         (ValidationEngine, Orchestrator)                 │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│               TwinCatQA.Plugin.Core                      │
│    (플러그인 로더, 규칙 레지스트리, 생명주기 관리)       │
└──────┬────────────┬────────────────────────┬────────────┘
       │            │                        │
┌──────▼──────┐ ┌──▼────────────────┐ ┌─────▼─────────┐
│   Built-in  │ │  Custom Plugins   │ │  Remote API   │
│    Rules    │ │  (DLL/.cs files)  │ │   Plugins     │
│  (18 rules) │ │                   │ │               │
└─────────────┘ └───────────────────┘ └───────────────┘
```

### 2.2 플러그인 로딩 메커니즘

#### 2.2.1 로딩 전략

```csharp
namespace TwinCatQA.Plugin.Core
{
    /// <summary>
    /// 플러그인 로딩 전략 인터페이스
    /// </summary>
    public interface IPluginLoader
    {
        /// <summary>
        /// 플러그인 소스에서 규칙을 로드합니다
        /// </summary>
        Task<IEnumerable<IValidationRule>> LoadRulesAsync(PluginSource source);

        /// <summary>
        /// 로더가 지원하는 플러그인 타입
        /// </summary>
        PluginType SupportedType { get; }
    }

    /// <summary>
    /// 플러그인 타입
    /// </summary>
    public enum PluginType
    {
        /// <summary>내장 규칙 (Infrastructure 레이어)</summary>
        BuiltIn,

        /// <summary>어셈블리 (.dll) 파일</summary>
        Assembly,

        /// <summary>C# 소스 코드 (.cs) - Roslyn 컴파일</summary>
        SourceCode,

        /// <summary>NuGet 패키지</summary>
        NuGetPackage,

        /// <summary>원격 API 엔드포인트</summary>
        RemoteApi
    }
}
```

#### 2.2.2 어셈블리 로더 구현

```csharp
namespace TwinCatQA.Plugin.Core.Loaders
{
    /// <summary>
    /// 어셈블리 파일(.dll)에서 규칙을 로드하는 로더
    /// </summary>
    public class AssemblyPluginLoader : IPluginLoader
    {
        private readonly ILogger<AssemblyPluginLoader> _logger;
        private readonly IRuleValidator _ruleValidator;

        public PluginType SupportedType => PluginType.Assembly;

        public AssemblyPluginLoader(
            ILogger<AssemblyPluginLoader> logger,
            IRuleValidator ruleValidator)
        {
            _logger = logger;
            _ruleValidator = ruleValidator;
        }

        public async Task<IEnumerable<IValidationRule>> LoadRulesAsync(PluginSource source)
        {
            var rules = new List<IValidationRule>();

            try
            {
                // AssemblyLoadContext를 사용한 격리된 로딩
                var loadContext = new PluginLoadContext(source.Path);
                var assembly = loadContext.LoadFromAssemblyPath(source.Path);

                // IValidationRule을 구현한 모든 타입 찾기
                var ruleTypes = assembly.GetTypes()
                    .Where(t => !t.IsAbstract &&
                                !t.IsInterface &&
                                typeof(IValidationRule).IsAssignableFrom(t));

                foreach (var ruleType in ruleTypes)
                {
                    // 규칙 인스턴스 생성
                    var rule = (IValidationRule)Activator.CreateInstance(ruleType);

                    // 메타데이터 검증
                    if (await _ruleValidator.ValidateAsync(rule))
                    {
                        rules.Add(rule);
                        _logger.LogInformation(
                            "플러그인 규칙 로드 완료: {RuleId} - {RuleName}",
                            rule.RuleId,
                            rule.RuleName);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "규칙 검증 실패: {RuleType}",
                            ruleType.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "어셈블리 로드 실패: {Path}",
                    source.Path);
                throw;
            }

            return rules;
        }
    }

    /// <summary>
    /// 플러그인 어셈블리 격리 컨텍스트
    /// </summary>
    internal class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginPath)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }
}
```

#### 2.2.3 소스 코드 로더 (Roslyn 컴파일)

```csharp
namespace TwinCatQA.Plugin.Core.Loaders
{
    /// <summary>
    /// C# 소스 코드(.cs)를 런타임 컴파일하여 로드하는 로더
    /// </summary>
    public class SourceCodePluginLoader : IPluginLoader
    {
        private readonly ILogger<SourceCodePluginLoader> _logger;
        private readonly IRuleValidator _ruleValidator;

        public PluginType SupportedType => PluginType.SourceCode;

        public async Task<IEnumerable<IValidationRule>> LoadRulesAsync(PluginSource source)
        {
            var rules = new List<IValidationRule>();

            try
            {
                // 소스 파일 읽기
                var sourceCode = await File.ReadAllTextAsync(source.Path);

                // Roslyn 구문 트리 생성
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

                // 메타데이터 참조 추가
                var references = new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(IValidationRule).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(CodeFile).Assembly.Location),
                    // 필요한 다른 참조 추가
                };

                // 컴파일 옵션
                var compilation = CSharpCompilation.Create(
                    assemblyName: Path.GetFileNameWithoutExtension(source.Path),
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                // 메모리 스트림에 컴파일
                using var ms = new MemoryStream();
                var result = compilation.Emit(ms);

                if (!result.Success)
                {
                    // 컴파일 오류 로깅
                    var failures = result.Diagnostics
                        .Where(diagnostic => diagnostic.IsWarningAsError ||
                                             diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (var diagnostic in failures)
                    {
                        _logger.LogError(
                            "컴파일 오류: {Location} - {Message}",
                            diagnostic.Location,
                            diagnostic.GetMessage());
                    }

                    throw new PluginCompilationException(
                        "소스 코드 컴파일 실패",
                        result.Diagnostics);
                }

                // 컴파일된 어셈블리 로드
                ms.Seek(0, SeekOrigin.Begin);
                var assembly = Assembly.Load(ms.ToArray());

                // 규칙 타입 추출
                var ruleTypes = assembly.GetTypes()
                    .Where(t => typeof(IValidationRule).IsAssignableFrom(t) &&
                                !t.IsAbstract);

                foreach (var ruleType in ruleTypes)
                {
                    var rule = (IValidationRule)Activator.CreateInstance(ruleType);

                    if (await _ruleValidator.ValidateAsync(rule))
                    {
                        rules.Add(rule);
                        _logger.LogInformation(
                            "소스 코드 규칙 로드 완료: {RuleId}",
                            rule.RuleId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "소스 코드 로드 실패: {Path}", source.Path);
                throw;
            }

            return rules;
        }
    }
}
```

#### 2.2.4 플러그인 레지스트리

```csharp
namespace TwinCatQA.Plugin.Core
{
    /// <summary>
    /// 로드된 모든 플러그인 규칙을 관리하는 레지스트리
    /// </summary>
    public class PluginRegistry : IPluginRegistry
    {
        private readonly Dictionary<string, IValidationRule> _rules = new();
        private readonly Dictionary<PluginType, IPluginLoader> _loaders = new();
        private readonly ILogger<PluginRegistry> _logger;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public PluginRegistry(
            IEnumerable<IPluginLoader> loaders,
            ILogger<PluginRegistry> logger)
        {
            _logger = logger;

            // 로더 등록
            foreach (var loader in loaders)
            {
                _loaders[loader.SupportedType] = loader;
            }
        }

        /// <summary>
        /// 플러그인 소스에서 규칙 로드 및 등록
        /// </summary>
        public async Task LoadFromSourceAsync(PluginSource source)
        {
            await _lock.WaitAsync();
            try
            {
                if (!_loaders.TryGetValue(source.Type, out var loader))
                {
                    throw new NotSupportedException(
                        $"플러그인 타입 {source.Type}은 지원되지 않습니다.");
                }

                var rules = await loader.LoadRulesAsync(source);

                foreach (var rule in rules)
                {
                    RegisterRule(rule, source);
                }

                _logger.LogInformation(
                    "플러그인 소스 로드 완료: {Source} ({Count}개 규칙)",
                    source.Path,
                    rules.Count());
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// 규칙 등록
        /// </summary>
        private void RegisterRule(IValidationRule rule, PluginSource source)
        {
            if (_rules.ContainsKey(rule.RuleId))
            {
                _logger.LogWarning(
                    "중복 규칙 ID: {RuleId} - 기존 규칙이 덮어쓰여집니다.",
                    rule.RuleId);
            }

            _rules[rule.RuleId] = rule;

            _logger.LogDebug(
                "규칙 등록: {RuleId} - {RuleName} (출처: {Source})",
                rule.RuleId,
                rule.RuleName,
                source.Path);
        }

        /// <summary>
        /// 규칙 ID로 조회
        /// </summary>
        public IValidationRule GetRule(string ruleId)
        {
            return _rules.TryGetValue(ruleId, out var rule) ? rule : null;
        }

        /// <summary>
        /// 모든 활성화된 규칙 조회
        /// </summary>
        public IEnumerable<IValidationRule> GetAllRules()
        {
            return _rules.Values.Where(r => r.IsEnabled);
        }

        /// <summary>
        /// 특정 언어를 지원하는 규칙 조회
        /// </summary>
        public IEnumerable<IValidationRule> GetRulesForLanguage(ProgrammingLanguage language)
        {
            return _rules.Values
                .Where(r => r.IsEnabled &&
                            r.SupportedLanguages.Contains(language));
        }

        /// <summary>
        /// 규칙 제거
        /// </summary>
        public bool UnregisterRule(string ruleId)
        {
            return _rules.Remove(ruleId);
        }
    }
}
```

### 2.3 플러그인 생명주기 관리

```csharp
namespace TwinCatQA.Plugin.Core
{
    /// <summary>
    /// 플러그인 생명주기 관리자
    /// </summary>
    public class PluginLifecycleManager
    {
        private readonly IPluginRegistry _registry;
        private readonly IConfigurationService _configService;
        private readonly ILogger<PluginLifecycleManager> _logger;

        public async Task InitializePluginsAsync()
        {
            var config = await _configService.LoadConfigurationAsync();

            // 1. 내장 규칙 로드
            await LoadBuiltInRulesAsync();

            // 2. 커스텀 규칙 디렉토리 로드
            if (!string.IsNullOrEmpty(config.Rules.CustomRulesPath))
            {
                await LoadFromDirectoryAsync(config.Rules.CustomRulesPath);
            }

            // 3. 설정 파일에 지정된 플러그인 로드
            foreach (var pluginSource in config.Plugins?.Sources ?? Enumerable.Empty<PluginSource>())
            {
                await _registry.LoadFromSourceAsync(pluginSource);
            }

            // 4. 규칙별 설정 적용
            ApplyRuleConfigurations(config.Rules.Configurations);

            _logger.LogInformation(
                "플러그인 초기화 완료: 총 {Count}개 규칙 로드됨",
                _registry.GetAllRules().Count());
        }

        private async Task LoadBuiltInRulesAsync()
        {
            var builtInSource = new PluginSource
            {
                Type = PluginType.BuiltIn,
                Path = typeof(IValidationRule).Assembly.Location
            };

            await _registry.LoadFromSourceAsync(builtInSource);
        }

        private async Task LoadFromDirectoryAsync(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                _logger.LogWarning(
                    "커스텀 규칙 디렉토리가 존재하지 않습니다: {Path}",
                    directoryPath);
                return;
            }

            // .dll 파일 로드
            var dllFiles = Directory.GetFiles(directoryPath, "*.dll");
            foreach (var dllFile in dllFiles)
            {
                var source = new PluginSource
                {
                    Type = PluginType.Assembly,
                    Path = dllFile
                };

                await _registry.LoadFromSourceAsync(source);
            }

            // .cs 파일 로드
            var csFiles = Directory.GetFiles(directoryPath, "*.cs");
            foreach (var csFile in csFiles)
            {
                var source = new PluginSource
                {
                    Type = PluginType.SourceCode,
                    Path = csFile
                };

                await _registry.LoadFromSourceAsync(source);
            }
        }

        private void ApplyRuleConfigurations(Dictionary<string, RuleConfiguration> configurations)
        {
            foreach (var (ruleId, config) in configurations)
            {
                var rule = _registry.GetRule(ruleId);
                if (rule == null)
                {
                    _logger.LogWarning(
                        "설정에 지정된 규칙을 찾을 수 없습니다: {RuleId}",
                        ruleId);
                    continue;
                }

                // 활성화 상태 설정
                rule.IsEnabled = config.Enabled;

                // 파라미터 적용
                if (config.Parameters.Any())
                {
                    rule.Configure(config.Parameters);
                }
            }
        }
    }
}
```

---

## 3. 규칙 정의 인터페이스 확장

### 3.1 확장된 IValidationRule 인터페이스

```csharp
namespace TwinCatQA.Domain.Contracts
{
    /// <summary>
    /// 확장된 검증 규칙 인터페이스
    /// </summary>
    public interface IValidationRule
    {
        // ===== 기본 속성 (기존) =====

        string RuleId { get; }
        string RuleName { get; }
        string Description { get; }
        ConstitutionPrinciple RelatedPrinciple { get; }
        ViolationSeverity DefaultSeverity { get; }
        bool IsEnabled { get; set; }
        ProgrammingLanguage[] SupportedLanguages { get; }

        // ===== 새로운 메타데이터 속성 =====

        /// <summary>
        /// 규칙 버전 (Semantic Versioning)
        /// </summary>
        string Version { get; }

        /// <summary>
        /// 규칙 작성자 또는 조직
        /// </summary>
        string Author { get; }

        /// <summary>
        /// 규칙 태그 (검색/필터링 용도)
        /// </summary>
        string[] Tags { get; }

        /// <summary>
        /// 규칙 카테고리 (보안, 성능, 가독성 등)
        /// </summary>
        RuleCategory Category { get; }

        /// <summary>
        /// 규칙이 호환되는 TwinCatQA 버전 범위
        /// </summary>
        string CompatibleVersionRange { get; }

        /// <summary>
        /// 규칙 실행에 필요한 종속성 목록
        /// </summary>
        RuleDependency[] Dependencies { get; }

        // ===== 기본 메서드 (기존) =====

        IEnumerable<Violation> Validate(CodeFile file);
        void Configure(Dictionary<string, object> parameters);

        // ===== 새로운 메서드 =====

        /// <summary>
        /// 규칙 초기화 (리소스 할당 등)
        /// </summary>
        Task InitializeAsync(IRuleContext context);

        /// <summary>
        /// 규칙 정리 (리소스 해제 등)
        /// </summary>
        Task DisposeAsync();

        /// <summary>
        /// 규칙 설정 스키마 반환 (JSON Schema)
        /// </summary>
        string GetConfigurationSchema();

        /// <summary>
        /// 규칙 자가 검증 (메타데이터, 구성 유효성 체크)
        /// </summary>
        ValidationResult SelfValidate();
    }

    /// <summary>
    /// 규칙 카테고리
    /// </summary>
    public enum RuleCategory
    {
        Security,        // 보안
        Performance,     // 성능
        Readability,     // 가독성
        Maintainability, // 유지보수성
        BestPractices,   // 모범 사례
        Safety,          // 안전성 (PLC 특화)
        Compliance,      // 규정 준수
        Custom           // 사용자 정의
    }

    /// <summary>
    /// 규칙 종속성
    /// </summary>
    public class RuleDependency
    {
        /// <summary>종속 규칙 ID</summary>
        public string RuleId { get; set; }

        /// <summary>최소 버전</summary>
        public string MinVersion { get; set; }

        /// <summary>선택적 종속성 여부</summary>
        public bool IsOptional { get; set; }
    }

    /// <summary>
    /// 규칙 실행 컨텍스트
    /// </summary>
    public interface IRuleContext
    {
        /// <summary>현재 프로젝트 경로</summary>
        string ProjectPath { get; }

        /// <summary>전역 설정</summary>
        QualitySettings Settings { get; }

        /// <summary>공유 캐시</summary>
        IRuleCache Cache { get; }

        /// <summary>로거</summary>
        ILogger Logger { get; }

        /// <summary>다른 규칙 조회</summary>
        IValidationRule GetRule(string ruleId);
    }
}
```

### 3.2 추상 기본 클래스 제공

```csharp
namespace TwinCatQA.Plugin.Core
{
    /// <summary>
    /// 규칙 구현을 위한 추상 기본 클래스
    /// 반복적인 보일러플레이트 코드를 줄여줍니다
    /// </summary>
    public abstract class ValidationRuleBase : IValidationRule
    {
        protected ILogger Logger { get; private set; }
        protected IRuleContext Context { get; private set; }

        // 기본 구현 제공
        public abstract string RuleId { get; }
        public abstract string RuleName { get; }
        public abstract string Description { get; }
        public abstract ConstitutionPrinciple RelatedPrinciple { get; }
        public virtual ViolationSeverity DefaultSeverity => ViolationSeverity.Medium;
        public virtual bool IsEnabled { get; set; } = true;
        public abstract ProgrammingLanguage[] SupportedLanguages { get; }

        // 메타데이터 기본값
        public virtual string Version => "1.0.0";
        public virtual string Author => "Unknown";
        public virtual string[] Tags => Array.Empty<string>();
        public virtual RuleCategory Category => RuleCategory.BestPractices;
        public virtual string CompatibleVersionRange => "*";
        public virtual RuleDependency[] Dependencies => Array.Empty<RuleDependency>();

        // 추상 메서드 (구현 필수)
        public abstract IEnumerable<Violation> Validate(CodeFile file);

        // 가상 메서드 (선택적 재정의)
        public virtual void Configure(Dictionary<string, object> parameters)
        {
            // 기본 구현: 파라미터 무시
        }

        public virtual async Task InitializeAsync(IRuleContext context)
        {
            Context = context;
            Logger = context.Logger;
            await Task.CompletedTask;
        }

        public virtual async Task DisposeAsync()
        {
            await Task.CompletedTask;
        }

        public virtual string GetConfigurationSchema()
        {
            // JSON Schema 기본 템플릿
            return @"{
                ""type"": ""object"",
                ""properties"": {},
                ""additionalProperties"": false
            }";
        }

        public virtual ValidationResult SelfValidate()
        {
            var errors = new List<string>();

            // 기본 검증
            if (string.IsNullOrWhiteSpace(RuleId))
                errors.Add("RuleId는 필수입니다.");

            if (string.IsNullOrWhiteSpace(RuleName))
                errors.Add("RuleName은 필수입니다.");

            if (SupportedLanguages == null || !SupportedLanguages.Any())
                errors.Add("최소 하나의 지원 언어가 필요합니다.");

            return new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        // 유틸리티 메서드
        protected Violation CreateViolation(
            string message,
            CodeFile file,
            int lineNumber,
            ViolationSeverity? severity = null)
        {
            return new Violation
            {
                RuleId = RuleId,
                RuleName = RuleName,
                Message = message,
                FilePath = file.Path,
                LineNumber = lineNumber,
                Severity = severity ?? DefaultSeverity,
                Principle = RelatedPrinciple
            };
        }
    }
}
```

### 3.3 사용자 정의 규칙 예제

```csharp
namespace CustomRules
{
    /// <summary>
    /// 사용자 정의 규칙 예제: 변수명에 Hungarian Notation 금지
    /// </summary>
    public class NoHungarianNotationRule : ValidationRuleBase
    {
        private readonly Regex _hungarianPattern = new Regex(
            @"^(b|i|n|s|str|arr|p|ptr)[A-Z]",
            RegexOptions.Compiled);

        // 필수 속성
        public override string RuleId => "CUSTOM-001";
        public override string RuleName => "Hungarian Notation 금지";
        public override string Description =>
            "변수명에 헝가리안 표기법 사용을 금지합니다 (현대적 IDE는 타입 추론 지원)";

        public override ConstitutionPrinciple RelatedPrinciple =>
            ConstitutionPrinciple.CodeReadability;

        public override ProgrammingLanguage[] SupportedLanguages =>
            new[] { ProgrammingLanguage.StructuredText };

        // 메타데이터
        public override string Version => "1.0.0";
        public override string Author => "우리 회사";
        public override string[] Tags => new[] { "naming", "convention", "readability" };
        public override RuleCategory Category => RuleCategory.Readability;

        // 설정 가능 파라미터
        private string[] _allowedPrefixes = Array.Empty<string>();

        public override void Configure(Dictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("AllowedPrefixes", out var prefixes))
            {
                _allowedPrefixes = ((IEnumerable<object>)prefixes)
                    .Select(p => p.ToString())
                    .ToArray();
            }
        }

        public override string GetConfigurationSchema()
        {
            return @"{
                ""type"": ""object"",
                ""properties"": {
                    ""AllowedPrefixes"": {
                        ""type"": ""array"",
                        ""items"": { ""type"": ""string"" },
                        ""description"": ""허용할 접두사 목록""
                    }
                }
            }";
        }

        // 검증 로직
        public override IEnumerable<Violation> Validate(CodeFile file)
        {
            var violations = new List<Violation>();

            foreach (var variable in file.AST.Variables)
            {
                if (_hungarianPattern.IsMatch(variable.Name))
                {
                    // 허용 목록 체크
                    var prefix = variable.Name.Substring(0, 1).ToLower();
                    if (_allowedPrefixes.Contains(prefix))
                        continue;

                    violations.Add(CreateViolation(
                        $"변수 '{variable.Name}'에 Hungarian Notation 사용됨. " +
                        $"'{SuggestBetterName(variable.Name)}' 사용을 권장합니다.",
                        file,
                        variable.LineNumber,
                        ViolationSeverity.Low
                    ));
                }
            }

            return violations;
        }

        private string SuggestBetterName(string hungarianName)
        {
            // 접두사 제거 후 첫 글자 소문자로
            var withoutPrefix = _hungarianPattern.Replace(hungarianName, "");
            return char.ToLower(withoutPrefix[0]) + withoutPrefix.Substring(1);
        }
    }
}
```

---

## 4. 설정 파일 스키마

### 4.1 YAML 설정 파일 형식

```yaml
# .twincat-qa.yml
# TwinCatQA 통합 설정 파일

# ===== 전역 설정 =====
global:
  defaultMode: Full  # Full | Incremental | Quick
  enableParallelProcessing: true
  maxDegreeOfParallelism: 4
  timeoutSeconds: 300
  logLevel: Info  # Debug | Info | Warning | Error

# ===== 플러그인 설정 =====
plugins:
  # 플러그인 소스 목록
  sources:
    # 로컬 어셈블리
    - type: Assembly
      path: ./plugins/CompanyRules.dll
      enabled: true

    # 로컬 소스 코드
    - type: SourceCode
      path: ./custom-rules/NoHungarianNotation.cs
      enabled: true

    # NuGet 패키지
    - type: NuGetPackage
      packageId: TwinCatQA.Rules.Industrial
      version: 2.1.0
      enabled: true

    # 원격 API
    - type: RemoteApi
      endpoint: https://api.company.com/twincat-qa/rules
      apiKey: ${QA_API_KEY}  # 환경 변수 참조
      enabled: false

  # 커스텀 규칙 디렉토리 (자동 스캔)
  customRulesPath: ./custom-rules

  # 플러그인 자동 업데이트
  autoUpdate:
    enabled: true
    schedule: daily  # daily | weekly | monthly
    allowPrerelease: false

# ===== 규칙 설정 =====
rules:
  # 모든 규칙 활성화 여부
  enableAllRules: true

  # 개별 규칙 설정
  configurations:
    # 내장 규칙
    FR-1-COMPLEXITY:
      enabled: true
      severity: High
      parameters:
        maxComplexity: 10
        warnThreshold: 7
      excludePatterns:
        - "**/Legacy/**"
        - "**/ThirdParty/**"

    FR-2-NAMING:
      enabled: true
      severity: Medium
      parameters:
        enforceCase: PascalCase  # PascalCase | camelCase
        allowUnderscore: false
        maxLength: 50

    # 커스텀 규칙
    CUSTOM-001:
      enabled: true
      severity: Low
      parameters:
        allowedPrefixes:
          - "fb"   # Function Block
          - "st"   # State
      excludePatterns:
        - "**/Generated/**"

    # 외부 플러그인 규칙
    IND-SAFETY-001:
      enabled: true
      severity: Critical
      parameters:
        safetyStandard: IEC61508  # IEC61508 | ISO13849
        silLevel: 2

# ===== 보고서 설정 =====
reports:
  generateHtml: true
  generatePdf: false
  generateJson: true
  outputPath: .twincat-qa/reports
  includeSourceCode: true
  fileNameTemplate: "qa-report-{timestamp}"
  keepReportsCount: 10

  # 보고서 템플릿
  template:
    type: Default  # Default | Minimal | Detailed | Custom
    customPath: ./templates/company-report.html

  # 차트 설정
  charts:
    enabled: true
    types:
      - ViolationsByRule
      - ViolationsBySeverity
      - TrendOverTime

# ===== Git 통합 설정 =====
git:
  enablePreCommitHook: true
  blockOnCriticalViolations: true
  blockOnHighViolations: false
  incrementalMode: true
  hooksPath: ""  # 자동 감지

  # 커밋 메시지 규칙
  commitMessage:
    addViolationCount: true
    template: "QA: {violations}개 위반 해결"

# ===== 프로필 =====
profiles:
  # 개발 프로필
  development:
    global:
      defaultMode: Incremental
      logLevel: Debug
    rules:
      enableAllRules: true

  # CI/CD 프로필
  ci:
    global:
      defaultMode: Full
      enableParallelProcessing: true
      maxDegreeOfParallelism: 8
    rules:
      configurations:
        FR-1-COMPLEXITY:
          severity: Critical
    git:
      blockOnCriticalViolations: true
      blockOnHighViolations: true

  # 빠른 검증 프로필
  quick:
    global:
      defaultMode: Quick
      timeoutSeconds: 60
    rules:
      enableAllRules: false
      configurations:
        FR-1-COMPLEXITY:
          enabled: true
        FR-2-NAMING:
          enabled: true

# ===== 제외 패턴 =====
exclude:
  files:
    - "**/obj/**"
    - "**/bin/**"
    - "**/*.generated.st"
    - "**/Temp/**"

  directories:
    - ".git"
    - ".vs"
    - "node_modules"
    - "_CompileInfo"

# ===== 알림 설정 =====
notifications:
  # Slack 통합
  slack:
    enabled: false
    webhookUrl: ${SLACK_WEBHOOK_URL}
    channel: "#twincat-qa"
    notifyOn:
      - CriticalViolations
      - BuildFailure

  # Email 알림
  email:
    enabled: false
    smtpServer: smtp.company.com
    recipients:
      - dev-team@company.com
    notifyOn:
      - CriticalViolations
