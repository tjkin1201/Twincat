using System.Collections.Generic;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Domain.Contracts
{
    /// <summary>
    /// 리포트 생성기 인터페이스
    ///
    /// 검증 결과를 HTML 및 PDF 형식으로 생성합니다.
    /// Razor Engine을 사용한 템플릿 렌더링과 iText를 사용한 PDF 생성을 지원합니다.
    /// </summary>
    public interface IReportGenerator
    {
        /// <summary>
        /// HTML 리포트를 생성합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        /// <param name="outputPath">출력 파일 경로</param>
        /// <returns>생성된 파일 경로</returns>
        string GenerateHtmlReport(ValidationSession session, string outputPath = null);

        /// <summary>
        /// PDF 리포트를 생성합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        /// <param name="outputPath">출력 파일 경로</param>
        /// <returns>생성된 파일 경로</returns>
        string GeneratePdfReport(ValidationSession session, string outputPath = null);

        /// <summary>
        /// 품질 추세 차트를 생성합니다.
        /// </summary>
        /// <param name="sessions">과거 검증 세션 목록</param>
        /// <returns>차트 데이터</returns>
        ChartData CreateQualityTrendChart(List<ValidationSession> sessions);

        /// <summary>
        /// 헌장 준수율 레이더 차트를 생성합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        /// <returns>차트 데이터</returns>
        ChartData CreateConstitutionComplianceChart(ValidationSession session);

        /// <summary>
        /// 심각도별 위반 분포 차트를 생성합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        /// <returns>차트 데이터</returns>
        ChartData CreateViolationDistributionChart(ValidationSession session);

        /// <summary>
        /// 코드 스니펫을 하이라이팅합니다 (HTML용).
        /// </summary>
        /// <param name="code">ST 코드</param>
        /// <param name="language">프로그래밍 언어</param>
        /// <returns>하이라이트된 HTML</returns>
        string HighlightCode(string code, ProgrammingLanguage language);

        /// <summary>
        /// 리포트 템플릿을 커스터마이징합니다.
        /// </summary>
        /// <param name="templateName">템플릿 이름 (예: "report.cshtml")</param>
        /// <param name="customTemplate">커스텀 템플릿 경로</param>
        void SetCustomTemplate(string templateName, string customTemplate);
    }
}
