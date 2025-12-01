namespace TwinCatQA.Infrastructure.Git;

/// <summary>
/// Git 저장소 통합 서비스 인터페이스
/// </summary>
public interface IGitService
{
    // 저장소 관리

    /// <summary>
    /// Git 저장소 초기화
    /// </summary>
    /// <param name="repoPath">저장소 경로</param>
    /// <returns>성공 여부</returns>
    bool InitializeRepository(string repoPath);

    /// <summary>
    /// Git 저장소 여부 확인
    /// </summary>
    /// <param name="path">검사할 경로</param>
    /// <returns>Git 저장소이면 true</returns>
    bool IsGitRepository(string path);

    /// <summary>
    /// 현재 커밋 해시 조회
    /// </summary>
    /// <param name="repoPath">저장소 경로</param>
    /// <returns>HEAD 커밋 해시 (SHA-1), 없으면 null</returns>
    string? GetCurrentCommitHash(string repoPath);

    /// <summary>
    /// 워킹 디렉토리가 깨끗한지 확인
    /// </summary>
    /// <param name="repoPath">저장소 경로</param>
    /// <returns>변경사항이 없으면 true</returns>
    bool IsWorkingDirectoryClean(string repoPath);

    // Diff 분석

    /// <summary>
    /// 변경된 파일 목록 조회
    /// </summary>
    /// <param name="repoPath">저장소 경로</param>
    /// <param name="diffTarget">비교 대상</param>
    /// <returns>변경된 파일 경로 목록</returns>
    IReadOnlyList<string> GetChangedFiles(string repoPath, DiffTarget diffTarget);

    /// <summary>
    /// 특정 파일의 변경된 라인 목록 조회
    /// </summary>
    /// <param name="repoPath">저장소 경로</param>
    /// <param name="filePath">파일 경로 (저장소 루트 기준 상대 경로)</param>
    /// <returns>변경된 라인 목록</returns>
    IReadOnlyList<LineChange> GetChangedLines(string repoPath, string filePath);

    /// <summary>
    /// 두 커밋 간 차이 조회
    /// </summary>
    /// <param name="repoPath">저장소 경로</param>
    /// <param name="fromCommit">비교 시작 커밋 해시</param>
    /// <param name="toCommit">비교 종료 커밋 해시</param>
    /// <returns>변경된 파일 경로 목록</returns>
    IReadOnlyList<string> GetDiffBetweenCommits(string repoPath, string fromCommit, string toCommit);

    // Pre-commit Hook

    /// <summary>
    /// Pre-commit Hook 설치
    /// </summary>
    /// <param name="repoPath">저장소 경로</param>
    /// <param name="blockOnCritical">Critical 위반 시 커밋 차단 여부</param>
    /// <returns>설치 성공 여부</returns>
    bool InstallPreCommitHook(string repoPath, bool blockOnCritical = true);

    /// <summary>
    /// Pre-commit Hook 제거
    /// </summary>
    /// <param name="repoPath">저장소 경로</param>
    /// <returns>제거 성공 여부</returns>
    bool UninstallPreCommitHook(string repoPath);

    /// <summary>
    /// Pre-commit Hook 설치 여부 확인
    /// </summary>
    /// <param name="repoPath">저장소 경로</param>
    /// <returns>설치되어 있으면 true</returns>
    bool IsPreCommitHookInstalled(string repoPath);

    // 컨텍스트 분석

    /// <summary>
    /// 변경 라인의 컨텍스트 범위 결정
    /// </summary>
    /// <param name="file">코드 파일 객체</param>
    /// <param name="changedLine">변경된 라인 번호</param>
    /// <returns>컨텍스트 범위 정보</returns>
    CodeContext DetermineContext(object file, int changedLine);
}
