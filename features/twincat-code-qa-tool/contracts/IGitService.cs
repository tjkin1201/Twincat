using System.Collections.Generic;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Domain.Contracts
{
    /// <summary>
    /// Git 서비스 인터페이스
    ///
    /// LibGit2Sharp를 사용하여 Git 작업을 수행합니다.
    /// Diff 분석, Hook 관리, 변경 사항 추출을 지원합니다.
    /// </summary>
    public interface IGitService
    {
        /// <summary>
        /// Git 저장소를 초기화합니다.
        /// </summary>
        /// <param name="repoPath">저장소 경로</param>
        void InitializeRepository(string repoPath);

        /// <summary>
        /// 저장소가 Git으로 관리되는지 확인합니다.
        /// </summary>
        /// <param name="path">경로</param>
        /// <returns>Git 저장소 여부</returns>
        bool IsGitRepository(string path);

        /// <summary>
        /// 변경된 파일 목록을 가져옵니다.
        /// </summary>
        /// <param name="repoPath">저장소 경로</param>
        /// <param name="diffTarget">비교 대상 (스테이징, 워킹 디렉토리 등)</param>
        /// <returns>변경된 파일 경로 목록</returns>
        List<string> GetChangedFiles(string repoPath, DiffTarget diffTarget = DiffTarget.Index);

        /// <summary>
        /// 특정 파일의 변경된 라인을 가져옵니다.
        /// </summary>
        /// <param name="repoPath">저장소 경로</param>
        /// <param name="filePath">파일 경로 (상대)</param>
        /// <returns>변경된 라인 목록</returns>
        List<LineChange> GetChangedLines(string repoPath, string filePath);

        /// <summary>
        /// Pre-commit hook을 설치합니다.
        /// </summary>
        /// <param name="repoPath">저장소 경로</param>
        /// <param name="blockOnCritical">Critical 위반 시 커밋 차단 여부</param>
        void InstallPreCommitHook(string repoPath, bool blockOnCritical = true);

        /// <summary>
        /// Pre-commit hook을 제거합니다.
        /// </summary>
        /// <param name="repoPath">저장소 경로</param>
        void UninstallPreCommitHook(string repoPath);

        /// <summary>
        /// Pre-commit hook이 설치되어 있는지 확인합니다.
        /// </summary>
        /// <param name="repoPath">저장소 경로</param>
        /// <returns>설치 여부</returns>
        bool IsPreCommitHookInstalled(string repoPath);

        /// <summary>
        /// 현재 커밋 해시를 가져옵니다.
        /// </summary>
        /// <param name="repoPath">저장소 경로</param>
        /// <returns>커밋 해시 (SHA-1)</returns>
        string GetCurrentCommitHash(string repoPath);

        /// <summary>
        /// 작업 디렉토리가 깨끗한지 확인합니다 (변경 사항 없음).
        /// </summary>
        /// <param name="repoPath">저장소 경로</param>
        /// <returns>깨끗한 상태 여부</returns>
        bool IsWorkingDirectoryClean(string repoPath);

        /// <summary>
        /// 두 커밋 간의 차이를 가져옵니다.
        /// </summary>
        /// <param name="repoPath">저장소 경로</param>
        /// <param name="fromCommit">시작 커밋</param>
        /// <param name="toCommit">종료 커밋 (null이면 HEAD)</param>
        /// <returns>변경된 파일 목록</returns>
        List<string> GetDiffBetweenCommits(string repoPath, string fromCommit, string toCommit = null);

        /// <summary>
        /// 변경 라인에 대한 컨텍스트 범위를 결정합니다.
        /// </summary>
        /// <param name="file">코드 파일</param>
        /// <param name="changedLine">변경된 라인 번호</param>
        /// <returns>컨텍스트 범위</returns>
        CodeContext DetermineContext(CodeFile file, int changedLine);
    }

    /// <summary>
    /// Diff 비교 대상
    /// </summary>
    public enum DiffTarget
    {
        Index,              // 스테이징 영역 vs HEAD
        WorkingDirectory,   // 워킹 디렉토리 vs 스테이징
        All                 // 워킹 디렉토리 vs HEAD
    }

    /// <summary>
    /// 라인 변경 정보
    /// </summary>
    public class LineChange
    {
        public int LineNumber { get; set; }
        public ChangeType Type { get; set; }
        public string Content { get; set; }
    }

    /// <summary>
    /// 변경 유형
    /// </summary>
    public enum ChangeType
    {
        Added,
        Modified,
        Deleted
    }

    /// <summary>
    /// 코드 컨텍스트 (증분 검증용)
    /// </summary>
    public class CodeContext
    {
        public ContextType Type { get; set; }
        public (int Start, int End) LineRange { get; set; }
        public string ElementName { get; set; }  // FB 이름, CASE 블록 등
    }

    /// <summary>
    /// 컨텍스트 유형
    /// </summary>
    public enum ContextType
    {
        FunctionBlock,      // Function Block 전체
        CaseBlock,          // CASE 문 블록
        ForLoop,            // FOR 루프
        IfBlock,            // IF 블록
        SurroundingLines    // 주변 라인 (±10줄)
    }
}
