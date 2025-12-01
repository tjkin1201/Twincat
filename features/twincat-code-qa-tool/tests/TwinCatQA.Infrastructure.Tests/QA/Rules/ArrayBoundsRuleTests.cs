using FluentAssertions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Infrastructure.QA.Rules;

namespace TwinCatQA.Infrastructure.Tests.QA.Rules;

/// <summary>
/// QA003 배열 범위 검사 누락 감지 규칙 테스트
/// </summary>
public class ArrayBoundsRuleTests
{
    private readonly ArrayBoundsRule _rule;

    public ArrayBoundsRuleTests()
    {
        _rule = new ArrayBoundsRule();
    }

    #region 긍정 테스트 (위반 감지)

    [Fact]
    public void CheckLogicChange_범위체크없는배열접근_Critical이슈반환()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "ProcessData",
            NewCode = "value := dataArray[index];",
            FilePath = "Test.TcPOU",
            StartLine = 10
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Critical);
        issues[0].RuleId.Should().Be("QA003");
        issues[0].Title.Should().Contain("배열 범위 검사 없이 접근");
        issues[0].Description.Should().Contain("dataArray[index]");
    }

    [Fact]
    public void CheckLogicChange_복잡한인덱스표현식_Critical이슈반환()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Modified,
            ElementName = "CalculateValue",
            NewCode = "result := values[counter + offset];",
            FilePath = "Test.TcPOU",
            StartLine = 15
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Critical);
        issues[0].Description.Should().Contain("메모리 오류 가능");
    }

    [Fact]
    public void CheckLogicChange_여러배열접근_여러Critical이슈반환()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "ProcessMultiple",
            NewCode = @"
                value1 := array1[idx1];
                value2 := array2[idx2];
                value3 := array3[idx3];
            ",
            FilePath = "Test.TcPOU",
            StartLine = 20
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().HaveCount(3);
        issues.Should().AllSatisfy(i => i.Severity.Should().Be(Severity.Critical));
    }

    [Fact]
    public void CheckLogicChange_변수인덱스사용_Critical이슈반환()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "DynamicAccess",
            NewCode = "currentValue := buffer[currentIndex];",
            FilePath = "Test.TcPOU",
            StartLine = 25
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Critical);
        issues[0].FilePath.Should().Be("Test.TcPOU");
        issues[0].Line.Should().Be(25);
    }

    #endregion

    #region 부정 테스트 (정상 케이스)

    [Fact]
    public void CheckLogicChange_상수인덱스사용_이슈없음()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "StaticAccess",
            NewCode = "value := dataArray[5];",
            FilePath = "Test.TcPOU",
            StartLine = 30
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckLogicChange_범위체크포함된코드_이슈없음()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "SafeAccess",
            NewCode = @"
                IF index >= 1 AND index <= 10 THEN
                    value := dataArray[index];
                END_IF
            ",
            FilePath = "Test.TcPOU",
            StartLine = 35
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckLogicChange_배열접근없음_이슈없음()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "NoArrayAccess",
            NewCode = "counter := counter + 1;",
            FilePath = "Test.TcPOU",
            StartLine = 40
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckLogicChange_삭제된로직_이슈없음()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Removed,
            ElementName = "OldFunction",
            OldCode = "value := dataArray[index];",
            FilePath = "Test.TcPOU",
            StartLine = 45
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
            StartLine = 50
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckLogicChange_중첩배열접근_Critical이슈반환()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "NestedArray",
            NewCode = "value := matrix[row][col];",
            FilePath = "Test.TcPOU",
            StartLine = 55
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().HaveCount(1); // matrix[row]만 검출 (첫 번째 레벨)
        issues[0].Severity.Should().Be(Severity.Critical);
    }

    [Fact]
    public void CheckLogicChange_문자열인덱싱_Critical이슈반환()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "StringAccess",
            NewCode = "char := message[position];",
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
    public void CheckLogicChange_산술연산인덱스_Critical이슈반환()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "ArithmeticIndex",
            NewCode = "value := data[i * 2 + 1];",
            FilePath = "Test.TcPOU",
            StartLine = 65
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Critical);
    }

    #endregion

    #region 변수 변경 및 데이터 타입 변경

    [Fact]
    public void CheckVariableChange_변수변경_이슈없음()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Added,
            VariableName = "newArray",
            NewDataType = "ARRAY[1..10] OF INT",
            FilePath = "Test.TcPOU",
            Line = 10
        };

        // Act
        var issues = _rule.CheckVariableChange(change);

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
        _rule.RuleId.Should().Be("QA003");
        _rule.RuleName.Should().Be("배열 범위 검사 누락");
        _rule.Severity.Should().Be(Severity.Critical);
        _rule.Description.Should().NotBeNullOrEmpty();
    }

    #endregion
}
