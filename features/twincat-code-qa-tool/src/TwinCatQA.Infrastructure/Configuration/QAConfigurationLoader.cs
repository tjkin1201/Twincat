using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TwinCatQA.Infrastructure.Configuration;

/// <summary>
/// QA 설정 파일(.twincat-qa.json)을 로드하고 관리하는 클래스
/// </summary>
public class QAConfigurationLoader
{
    private readonly ILogger<QAConfigurationLoader> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private QAConfiguration? _cachedConfiguration;
    private string? _cachedConfigPath;

    /// <summary>
    /// 기본 설정 파일 이름
    /// </summary>
    public const string DefaultConfigFileName = ".twincat-qa.json";

    public QAConfigurationLoader(ILogger<QAConfigurationLoader> logger)
    {
        _logger = logger;

        // JSON 직렬화 옵션 설정
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true
        };
    }

    /// <summary>
    /// 지정된 디렉토리에서 설정 파일을 로드
    /// </summary>
    /// <param name="projectRootPath">프로젝트 루트 디렉토리 경로</param>
    /// <returns>로드된 QA 설정 (파일이 없으면 기본 설정 반환)</returns>
    public async Task<QAConfiguration> LoadConfigurationAsync(string projectRootPath)
    {
        if (string.IsNullOrWhiteSpace(projectRootPath))
        {
            _logger.LogWarning("프로젝트 루트 경로가 지정되지 않아 기본 설정을 사용합니다");
            return GetDefaultConfiguration();
        }

        var configPath = FindConfigurationFile(projectRootPath);

        // 캐시된 설정이 있고 경로가 동일하면 캐시 반환
        if (_cachedConfiguration != null && _cachedConfigPath == configPath)
        {
            _logger.LogDebug("캐시된 설정 파일을 사용합니다: {ConfigPath}", configPath);
            return _cachedConfiguration;
        }

        if (string.IsNullOrEmpty(configPath))
        {
            _logger.LogInformation("설정 파일을 찾을 수 없어 기본 설정을 사용합니다");
            var defaultConfig = GetDefaultConfiguration();
            _cachedConfiguration = defaultConfig;
            _cachedConfigPath = null;
            return defaultConfig;
        }

        try
        {
            _logger.LogInformation("설정 파일을 로드합니다: {ConfigPath}", configPath);

            var jsonContent = await File.ReadAllTextAsync(configPath);
            var configuration = JsonSerializer.Deserialize<QAConfiguration>(jsonContent, _jsonOptions);

            if (configuration == null)
            {
                _logger.LogWarning("설정 파일 파싱에 실패하여 기본 설정을 사용합니다");
                return GetDefaultConfiguration();
            }

            // 기본값과 병합
            var mergedConfig = MergeWithDefaults(configuration);

            // 설정 유효성 검증
            ValidateConfiguration(mergedConfig);

            // 캐시 저장
            _cachedConfiguration = mergedConfig;
            _cachedConfigPath = configPath;

            _logger.LogInformation("설정 파일 로드 완료: {ProjectName} (버전: {Version})",
                mergedConfig.ProjectName ?? "Unknown",
                mergedConfig.Version);

            return mergedConfig;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "설정 파일 JSON 파싱 오류: {ConfigPath}", configPath);
            throw new InvalidOperationException($"설정 파일 파싱 실패: {configPath}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "설정 파일 로드 중 오류 발생: {ConfigPath}", configPath);
            throw;
        }
    }

    /// <summary>
    /// 프로젝트 루트에서 설정 파일을 찾음 (상위 디렉토리까지 검색)
    /// </summary>
    /// <param name="startPath">검색 시작 경로</param>
    /// <returns>설정 파일 전체 경로 (찾지 못하면 null)</returns>
    private string? FindConfigurationFile(string startPath)
    {
        var currentDir = new DirectoryInfo(startPath);

        while (currentDir != null)
        {
            var configPath = Path.Combine(currentDir.FullName, DefaultConfigFileName);

            if (File.Exists(configPath))
            {
                _logger.LogDebug("설정 파일 발견: {ConfigPath}", configPath);
                return configPath;
            }

            currentDir = currentDir.Parent;

            // 루트 드라이브까지 도달하면 중단
            if (currentDir == null || currentDir.Parent == null)
            {
                break;
            }
        }

        _logger.LogDebug("설정 파일을 찾을 수 없습니다: {StartPath}", startPath);
        return null;
    }

    /// <summary>
    /// 기본 설정을 반환
    /// </summary>
    public static QAConfiguration GetDefaultConfiguration()
    {
        return new QAConfiguration
        {
            Version = "2.0",
            ProjectName = "Default",
            GlobalExclusions = new GlobalExclusions
            {
                Files = new List<string>
                {
                    "**/bin/**",
                    "**/obj/**",
                    "**/.vs/**"
                },
                Directories = new List<string>
                {
                    "**/bin",
                    "**/obj",
                    "**/.vs"
                },
                Rules = new List<string>()
            },
            RuleOverrides = new Dictionary<string, RuleOverride>(),
            FileOverrides = new Dictionary<string, FileOverride>(),
            InlineSuppressions = new InlineSuppressionConfig
            {
                Enabled = true
            }
        };
    }

    /// <summary>
    /// 로드된 설정을 기본값과 병합
    /// </summary>
    private QAConfiguration MergeWithDefaults(QAConfiguration loaded)
    {
        var defaultConfig = GetDefaultConfiguration();

        // 전역 제외 파일에 기본 패턴 추가 (중복 제거)
        var allExcludedFiles = defaultConfig.GlobalExclusions.Files
            .Concat(loaded.GlobalExclusions.Files)
            .Distinct()
            .ToList();

        var allExcludedDirs = defaultConfig.GlobalExclusions.Directories
            .Concat(loaded.GlobalExclusions.Directories)
            .Distinct()
            .ToList();

        loaded.GlobalExclusions.Files = allExcludedFiles;
        loaded.GlobalExclusions.Directories = allExcludedDirs;

        return loaded;
    }

    /// <summary>
    /// 설정의 유효성을 검증
    /// </summary>
    private void ValidateConfiguration(QAConfiguration config)
    {
        // 버전 검증
        if (string.IsNullOrWhiteSpace(config.Version))
        {
            _logger.LogWarning("설정 파일 버전이 지정되지 않았습니다");
        }
        else if (!config.Version.StartsWith("2."))
        {
            _logger.LogWarning("지원하지 않는 설정 파일 버전: {Version}", config.Version);
        }

        // 심각도 값 검증
        var validSeverities = new[] { "Info", "Warning", "Error", "Critical" };

        foreach (var (ruleId, ruleOverride) in config.RuleOverrides)
        {
            if (ruleOverride.Severity != null &&
                !validSeverities.Contains(ruleOverride.Severity, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("규칙 {RuleId}의 심각도 값이 유효하지 않습니다: {Severity}",
                    ruleId, ruleOverride.Severity);
            }
        }

        foreach (var (pattern, fileOverride) in config.FileOverrides)
        {
            if (fileOverride.MinSeverity != null &&
                !validSeverities.Contains(fileOverride.MinSeverity, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("파일 패턴 {Pattern}의 최소 심각도 값이 유효하지 않습니다: {Severity}",
                    pattern, fileOverride.MinSeverity);
            }
        }

        _logger.LogDebug("설정 유효성 검증 완료");
    }

    /// <summary>
    /// 캐시된 설정을 초기화 (설정 파일 변경 시 사용)
    /// </summary>
    public void ClearCache()
    {
        _cachedConfiguration = null;
        _cachedConfigPath = null;
        _logger.LogDebug("설정 캐시가 초기화되었습니다");
    }

    /// <summary>
    /// 기본 설정 파일을 생성
    /// </summary>
    /// <param name="projectRootPath">프로젝트 루트 디렉토리</param>
    /// <param name="projectName">프로젝트 이름</param>
    public async Task CreateDefaultConfigFileAsync(string projectRootPath, string? projectName = null)
    {
        var configPath = Path.Combine(projectRootPath, DefaultConfigFileName);

        if (File.Exists(configPath))
        {
            _logger.LogWarning("설정 파일이 이미 존재합니다: {ConfigPath}", configPath);
            throw new InvalidOperationException($"설정 파일이 이미 존재합니다: {configPath}");
        }

        var defaultConfig = GetDefaultConfiguration();
        defaultConfig.ProjectName = projectName ?? Path.GetFileName(projectRootPath);

        var jsonContent = JsonSerializer.Serialize(defaultConfig, _jsonOptions);
        await File.WriteAllTextAsync(configPath, jsonContent);

        _logger.LogInformation("기본 설정 파일이 생성되었습니다: {ConfigPath}", configPath);
    }
}
