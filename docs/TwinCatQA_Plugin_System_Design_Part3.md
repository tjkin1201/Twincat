# TwinCatQA 플러그인 시스템 설계 (Part 3)

## 6.2 검증 API 컨트롤러

```csharp
namespace TwinCatQA.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ValidateController : ControllerBase
    {
        private readonly IValidationEngine _engine;
        private readonly IPluginRegistry _registry;
        private readonly IBackgroundJobQueue _jobQueue;
        private readonly ILogger<ValidateController> _logger;

        public ValidateController(
            IValidationEngine engine,
            IPluginRegistry registry,
            IBackgroundJobQueue jobQueue,
            ILogger<ValidateController> logger)
        {
            _engine = engine;
            _registry = registry;
            _jobQueue = jobQueue;
            _logger = logger;
        }

        /// <summary>
        /// 코드 검증 실행
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ValidationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationResponse), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Validate([FromBody] ValidationRequest request)
        {
            if (request == null || request.Files == null || !request.Files.Any())
            {
                return BadRequest(new { message = "검증할 파일이 없습니다." });
            }

            var jobId = Guid.NewGuid().ToString();

            if (request.Async)
            {
                // 비동기 실행: 백그라운드 작업 큐에 추가
                await _jobQueue.QueueBackgroundWorkItemAsync(async token =>
                {
                    await ExecuteValidationAsync(jobId, request, token);
                });

                _logger.LogInformation(
                    "비동기 검증 작업 큐에 추가: {JobId} by {User}",
                    jobId,
                    User.Identity.Name);

                return Accepted(new ValidationResponse
                {
                    JobId = jobId,
                    Status = "Queued"
                });
            }
            else
            {
                // 동기 실행
                var result = await ExecuteValidationAsync(jobId, request, CancellationToken.None);

                return Ok(new ValidationResponse
                {
                    JobId = jobId,
                    Status = "Completed",
                    Result = MapToDto(result)
                });
            }
        }

        /// <summary>
        /// 검증 작업 상태 조회
        /// </summary>
        [HttpGet("{jobId}")]
        [ProducesResponseType(typeof(ValidationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetValidationStatus(string jobId)
        {
            var status = await _jobQueue.GetJobStatusAsync(jobId);
            if (status == null)
            {
                return NotFound(new { message = $"작업을 찾을 수 없습니다: {jobId}" });
            }

            return Ok(new ValidationResponse
            {
                JobId = jobId,
                Status = status.Status,
                Result = status.IsCompleted ? MapToDto(status.Result) : null
            });
        }

        /// <summary>
        /// 검증 결과 조회
        /// </summary>
        [HttpGet("{jobId}/results")]
        [ProducesResponseType(typeof(ValidationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetValidationResults(string jobId)
        {
            var result = await _jobQueue.GetJobResultAsync(jobId);
            if (result == null)
            {
                return NotFound(new { message = $"결과를 찾을 수 없습니다: {jobId}" });
            }

            return Ok(MapToDto(result));
        }

        private async Task<ValidationSession> ExecuteValidationAsync(
            string jobId,
            ValidationRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                await _jobQueue.UpdateJobStatusAsync(jobId, "Running");

                // 코드 파일 변환
                var codeFiles = request.Files.Select(f => new CodeFile
                {
                    Path = f.Path,
                    Content = f.Content,
                    Language = Enum.Parse<ProgrammingLanguage>(f.Language, ignoreCase: true)
                }).ToList();

                // 규칙 선택
                var rules = request.RuleIds != null && request.RuleIds.Any()
                    ? request.RuleIds.Select(id => _registry.GetRule(id)).Where(r => r != null)
                    : _registry.GetAllRules();

                // 규칙 설정 재정의 적용
                if (request.RuleConfigurations != null)
                {
                    foreach (var (ruleId, config) in request.RuleConfigurations)
                    {
                        var rule = _registry.GetRule(ruleId);
                        if (rule != null)
                        {
                            rule.IsEnabled = config.Enabled;
                            if (config.Parameters != null)
                            {
                                rule.Configure(config.Parameters);
                            }
                        }
                    }
                }

                // 검증 실행
                var session = await _engine.ValidateAsync(codeFiles, rules.ToList(), cancellationToken);

                await _jobQueue.UpdateJobStatusAsync(jobId, "Completed", session);

                _logger.LogInformation(
                    "검증 완료: {JobId}, Files: {FileCount}, Violations: {ViolationCount}",
                    jobId,
                    codeFiles.Count,
                    session.TotalViolations);

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "검증 실행 실패: {JobId}", jobId);
                await _jobQueue.UpdateJobStatusAsync(jobId, "Failed");
                throw;
            }
        }

        private ValidationResultDto MapToDto(ValidationSession session)
        {
            return new ValidationResultDto
            {
                SessionId = session.SessionId,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                TotalFiles = session.TotalFiles,
                TotalViolations = session.TotalViolations,
                ViolationsBySeverity = session.ViolationsBySeverity,
                Violations = session.Violations.Select(v => new ViolationDto
                {
                    RuleId = v.RuleId,
                    RuleName = v.RuleName,
                    Message = v.Message,
                    FilePath = v.FilePath,
                    LineNumber = v.LineNumber,
                    ColumnNumber = v.ColumnNumber,
                    Severity = v.Severity.ToString(),
                    Category = v.Category?.ToString(),
                    CodeSnippet = v.CodeSnippet,
                    Recommendation = v.Recommendation
                }).ToList()
            };
        }
    }

    /// <summary>
    /// 백그라운드 작업 큐 인터페이스
    /// </summary>
    public interface IBackgroundJobQueue
    {
        Task QueueBackgroundWorkItemAsync(Func<CancellationToken, Task> workItem);
        Task<JobStatus> GetJobStatusAsync(string jobId);
        Task<ValidationSession> GetJobResultAsync(string jobId);
        Task UpdateJobStatusAsync(string jobId, string status, ValidationSession result = null);
    }

    /// <summary>
    /// 작업 상태
    /// </summary>
    public class JobStatus
    {
        public string JobId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsCompleted => Status == "Completed" || Status == "Failed";
        public ValidationSession Result { get; set; }
    }
}
```

### 6.3 gRPC API 설계

#### 6.3.1 Proto 정의

```protobuf
// twincat_qa.proto
syntax = "proto3";

package twincatqa.v1;

option csharp_namespace = "TwinCatQA.API.Grpc";

// TwinCatQA 검증 서비스
service ValidationService {
  // 코드 검증 (단방향 RPC)
  rpc Validate (ValidateRequest) returns (ValidateResponse);

  // 스트리밍 검증 (서버 스트리밍)
  rpc ValidateStream (ValidateRequest) returns (stream ValidationProgress);

  // 규칙 목록 조회
  rpc ListRules (ListRulesRequest) returns (ListRulesResponse);

  // 규칙 상세 조회
  rpc GetRule (GetRuleRequest) returns (Rule);

  // 검증 작업 상태 조회
  rpc GetJobStatus (GetJobStatusRequest) returns (JobStatus);
}

// 검증 요청
message ValidateRequest {
  repeated CodeFile files = 1;
  repeated string rule_ids = 2;
  map<string, RuleConfiguration> rule_configurations = 3;
  bool async = 4;
}

// 코드 파일
message CodeFile {
  string path = 1;
  string content = 2;
  string language = 3;
}

// 규칙 설정
message RuleConfiguration {
  bool enabled = 1;
  string severity = 2;
  map<string, string> parameters = 3;
  repeated string exclude_patterns = 4;
}

// 검증 응답
message ValidateResponse {
  string job_id = 1;
  string status = 2;
  ValidationResult result = 3;
}

// 검증 결과
message ValidationResult {
  string session_id = 1;
  int64 start_time = 2;
  int64 end_time = 3;
  int32 total_files = 4;
  int32 total_violations = 5;
  map<string, int32> violations_by_severity = 6;
  repeated Violation violations = 7;
}

// 위반 사항
message Violation {
  string rule_id = 1;
  string rule_name = 2;
  string message = 3;
  string file_path = 4;
  int32 line_number = 5;
  int32 column_number = 6;
  string severity = 7;
  string category = 8;
  string code_snippet = 9;
  string recommendation = 10;
}

// 검증 진행 상황 (스트리밍)
message ValidationProgress {
  string job_id = 1;
  string status = 2;
  int32 files_processed = 3;
  int32 total_files = 4;
  int32 violations_found = 5;
  string current_file = 6;
  double progress_percentage = 7;
}

// 규칙 목록 요청
message ListRulesRequest {
  string category = 1;
  string language = 2;
  bool enabled = 3;
}

// 규칙 목록 응답
message ListRulesResponse {
  repeated Rule rules = 1;
}

// 규칙
message Rule {
  string rule_id = 1;
  string rule_name = 2;
  string description = 3;
  string version = 4;
  string author = 5;
  repeated string tags = 6;
  string category = 7;
  string severity = 8;
  bool is_enabled = 9;
  repeated string supported_languages = 10;
  string configuration_schema = 11;
}

// 규칙 조회 요청
message GetRuleRequest {
  string rule_id = 1;
}

// 작업 상태 요청
message GetJobStatusRequest {
  string job_id = 1;
}

// 작업 상태
message JobStatus {
  string job_id = 1;
  string status = 2;
  int64 created_at = 3;
  int64 completed_at = 4;
  ValidationResult result = 5;
}
```

#### 6.3.2 gRPC 서비스 구현

```csharp
namespace TwinCatQA.API.Grpc
{
    /// <summary>
    /// gRPC 검증 서비스 구현
    /// </summary>
    public class ValidationServiceImpl : ValidationService.ValidationServiceBase
    {
        private readonly IValidationEngine _engine;
        private readonly IPluginRegistry _registry;
        private readonly IBackgroundJobQueue _jobQueue;
        private readonly ILogger<ValidationServiceImpl> _logger;

        public ValidationServiceImpl(
            IValidationEngine engine,
            IPluginRegistry registry,
            IBackgroundJobQueue jobQueue,
            ILogger<ValidationServiceImpl> logger)
        {
            _engine = engine;
            _registry = registry;
            _jobQueue = jobQueue;
            _logger = logger;
        }

        /// <summary>
        /// 코드 검증 (단방향 RPC)
        /// </summary>
        public override async Task<ValidateResponse> Validate(
            ValidateRequest request,
            ServerCallContext context)
        {
            var jobId = Guid.NewGuid().ToString();

            if (request.Async)
            {
                // 비동기 실행
                await _jobQueue.QueueBackgroundWorkItemAsync(async token =>
                {
                    await ExecuteValidationAsync(jobId, request, token);
                });

                return new ValidateResponse
                {
                    JobId = jobId,
                    Status = "Queued"
                };
            }
            else
            {
                // 동기 실행
                var session = await ExecuteValidationAsync(
                    jobId,
                    request,
                    context.CancellationToken);

                return new ValidateResponse
                {
                    JobId = jobId,
                    Status = "Completed",
                    Result = MapToProto(session)
                };
            }
        }

        /// <summary>
        /// 스트리밍 검증 (서버 스트리밍)
        /// </summary>
        public override async Task ValidateStream(
            ValidateRequest request,
            IServerStreamWriter<ValidationProgress> responseStream,
            ServerCallContext context)
        {
            var jobId = Guid.NewGuid().ToString();

            // 진행 상황 리포터
            var progressReporter = new Progress<ValidationProgress>(async progress =>
            {
                progress.JobId = jobId;
                await responseStream.WriteAsync(progress);
            });

            try
            {
                await ExecuteValidationWithProgressAsync(
                    jobId,
                    request,
                    progressReporter,
                    context.CancellationToken);

                // 최종 완료 메시지
                await responseStream.WriteAsync(new ValidationProgress
                {
                    JobId = jobId,
                    Status = "Completed",
                    ProgressPercentage = 100.0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "스트리밍 검증 실패: {JobId}", jobId);

                await responseStream.WriteAsync(new ValidationProgress
                {
                    JobId = jobId,
                    Status = "Failed",
                    ProgressPercentage = 0
                });
            }
        }

        /// <summary>
        /// 규칙 목록 조회
        /// </summary>
        public override async Task<ListRulesResponse> ListRules(
            ListRulesRequest request,
            ServerCallContext context)
        {
            var rules = _registry.GetAllRules();

            // 필터링
            if (!string.IsNullOrEmpty(request.Category))
            {
                rules = rules.Where(r =>
                    r.Category.ToString().Equals(request.Category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(request.Language))
            {
                var lang = Enum.Parse<ProgrammingLanguage>(request.Language, ignoreCase: true);
                rules = rules.Where(r => r.SupportedLanguages.Contains(lang));
            }

            if (request.Enabled)
            {
                rules = rules.Where(r => r.IsEnabled);
            }

            var response = new ListRulesResponse();
            response.Rules.AddRange(rules.Select(MapRuleToProto));

            return response;
        }

        /// <summary>
        /// 규칙 상세 조회
        /// </summary>
        public override async Task<Rule> GetRule(
            GetRuleRequest request,
            ServerCallContext context)
        {
            var rule = _registry.GetRule(request.RuleId);
            if (rule == null)
            {
                throw new RpcException(new Status(
                    StatusCode.NotFound,
                    $"규칙을 찾을 수 없습니다: {request.RuleId}"));
            }

            return MapRuleToProto(rule);
        }

        /// <summary>
        /// 검증 작업 상태 조회
        /// </summary>
        public override async Task<JobStatus> GetJobStatus(
            GetJobStatusRequest request,
            ServerCallContext context)
        {
            var status = await _jobQueue.GetJobStatusAsync(request.JobId);
            if (status == null)
            {
                throw new RpcException(new Status(
                    StatusCode.NotFound,
                    $"작업을 찾을 수 없습니다: {request.JobId}"));
            }

            return new JobStatus
            {
                JobId = status.JobId,
                Status = status.Status,
                CreatedAt = status.CreatedAt.ToUnixTimeMilliseconds(),
                CompletedAt = status.CompletedAt?.ToUnixTimeMilliseconds() ?? 0,
                Result = status.Result != null ? MapToProto(status.Result) : null
            };
        }

        private async Task<ValidationSession> ExecuteValidationAsync(
            string jobId,
            ValidateRequest request,
            CancellationToken cancellationToken)
        {
            var codeFiles = request.Files.Select(f => new CodeFile
            {
                Path = f.Path,
                Content = f.Content,
                Language = Enum.Parse<ProgrammingLanguage>(f.Language, ignoreCase: true)
            }).ToList();

            var rules = request.RuleIds.Any()
                ? request.RuleIds.Select(id => _registry.GetRule(id)).Where(r => r != null)
                : _registry.GetAllRules();

            // 규칙 설정 적용
            ApplyRuleConfigurations(request.RuleConfigurations);

            return await _engine.ValidateAsync(codeFiles, rules.ToList(), cancellationToken);
        }

        private async Task ExecuteValidationWithProgressAsync(
            string jobId,
            ValidateRequest request,
            IProgress<ValidationProgress> progressReporter,
            CancellationToken cancellationToken)
        {
            var codeFiles = request.Files.Select(f => new CodeFile
            {
                Path = f.Path,
                Content = f.Content,
                Language = Enum.Parse<ProgrammingLanguage>(f.Language, ignoreCase: true)
            }).ToList();

            var rules = request.RuleIds.Any()
                ? request.RuleIds.Select(id => _registry.GetRule(id)).Where(r => r != null).ToList()
                : _registry.GetAllRules().ToList();

            ApplyRuleConfigurations(request.RuleConfigurations);

            var totalFiles = codeFiles.Count;
            var filesProcessed = 0;
            var violationsFound = 0;

            foreach (var file in codeFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // 진행 상황 보고
                progressReporter.Report(new ValidationProgress
                {
                    Status = "Running",
                    FilesProcessed = filesProcessed,
                    TotalFiles = totalFiles,
                    ViolationsFound = violationsFound,
                    CurrentFile = file.Path,
                    ProgressPercentage = (filesProcessed / (double)totalFiles) * 100
                });

                // 파일 검증
                var fileViolations = await _engine.ValidateFileAsync(file, rules, cancellationToken);
                violationsFound += fileViolations.Count();

                filesProcessed++;
            }
        }

        private void ApplyRuleConfigurations(
            IDictionary<string, RuleConfiguration> configurations)
        {
            foreach (var (ruleId, config) in configurations)
            {
                var rule = _registry.GetRule(ruleId);
                if (rule != null)
                {
                    rule.IsEnabled = config.Enabled;

                    if (config.Parameters.Any())
                    {
                        var parameters = config.Parameters.ToDictionary(
                            kvp => kvp.Key,
                            kvp => (object)kvp.Value);
                        rule.Configure(parameters);
                    }
                }
            }
        }

        private ValidationResult MapToProto(ValidationSession session)
        {
            var result = new ValidationResult
            {
                SessionId = session.SessionId,
                StartTime = session.StartTime.ToUnixTimeMilliseconds(),
                EndTime = session.EndTime.ToUnixTimeMilliseconds(),
                TotalFiles = session.TotalFiles,
                TotalViolations = session.TotalViolations
            };

            result.ViolationsBySeverity.Add(session.ViolationsBySeverity);
            result.Violations.AddRange(session.Violations.Select(v => new Violation
            {
                RuleId = v.RuleId,
                RuleName = v.RuleName,
                Message = v.Message,
                FilePath = v.FilePath,
                LineNumber = v.LineNumber,
                ColumnNumber = v.ColumnNumber,
                Severity = v.Severity.ToString(),
                Category = v.Category?.ToString() ?? "",
                CodeSnippet = v.CodeSnippet ?? "",
                Recommendation = v.Recommendation ?? ""
            }));

            return result;
        }

        private Rule MapRuleToProto(IValidationRule rule)
        {
            var protoRule = new Rule
            {
                RuleId = rule.RuleId,
                RuleName = rule.RuleName,
                Description = rule.Description,
                Version = rule.Version,
                Author = rule.Author,
                Category = rule.Category.ToString(),
                Severity = rule.DefaultSeverity.ToString(),
                IsEnabled = rule.IsEnabled,
                ConfigurationSchema = rule.GetConfigurationSchema()
            };

            protoRule.Tags.AddRange(rule.Tags);
            protoRule.SupportedLanguages.AddRange(
                rule.SupportedLanguages.Select(l => l.ToString()));

            return protoRule;
        }
    }
}
```

### 6.4 원격 API 플러그인 로더

```csharp
namespace TwinCatQA.Plugin.Core.Loaders
{
    /// <summary>
    /// 원격 API를 통해 규칙을 로드하는 로더
    /// </summary>
    public class RemoteApiPluginLoader : IPluginLoader
    {
        private readonly ILogger<RemoteApiPluginLoader> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public PluginType SupportedType => PluginType.RemoteApi;

        public RemoteApiPluginLoader(
            ILogger<RemoteApiPluginLoader> logger,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IEnumerable<IValidationRule>> LoadRulesAsync(PluginSource source)
        {
            var httpClient = _httpClientFactory.CreateClient("TwinCatQA.RemoteApi");

            // 인증 헤더 설정
            if (!string.IsNullOrEmpty(source.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", source.ApiKey);
            }

            // 원격 API에서 규칙 목록 가져오기
            var response = await httpClient.GetAsync($"{source.Endpoint}/rules");
            response.EnsureSuccessStatusCode();

            var ruleDtos = await response.Content.ReadFromJsonAsync<List<RuleDto>>();

            // RuleDto를 원격 프록시 규칙으로 래핑
            var rules = ruleDtos.Select(dto => new RemoteValidationRuleProxy(
                dto,
                source.Endpoint,
                source.ApiKey,
                httpClient,
                _logger
            )).ToList();

            _logger.LogInformation(
                "원격 API에서 {Count}개 규칙 로드: {Endpoint}",
                rules.Count,
                source.Endpoint);

            return rules;
        }
    }

    /// <summary>
    /// 원격 API 규칙 프록시
    /// 실제 검증은 원격 API를 호출하여 수행
    /// </summary>
    public class RemoteValidationRuleProxy : IValidationRule
    {
        private readonly RuleDto _metadata;
        private readonly string _apiEndpoint;
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public RemoteValidationRuleProxy(
            RuleDto metadata,
            string apiEndpoint,
            string apiKey,
            HttpClient httpClient,
            ILogger logger)
        {
            _metadata = metadata;
            _apiEndpoint = apiEndpoint;
            _apiKey = apiKey;
            _httpClient = httpClient;
            _logger = logger;
        }

        // 메타데이터 속성
        public string RuleId => _metadata.RuleId;
        public string RuleName => _metadata.RuleName;
        public string Description => _metadata.Description;
        public ConstitutionPrinciple RelatedPrinciple => ConstitutionPrinciple.CodeQuality;
        public ViolationSeverity DefaultSeverity =>
            Enum.Parse<ViolationSeverity>(_metadata.Severity);
        public bool IsEnabled { get; set; } = true;
        public ProgrammingLanguage[] SupportedLanguages =>
            _metadata.SupportedLanguages.Select(l =>
                Enum.Parse<ProgrammingLanguage>(l)).ToArray();

        public string Version => _metadata.Version;
        public string Author => _metadata.Author;
        public string[] Tags => _metadata.Tags;
        public RuleCategory Category => Enum.Parse<RuleCategory>(_metadata.Category);
        public string CompatibleVersionRange => "*";
        public RuleDependency[] Dependencies => Array.Empty<RuleDependency>();

        /// <summary>
        /// 원격 API를 호출하여 검증 수행
        /// </summary>
        public IEnumerable<Violation> Validate(CodeFile file)
        {
            // 동기 메서드이므로 비동기 호출을 블로킹
            return ValidateAsync(file).GetAwaiter().GetResult();
        }

        private async Task<IEnumerable<Violation>> ValidateAsync(CodeFile file)
        {
            try
            {
                var request = new
                {
                    ruleId = RuleId,
                    file = new
                    {
                        path = file.Path,
                        content = file.Content,
                        language = file.Language.ToString()
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_apiEndpoint}/validate/single",
                    request);

                response.EnsureSuccessStatusCode();

                var violationDtos = await response.Content
                    .ReadFromJsonAsync<List<ViolationDto>>();

                return violationDtos.Select(dto => new Violation
                {
                    RuleId = dto.RuleId,
                    RuleName = dto.RuleName,
                    Message = dto.Message,
                    FilePath = dto.FilePath,
                    LineNumber = dto.LineNumber,
                    ColumnNumber = dto.ColumnNumber,
                    Severity = Enum.Parse<ViolationSeverity>(dto.Severity),
                    CodeSnippet = dto.CodeSnippet,
                    Recommendation = dto.Recommendation
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "원격 API 규칙 실행 실패: {RuleId}, File: {FilePath}",
                    RuleId,
                    file.Path);

                return Enumerable.Empty<Violation>();
            }
        }

        public void Configure(Dictionary<string, object> parameters)
        {
            // 원격 API 규칙은 설정을 로컬에서 관리하지 않음
            // 필요 시 API를 통해 설정 업데이트
        }

        public async Task InitializeAsync(IRuleContext context)
        {
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await Task.CompletedTask;
        }

        public string GetConfigurationSchema()
        {
            return _metadata.ConfigurationSchema;
        }

        public ValidationResult SelfValidate()
        {
            return new ValidationResult { IsValid = true };
        }
    }
}
```

---

## 7. 구현 로드맵

### 7.1 Phase 1: 플러그인 코어 (4주)

**목표**: 플러그인 로딩 및 레지스트리 구현

**작업 항목**:
1. **Week 1-2: 기본 인프라**
   - [ ] `TwinCatQA.Plugin.Core` 프로젝트 생성
   - [ ] `IPluginLoader` 인터페이스 정의
   - [ ] `PluginRegistry` 구현
   - [ ] `PluginLifecycleManager` 구현
   - [ ] 단위 테스트 작성

2. **Week 3: 어셈블리 로더**
   - [ ] `AssemblyPluginLoader` 구현
   - [ ] `PluginLoadContext` (격리 로딩) 구현
   - [ ] 어셈블리 로딩 테스트
   - [ ] 예제 플러그인 DLL 생성

3. **Week 4: 소스 코드 로더**
   - [ ] Roslyn 컴파일러 통합
   - [ ] `SourceCodePluginLoader` 구현
   - [ ] 컴파일 오류 처리
   - [ ] 동적 컴파일 테스트

**산출물**:
- 플러그인 코어 라이브러리 (NuGet 패키지)
- 단위 테스트 스위트
- 개발자 문서

---

### 7.2 Phase 2: 설정 시스템 확장 (3주)

**목표**: YAML/JSON 설정 파일 지원 및 환경 변수 치환

**작업 항목**:
1. **Week 1: 설정 파서**
   - [ ] YAML 파서 통합 (YamlDotNet)
   - [ ] JSON Schema 정의
   - [ ] `ConfigurationService` 확장
   - [ ] 환경 변수 치환 구현

2. **Week 2: 프로필 및 검증**
   - [ ] 프로필 시스템 구현
   - [ ] 설정 검증 로직
   - [ ] 기본 설정 템플릿 생성
   - [ ] VSCode 확장 (설정 자동 완성)

3. **Week 3: 통합 및 테스트**
   - [ ] CLI에 설정 시스템 통합
   - [ ] 설정 파일 마이그레이션 도구
   - [ ] 통합 테스트
   - [ ] 사용자 가이드 작성

**산출물**:
- 확장된 설정 시스템
- 설정 파일 템플릿
- VSCode 확장 (선택적)
- 마이그레이션 가이드

---

### 7.3 Phase 3: NuGet 패키지 지원 (3주)

**목표**: NuGet을 통한 규칙 패키지 배포 및 설치

**작업 항목**:
1. **Week 1: NuGet 통합**
   - [ ] `NuGetPackageManager` 구현
   - [ ] 패키지 다운로드 및 추출
   - [ ] `NuGetPluginLoader` 구현
   - [ ] 로컬 패키지 캐시 관리

2. **Week 2: 패키지 자동 업데이트**
   - [ ] 버전 체크 로직
   - [ ] 자동 업데이트 스케줄러
   - [ ] 롤백 메커니즘
   - [ ] 업데이트 알림

3. **Week 3: 배포 도구**
   - [ ] 규칙 패키지 템플릿 (cookiecutter)
   - [ ] `.nuspec` 생성 도구
   - [ ] CI/CD 파이프라인 예제
   - [ ] Private Feed 설정 가이드

**산출물**:
- NuGet 패키지 로더
- 규칙 패키지 템플릿
- 패키지 작성 가이드
- 예제 규칙 패키지

---

### 7.4 Phase 4: REST API (4주)

**목표**: 웹 API를 통한 원격 규칙 실행

**작업 항목**:
1. **Week 1-2: API 서버**
   - [ ] ASP.NET Core Web API 프로젝트
   - [ ] `RulesController` 구현
   - [ ] `ValidateController` 구현
   - [ ] JWT 인증 구현
   - [ ] Swagger/OpenAPI 문서

2. **Week 3: 백그라운드 작업**
   - [ ] `IBackgroundJobQueue` 구현
   - [ ] 비동기 검증 작업 큐
   - [ ] 작업 상태 추적
   - [ ] Redis/RabbitMQ 통합 (선택적)

3. **Week 4: 클라이언트 SDK**
   - [ ] .NET HTTP 클라이언트
   - [ ] `RemoteApiPluginLoader` 구현
   - [ ] API 통합 테스트
   - [ ] Postman 컬렉션

**산출물**:
- REST API 서버
- 클라이언트 SDK
- API 문서 (Swagger)
- Postman 컬렉션

---

### 7.5 Phase 5: gRPC API (3주)

**목표**: 고성능 gRPC API 지원

**작업 항목**:
1. **Week 1: Proto 정의**
   - [ ] `.proto` 파일 작성
   - [ ] C# 코드 생성
   - [ ] 데이터 모델 매핑

2. **Week 2: gRPC 서버**
   - [ ] `ValidationServiceImpl` 구현
   - [ ] 서버 스트리밍 검증
   - [ ] gRPC 인증 (TLS, JWT)
   - [ ] 성능 테스트

3. **Week 3: gRPC 클라이언트**
   - [ ] .NET gRPC 클라이언트
   - [ ] 스트리밍 진행 상황 처리
   - [ ] 통합 테스트
   - [ ] 벤치마크 비교 (REST vs gRPC)

**산출물**:
- gRPC API 서버
- gRPC 클라이언트 SDK
- 성능 비교 보고서
- gRPC 사용 가이드

---

### 7.6 Phase 6: CI/CD 통합 (2주)

**목표**: GitHub Actions, Azure DevOps 통합

**작업 항목**:
1. **Week 1: GitHub Actions**
   - [ ] GitHub Action 작성
   - [ ] PR 검증 워크플로우
   - [ ] 검증 결과 코멘트
   - [ ] Status Checks 통합

2. **Week 2: Azure DevOps**
   - [ ] Azure Pipelines 태스크
   - [ ] Build Validation
   - [ ] 보고서 아티팩트 업로드
   - [ ] 게이트 정책 설정

**산출물**:
- GitHub Action
- Azure DevOps 확장
- CI/CD 통합 가이드

---

### 7.7 Phase 7: 문서화 및 예제 (2주)

**목표**: 종합 문서 및 예제 작성

**작업 항목**:
1. **Week 1: 개발자 가이드**
   - [ ] 플러그인 개발 튜토리얼
   - [ ] 규칙 작성 가이드
   - [ ] API 사용법
   - [ ] 배포 가이드

2. **Week 2: 예제 및 샘플**
   - [ ] 5개 이상 예제 규칙
   - [ ] 조직별 규칙 패키지 샘플
   - [ ] API 통합 예제 (Python, TypeScript)
   - [ ] VSCode 확장 예제

**산출물**:
- 개발자 문서 사이트
- 예제 리포지토리
- 비디오 튜토리얼 (선택적)

---

## 타임라인 요약

```
Phase 1: 플러그인 코어                [1-4주]   ████
Phase 2: 설정 시스템 확장             [5-7주]   ███
Phase 3: NuGet 패키지 지원            [8-10주]  ███
Phase 4: REST API                     [11-14주] ████
Phase 5: gRPC API                     [15-17주] ███
Phase 6: CI/CD 통합                   [18-19주] ██
Phase 7: 문서화 및 예제               [20-21주] ██

총 소요 시간: 약 21주 (5.25개월)
```

---

## 8. 성공 지표 (KPI)

### 8.1 기술적 지표

- **플러그인 로딩 성능**: < 500ms (10개 규칙 기준)
- **API 응답 시간**: < 1초 (단일 파일 검증)
- **동시 요청 처리**: 100 RPS 이상
- **패키지 설치 시간**: < 10초 (평균 규칙 패키지)

### 8.2 사용성 지표

- **사용자 정의 규칙 작성 시간**: < 30분 (간단한 규칙)
- **설정 파일 작성 시간**: < 10분
- **플러그인 배포 시간**: < 1시간 (처음 배포 기준)

### 8.3 품질 지표

- **테스트 커버리지**: > 80%
- **문서 완성도**: 모든 공개 API 문서화
- **예제 코드**: 모든 주요 기능에 대한 예제 제공

---

## 9. 리스크 및 완화 전략

### 리스크 1: 플러그인 보안
- **리스크**: 악의적인 플러그인이 시스템을 손상시킬 수 있음
- **완화**:
  - 플러그인 샌드박싱 (AssemblyLoadContext 격리)
  - 디지털 서명 검증
  - 권한 시스템 (읽기 전용 접근)
  - 허용 목록 기반 API 호출

### 리스크 2: 버전 호환성
- **리스크**: 플러그인과 코어 버전 불일치
- **완화**:
  - Semantic Versioning 강제
  - 호환성 매트릭스 제공
  - 자동 호환성 체크
  - 레거시 API 지원 레이어

### 리스크 3: 성능 저하
- **리스크**: 너무 많은 플러그인으로 인한 성능 저하
- **완화**:
  - Lazy Loading (필요 시에만 로드)
  - 규칙 캐싱
  - 병렬 실행
  - 성능 프로파일링 도구 제공

### 리스크 4: 복잡성 증가
- **리스크**: 플러그인 시스템이 너무 복잡해짐
- **완화**:
  - 간단한 기본 템플릿 제공
  - 단계별 마이그레이션 가이드
  - CLI 마법사 (yeoman-generator 스타일)
  - 커뮤니티 지원 채널

---

## 10. 결론

본 설계는 TwinCatQA를 확장 가능하고 유연한 플러그인 시스템으로 발전시키기 위한 종합 계획입니다.

**핵심 성과**:
1. **코드 재컴파일 없이** 새 규칙 추가 가능
2. **NuGet 패키지**를 통한 규칙 배포
3. **REST/gRPC API**를 통한 원격 실행
4. **YAML/JSON** 기반 유연한 설정
5. **조직별 규칙 패키지** 관리

**다음 단계**:
- [ ] 이해관계자 검토 및 승인
- [ ] Phase 1 작업 시작
- [ ] 프로토타입 개발 (2주)
- [ ] 커뮤니티 피드백 수집
