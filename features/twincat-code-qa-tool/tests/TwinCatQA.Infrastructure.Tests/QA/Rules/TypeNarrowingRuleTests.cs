using FluentAssertions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Infrastructure.QA.Rules;

namespace TwinCatQA.Infrastructure.Tests.QA.Rules;

/// <summary>
/// QA001 타입 축소 감지 규칙 테스트
/// </summary>
public class TypeNarrowingRuleTests
{
    private readonly TypeNarrowingRule _rule;

    public TypeNarrowingRuleTests()
    {
        _rule = new TypeNarrowingRule();
    }

    #region 긍정 테스트 (위반 감지)

    [Fact]
    public void CheckVariableChange_DINT에서INT로축소_Critical이슈반환()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Modified,
            VariableName = "counter",
            OldDataType = "DINT",
            NewDataType = "INT",
            FilePath = "Test.TcPOU",
            Line = 10
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Critical);
        issues[0].RuleId.Should().Be("QA001");
        issues[0].Title.Should().Contain("타입 축소");
        issues[0].Description.Should().Contain("DINT");
        issues[0].Description.Should().Contain("INT");
    }

    [Fact]
    public void CheckVariableChange_LINT에서DINT로축소_Critical이슈반환()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Modified,
            VariableName = "largeCounter",
            OldDataType = "LINT",
            NewDataType = "DINT",
            FilePath = "Test.TcPOU",
            Line = 15
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Critical);
        issues[0].Description.Should().Contain("값 범위 초과 가능");
    }

    [Fact]
    public void CheckVariableChange_LREAL에서REAL로축소_Critical이슈반환()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Modified,
            VariableName = "temperature",
            OldDataType = "LREAL",
            NewDataType = "REAL",
            FilePath = "Test.TcPOU",
            Line = 20
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Critical);
        issues[0].FilePath.Should().Be("Test.TcPOU");
        issues[0].Line.Should().Be(20);
    }

    [Fact]
    public void CheckVariableChange_배열타입DINT에서INT로축소_Critical이슈반환()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Modified,
            VariableName = "dataArray",
            OldDataType = "ARRAY[1..10] OF DINT",
            NewDataType = "ARRAY[1..10] OF INT",
            FilePath = "Test.TcPOU",
            Line = 25
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Critical);
        issues[0].Description.Should().Contain("DINT");
        issues[0].Description.Should().Contain("INT");
    }

    #endregion

    #region 부정 테스트 (정상 케이스)

    [Fact]
    public void CheckVariableChange_INT에서DINT로확장_이슈없음()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Modified,
            VariableName = "counter",
            OldDataType = "INT",
            NewDataType = "DINT",
            FilePath = "Test.TcPOU",
            Line = 30
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckVariableChange_REAL에서LREAL로확장_이슈없음()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Modified,
            VariableName = "value",
            OldDataType = "REAL",
            NewDataType = "LREAL",
            FilePath = "Test.TcPOU",
            Line = 35
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckVariableChange_타입변경없음_이슈없음()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Modified,
            VariableName = "counter",
            OldDataType = "DINT",
            NewDataType = "DINT",
            FilePath = "Test.TcPOU",
            Line = 40
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckVariableChange_새로추가된변수_이슈없음()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Added,
            VariableName = "newVar",
            NewDataType = "INT",
            FilePath = "Test.TcPOU",
            Line = 45
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    #endregion

    #region 엣지 케이스

    [Fact]
    public void CheckVariableChange_알수없는타입변경_이슈없음()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Modified,
            VariableName = "customVar",
            OldDataType = "T_CustomType",
            NewDataType = "T_AnotherType",
            FilePath = "Test.TcPOU",
            Line = 50
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckVariableChange_타입정보없음_이슈없음()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Modified,
            VariableName = "unknownVar",
            OldDataType = null,
            NewDataType = null,
            FilePath = "Test.TcPOU",
            Line = 55
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckVariableChange_부호있는타입에서부호없는타입으로_이슈없음()
    {
        // Arrange - 같은 크기지만 부호만 다른 경우
        var change = new VariableChange
        {
            ChangeType = ChangeType.Modified,
            VariableName = "value",
            OldDataType = "DINT",
            NewDataType = "UDINT",
            FilePath = "Test.TcPOU",
            Line = 60
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

        // Assert
        issues.Should().BeEmpty(); // 같은 비트 크기이므로 축소 아님
    }

    #endregion

    #region 로직 변경 및 데이터 타입 변경

    [Fact]
    public void CheckLogicChange_로직변경_이슈없음()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Modified,
            ElementName = "TestFunction",
            NewCode = "counter := counter + 1;",
            FilePath = "Test.TcPOU",
            StartLine = 10
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckDataTypeChange_데이터타입변경_이슈없음()
    {
        // Arrange
        var change = new DataTypeChange
        {
            ChangeType = ChangeType.Modified,
            TypeName = "ST_TestStruct",
            Kind = DataTypeKind.Struct,
            FilePath = "Test.TcDUT",
            Line = 5
        };

        // Act
        var issues = _rule.CheckDataTypeChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    #endregion

    #region 규칙 메타데이터 테스트

    [Fact]
    public void RuleMetadata_올바른정보반환()
    {
        // Assert
        _rule.RuleId.Should().Be("QA001");
        _rule.RuleName.Should().Be("타입 축소 감지");
        _rule.Severity.Should().Be(Severity.Critical);
        _rule.Description.Should().NotBeNullOrEmpty();
    }

    #endregion
}
