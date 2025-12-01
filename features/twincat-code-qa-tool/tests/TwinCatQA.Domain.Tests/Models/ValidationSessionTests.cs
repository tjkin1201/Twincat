using FluentAssertions;
using TwinCatQA.Domain.Models;
using Xunit;

namespace TwinCatQA.Domain.Tests.Models;

/// <summary>
/// ValidationSession 도메인 모델 테스트
/// </summary>
public class ValidationSessionTests
{
    #region Complete 메서드 테스트

    /// <summary>
    /// Complete 호출 시 EndTime과 Duration이 올바르게 설정되는지 검증
    /// </summary>
    [Fact]
    public void Complete_ShouldSetEndTimeAndDuration()
    {
        // Arrange (준비)
        var session = new ValidationSession
        {
            ProjectPath = @"D:\Projects\TestProject\TestProject.tsproj",
            ProjectName = "TestProject",
            Mode = ValidationMode.Full
        };

        // 테스트 파일 추가
        session.ScannedFiles.Add(new CodeFile
        {
            FilePath = @"D:\Projects\TestProject\FB_Motor.TcPOU",
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 150
        });

        session.ScannedFiles.Add(new CodeFile
        {
            FilePath = @"D:\Projects\TestProject\FB_Valve.TcPOU",
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 80
        });

        var startTime = session.StartTime;

        // Act (실행)
        session.Complete();

        // Assert (검증)
        session.EndTime.Should().NotBeNull("Complete 호출 시 EndTime이 설정되어야 함");
        session.EndTime.Should().BeAfter(startTime, "EndTime은 StartTime 이후여야 함");
        session.Duration.Should().BeGreaterThan(TimeSpan.Zero, "Duration은 양수여야 함");
        session.ScannedFilesCount.Should().Be(2, "스캔된 파일 개수가 설정되어야 함");
        session.TotalLinesOfCode.Should().Be(230, "총 라인 수가 합산되어야 함");
    }

    /// <summary>
    /// Complete 호출 없이 Duration 조회 시 현재 시간 기준 계산 검증
    /// </summary>
    [Fact]
    public void Duration_WithoutComplete_ShouldCalculateFromCurrentTime()
    {
        // Arrange
        var session = new ValidationSession
        {
            ProjectPath = @"D:\Projects\TestProject\TestProject.tsproj",
            ProjectName = "TestProject",
            Mode = ValidationMode.Full
        };

        // 약간의 시간 경과 시뮬레이션
        Thread.Sleep(100);

        // Act
        var duration = session.Duration;

        // Assert
        duration.Should().BeGreaterThan(TimeSpan.FromMilliseconds(50),
            "Complete 호출 전에도 Duration은 현재 시간 기준으로 계산되어야 함");
    }

    #endregion

    #region CalculateQualityScore 메서드 테스트

    /// <summary>
    /// 위반 사항이 있을 때 품질 점수를 올바르게 계산하는지 검증
    /// </summary>
    [Fact]
    public void CalculateQualityScore_WithViolations_ShouldReturnCorrectScore()
    {
        // Arrange
        var session = new ValidationSession
        {
            ProjectPath = @"D:\Projects\TestProject\TestProject.tsproj",
            ProjectName = "TestProject",
            Mode = ValidationMode.Full
        };

        // 파일 추가 (2개)
        session.ScannedFiles.Add(new CodeFile
        {
            FilePath = @"D:\Projects\TestProject\FB_Motor.TcPOU",
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 150
        });

        session.ScannedFiles.Add(new CodeFile
        {
            FilePath = @"D:\Projects\TestProject\FB_Valve.TcPOU",
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 80
        });

        // 위반 사항 추가
        // Critical: 10점, High: 5점, Medium: 2점, Low: 1점
        session.Violations.Add(new Violation
        {
            RuleId = "FR-4-COMPLEXITY",
            RuleName = "복잡도 초과",
            Severity = ViolationSeverity.Critical, // -10점
            FilePath = @"D:\Projects\TestProject\FB_Motor.TcPOU",
            Line = 45
        });

        session.Violations.Add(new Violation
        {
            RuleId = "FR-1-KOREAN-COMMENT",
            RuleName = "한글 주석 위반",
            Severity = ViolationSeverity.High, // -5점
            FilePath = @"D:\Projects\TestProject\FB_Motor.TcPOU",
            Line = 12
        });

        session.Violations.Add(new Violation
        {
            RuleId = "FR-5-NAMING",
            RuleName = "명명 규칙 위반",
            Severity = ViolationSeverity.Medium, // -2점
            FilePath = @"D:\Projects\TestProject\FB_Valve.TcPOU",
            Line = 8
        });

        // Act
        session.CalculateQualityScore();

        // Assert
        // 총 페널티: 10 + 5 + 2 = 17점
        // 파일당 페널티: 17 / 2 = 8.5점
        // 품질 점수: 100 - 8.5 = 91.5점
        session.OverallQualityScore.Should().Be(91.5,
            "품질 점수는 100에서 (페널티 / 파일 수)를 뺀 값이어야 함");
    }

    /// <summary>
    /// 파일이 없을 때 품질 점수는 0이어야 함
    /// </summary>
    [Fact]
    public void CalculateQualityScore_WithNoFiles_ShouldReturnZero()
    {
        // Arrange
        var session = new ValidationSession
        {
            ProjectPath = @"D:\Projects\TestProject\TestProject.tsproj",
            ProjectName = "TestProject",
            Mode = ValidationMode.Full
        };

        // Act
        session.CalculateQualityScore();

        // Assert
        session.OverallQualityScore.Should().Be(0, "파일이 없으면 품질 점수는 0이어야 함");
    }

    /// <summary>
    /// 위반 사항이 없을 때 품질 점수는 100이어야 함
    /// </summary>
    [Fact]
    public void CalculateQualityScore_WithNoViolations_ShouldReturn100()
    {
        // Arrange
        var session = new ValidationSession
        {
            ProjectPath = @"D:\Projects\TestProject\TestProject.tsproj",
            ProjectName = "TestProject",
            Mode = ValidationMode.Full
        };

        session.ScannedFiles.Add(new CodeFile
        {
            FilePath = @"D:\Projects\TestProject\FB_Perfect.TcPOU",
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 100
        });

        // Act
        session.CalculateQualityScore();

        // Assert
        session.OverallQualityScore.Should().Be(100, "위반 사항이 없으면 만점이어야 함");
    }

    /// <summary>
    /// 품질 점수는 최소 0점 이상이어야 함 (음수 방지)
    /// </summary>
    [Fact]
    public void CalculateQualityScore_WithManyViolations_ShouldNotGoBelowZero()
    {
        // Arrange
        var session = new ValidationSession
        {
            ProjectPath = @"D:\Projects\TestProject\TestProject.tsproj",
            ProjectName = "TestProject",
            Mode = ValidationMode.Full
        };

        session.ScannedFiles.Add(new CodeFile
        {
            FilePath = @"D:\Projects\TestProject\FB_Bad.TcPOU",
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 100
        });

        // 대량의 Critical 위반 추가 (페널티가 100점을 초과하도록)
        for (int i = 0; i < 20; i++)
        {
            session.Violations.Add(new Violation
            {
                RuleId = $"RULE-{i}",
                RuleName = $"위반 {i}",
                Severity = ViolationSeverity.Critical, // 각 -10점
                FilePath = @"D:\Projects\TestProject\FB_Bad.TcPOU",
                Line = i + 1
            });
        }

        // Act
        session.CalculateQualityScore();

        // Assert
        session.OverallQualityScore.Should().BeGreaterOrEqualTo(0,
            "품질 점수는 음수가 될 수 없음");
    }

    #endregion

    #region CalculateConstitutionCompliance 메서드 테스트

    /// <summary>
    /// 헌장 원칙별 준수율을 올바르게 계산하는지 검증
    /// </summary>
    [Fact]
    public void CalculateConstitutionCompliance_ShouldReturnCorrectRatio()
    {
        // Arrange
        var session = new ValidationSession
        {
            ProjectPath = @"D:\Projects\TestProject\TestProject.tsproj",
            ProjectName = "TestProject",
            Mode = ValidationMode.Full
        };

        // 파일 3개 추가
        for (int i = 1; i <= 3; i++)
        {
            session.ScannedFiles.Add(new CodeFile
            {
                FilePath = $@"D:\Projects\TestProject\FB_Test{i}.TcPOU",
                Type = FileType.POU,
                Language = ProgrammingLanguage.ST,
                LineCount = 100
            });
        }

        // 헌장 원칙 1 (한글 우선) 위반 2건 추가
        session.Violations.Add(new Violation
        {
            RuleId = "FR-1-KOREAN",
            RuleName = "한글 주석 위반",
            RelatedPrinciple = ConstitutionPrinciple.KoreanFirst,
            Severity = ViolationSeverity.High,
            FilePath = @"D:\Projects\TestProject\FB_Test1.TcPOU",
            Line = 10
        });

        session.Violations.Add(new Violation
        {
            RuleId = "FR-1-KOREAN",
            RuleName = "한글 주석 위반",
            RelatedPrinciple = ConstitutionPrinciple.KoreanFirst,
            Severity = ViolationSeverity.High,
            FilePath = @"D:\Projects\TestProject\FB_Test2.TcPOU",
            Line = 15
        });

        // 헌장 원칙 5 (명명 규칙) 위반 1건 추가
        session.Violations.Add(new Violation
        {
            RuleId = "FR-5-NAMING",
            RuleName = "명명 규칙 위반",
            RelatedPrinciple = ConstitutionPrinciple.NamingConvention,
            Severity = ViolationSeverity.Medium,
            FilePath = @"D:\Projects\TestProject\FB_Test3.TcPOU",
            Line = 5
        });

        // Act
        session.CalculateConstitutionCompliance();

        // Assert
        // 원칙 1 (한글 우선): 3개 파일 중 2개 위반 = 1 - (2/3) = 0.333...
        session.ConstitutionCompliance.Should().ContainKey(ConstitutionPrinciple.KoreanFirst);
        session.ConstitutionCompliance[ConstitutionPrinciple.KoreanFirst]
            .Should().BeApproximately(0.333, 0.01,
                "원칙 1은 3개 파일 중 2개 위반이므로 약 33.3% 준수율");

        // 원칙 5 (명명 규칙): 3개 파일 중 1개 위반 = 1 - (1/3) = 0.666...
        session.ConstitutionCompliance.Should().ContainKey(ConstitutionPrinciple.NamingConvention);
        session.ConstitutionCompliance[ConstitutionPrinciple.NamingConvention]
            .Should().BeApproximately(0.667, 0.01,
                "원칙 5는 3개 파일 중 1개 위반이므로 약 66.7% 준수율");

        // 위반이 없는 다른 원칙들은 100% 준수율
        session.ConstitutionCompliance.Should().ContainKey(ConstitutionPrinciple.RealTimeSafety);
        session.ConstitutionCompliance[ConstitutionPrinciple.RealTimeSafety]
            .Should().Be(1.0, "위반이 없는 원칙은 100% 준수율");
    }

    /// <summary>
    /// 위반 사항이 없을 때 모든 원칙의 준수율이 100%인지 검증
    /// </summary>
    [Fact]
    public void CalculateConstitutionCompliance_WithNoViolations_ShouldReturnAllOnes()
    {
        // Arrange
        var session = new ValidationSession
        {
            ProjectPath = @"D:\Projects\TestProject\TestProject.tsproj",
            ProjectName = "TestProject",
            Mode = ValidationMode.Full
        };

        session.ScannedFiles.Add(new CodeFile
        {
            FilePath = @"D:\Projects\TestProject\FB_Perfect.TcPOU",
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 100
        });

        // Act
        session.CalculateConstitutionCompliance();

        // Assert
        // None을 제외한 모든 원칙은 1.0 (100%)
        session.ConstitutionCompliance.Should().NotContainKey(ConstitutionPrinciple.None);

        foreach (var principle in session.ConstitutionCompliance.Keys)
        {
            session.ConstitutionCompliance[principle].Should().Be(1.0,
                $"위반이 없으면 모든 원칙의 준수율은 100%여야 함 (원칙: {principle})");
        }
    }

    /// <summary>
    /// 준수율이 최소 0 이상 최대 1 이하인지 검증
    /// </summary>
    [Fact]
    public void CalculateConstitutionCompliance_ShouldBeBetweenZeroAndOne()
    {
        // Arrange
        var session = new ValidationSession
        {
            ProjectPath = @"D:\Projects\TestProject\TestProject.tsproj",
            ProjectName = "TestProject",
            Mode = ValidationMode.Full
        };

        session.ScannedFiles.Add(new CodeFile
        {
            FilePath = @"D:\Projects\TestProject\FB_Test.TcPOU",
            Type = FileType.POU,
            Language = ProgrammingLanguage.ST,
            LineCount = 100
        });

        // 대량의 위반 추가
        for (int i = 0; i < 10; i++)
        {
            session.Violations.Add(new Violation
            {
                RuleId = $"RULE-{i}",
                RuleName = $"위반 {i}",
                RelatedPrinciple = ConstitutionPrinciple.KoreanFirst,
                Severity = ViolationSeverity.High,
                FilePath = @"D:\Projects\TestProject\FB_Test.TcPOU",
                Line = i + 1
            });
        }

        // Act
        session.CalculateConstitutionCompliance();

        // Assert
        foreach (var compliance in session.ConstitutionCompliance.Values)
        {
            compliance.Should().BeInRange(0.0, 1.0,
                "준수율은 0 이상 1 이하여야 함");
        }
    }

    #endregion

    #region ViolationsBySeverity 속성 테스트

    /// <summary>
    /// 심각도별 위반 수를 올바르게 집계하는지 검증
    /// </summary>
    [Fact]
    public void ViolationsBySeverity_ShouldGroupBySeverityCorrectly()
    {
        // Arrange
        var session = new ValidationSession
        {
            ProjectPath = @"D:\Projects\TestProject\TestProject.tsproj",
            ProjectName = "TestProject",
            Mode = ValidationMode.Full
        };

        // 다양한 심각도의 위반 추가
        session.Violations.Add(new Violation
        {
            RuleId = "RULE-1",
            Severity = ViolationSeverity.Critical,
            FilePath = "test.pou",
            Line = 1
        });

        session.Violations.Add(new Violation
        {
            RuleId = "RULE-2",
            Severity = ViolationSeverity.Critical,
            FilePath = "test.pou",
            Line = 2
        });

        session.Violations.Add(new Violation
        {
            RuleId = "RULE-3",
            Severity = ViolationSeverity.High,
            FilePath = "test.pou",
            Line = 3
        });

        session.Violations.Add(new Violation
        {
            RuleId = "RULE-4",
            Severity = ViolationSeverity.Medium,
            FilePath = "test.pou",
            Line = 4
        });

        session.Violations.Add(new Violation
        {
            RuleId = "RULE-5",
            Severity = ViolationSeverity.Medium,
            FilePath = "test.pou",
            Line = 5
        });

        session.Violations.Add(new Violation
        {
            RuleId = "RULE-6",
            Severity = ViolationSeverity.Medium,
            FilePath = "test.pou",
            Line = 6
        });

        session.Violations.Add(new Violation
        {
            RuleId = "RULE-7",
            Severity = ViolationSeverity.Low,
            FilePath = "test.pou",
            Line = 7
        });

        // Act
        var violationsBySeverity = session.ViolationsBySeverity;

        // Assert
        violationsBySeverity.Should().ContainKey(ViolationSeverity.Critical)
            .WhoseValue.Should().Be(2, "Critical 위반 2건");
        violationsBySeverity.Should().ContainKey(ViolationSeverity.High)
            .WhoseValue.Should().Be(1, "High 위반 1건");
        violationsBySeverity.Should().ContainKey(ViolationSeverity.Medium)
            .WhoseValue.Should().Be(3, "Medium 위반 3건");
        violationsBySeverity.Should().ContainKey(ViolationSeverity.Low)
            .WhoseValue.Should().Be(1, "Low 위반 1건");
    }

    #endregion

    #region 생성자 및 기본값 테스트

    /// <summary>
    /// ValidationSession 생성 시 기본값이 올바르게 설정되는지 검증
    /// </summary>
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var session = new ValidationSession
        {
            ProjectPath = @"D:\Projects\TestProject\TestProject.tsproj",
            ProjectName = "TestProject",
            Mode = ValidationMode.Full
        };

        // Assert
        session.SessionId.Should().NotBeEmpty("SessionId는 자동 생성되어야 함");
        session.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1),
            "StartTime은 생성 시점이어야 함");
        session.EndTime.Should().BeNull("초기에는 EndTime이 null이어야 함");
        session.ScannedFiles.Should().NotBeNull().And.BeEmpty("ScannedFiles는 빈 리스트로 초기화되어야 함");
        session.Violations.Should().NotBeNull().And.BeEmpty("Violations는 빈 리스트로 초기화되어야 함");
        session.ConstitutionCompliance.Should().NotBeNull().And.BeEmpty(
            "ConstitutionCompliance는 빈 딕셔너리로 초기화되어야 함");
        session.ExecutedBy.Should().Be(Environment.UserName, "ExecutedBy는 현재 사용자명이어야 함");
    }

    #endregion
}
