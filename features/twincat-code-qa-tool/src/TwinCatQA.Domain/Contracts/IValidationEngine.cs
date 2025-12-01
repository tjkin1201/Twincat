using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Domain.Contracts
{
    /// <summary>
    /// 검증 엔진 인터페이스
    ///
    /// 검증 세션의 전체 수명 주기를 관리하며,
    /// 파일 스캔, 파싱, 규칙 실행, 리포트 생성을 오케스트레이션합니다.
    /// 의존성 역전 원칙(DIP)에 따라 구현 세부사항에 의존하지 않습니다.
    /// </summary>
    public interface IValidationEngine
    {
        /// <summary>
        /// 활성화된 모든 검증 규칙 목록을 가져옵니다.
        /// 비활성화된 규칙은 포함되지 않습니다.
        /// </summary>
        IEnumerable<IValidationRule> ActiveRules { get; }

        /// <summary>
        /// 현재 실행 중인 세션 목록을 가져옵니다.
        /// 완료되거나 취소된 세션은 포함되지 않습니다.
        /// </summary>
        IEnumerable<ValidationSession> RunningSessions { get; }

        /// <summary>
        /// 새로운 검증 세션을 시작합니다.
        /// 세션 ID를 생성하고 초기 상태를 설정합니다.
        /// </summary>
        /// <param name="projectPath">TwinCAT 프로젝트 경로 (.tsproj 파일 경로)</param>
        /// <param name="mode">검증 모드 (전체/증분). 증분 모드는 Git 변경 사항만 검증합니다.</param>
        /// <returns>생성된 검증 세션 객체</returns>
        /// <exception cref="ArgumentException">projectPath가 유효하지 않을 때</exception>
        ValidationSession StartSession(string projectPath, ValidationMode mode);

        /// <summary>
        /// 프로젝트 파일을 스캔하고 CodeFile 목록을 생성합니다.
        /// .TcPOU, .TcDUT, .TcGVL 파일을 재귀적으로 탐색합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        /// <exception cref="InvalidOperationException">세션이 유효하지 않을 때</exception>
        void ScanFiles(ValidationSession session);

        /// <summary>
        /// 스캔된 파일을 파싱하고 AST를 생성합니다.
        /// ANTLR4 파서를 사용하여 Structured Text 코드를 분석합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        /// <param name="progressCallback">
        /// 진행률 콜백 함수 (0.0 ~ 1.0 범위).
        /// UI 업데이트나 진행 상황 로깅에 사용됩니다.
        /// </param>
        void ParseFiles(ValidationSession session, Action<double>? progressCallback = null);

        /// <summary>
        /// 모든 활성화된 규칙을 실행하여 위반 사항을 수집합니다.
        /// 각 파일에 대해 적용 가능한 규칙만 실행하여 성능을 최적화합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        /// <param name="progressCallback">진행률 콜백 함수 (0.0 ~ 1.0 범위)</param>
        void RunValidation(ValidationSession session, Action<double>? progressCallback = null);

        /// <summary>
        /// 품질 점수를 계산합니다.
        /// 위반 사항 심각도와 수량에 기반하여 0-100점 사이의 점수를 계산합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        void CalculateQualityScores(ValidationSession session);

        /// <summary>
        /// HTML 및 PDF 리포트를 생성합니다.
        /// Razor 템플릿 엔진을 사용하여 시각화된 리포트를 생성합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        void GenerateReports(ValidationSession session);

        /// <summary>
        /// 검증 세션을 완료하고 저장합니다.
        /// 세션 종료 시간을 기록하고 결과를 영구 저장소에 저장합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        void CompleteSession(ValidationSession session);

        /// <summary>
        /// 검증 세션을 취소합니다.
        /// 실행 중인 검증 작업을 중단하고 세션 상태를 취소로 변경합니다.
        /// </summary>
        /// <param name="sessionId">취소할 세션의 고유 ID</param>
        /// <exception cref="KeyNotFoundException">세션 ID가 존재하지 않을 때</exception>
        void CancelSession(Guid sessionId);
    }

    /// <summary>
    /// 비동기 검증 엔진 인터페이스
    ///
    /// 장시간 실행되는 검증 작업을 비동기적으로 수행합니다.
    /// 대용량 프로젝트나 전체 검증에 사용합니다.
    /// </summary>
    public interface IAsyncValidationEngine
    {
        /// <summary>
        /// 비동기적으로 새로운 검증 세션을 시작합니다.
        /// </summary>
        /// <param name="projectPath">TwinCAT 프로젝트 경로</param>
        /// <param name="mode">검증 모드</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>생성된 검증 세션</returns>
        Task<ValidationSession> StartSessionAsync(
            string projectPath,
            ValidationMode mode,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 비동기적으로 파일을 스캔합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        /// <param name="cancellationToken">취소 토큰</param>
        Task ScanFilesAsync(
            ValidationSession session,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 비동기적으로 파일을 파싱합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        /// <param name="progress">진행률 리포터 (0.0 ~ 1.0)</param>
        /// <param name="cancellationToken">취소 토큰</param>
        Task ParseFilesAsync(
            ValidationSession session,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 비동기적으로 검증을 실행합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        /// <param name="progress">진행률 리포터 (0.0 ~ 1.0)</param>
        /// <param name="cancellationToken">취소 토큰</param>
        Task RunValidationAsync(
            ValidationSession session,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 비동기적으로 전체 검증 파이프라인을 실행합니다.
        /// 스캔 → 파싱 → 검증 → 점수 계산 → 리포트 생성을 순차적으로 수행합니다.
        /// </summary>
        /// <param name="projectPath">TwinCAT 프로젝트 경로</param>
        /// <param name="mode">검증 모드</param>
        /// <param name="progress">진행률 리포터 (0.0 ~ 1.0)</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>완료된 검증 세션</returns>
        Task<ValidationSession> RunFullValidationAsync(
            string projectPath,
            ValidationMode mode,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default);
    }
}
