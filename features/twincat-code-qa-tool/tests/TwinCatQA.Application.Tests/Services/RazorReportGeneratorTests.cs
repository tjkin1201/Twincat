// TODO: RazorReportGenerator 구현 후 테스트 활성화
// 현재는 SimpleHtmlReportGenerator로 대체됨
#if FALSE  // 임시 비활성화 - RazorReportGenerator 미구현
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TwinCatQA.Application.Services;
using TwinCatQA.Domain.Models;
using Xunit;

namespace TwinCatQA.Application.Tests.Services
{
    /// <summary>
    /// RazorReportGenerator 단위 테스트 (비활성화됨 - SimpleHtmlReportGenerator 사용 중)
    /// </summary>
    public class RazorReportGeneratorTests : IDisposable
    {
        private readonly RazorReportGenerator _generator;
        private readonly string _testOutputDir;
        private readonly List<string> _createdFiles;

        public RazorReportGeneratorTests()
        {
            _generator = new RazorReportGenerator();
            _testOutputDir = Path.Combine(Path.GetTempPath(), "TwinCatQA_Tests", Guid.NewGuid().ToString());
            _createdFiles = new List<string>();

            // 테스트 출력 디렉토리 생성
            Directory.CreateDirectory(_testOutputDir);
        }

        /// <summary>
        /// 테스트 후 임시 파일 정리
        /// </summary>
        public void Dispose()
        {
            foreach (var file in _createdFiles)
            {
                if (File.Exists(file))
                {
                    try { File.Delete(file); } catch { /* 무시 */ }
                }
            }

            if (Directory.Exists(_testOutputDir))
            {
                try { Directory.Delete(_testOutputDir, true); } catch { /* 무시 */ }
            }
        }

        /// <summary>
        /// 테스트용 검증 세션 생성
        /// </summary>
        private ValidationSession CreateTestSession()
        {
            var session = new ValidationSession
            {
                SessionId = Guid.NewGuid(),
                ProjectPath = @"C:\TestProject\MyTwinCatProject.tsproj",
                StartTime = DateTime.Now.AddMinutes(-5),
                EndTime = DateTime.Now,
                OverallQualityScore = 85.5
            };

            // 위반 사항 추가
            session.Violations.Add(new Violation
            {
                Id = Guid.NewGuid(),
                RuleId = "NAM001",
                Severity = ViolationSeverity.High,
                Message = "함수 이름이 명명 규칙을 위반했습니다.",
                FilePath = @"POUs\MAIN.TcPOU",
                Line = 10,
                Suggestion = "함수 이름을 PascalCase로 변경하세요. 예: MyFunction",
                CodeSnippet = "FUNCTION my_function : BOOL"
            });

            session.Violations.Add(new Violation
            {
                Id = Guid.NewGuid(),
                RuleId = "CPX001",
                Severity = ViolationSeverity.Medium,
                Message = "함수의 순환 복잡도가 너무 높습니다.",
                FilePath = @"POUs\Controller.TcPOU",
                Line = 25,
                Suggestion = "함수를 더 작은 단위로 분리하세요.",
                CodeSnippet = "IF condition1 THEN\n  IF condition2 THEN\n    // ...\n  END_IF\nEND_IF"
            });

            session.Violations.Add(new Violation
            {
                Id = Guid.NewGuid(),
                RuleId = "SAF001",
                Severity = ViolationSeverity.Critical,
                Message = "널 참조 가능성이 있습니다.",
                FilePath = @"POUs\DataHandler.TcPOU",
                Line = 42,
                Suggestion = "포인터 사용 전에 유효성을 검사하세요.",
                CodeSnippet = "pData^ := newValue;"
            });

            session.Violations.Add(new Violation
            {
                Id = Guid.NewGuid(),
                RuleId = "STY001",
                Severity = ViolationSeverity.Low,
                Message = "들여쓰기가 일관적이지 않습니다.",
                FilePath = @"POUs\MAIN.TcPOU",
                Line = 15,
                Suggestion = "탭 또는 스페이스 중 하나만 일관되게 사용하세요.",
                CodeSnippet = "  IF x > 0 THEN\n    DoSomething();\n  END_IF"
            });

            return session;
        }

        [Fact]
        public async Task GenerateHtmlReport_ValidSession_CreatesHtmlFile()
        {
            // Arrange
            var session = CreateTestSession();
            var outputPath = Path.Combine(_testOutputDir, "test-report.html");
            _createdFiles.Add(outputPath);

            // Act
            var result = await _generator.GenerateHtmlReportAsync(session, outputPath);

            // Assert
            Assert.True(File.Exists(result), "HTML 파일이 생성되어야 합니다.");
            var content = await File.ReadAllTextAsync(result);
            Assert.Contains("TwinCAT 코드 품질 검증 리포트", content);
            Assert.Contains("품질 점수", content);
            Assert.Contains("85.5", content);
        }

        [Fact]
        public async Task GenerateHtmlReport_ContainsViolations_DisplaysAllViolations()
        {
            // Arrange
            var session = CreateTestSession();
            var outputPath = Path.Combine(_testOutputDir, "violations-report.html");
            _createdFiles.Add(outputPath);

            // Act
            var result = await _generator.GenerateHtmlReportAsync(session, outputPath);
            var content = await File.ReadAllTextAsync(result);

            // Assert
            Assert.Contains("NAM001", content);
            Assert.Contains("CPX001", content);
            Assert.Contains("SAF001", content);
            Assert.Contains("STY001", content);
            // 위반 섹션이 있는지 확인 (메시지는 HTML escape로 인해 다를 수 있음)
            Assert.Contains("위반 사항 상세", content);
        }

        [Fact]
        public async Task GenerateHtmlReport_ContainsCharts_IncludesChartData()
        {
            // Arrange
            var session = CreateTestSession();
            var outputPath = Path.Combine(_testOutputDir, "charts-report.html");
            _createdFiles.Add(outputPath);

            // Act
            var result = await _generator.GenerateHtmlReportAsync(session, outputPath);
            var content = await File.ReadAllTextAsync(result);

            // Assert
            Assert.Contains("Chart.js", content);
            Assert.Contains("qualityTrendChart", content);
            Assert.Contains("constitutionComplianceChart", content);
            Assert.Contains("violationDistributionChart", content);
        }

        [Fact]
        public async Task GenerateHtmlReport_NullSession_ThrowsArgumentNullException()
        {
            // Arrange
            var outputPath = Path.Combine(_testOutputDir, "null-report.html");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await _generator.GenerateHtmlReportAsync(null!, outputPath)
            );
        }

        [Fact]
        public async Task GenerateHtmlReport_EmptyOutputPath_ThrowsArgumentException()
        {
            // Arrange
            var session = CreateTestSession();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await _generator.GenerateHtmlReportAsync(session, string.Empty)
            );
        }

        [Fact]
        public void CreateQualityTrendChart_SingleSession_ReturnsLineChart()
        {
            // Arrange
            var session = CreateTestSession();
            var sessions = new List<ValidationSession> { session };

            // Act
            var chartData = _generator.CreateQualityTrendChart(sessions);

            // Assert
            Assert.NotNull(chartData);
            Assert.Equal("line", chartData.Type);
            Assert.Single(chartData.Data.Datasets);
            Assert.Equal("품질 점수", chartData.Data.Datasets[0].Label);
        }

        [Fact]
        public void CreateConstitutionComplianceChart_ValidSession_ReturnsRadarChart()
        {
            // Arrange
            var session = CreateTestSession();

            // Act
            var chartData = _generator.CreateConstitutionComplianceChart(session);

            // Assert
            Assert.NotNull(chartData);
            Assert.Equal("radar", chartData.Type);
            Assert.Equal(8, chartData.Data.Labels.Count); // 8가지 헌장 원칙
        }

        [Fact]
        public void CreateViolationDistributionChart_ValidSession_ReturnsPieChart()
        {
            // Arrange
            var session = CreateTestSession();

            // Act
            var chartData = _generator.CreateViolationDistributionChart(session);

            // Assert
            Assert.NotNull(chartData);
            Assert.Equal("pie", chartData.Type);
            Assert.True(chartData.Data.Labels.Count > 0);
            Assert.True(chartData.Data.Datasets[0].Data.Count > 0);
        }

        [Fact]
        public void HighlightCode_StructuredTextCode_ReturnsHighlightedHtml()
        {
            // Arrange
            var code = "FUNCTION MyFunction : BOOL\nVAR\n  x : INT;\nEND_VAR";

            // Act
            var result = _generator.HighlightCode(code, "st");

            // Assert
            Assert.Contains("<pre", result);
            Assert.Contains("<code", result);
            Assert.Contains("keyword", result); // 키워드 하이라이팅 확인
        }

        [Fact]
        public async Task GeneratePdfReport_NotImplemented_ThrowsNotImplementedException()
        {
            // Arrange
            var session = CreateTestSession();
            var outputPath = Path.Combine(_testOutputDir, "test-report.pdf");

            // Act & Assert
            await Assert.ThrowsAsync<NotImplementedException>(
                async () => await _generator.GeneratePdfReportAsync(session, outputPath)
            );
        }
    }
}
#endif
