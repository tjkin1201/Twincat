using System.Collections.Concurrent;
using TwinCatQA.Application.Services;
using TwinCatQA.Domain.Contracts;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;

namespace TwinCatQA.VsExtension.Services;

/// <summary>
/// 코드 분석 서비스 구현
/// QARuleEngine과 ValidationEngine을 통합하여 TcPOU 파일 분석 제공
/// </summary>
public class AnalysisService : IAnalysisService
{
    private readonly IValidationEngine _validationEngine;
    private readonly QARuleEngine _qaRuleEngine;
    private readonly IParserService _parserService;
    private readonly AnalysisResultCache _cache;
    private readonly SemaphoreSlim _semaphore;

    /// <summary>
    /// 분석 서비스 생성자
    /// </summary>
    public AnalysisService(
        IValidationEngine validationEngine,
        QARuleEngine qaRuleEngine,
        IParserService parserService,
        AnalysisResultCache cache)
    {
        _validationEngine = validationEngine ?? throw new ArgumentNullException(nameof(validationEngine));
        _qaRuleEngine = qaRuleEngine ?? throw new ArgumentNullException(nameof(qaRuleEngine));
        _parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));

        // 동시 분석 제한 (CPU 코어 수만큼)
        _semaphore = new SemaphoreSlim(Environment.ProcessorCount);
    }

    #region 문서 분석

    /// <summary>
    /// 단일 문서 전체 분석
    /// </summary>
    public async Task<List<Violation>> AnalyzeDocumentAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("파일 경로가 유효하지 않습니다.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"파일을 찾을 수 없습니다: {filePath}");

        // 캐시 확인
        var cached = _cache.Get(filePath);
        if (cached != null)
            return cached;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // 파일 내용 읽기
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);

            // 분석 수행
            var violations = await AnalyzeContentInternalAsync(
                filePath,
                content,
                cancellationToken);

            // 캐시 저장
            _cache.Set(filePath, violations);

            return violations;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// 문서 내용으로 직접 분석
    /// </summary>
    public async Task<List<Violation>> AnalyzeContentAsync(
        string filePath,
        string content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("파일 경로가 유효하지 않습니다.", nameof(filePath));

        if (string.IsNullOrEmpty(content))
            return new List<Violation>();

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await AnalyzeContentInternalAsync(filePath, content, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region 증분 분석

    /// <summary>
    /// 변경된 라인만 증분 분석
    /// </summary>
    public async Task<List<Violation>> AnalyzeIncrementalAsync(
        string filePath,
        IEnumerable<int> changedLines,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("파일 경로가 유효하지 않습니다.", nameof(filePath));

        var lineNumbers = changedLines.ToHashSet();
        if (lineNumbers.Count == 0)
            return new List<Violation>();

        // 전체 분석 수행
        var allViolations = await AnalyzeDocumentAsync(filePath, cancellationToken);

        // 변경된 라인과 관련된 위반 사항만 필터링
        // (변경된 라인 ±5줄 범위 포함)
        return allViolations
            .Where(v => lineNumbers.Any(line => Math.Abs(v.Line - line) <= 5))
            .ToList();
    }

    /// <summary>
    /// 특정 텍스트 범위만 분석
    /// </summary>
    public async Task<List<Violation>> AnalyzeRangeAsync(
        string filePath,
        int startLine,
        int endLine,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("파일 경로가 유효하지 않습니다.", nameof(filePath));

        if (startLine < 1 || endLine < startLine)
            throw new ArgumentException("라인 범위가 유효하지 않습니다.");

        // 전체 분석 수행
        var allViolations = await AnalyzeDocumentAsync(filePath, cancellationToken);

        // 지정된 범위의 위반 사항만 필터링
        return allViolations
            .Where(v => v.Line >= startLine && v.Line <= endLine)
            .ToList();
    }

    #endregion

    #region 프로젝트 분석

    /// <summary>
    /// 전체 프로젝트 분석 (병렬 처리)
    /// </summary>
    public async Task<Dictionary<string, List<Violation>>> AnalyzeProjectAsync(
        string projectPath,
        IProgress<AnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("프로젝트 경로가 유효하지 않습니다.", nameof(projectPath));

        if (!Directory.Exists(projectPath))
            throw new DirectoryNotFoundException($"프로젝트 폴더를 찾을 수 없습니다: {projectPath}");

        // TcPOU 파일 검색
        var files = Directory.GetFiles(projectPath, "*.TcPOU", SearchOption.AllDirectories)
            .Where(f => !f.Contains("_CompileInfo")) // 컴파일 생성 파일 제외
            .ToList();

        var results = new ConcurrentDictionary<string, List<Violation>>();
        var processedCount = 0;
        var totalViolations = 0;

        // 병렬 분석
        await Parallel.ForEachAsync(
            files,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken
            },
            async (file, ct) =>
            {
                try
                {
                    var violations = await AnalyzeDocumentAsync(file, ct);
                    results.TryAdd(file, violations);

                    var current = Interlocked.Increment(ref processedCount);
                    Interlocked.Add(ref totalViolations, violations.Count);

                    // 진행률 리포트
                    progress?.Report(new AnalysisProgress
                    {
                        TotalFiles = files.Count,
                        ProcessedFiles = current,
                        CurrentFile = file,
                        TotalViolations = totalViolations
                    });
                }
                catch (Exception ex)
                {
                    // 개별 파일 실패 시 빈 결과 추가 (전체 분석 중단 방지)
                    results.TryAdd(file, new List<Violation>
                    {
                        new Violation
                        {
                            RuleId = "ANALYSIS-ERROR",
                            RuleName = "분석 오류",
                            Severity = ViolationSeverity.Critical,
                            FilePath = file,
                            Line = 1,
                            Message = $"파일 분석 중 오류 발생: {ex.Message}",
                            RelatedPrinciple = ConstitutionPrinciple.None
                        }
                    });
                }
            });

        return results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    #endregion

    #region QA 이슈 분석

    /// <summary>
    /// QA 규칙 엔진을 사용한 고급 분석
    /// </summary>
    public async Task<List<QAIssue>> AnalyzeWithQARulesAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("파일 경로가 유효하지 않습니다.", nameof(filePath));

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // 파일 파싱
            var codeFile = await _parserService.ParseFileAsync(filePath);

            // TODO: QARuleEngine은 변경사항 비교 기반이므로
            // 단일 파일 분석을 위한 어댑터 로직 필요
            // 현재는 빈 결과 반환

            return new List<QAIssue>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region 캐시 관리

    /// <summary>
    /// 분석 결과 캐시 무효화
    /// </summary>
    public void InvalidateCache(string? filePath = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _cache.Clear();
        }
        else
        {
            _cache.Remove(filePath);
        }
    }

    /// <summary>
    /// 캐시된 결과 가져오기
    /// </summary>
    public List<Violation>? GetCachedResult(string filePath)
    {
        return _cache.Get(filePath);
    }

    #endregion

    #region 내부 구현

    /// <summary>
    /// 내부 분석 로직
    /// </summary>
    private async Task<List<Violation>> AnalyzeContentInternalAsync(
        string filePath,
        string content,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. 파일 파싱
            var codeFile = await _parserService.ParseContentAsync(filePath, content);

            // 2. ValidationEngine으로 검증
            var violations = await _validationEngine.ValidateFileAsync(codeFile);

            return violations.ToList();
        }
        catch (Exception ex)
        {
            // 파싱/검증 실패 시 오류 위반 사항 반환
            return new List<Violation>
            {
                new Violation
                {
                    RuleId = "PARSE-ERROR",
                    RuleName = "파싱 오류",
                    Severity = ViolationSeverity.Critical,
                    FilePath = filePath,
                    Line = 1,
                    Column = 1,
                    Message = $"파일 파싱/검증 중 오류 발생: {ex.Message}",
                    RelatedPrinciple = ConstitutionPrinciple.None,
                    CodeSnippet = content.Split('\n').Take(5).Aggregate((a, b) => a + "\n" + b)
                }
            };
        }
    }

    #endregion
}
