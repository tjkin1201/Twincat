using System;
using System.Collections.Generic;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Domain.Contracts
{
    /// <summary>
    /// 검증 엔진 인터페이스
    ///
    /// 검증 세션의 전체 수명 주기를 관리하며,
    /// 파일 스캔, 파싱, 규칙 실행, 리포트 생성을 오케스트레이션합니다.
    /// </summary>
    public interface IValidationEngine
    {
        /// <summary>
        /// 새로운 검증 세션을 시작합니다.
        /// </summary>
        /// <param name="projectPath">TwinCAT 프로젝트 경로 (.tsproj)</param>
        /// <param name="mode">검증 모드 (전체/증분)</param>
        /// <returns>생성된 검증 세션</returns>
        ValidationSession StartSession(string projectPath, ValidationMode mode);

        /// <summary>
        /// 프로젝트 파일을 스캔하고 CodeFile 목록을 생성합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        void ScanFiles(ValidationSession session);

        /// <summary>
        /// 스캔된 파일을 파싱하고 AST를 생성합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        /// <param name="progressCallback">진행률 콜백 (0.0 ~ 1.0)</param>
        void ParseFiles(ValidationSession session, Action<double> progressCallback = null);

        /// <summary>
        /// 모든 활성화된 규칙을 실행하여 위반 사항을 수집합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        /// <param name="progressCallback">진행률 콜백</param>
        void RunValidation(ValidationSession session, Action<double> progressCallback = null);

        /// <summary>
        /// 품질 점수를 계산합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        void CalculateQualityScores(ValidationSession session);

        /// <summary>
        /// HTML 및 PDF 리포트를 생성합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        void GenerateReports(ValidationSession session);

        /// <summary>
        /// 검증 세션을 완료하고 저장합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        void CompleteSession(ValidationSession session);

        /// <summary>
        /// 검증 세션을 취소합니다.
        /// </summary>
        /// <param name="sessionId">세션 ID</param>
        void CancelSession(Guid sessionId);

        /// <summary>
        /// 활성화된 모든 검증 규칙 목록
        /// </summary>
        IEnumerable<IValidationRule> ActiveRules { get; }

        /// <summary>
        /// 현재 실행 중인 세션 목록
        /// </summary>
        IEnumerable<ValidationSession> RunningSessions { get; }
    }
}
