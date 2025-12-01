using System;
using System.Collections.Generic;
using TwinCatQA.Application.Services;
using TwinCatQA.Domain.Models;
using Xunit;

namespace TwinCatQA.Application.Tests.Services
{
    /// <summary>
    /// ChartDataBuilder 단위 테스트
    /// </summary>
    public class ChartDataBuilderTests
    {
        private readonly ChartDataBuilder _builder;

        public ChartDataBuilderTests()
        {
            _builder = new ChartDataBuilder();
        }

        /// <summary>
        /// 테스트용 검증 세션 생성
        /// </summary>
        private ValidationSession CreateTestSession(double score, int criticalCount, int highCount, int mediumCount, int lowCount)
        {
            var session = new ValidationSession
            {
                SessionId = Guid.NewGuid(),
                ProjectPath = @"C:\TestProject\Test.tsproj",
                OverallQualityScore = score
            };

            // Critical 위반 추가
            for (int i = 0; i < criticalCount; i++)
            {
                session.Violations.Add(new Violation
                {
                    Id = Guid.NewGuid(),
                    RuleId = $"SAF{i:D3}",
                    Severity = ViolationSeverity.Critical,
                    Message = $"Critical violation {i}",
                    FilePath = "test.st",
                    Line = i + 1
                });
            }

            // High 위반 추가
            for (int i = 0; i < highCount; i++)
            {
                session.Violations.Add(new Violation
                {
                    Id = Guid.NewGuid(),
                    RuleId = $"NAM{i:D3}",
                    Severity = ViolationSeverity.High,
                    Message = $"High violation {i}",
                    FilePath = "test.st",
                    Line = i + 1
                });
            }

            // Medium 위반 추가
            for (int i = 0; i < mediumCount; i++)
            {
                session.Violations.Add(new Violation
                {
                    Id = Guid.NewGuid(),
                    RuleId = $"CPX{i:D3}",
                    Severity = ViolationSeverity.Medium,
                    Message = $"Medium violation {i}",
                    FilePath = "test.st",
                    Line = i + 1
                });
            }

            // Low 위반 추가
            for (int i = 0; i < lowCount; i++)
            {
                session.Violations.Add(new Violation
                {
                    Id = Guid.NewGuid(),
                    RuleId = $"STY{i:D3}",
                    Severity = ViolationSeverity.Low,
                    Message = $"Low violation {i}",
                    FilePath = "test.st",
                    Line = i + 1
                });
            }

            return session;
        }

        [Fact]
        public void BuildQualityTrendData_SingleSession_ReturnsLineChartData()
        {
            // Arrange
            var session = CreateTestSession(85.0, 1, 2, 3, 4);
            var sessions = new List<ValidationSession> { session };

            // Act
            var chartData = _builder.BuildQualityTrendData(sessions);

            // Assert
            Assert.NotNull(chartData);
            Assert.Equal("line", chartData.Type);
            Assert.Single(chartData.Data.Labels);
            Assert.Single(chartData.Data.Datasets);
            Assert.Equal("품질 점수", chartData.Data.Datasets[0].Label);
            Assert.Equal(85.0, chartData.Data.Datasets[0].Data[0]);
        }

        [Fact]
        public void BuildQualityTrendData_MultipleSessions_OrdersByTime()
        {
            // Arrange
            var time1 = DateTime.Now.AddHours(-2);
            var time2 = DateTime.Now.AddHours(-1);
            var time3 = DateTime.Now;

            var session1 = new ValidationSession
            {
                SessionId = Guid.NewGuid(),
                ProjectPath = @"C:\TestProject\Test.tsproj",
                OverallQualityScore = 80.0,
                StartTime = time1
            };

            var session2 = new ValidationSession
            {
                SessionId = Guid.NewGuid(),
                ProjectPath = @"C:\TestProject\Test.tsproj",
                OverallQualityScore = 85.0,
                StartTime = time2
            };

            var session3 = new ValidationSession
            {
                SessionId = Guid.NewGuid(),
                ProjectPath = @"C:\TestProject\Test.tsproj",
                OverallQualityScore = 90.0,
                StartTime = time3
            };

            var sessions = new List<ValidationSession> { session3, session1, session2 };

            // Act
            var chartData = _builder.BuildQualityTrendData(sessions);

            // Assert
            Assert.Equal(3, chartData.Data.Labels.Count);
            Assert.Equal(3, chartData.Data.Datasets[0].Data.Count);
            Assert.Equal(80.0, chartData.Data.Datasets[0].Data[0]); // 가장 오래된 세션
            Assert.Equal(85.0, chartData.Data.Datasets[0].Data[1]);
            Assert.Equal(90.0, chartData.Data.Datasets[0].Data[2]); // 가장 최근 세션
        }

        [Fact]
        public void BuildQualityTrendData_EmptyList_ReturnsEmptyChart()
        {
            // Arrange
            var sessions = new List<ValidationSession>();

            // Act
            var chartData = _builder.BuildQualityTrendData(sessions);

            // Assert
            Assert.NotNull(chartData);
            Assert.Equal("line", chartData.Type);
            Assert.Empty(chartData.Data.Labels);
            Assert.Empty(chartData.Data.Datasets);
        }

        [Fact]
        public void BuildConstitutionRadarData_ValidSession_ReturnsRadarChartWith8Principles()
        {
            // Arrange
            var session = CreateTestSession(85.0, 2, 3, 4, 5);

            // Act
            var chartData = _builder.BuildConstitutionRadarData(session);

            // Assert
            Assert.NotNull(chartData);
            Assert.Equal("radar", chartData.Type);
            Assert.Equal(8, chartData.Data.Labels.Count); // 8가지 헌장 원칙
            Assert.Single(chartData.Data.Datasets);

            // 레이블 검증
            Assert.Contains("명확성", chartData.Data.Labels);
            Assert.Contains("일관성", chartData.Data.Labels);
            Assert.Contains("단순성", chartData.Data.Labels);
            Assert.Contains("모듈화", chartData.Data.Labels);
            Assert.Contains("안전성", chartData.Data.Labels);
            Assert.Contains("성능", chartData.Data.Labels);
            Assert.Contains("유지보수성", chartData.Data.Labels);
            Assert.Contains("표준준수", chartData.Data.Labels);
        }

        [Fact]
        public void BuildConstitutionRadarData_NoViolations_ReturnsFullCompliance()
        {
            // Arrange
            var session = CreateTestSession(100.0, 0, 0, 0, 0);

            // Act
            var chartData = _builder.BuildConstitutionRadarData(session);

            // Assert
            Assert.Equal(8, chartData.Data.Datasets[0].Data.Count);
            foreach (var score in chartData.Data.Datasets[0].Data)
            {
                Assert.True(score >= 0 && score <= 100, "준수율은 0-100 범위여야 합니다.");
            }
        }

        [Fact]
        public void BuildViolationPieData_AllSeverities_IncludesAllCategories()
        {
            // Arrange
            var session = CreateTestSession(70.0, 2, 3, 4, 5);

            // Act
            var chartData = _builder.BuildViolationPieData(session);

            // Assert
            Assert.NotNull(chartData);
            Assert.Equal("pie", chartData.Type);
            Assert.Equal(4, chartData.Data.Labels.Count); // 4가지 심각도
            Assert.Contains("Critical", chartData.Data.Labels);
            Assert.Contains("High", chartData.Data.Labels);
            Assert.Contains("Medium", chartData.Data.Labels);
            Assert.Contains("Low", chartData.Data.Labels);

            // 데이터 개수 검증
            var dataset = chartData.Data.Datasets[0];
            Assert.Equal(2.0, dataset.Data[0]); // Critical: 2개
            Assert.Equal(3.0, dataset.Data[1]); // High: 3개
            Assert.Equal(4.0, dataset.Data[2]); // Medium: 4개
            Assert.Equal(5.0, dataset.Data[3]); // Low: 5개
        }

        [Fact]
        public void BuildViolationPieData_OnlyCritical_ShowsOnlyOneCategory()
        {
            // Arrange
            var session = CreateTestSession(50.0, 5, 0, 0, 0);

            // Act
            var chartData = _builder.BuildViolationPieData(session);

            // Assert
            Assert.Single(chartData.Data.Labels);
            Assert.Equal("Critical", chartData.Data.Labels[0]);
            Assert.Equal(5.0, chartData.Data.Datasets[0].Data[0]);
        }

        [Fact]
        public void BuildViolationPieData_NoViolations_ReturnsEmptyChart()
        {
            // Arrange
            var session = CreateTestSession(100.0, 0, 0, 0, 0);

            // Act
            var chartData = _builder.BuildViolationPieData(session);

            // Assert
            Assert.NotNull(chartData);
            Assert.Equal("pie", chartData.Type);
            Assert.Empty(chartData.Data.Labels);
            Assert.Empty(chartData.Data.Datasets[0].Data);
        }

        [Fact]
        public void BuildViolationPieData_HasCorrectColors()
        {
            // Arrange
            var session = CreateTestSession(70.0, 1, 1, 1, 1);

            // Act
            var chartData = _builder.BuildViolationPieData(session);

            // Assert
            var colors = (List<string>)chartData.Data.Datasets[0].BackgroundColor!;
            Assert.Contains("#dc3545", colors); // Critical - 빨강
            Assert.Contains("#fd7e14", colors); // High - 주황
            Assert.Contains("#ffc107", colors); // Medium - 노랑
            Assert.Contains("#0dcaf0", colors); // Low - 파랑
        }

        [Fact]
        public void BuildConstitutionRadarData_NullSession_ReturnsEmptyChart()
        {
            // Act
            var chartData = _builder.BuildConstitutionRadarData(null!);

            // Assert
            Assert.NotNull(chartData);
            Assert.Equal("radar", chartData.Type);
            Assert.Empty(chartData.Data.Labels);
        }

        [Fact]
        public void BuildViolationPieData_NullSession_ReturnsEmptyChart()
        {
            // Act
            var chartData = _builder.BuildViolationPieData(null!);

            // Assert
            Assert.NotNull(chartData);
            Assert.Equal("pie", chartData.Type);
            Assert.Empty(chartData.Data.Labels);
        }
    }
}
