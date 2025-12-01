using FluentAssertions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Infrastructure.QA.Rules;

namespace TwinCatQA.Infrastructure.Tests.QA.Rules;

/// <summary>
/// QA005 부동소수점 직접 비교 감지 규칙 테스트
/// </summary>
public class FloatingPointComparisonRuleTests
{
    private readonly FloatingPointComparisonRule _rule;

    public FloatingPointComparisonRuleTests()
    {
        _rule = new FloatingPointComparisonRule();
    }

    #region 긍정 테스트 (위반 감지)

    [Fact]
    public void CheckLogicChange_부동소수점등호비교_Critical이슈반환()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "CheckTemperature",
            NewCode = "IF temperature = 25.5 THEN",
            FilePath = "Test.TcPOU",
            StartLine = 10
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Critical);
        issues[0].RuleId.Should().Be("QA005");
        issues[0].Title.Should().Contain("부동소수점 직접 비교");
        issues[0].Description.Should().Contain("temperature");
    }

    [Fact]
    public void CheckLogicChange_부동소수점부등비교_Critical이슈반환()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Modified,
            ElementName = "CheckValue",
            NewCode = "IF value <> 0.0 THEN",
            FilePath = "Test.TcPOU",
            StartLine = 15
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Critical);
        issues[0].Description.Should().Contain("오작동 가능");
    }

    [Fact]
    public void CheckLogicChange_과학적표기법비교_Critical이슈반환()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "CheckSmallValue",
            NewCode = "IF epsilon = 1.0E-6 THEN",
            FilePath = "Test.TcPOU",
            StartLine = 20
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Critical);
        issues[0].FilePath.Should().Be("Test.TcPOU");
        issues[0].Line.Should().Be(20);
    }

    [Fact]
    public void CheckLogicChange_WHILE문부동소수점비교_Critical이슈반환()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "WaitLoop",
            NewCode = "WHILE currentSpeed = targetSpeed DO",
            FilePath = "Test.TcPOU",
            StartLine = 25
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Critical);
    }

    #endregion

    #region 부정 테스트 (정상 케이스)

    [Fact]
    public void CheckLogicChange_Epsilon비교사용_이슈없음()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "SafeComparison",
            NewCode = "IF ABS(temperature - setpoint) < 0.1 THEN",
            FilePath = "Test.TcPOU",
            StartLine = 30
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckLogicChange_부등호비교_이슈없음()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "RangeCheck",
            NewCode = "IF temperature >= 20.0 THEN",
            FilePath = "Test.TcPOU",
            StartLine = 35
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckLogicChange_정수비교_이슈없음()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "IntegerComparison",
            NewCode = "IF idx = 100 THEN", // 부동소수점 패턴이 없는 간단한 변수명
            FilePath = "Test.TcPOU",
            StartLine = 40
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().BeEmpty("소수점이나 부동소수점 키워드가 없으면 이슈 없음");
    }

    [Fact]
    public void CheckLogicChange_할당문_이슈없음()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "Assignment",
            NewCode = "temperature := 25.5;",
            FilePath = "Test.TcPOU",
            StartLine = 45
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckLogicChange_비조건문_이슈없음()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "SimpleAssignment",
            NewCode = "value := 10.5;",
            FilePath = "Test.TcPOU",
            StartLine = 50
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    #endregion

    #region 엣지 케이스

    [Fact]
    public void CheckLogicChange_빈코드_이슈없음()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "EmptyFunction",
            NewCode = "",
            FilePath = "Test.TcPOU",
            StartLine = 55
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckLogicChange_ELSIF문부동소수점비교_Critical이슈반환()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "MultiCondition",
            NewCode = @"
                IF condition1 THEN
                    // ...
                ELSIF temperature = 25.0 THEN
                    // ...
                END_IF
            ",
            FilePath = "Test.TcPOU",
            StartLine = 60
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Critical);
    }

    [Fact]
    public void CheckLogicChange_복잡한부동소수점표현식_Critical이슈반환()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "ComplexExpression",
            NewCode = "IF (temp1 + temp2) / 2.0 = average THEN",
            FilePath = "Test.TcPOU",
            StartLine = 65
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().HaveCountGreaterThan(0);
        issues[0].Severity.Should().Be(Severity.Critical);
    }

    [Fact]
    public void CheckLogicChange_변수명에Real포함_Critical이슈반환()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "RealVariableCheck",
            NewCode = "IF realValue = targetReal THEN",
            FilePath = "Test.TcPOU",
            StartLine = 70
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Critical);
    }

    #endregion

    #region 데이터 타입 변경 테스트

    [Fact]
    public void CheckDataTypeChange_부동소수점타입으로변경_Warning이슈반환()
    {
        // Arrange
        var change = new DataTypeChange
        {
            ChangeType = ChangeType.Modified,
            TypeName = "ST_TestStruct",
            Kind = DataTypeKind.Struct,
            FilePath = "Test.TcDUT",
            Line = 5,
            FieldChanges = new List<FieldChange>
            {
                new FieldChange
                {
                    ChangeType = ChangeType.Modified,
                    FieldName = "value",
                    OldDataType = "INT",
                    NewDataType = "REAL"
                }
            }
        };

        // Act
        var issues = _rule.CheckDataTypeChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Warning);
        issues[0].Description.Should().Contain("부동소수점 타입으로 변경");
    }

    [Fact]
    public void CheckDataTypeChange_LREAL타입으로변경_Warning이슈반환()
    {
        // Arrange
        var change = new DataTypeChange
        {
            ChangeType = ChangeType.Modified,
            TypeName = "ST_TestStruct",
            Kind = DataTypeKind.Struct,
            FilePath = "Test.TcDUT",
            Line = 10,
            FieldChanges = new List<FieldChange>
            {
                new FieldChange
                {
                    ChangeType = ChangeType.Modified,
                    FieldName = "preciseValue",
                    OldDataType = "DINT",
                    NewDataType = "LREAL"
                }
            }
        };

        // Act
        var issues = _rule.CheckDataTypeChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Warning);
    }

    [Fact]
    public void CheckDataTypeChange_정수타입변경_이슈없음()
    {
        // Arrange
        var change = new DataTypeChange
        {
            ChangeType = ChangeType.Modified,
            TypeName = "ST_TestStruct",
            Kind = DataTypeKind.Struct,
            FilePath = "Test.TcDUT",
            Line = 15,
            FieldChanges = new List<FieldChange>
            {
                new FieldChange
                {
                    ChangeType = ChangeType.Modified,
                    FieldName = "counter",
                    OldDataType = "INT",
                    NewDataType = "DINT"
                }
            }
        };

        // Act
        var issues = _rule.CheckDataTypeChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckDataTypeChange_부동소수점간변경_이슈없음()
    {
        // Arrange
        var change = new DataTypeChange
        {
            ChangeType = ChangeType.Modified,
            TypeName = "ST_TestStruct",
            Kind = DataTypeKind.Struct,
            FilePath = "Test.TcDUT",
            Line = 20,
            FieldChanges = new List<FieldChange>
            {
                new FieldChange
                {
                    ChangeType = ChangeType.Modified,
                    FieldName = "floatValue",
                    OldDataType = "REAL",
                    NewDataType = "LREAL"
                }
            }
        };

        // Act
        var issues = _rule.CheckDataTypeChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    #endregion

    #region 변수 변경 테스트

    [Fact]
    public void CheckVariableChange_변수변경_이슈없음()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Added,
            VariableName = "temperature",
            NewDataType = "REAL",
            FilePath = "Test.TcPOU",
            Line = 10
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    #endregion

    #region 규칙 메타데이터 테스트

    [Fact]
    public void RuleMetadata_올바른정보반환()
    {
        // Assert
        _rule.RuleId.Should().Be("QA005");
        _rule.RuleName.Should().Be("부동소수점 직접 비교");
        _rule.Severity.Should().Be(Severity.Critical);
        _rule.Description.Should().NotBeNullOrEmpty();
    }

    #endregion
}
