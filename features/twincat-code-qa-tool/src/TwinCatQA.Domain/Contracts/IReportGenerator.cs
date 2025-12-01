using System.Collections.Generic;
using System.Threading.Tasks;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Domain.Contracts
{
    /// <summary>
    /// 리포트 생성기 인터페이스
    ///
    /// 검증 결과를 HTML 및 PDF 형식으로 생성합니다.
    /// Razor Engine을 사용한 템플릿 렌더링과 iText를 사용한 PDF 생성을 지원합니다.
    /// 인터페이스 분리 원칙(ISP)에 따라 리포트 생성 기능만 제공합니다.
    /// </summary>
    public interface IReportGenerator
    {
        /// <summary>
        /// HTML 리포트를 생성합니다.
        /// Razor 템플릿 엔진을 사용하여 대화형 웹 리포트를 생성합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        /// <param name="outputPath">
        /// 출력 파일 경로 (선택적).
        /// null이면 기본 경로 (./reports/{sessionId}.html)를 사용합니다.
        /// </param>
        /// <returns>생성된 HTML 파일의 절대 경로</returns>
        string GenerateHtmlReport(ValidationSession session, string? outputPath = null);

        /// <summary>
        /// PDF 리포트를 생성합니다.
        /// iText 라이브러리를 사용하여 인쇄 가능한 PDF 문서를 생성합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        /// <param name="outputPath">
        /// 출력 파일 경로 (선택적).
        /// null이면 기본 경로 (./reports/{sessionId}.pdf)를 사용합니다.
        /// </param>
        /// <returns>생성된 PDF 파일의 절대 경로</returns>
        string GeneratePdfReport(ValidationSession session, string? outputPath = null);

        /// <summary>
        /// 품질 추세 차트를 생성합니다.
        /// 시간에 따른 품질 점수 변화를 시각화합니다.
        /// </summary>
        /// <param name="sessions">
        /// 과거 검증 세션 목록 (시간순 정렬).
        /// 최소 2개 이상의 세션이 필요합니다.
        /// </param>
        /// <returns>
        /// 차트 데이터 객체.
        /// Chart.js 또는 다른 차트 라이브러리에서 사용 가능한 형식입니다.
        /// </returns>
        ChartData CreateQualityTrendChart(List<ValidationSession> sessions);

        /// <summary>
        /// 헌장 준수율 레이더 차트를 생성합니다.
        /// 각 헌장 원칙에 대한 준수율을 육각형 레이더로 표시합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        /// <returns>
        /// 차트 데이터 객체.
        /// 각 헌장 원칙(단순성, 일관성, 명확성 등)별 점수를 포함합니다.
        /// </returns>
        ChartData CreateConstitutionComplianceChart(ValidationSession session);

        /// <summary>
        /// 심각도별 위반 분포 차트를 생성합니다.
        /// Critical, Warning, Info별 위반 건수를 파이 차트로 표시합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        /// <returns>
        /// 차트 데이터 객체.
        /// 심각도별 위반 건수와 비율을 포함합니다.
        /// </returns>
        ChartData CreateViolationDistributionChart(ValidationSession session);

        /// <summary>
        /// 코드 스니펫을 하이라이팅합니다 (HTML용).
        /// Prism.js 또는 Highlight.js를 사용하여 구문 강조를 적용합니다.
        /// </summary>
        /// <param name="code">ST (Structured Text) 코드</param>
        /// <param name="language">프로그래밍 언어 (ST, LAD, SFC 등)</param>
        /// <returns>
        /// 하이라이트된 HTML 문자열.
        /// CSS 클래스가 적용된 span 태그를 포함합니다.
        /// </returns>
        string HighlightCode(string code, ProgrammingLanguage language);

        /// <summary>
        /// 리포트 템플릿을 커스터마이징합니다.
        /// 기본 템플릿 대신 사용자 정의 Razor 템플릿을 사용할 수 있습니다.
        /// </summary>
        /// <param name="templateName">
        /// 템플릿 이름 (예: "report.cshtml", "summary.cshtml").
        /// </param>
        /// <param name="customTemplatePath">
        /// 커스텀 템플릿 파일의 절대 경로.
        /// Razor 문법을 사용한 .cshtml 파일이어야 합니다.
        /// </param>
        /// <exception cref="System.IO.FileNotFoundException">
        /// 템플릿 파일이 존재하지 않을 때
        /// </exception>
        void SetCustomTemplate(string templateName, string customTemplatePath);
    }

    /// <summary>
    /// 비동기 리포트 생성기 인터페이스
    ///
    /// 대용량 리포트나 PDF 변환 작업을 비동기적으로 수행합니다.
    /// I/O 바운드 작업에 최적화되어 있습니다.
    /// </summary>
    public interface IAsyncReportGenerator
    {
        /// <summary>
        /// 비동기적으로 HTML 리포트를 생성합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        /// <param name="outputPath">출력 파일 경로 (선택적)</param>
        /// <returns>생성된 HTML 파일의 절대 경로</returns>
        Task<string> GenerateHtmlReportAsync(ValidationSession session, string? outputPath = null);

        /// <summary>
        /// 비동기적으로 PDF 리포트를 생성합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        /// <param name="outputPath">출력 파일 경로 (선택적)</param>
        /// <returns>생성된 PDF 파일의 절대 경로</returns>
        Task<string> GeneratePdfReportAsync(ValidationSession session, string? outputPath = null);

        /// <summary>
        /// 비동기적으로 HTML과 PDF 리포트를 동시에 생성합니다.
        /// </summary>
        /// <param name="session">검증 세션</param>
        /// <returns>
        /// 생성된 파일 경로 튜플 (HTML 경로, PDF 경로)
        /// </returns>
        Task<(string HtmlPath, string PdfPath)> GenerateBothReportsAsync(ValidationSession session);
    }

    /// <summary>
    /// 차트 데이터
    ///
    /// Chart.js, D3.js 등 차트 라이브러리에서 사용할 수 있는 데이터 구조입니다.
    /// </summary>
    public class ChartData
    {
        /// <summary>
        /// 차트 제목을 가져오거나 설정합니다.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 차트 타입을 가져오거나 설정합니다 (line, bar, pie, radar 등).
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 레이블 목록을 가져오거나 설정합니다 (X축 또는 범주).
        /// </summary>
        public List<string> Labels { get; set; } = new List<string>();

        /// <summary>
        /// 데이터셋 목록을 가져오거나 설정합니다.
        /// 각 데이터셋은 하나의 시리즈를 나타냅니다.
        /// </summary>
        public List<ChartDataset> Datasets { get; set; } = new List<ChartDataset>();

        /// <summary>
        /// 차트 옵션을 가져오거나 설정합니다 (JSON 직렬화 가능).
        /// </summary>
        public object? Options { get; set; }
    }

    /// <summary>
    /// 차트 데이터셋
    ///
    /// 차트의 하나의 데이터 시리즈를 나타냅니다.
    /// </summary>
    public class ChartDataset
    {
        /// <summary>
        /// 데이터셋 레이블을 가져오거나 설정합니다.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// 데이터 값 목록을 가져오거나 설정합니다.
        /// </summary>
        public List<double> Data { get; set; } = new List<double>();

        /// <summary>
        /// 배경색을 가져오거나 설정합니다 (CSS 색상 문자열).
        /// </summary>
        public string BackgroundColor { get; set; } = string.Empty;

        /// <summary>
        /// 테두리 색을 가져오거나 설정합니다 (CSS 색상 문자열).
        /// </summary>
        public string BorderColor { get; set; } = string.Empty;

        /// <summary>
        /// 테두리 두께를 가져오거나 설정합니다 (픽셀).
        /// </summary>
        public int BorderWidth { get; set; } = 1;
    }
}
