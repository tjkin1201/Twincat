using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace TwinCatQA.Infrastructure.Git;

/// <summary>
/// LibGit2Sharp를 사용한 Git 저장소 통합 서비스 구현
/// </summary>
public class LibGit2Service : IGitService
{
    private readonly ILogger<LibGit2Service> _logger;
    private readonly DiffParser _diffParser;
    private readonly ContextAnalyzer _contextAnalyzer;

    public LibGit2Service(ILogger<LibGit2Service> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _diffParser = new DiffParser();
        _contextAnalyzer = new ContextAnalyzer();
    }

    #region 저장소 관리

    /// <summary>
    /// Git 저장소 초기화
    /// </summary>
    public bool InitializeRepository(string repoPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(repoPath))
            {
                _logger.LogWarning("저장소 경로가 비어있습니다");
                return false;
            }

            if (IsGitRepository(repoPath))
            {
                _logger.LogInformation("이미 Git 저장소가 존재합니다: {RepoPath}", repoPath);
                return true;
            }

            // 디렉토리가 없으면 생성
            if (!Directory.Exists(repoPath))
            {
                Directory.CreateDirectory(repoPath);
            }

            // Git 저장소 초기화
            Repository.Init(repoPath);
            _logger.LogInformation("Git 저장소 초기화 완료: {RepoPath}", repoPath);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Git 저장소 초기화 실패: {RepoPath}", repoPath);
            return false;
        }
    }

    /// <summary>
    /// Git 저장소 여부 확인
    /// </summary>
    public bool IsGitRepository(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return false;
            }

            // .git 디렉토리 탐색 (상위 디렉토리까지 검색)
            var currentPath = new DirectoryInfo(path);

            while (currentPath != null)
            {
                var gitPath = Path.Combine(currentPath.FullName, ".git");

                if (Directory.Exists(gitPath) || File.Exists(gitPath))
                {
                    return true;
                }

                currentPath = currentPath.Parent;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Git 저장소 확인 중 오류 발생: {Path}", path);
            return false;
        }
    }

    /// <summary>
    /// 현재 커밋 해시 조회
    /// </summary>
    public string? GetCurrentCommitHash(string repoPath)
    {
        try
        {
            if (!IsGitRepository(repoPath))
            {
                _logger.LogWarning("Git 저장소가 아닙니다: {RepoPath}", repoPath);
                return null;
            }

            using var repo = new Repository(FindGitDirectory(repoPath));

            // HEAD 커밋 해시 반환
            var headCommit = repo.Head.Tip;
            if (headCommit == null)
            {
                _logger.LogWarning("HEAD 커밋이 없습니다 (빈 저장소): {RepoPath}", repoPath);
                return null;
            }

            return headCommit.Sha;
        }
        catch (RepositoryNotFoundException ex)
        {
            _logger.LogWarning(ex, "Git 저장소를 찾을 수 없습니다: {RepoPath}", repoPath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "커밋 해시 조회 실패: {RepoPath}", repoPath);
            return null;
        }
    }

    /// <summary>
    /// 워킹 디렉토리가 깨끗한지 확인
    /// </summary>
    public bool IsWorkingDirectoryClean(string repoPath)
    {
        try
        {
            if (!IsGitRepository(repoPath))
            {
                _logger.LogWarning("Git 저장소가 아닙니다: {RepoPath}", repoPath);
                return false;
            }

            using var repo = new Repository(FindGitDirectory(repoPath));

            // 상태 확인: Modified, Added, Removed, Missing 등
            var status = repo.RetrieveStatus();

            return !status.IsDirty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "워킹 디렉토리 상태 확인 실패: {RepoPath}", repoPath);
            return false;
        }
    }

    #endregion

    #region Diff 분석

    /// <summary>
    /// 변경된 파일 목록 조회
    /// </summary>
    public IReadOnlyList<string> GetChangedFiles(string repoPath, DiffTarget diffTarget)
    {
        try
        {
            if (!IsGitRepository(repoPath))
            {
                _logger.LogWarning("Git 저장소가 아닙니다: {RepoPath}", repoPath);
                return Array.Empty<string>();
            }

            using var repo = new Repository(FindGitDirectory(repoPath));

            TreeChanges? changes = diffTarget switch
            {
                DiffTarget.Index => GetIndexDiff(repo),
                DiffTarget.WorkingDirectory => GetWorkingDirectoryDiff(repo),
                DiffTarget.All => GetAllDiff(repo),
                _ => null
            };

            if (changes == null)
            {
                return Array.Empty<string>();
            }

            // 변경된 파일 경로 추출
            var changedFiles = changes
                .Select(c => c.Path)
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct()
                .ToList();

            _logger.LogInformation("변경된 파일 {Count}개 발견 (Target: {Target})",
                changedFiles.Count, diffTarget);

            return changedFiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "변경 파일 조회 실패: {RepoPath}", repoPath);
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// 특정 파일의 변경된 라인 목록 조회
    /// </summary>
    public IReadOnlyList<LineChange> GetChangedLines(string repoPath, string filePath)
    {
        try
        {
            if (!IsGitRepository(repoPath))
            {
                _logger.LogWarning("Git 저장소가 아닙니다: {RepoPath}", repoPath);
                return Array.Empty<LineChange>();
            }

            using var repo = new Repository(FindGitDirectory(repoPath));

            // 워킹 디렉토리 vs HEAD 비교
            var changes = repo.Diff.Compare<TreeChanges>(
                repo.Head.Tip?.Tree,
                DiffTargets.WorkingDirectory | DiffTargets.Index
            );

            // 해당 파일의 Patch 추출
            var patch = repo.Diff.Compare<Patch>(
                repo.Head.Tip?.Tree,
                DiffTargets.WorkingDirectory | DiffTargets.Index,
                new[] { filePath }
            );

            if (patch == null)
            {
                return Array.Empty<LineChange>();
            }

            // Patch를 LineChange 목록으로 변환
            var lineChanges = _diffParser.ParsePatch(patch, filePath);

            _logger.LogInformation("파일 {FilePath}에서 {Count}개 라인 변경 발견",
                filePath, lineChanges.Count);

            return lineChanges;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "변경 라인 조회 실패: {RepoPath}, {FilePath}", repoPath, filePath);
            return Array.Empty<LineChange>();
        }
    }

    /// <summary>
    /// 두 커밋 간 차이 조회
    /// </summary>
    public IReadOnlyList<string> GetDiffBetweenCommits(string repoPath, string fromCommit, string toCommit)
    {
        try
        {
            if (!IsGitRepository(repoPath))
            {
                _logger.LogWarning("Git 저장소가 아닙니다: {RepoPath}", repoPath);
                return Array.Empty<string>();
            }

            using var repo = new Repository(FindGitDirectory(repoPath));

            // 커밋 객체 조회
            var fromCommitObj = repo.Lookup<Commit>(fromCommit);
            var toCommitObj = repo.Lookup<Commit>(toCommit);

            if (fromCommitObj == null || toCommitObj == null)
            {
                _logger.LogWarning("커밋을 찾을 수 없습니다: {From} -> {To}", fromCommit, toCommit);
                return Array.Empty<string>();
            }

            // Tree 간 Diff 비교
            var changes = repo.Diff.Compare<TreeChanges>(
                fromCommitObj.Tree,
                toCommitObj.Tree
            );

            var changedFiles = changes
                .Select(c => c.Path)
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct()
                .ToList();

            _logger.LogInformation("커밋 간 {Count}개 파일 변경: {From} -> {To}",
                changedFiles.Count, fromCommit, toCommit);

            return changedFiles;
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "커밋을 찾을 수 없습니다: {From} -> {To}", fromCommit, toCommit);
            return Array.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "커밋 간 Diff 조회 실패: {RepoPath}", repoPath);
            return Array.Empty<string>();
        }
    }

    #endregion

    #region Diff 헬퍼 메서드

    /// <summary>
    /// Index vs HEAD Diff
    /// </summary>
    private TreeChanges? GetIndexDiff(Repository repo)
    {
        try
        {
            return repo.Diff.Compare<TreeChanges>(
                repo.Head.Tip?.Tree,
                DiffTargets.Index
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Index Diff 조회 실패");
            return null;
        }
    }

    /// <summary>
    /// 워킹 디렉토리 vs Index Diff
    /// </summary>
    private TreeChanges? GetWorkingDirectoryDiff(Repository repo)
    {
        try
        {
            // LibGit2Sharp 0.27.0에서는 DiffTargets를 사용
            return repo.Diff.Compare<TreeChanges>(
                repo.Head?.Tip?.Tree,
                DiffTargets.WorkingDirectory | DiffTargets.Index
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "워킹 디렉토리 Diff 조회 실패");
            return null;
        }
    }

    /// <summary>
    /// 워킹 디렉토리 vs HEAD Diff (모든 변경사항)
    /// </summary>
    private TreeChanges? GetAllDiff(Repository repo)
    {
        try
        {
            return repo.Diff.Compare<TreeChanges>(
                repo.Head.Tip?.Tree,
                DiffTargets.WorkingDirectory | DiffTargets.Index
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "전체 Diff 조회 실패");
            return null;
        }
    }

    #endregion

    #region Pre-commit Hook

    /// <summary>
    /// Pre-commit Hook 설치
    /// </summary>
    public bool InstallPreCommitHook(string repoPath, bool blockOnCritical = true)
    {
        try
        {
            if (!IsGitRepository(repoPath))
            {
                _logger.LogWarning("Git 저장소가 아닙니다: {RepoPath}", repoPath);
                return false;
            }

            var gitDir = FindGitDirectory(repoPath);
            var hooksDir = Path.Combine(gitDir, "hooks");

            // hooks 디렉토리 생성
            if (!Directory.Exists(hooksDir))
            {
                Directory.CreateDirectory(hooksDir);
            }

            // 운영체제별 Hook 파일 설치
            bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            string hookFileName = isWindows ? "pre-commit.bat" : "pre-commit";
            string hookPath = Path.Combine(hooksDir, hookFileName);

            // Hook 스크립트 내용 읽기
            string scriptContent = GetPreCommitHookScript(isWindows, blockOnCritical);

            // Hook 파일 작성
            File.WriteAllText(hookPath, scriptContent);

            // Unix 시스템에서 실행 권한 부여
            if (!isWindows)
            {
                try
                {
                    // chmod +x 실행
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "chmod",
                            Arguments = $"+x \"{hookPath}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.WaitForExit();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "실행 권한 부여 실패 (수동으로 chmod +x 실행 필요): {HookPath}", hookPath);
                }
            }

            _logger.LogInformation("Pre-commit Hook 설치 완료: {HookPath}", hookPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pre-commit Hook 설치 실패: {RepoPath}", repoPath);
            return false;
        }
    }

    /// <summary>
    /// Pre-commit Hook 제거
    /// </summary>
    public bool UninstallPreCommitHook(string repoPath)
    {
        try
        {
            if (!IsGitRepository(repoPath))
            {
                _logger.LogWarning("Git 저장소가 아닙니다: {RepoPath}", repoPath);
                return false;
            }

            var gitDir = FindGitDirectory(repoPath);
            var hooksDir = Path.Combine(gitDir, "hooks");

            // Hook 파일 경로
            var hookPaths = new[]
            {
                Path.Combine(hooksDir, "pre-commit"),
                Path.Combine(hooksDir, "pre-commit.bat")
            };

            bool removed = false;

            foreach (var hookPath in hookPaths)
            {
                if (File.Exists(hookPath))
                {
                    File.Delete(hookPath);
                    _logger.LogInformation("Pre-commit Hook 제거 완료: {HookPath}", hookPath);
                    removed = true;
                }
            }

            if (!removed)
            {
                _logger.LogWarning("제거할 Hook 파일이 없습니다: {RepoPath}", repoPath);
            }

            return removed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pre-commit Hook 제거 실패: {RepoPath}", repoPath);
            return false;
        }
    }

    /// <summary>
    /// Pre-commit Hook 설치 여부 확인
    /// </summary>
    public bool IsPreCommitHookInstalled(string repoPath)
    {
        try
        {
            if (!IsGitRepository(repoPath))
            {
                return false;
            }

            var gitDir = FindGitDirectory(repoPath);
            var hooksDir = Path.Combine(gitDir, "hooks");

            // Hook 파일 존재 여부 확인
            var hookPaths = new[]
            {
                Path.Combine(hooksDir, "pre-commit"),
                Path.Combine(hooksDir, "pre-commit.bat")
            };

            return hookPaths.Any(File.Exists);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Hook 설치 여부 확인 중 오류: {RepoPath}", repoPath);
            return false;
        }
    }

    /// <summary>
    /// Pre-commit Hook 스크립트 내용 가져오기
    /// </summary>
    private string GetPreCommitHookScript(bool isWindows, bool blockOnCritical)
    {
        string templateFileName = isWindows ? "pre-commit.bat" : "pre-commit.sh";
        string templatePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Git",
            "Templates",
            templateFileName
        );

        // 템플릿 파일이 있으면 사용
        if (File.Exists(templatePath))
        {
            return File.ReadAllText(templatePath);
        }

        // 템플릿 파일이 없으면 기본 스크립트 생성
        if (isWindows)
        {
            return GetDefaultWindowsHookScript(blockOnCritical);
        }
        else
        {
            return GetDefaultBashHookScript(blockOnCritical);
        }
    }

    /// <summary>
    /// 기본 Bash Hook 스크립트
    /// </summary>
    private string GetDefaultBashHookScript(bool blockOnCritical)
    {
        string failOnCritical = blockOnCritical ? "--fail-on-critical" : "";

        return $@"#!/bin/bash
# TwinCAT 코드 품질 검증 Pre-commit Hook
# 자동 생성됨: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

echo ""TwinCAT 코드 품질 검증 중...""

# CLI 도구 실행 (증분 검증 모드)
dotnet twincat-qa validate --mode Incremental {failOnCritical}

if [ $? -ne 0 ]; then
    echo ""❌ 품질 검증 실패: Critical 위반이 발견되었습니다.""
    echo ""   커밋을 차단합니다. 위반 사항을 수정한 후 다시 시도하세요.""
    exit 1
fi

echo ""✅ 품질 검증 통과""
exit 0
";
    }

    /// <summary>
    /// 기본 Windows Hook 스크립트
    /// </summary>
    private string GetDefaultWindowsHookScript(bool blockOnCritical)
    {
        string failOnCritical = blockOnCritical ? "--fail-on-critical" : "";

        return $@"@echo off
REM TwinCAT 코드 품질 검증 Pre-commit Hook
REM 자동 생성됨: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

echo TwinCAT 코드 품질 검증 중...

dotnet twincat-qa validate --mode Incremental {failOnCritical}

if %ERRORLEVEL% NEQ 0 (
    echo ❌ 품질 검증 실패: Critical 위반이 발견되었습니다.
    echo    커밋을 차단합니다. 위반 사항을 수정한 후 다시 시도하세요.
    exit /b 1
)

echo ✅ 품질 검증 통과
exit /b 0
";
    }

    #endregion

    #region 컨텍스트 분석

    /// <summary>
    /// 변경 라인의 컨텍스트 범위 결정
    /// </summary>
    public CodeContext DetermineContext(object file, int changedLine)
    {
        try
        {
            // ContextAnalyzer를 사용하여 컨텍스트 결정
            // file 객체에서 AST 추출 (동적 타입)
            dynamic dynamicFile = file;
            dynamic? ast = dynamicFile?.SyntaxTree;

            return _contextAnalyzer.DetermineContext(dynamicFile, ast, changedLine);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "컨텍스트 결정 실패, 기본 범위 반환: Line {Line}", changedLine);

            // 오류 발생 시 기본 주변 범위 반환
            var surroundingLines = _contextAnalyzer.GetSurroundingLines(file, changedLine);
            return new CodeContext
            {
                StartLine = surroundingLines.startLine,
                EndLine = surroundingLines.endLine,
                ContextType = "Surrounding",
                ContextName = $"Line {changedLine} ±10"
            };
        }
    }

    #endregion

    #region 헬퍼 메서드

    /// <summary>
    /// .git 디렉토리 경로 찾기
    /// </summary>
    private string FindGitDirectory(string path)
    {
        var currentPath = new DirectoryInfo(path);

        while (currentPath != null)
        {
            var gitPath = Path.Combine(currentPath.FullName, ".git");

            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return currentPath.FullName;
            }

            currentPath = currentPath.Parent;
        }

        // 찾지 못하면 원래 경로 반환
        return path;
    }

    #endregion
}
