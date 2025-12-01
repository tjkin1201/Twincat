using System.Collections.Generic;
using System.Threading.Tasks;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Domain.Contracts
{
    /// <summary>
    /// Git 서비스 인터페이스
    ///
    /// LibGit2Sharp를 사용하여 Git 작업을 수행합니다.
    /// Diff 분석, Hook 관리, 변경 사항 추출을 지원합니다.
    /// 인터페이스 분리 원칙(ISP)에 따라 Git 관련 기능만 제공합니다.
    /// </summary>
    public interface IGitService
    {
        /// <summary>
        /// Git 저장소를 초기화합니다.
        /// 이미 Git 저장소인 경우 아무 작업도 수행하지 않습니다.
        /// </summary>
        /// <param name="repoPath">저장소 경로 (프로젝트 루트)</param>
        /// <exception cref="System.IO.DirectoryNotFoundException">
        /// 경로가 존재하지 않을 때
        /// </exception>
        void InitializeRepository(string repoPath);

        /// <summary>
        /// 저장소가 Git으로 관리되는지 확인합니다.
        /// .git 디렉토리 존재 여부를 검사합니다.
        /// </summary>
        /// <param name="path">검사할 경로</param>
        /// <returns>
        /// Git 저장소 여부.
        /// 경로가 Git 저장소이거나 Git 저장소 내부이면 true를 반환합니다.
        /// </returns>
        bool IsGitRepository(string path);

        /// <summary>
        /// 변경된 파일 목록을 가져옵니다.
        /// Diff를 분석하여 추가/수정/삭제된 파일을 찾습니다.
        /// </summary>
        /// <param name="repoPath">저장소 경로</param>
        /// <param name="diffTarget">
        /// 비교 대상 (스테이징, 워킹 디렉토리 등).
        /// 기본값은 Index (스테이징 영역 vs HEAD)입니다.
        /// </param>
        /// <returns>
        /// 변경된 파일의 상대 경로 목록.
        /// TwinCAT 파일(.TcPOU, .TcDUT 등)만 필터링됩니다.
        /// </returns>
        List<string> GetChangedFiles(string repoPath, DiffTarget diffTarget = DiffTarget.Index);

        /// <summary>
        /// 특정 파일의 변경된 라인을 가져옵니다.
        /// 라인 단위 Diff를 분석하여 추가/수정/삭제 정보를 제공합니다.
        /// </summary>
        /// <param name="repoPath">저장소 경로</param>
        /// <param name="filePath">파일 경로 (저장소 루트 기준 상대 경로)</param>
        /// <returns>
        /// 변경된 라인 목록.
        /// 각 라인의 번호, 변경 유형, 내용을 포함합니다.
        /// </returns>
        List<LineChange> GetChangedLines(string repoPath, string filePath);

        /// <summary>
        /// Pre-commit hook을 설치합니다.
        /// .git/hooks/pre-commit 파일을 생성하고 실행 권한을 부여합니다.
        /// </summary>
        /// <param name="repoPath">저장소 경로</param>
        /// <param name="blockOnCritical">
        /// Critical 위반 시 커밋 차단 여부.
        /// true이면 Critical 위반 발견 시 커밋을 거부합니다.
        /// </param>
        void InstallPreCommitHook(string repoPath, bool blockOnCritical = true);

        /// <summary>
        /// Pre-commit hook을 제거합니다.
        /// .git/hooks/pre-commit 파일을 삭제합니다.
        /// </summary>
        /// <param name="repoPath">저장소 경로</param>
        void UninstallPreCommitHook(string repoPath);

        /// <summary>
        /// Pre-commit hook이 설치되어 있는지 확인합니다.
        /// </summary>
        /// <param name="repoPath">저장소 경로</param>
        /// <returns>
        /// 설치 여부.
        /// pre-commit 파일이 존재하고 실행 가능하면 true를 반환합니다.
        /// </returns>
        bool IsPreCommitHookInstalled(string repoPath);

        /// <summary>
        /// 현재 커밋 해시를 가져옵니다.
        /// HEAD가 가리키는 커밋의 SHA-1 해시를 반환합니다.
        /// </summary>
        /// <param name="repoPath">저장소 경로</param>
        /// <returns>
        /// 커밋 해시 (40자 16진수 문자열).
        /// Detached HEAD 상태에서도 정상 작동합니다.
        /// </returns>
        string GetCurrentCommitHash(string repoPath);

        /// <summary>
        /// 작업 디렉토리가 깨끗한지 확인합니다 (변경 사항 없음).
        /// 스테이징 영역과 워킹 디렉토리 모두 변경이 없어야 합니다.
        /// </summary>
        /// <param name="repoPath">저장소 경로</param>
        /// <returns>
        /// 깨끗한 상태 여부.
        /// Untracked 파일은 무시합니다.
        /// </returns>
        bool IsWorkingDirectoryClean(string repoPath);

        /// <summary>
        /// 두 커밋 간의 차이를 가져옵니다.
        /// 커밋 범위의 모든 변경된 파일을 반환합니다.
        /// </summary>
        /// <param name="repoPath">저장소 경로</param>
        /// <param name="fromCommit">시작 커밋 (SHA-1 해시 또는 브랜치명)</param>
        /// <param name="toCommit">
        /// 종료 커밋 (SHA-1 해시 또는 브랜치명).
        /// null이면 HEAD를 사용합니다.
        /// </param>
        /// <returns>
        /// 변경된 파일 목록 (상대 경로).
        /// TwinCAT 파일만 필터링됩니다.
        /// </returns>
        List<string> GetDiffBetweenCommits(string repoPath, string fromCommit, string? toCommit = null);

        /// <summary>
        /// 변경 라인에 대한 컨텍스트 범위를 결정합니다.
        /// 증분 검증 시 어떤 범위를 검증할지 결정하는 데 사용됩니다.
        /// </summary>
        /// <param name="file">코드 파일 (AST 포함)</param>
        /// <param name="changedLine">변경된 라인 번호</param>
        /// <returns>
        /// 코드 컨텍스트 객체.
        /// 변경된 라인이 속한 Function Block이나 제어 구조를 식별합니다.
        /// </returns>
        CodeContext DetermineContext(CodeFile file, int changedLine);
    }

    /// <summary>
    /// 비동기 Git 서비스 인터페이스
    ///
    /// Git 작업을 비동기적으로 수행합니다.
    /// 대용량 저장소나 많은 파일을 처리할 때 사용합니다.
    /// </summary>
    public interface IAsyncGitService
    {
        /// <summary>
        /// 비동기적으로 변경된 파일 목록을 가져옵니다.
        /// </summary>
        /// <param name="repoPath">저장소 경로</param>
        /// <param name="diffTarget">비교 대상</param>
        /// <returns>변경된 파일 경로 목록</returns>
        Task<List<string>> GetChangedFilesAsync(
            string repoPath,
            DiffTarget diffTarget = DiffTarget.Index);

        /// <summary>
        /// 비동기적으로 변경된 라인을 가져옵니다.
        /// </summary>
        /// <param name="repoPath">저장소 경로</param>
        /// <param name="filePath">파일 경로 (상대)</param>
        /// <returns>변경된 라인 목록</returns>
        Task<List<LineChange>> GetChangedLinesAsync(string repoPath, string filePath);

        /// <summary>
        /// 비동기적으로 커밋 간 차이를 가져옵니다.
        /// </summary>
        /// <param name="repoPath">저장소 경로</param>
        /// <param name="fromCommit">시작 커밋</param>
        /// <param name="toCommit">종료 커밋</param>
        /// <returns>변경된 파일 목록</returns>
        Task<List<string>> GetDiffBetweenCommitsAsync(
            string repoPath,
            string fromCommit,
            string? toCommit = null);
    }

    /// <summary>
    /// Diff 비교 대상
    ///
    /// Git Diff를 수행할 때 어떤 영역을 비교할지 지정합니다.
    /// </summary>
    public enum DiffTarget
    {
        /// <summary>
        /// 스테이징 영역 vs HEAD 커밋.
        /// git diff --cached와 동일합니다.
        /// </summary>
        Index,

        /// <summary>
        /// 워킹 디렉토리 vs 스테이징 영역.
        /// git diff와 동일합니다.
        /// </summary>
        WorkingDirectory,

        /// <summary>
        /// 워킹 디렉토리 vs HEAD 커밋.
        /// git diff HEAD와 동일합니다.
        /// </summary>
        All
    }

    /// <summary>
    /// 라인 변경 정보
    ///
    /// Git Diff의 라인 단위 변경 사항을 나타냅니다.
    /// </summary>
    public class LineChange
    {
        /// <summary>
        /// 라인 번호를 가져오거나 설정합니다 (1부터 시작).
        /// 삭제된 라인은 이전 파일 기준 번호를 사용합니다.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// 변경 유형을 가져오거나 설정합니다 (추가/수정/삭제).
        /// </summary>
        public ChangeType Type { get; set; }

        /// <summary>
        /// 라인 내용을 가져오거나 설정합니다.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 변경 정보를 문자열로 반환합니다.
        /// </summary>
        public override string ToString()
        {
            string prefix = Type == ChangeType.Added ? "+" :
                          Type == ChangeType.Deleted ? "-" : "~";
            return $"{prefix} {LineNumber}: {Content}";
        }
    }

    /// <summary>
    /// 변경 유형
    ///
    /// Git Diff에서 라인의 변경 상태를 나타냅니다.
    /// </summary>
    public enum ChangeType
    {
        /// <summary>
        /// 추가된 라인 (git diff의 + 라인).
        /// </summary>
        Added,

        /// <summary>
        /// 수정된 라인 (삭제 후 추가로 표현됨).
        /// </summary>
        Modified,

        /// <summary>
        /// 삭제된 라인 (git diff의 - 라인).
        /// </summary>
        Deleted
    }

    /// <summary>
    /// 코드 컨텍스트 (증분 검증용)
    ///
    /// 변경된 라인이 속한 코드 블록의 범위를 나타냅니다.
    /// 증분 검증 시 어느 범위를 검증할지 결정하는 데 사용됩니다.
    /// </summary>
    public class CodeContext
    {
        /// <summary>
        /// 컨텍스트 유형을 가져오거나 설정합니다.
        /// </summary>
        public ContextType Type { get; set; }

        /// <summary>
        /// 라인 범위를 가져오거나 설정합니다 (시작, 종료).
        /// 1부터 시작하며, 범위는 포함(inclusive)입니다.
        /// </summary>
        public (int Start, int End) LineRange { get; set; }

        /// <summary>
        /// 요소 이름을 가져오거나 설정합니다.
        /// Function Block 이름, CASE 블록 레이블 등을 포함합니다.
        /// </summary>
        public string ElementName { get; set; } = string.Empty;

        /// <summary>
        /// 컨텍스트 설명을 문자열로 반환합니다.
        /// </summary>
        public override string ToString()
        {
            return $"{Type}: {ElementName} (라인 {LineRange.Start}-{LineRange.End})";
        }
    }

    /// <summary>
    /// 컨텍스트 유형
    ///
    /// 코드에서 식별 가능한 블록 단위를 나타냅니다.
    /// </summary>
    public enum ContextType
    {
        /// <summary>
        /// Function Block 전체.
        /// FUNCTION_BLOCK ~ END_FUNCTION_BLOCK 범위입니다.
        /// </summary>
        FunctionBlock,

        /// <summary>
        /// CASE 문 블록.
        /// CASE ~ END_CASE 범위입니다.
        /// </summary>
        CaseBlock,

        /// <summary>
        /// FOR 루프.
        /// FOR ~ END_FOR 범위입니다.
        /// </summary>
        ForLoop,

        /// <summary>
        /// IF 블록.
        /// IF ~ END_IF 범위입니다.
        /// </summary>
        IfBlock,

        /// <summary>
        /// 주변 라인 (컨텍스트 식별 실패 시).
        /// 변경된 라인 ±10줄 범위입니다.
        /// </summary>
        SurroundingLines
    }
}
