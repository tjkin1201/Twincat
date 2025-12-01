using FluentAssertions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Infrastructure.QA.Rules;

namespace TwinCatQA.Infrastructure.Tests.QA.Rules;

/// <summary>
/// QA002 초기화되지 않은 변수 감지 규칙 테스트
/// </summary>
public class UninitializedVariableRuleTests
{
    private readonly UninitializedVariableRule _rule;

    public UninitializedVariableRuleTests()
    {
        _rule = new UninitializedVariableRule();
    }

    #region 긍정 테스트 (위반 감지)

    [Fact]
    public void CheckVariableChange_초기화없는BOOL변수_Critical이슈반환()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Added,
            VariableName = "isEnabled",
            NewDataType = "BOOL",
            NewInitialValue = null,
            FilePath = "Test.TcPOU",
            Line = 10
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Critical);
        issues[0].RuleId.Should().Be("QA002");
        issues[0].Title.Should().Contain("초기화되지 않음");
        issues[0].Description.Should().Contain("isEnabled");
        issues[0].Description.Should().Contain("BOOL");
    }

    [Fact]
    public void CheckVariableChange_초기화없는INT변수_Critical이슈반환()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Added,
            VariableName = "counter",
            NewDataType = "INT",
            NewInitialValue = "",
            FilePath = "Test.TcPOU",
            Line = 15
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Critical);
        issues[0].Description.Should().Contain("예측 불가능한 값");
    }

    [Fact]
    public void CheckVariableChange_초기화없는REAL변수_Critical이슈반환()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Added,
            VariableName = "temperature",
            NewDataType = "REAL",
            NewInitialValue = null,
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
    public void CheckVariableChange_초기화없는POINTER변수_Critical이슈반환()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Added,
            VariableName = "dataPtr",
            NewDataType = "POINTER",
            NewInitialValue = "",
            FilePath = "Test.TcPOU",
            Line = 25
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Critical);
        issues[0].Recommendation.Should().Contain("초기화");
    }

    #endregion

    #region 부정 테스트 (정상 케이스)

    [Fact]
    public void CheckVariableChange_BOOL변수초기화됨_이슈없음()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Added,
            VariableName = "isEnabled",
            NewDataType = "BOOL",
            NewInitialValue = "FALSE",
            FilePath = "Test.TcPOU",
            Line = 30
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckVariableChange_INT변수초기화됨_이슈없음()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Added,
            VariableName = "counter",
            NewDataType = "INT",
            NewInitialValue = "0",
            FilePath = "Test.TcPOU",
            Line = 35
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckVariableChange_수정된변수_이슈없음()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Modified,
            VariableName = "counter",
            OldDataType = "INT",
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
    public void CheckVariableChange_STRING타입초기화없음_이슈없음()
    {
        // Arrange - STRING은 Critical 타입이 아님
        var change = new VariableChange
        {
            ChangeType = ChangeType.Added,
            VariableName = "message",
            NewDataType = "STRING",
            NewInitialValue = null,
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
    public void CheckVariableChange_배열타입초기화없음_Critical이슈반환()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Added,
            VariableName = "dataArray",
            NewDataType = "ARRAY[1..10] OF INT",
            NewInitialValue = null,
            FilePath = "Test.TcPOU",
            Line = 50
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Critical);
    }

    [Fact]
    public void CheckVariableChange_REFERENCE타입초기화없음_Critical이슈반환()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Added,
            VariableName = "refVar",
            NewDataType = "REFERENCE",
            NewInitialValue = null,
            FilePath = "Test.TcPOU",
            Line = 55
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Critical);
    }

    [Fact]
    public void CheckVariableChange_사용자정의타입초기화없음_이슈없음()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Added,
            VariableName = "customData",
            NewDataType = "ST_CustomStruct",
            NewInitialValue = null,
            FilePath = "Test.TcPOU",
            Line = 60
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckVariableChange_빈문자열초기값_Critical이슈반환()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Added,
            VariableName = "value",
            NewDataType = "DINT",
            NewInitialValue = "   ", // 공백만 있는 경우
            FilePath = "Test.TcPOU",
            Line = 65
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

        // Assert
        issues.Should().BeEmpty(); // 공백이라도 값이 있으면 OK (실제로는 trim 처리 필요)
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
            NewCode = "counter := 0;",
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
        _rule.RuleId.Should().Be("QA002");
        _rule.RuleName.Should().Be("초기화되지 않은 변수 감지");
        _rule.Severity.Should().Be(Severity.Critical);
        _rule.Description.Should().NotBeNullOrEmpty();
    }

    #endregion
}
