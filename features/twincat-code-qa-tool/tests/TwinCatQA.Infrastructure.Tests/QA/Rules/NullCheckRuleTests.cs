using FluentAssertions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Infrastructure.QA.Rules;

namespace TwinCatQA.Infrastructure.Tests.QA.Rules;

/// <summary>
/// QA004 NULL 포인터 검사 누락 감지 규칙 테스트
/// </summary>
public class NullCheckRuleTests
{
    private readonly NullCheckRule _rule;

    public NullCheckRuleTests()
    {
        _rule = new NullCheckRule();
    }

    #region 긍정 테스트 (위반 감지)

    [Fact]
    public void CheckLogicChange_NULL체크없는포인터역참조_Critical이슈반환()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "ProcessData",
            NewCode = "value := dataPtr^;",
            FilePath = "Test.TcPOU",
            StartLine = 10
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Critical);
        issues[0].RuleId.Should().Be("QA004");
        issues[0].Title.Should().Contain("NULL 체크 없이 포인터 사용");
        issues[0].Description.Should().Contain("dataPtr");
    }

    [Fact]
    public void CheckLogicChange_NULL체크없는레퍼런스접근_Critical이슈반환()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Modified,
            ElementName = "AccessMember",
            NewCode = "value := refData.member;",
            FilePath = "Test.TcPOU",
            StartLine = 15
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Critical);
        issues[0].Description.Should().Contain("런타임 오류 가능");
    }

    [Fact]
    public void CheckLogicChange_여러포인터역참조_여러Critical이슈반환()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "ProcessMultiple",
            NewCode = @"
                value1 := ptr1^;
                value2 := ptr2^;
                value3 := ptr3^;
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
    public void CheckLogicChange_구조체멤버접근_Critical이슈반환()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "AccessStruct",
            NewCode = "temperature := sensorData.temperature;",
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
    public void CheckLogicChange_NULL체크포함된코드_이슈없음()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "SafeAccess",
            NewCode = @"
                IF dataPtr <> NULL THEN
                    value := dataPtr^;
                END_IF
            ",
            FilePath = "Test.TcPOU",
            StartLine = 30
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void CheckLogicChange_ISVALIDREF사용_이슈없음()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "SafeRefAccess",
            NewCode = @"
                IF __ISVALIDREF(refData) THEN
                    value := refData.member;
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
    public void CheckLogicChange_포인터사용없음_이슈없음()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "NoPointerUsage",
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
            OldCode = "value := dataPtr^;",
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
    public void CheckLogicChange_중첩포인터역참조_Critical이슈반환()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "NestedPointer",
            NewCode = "value := ptrToPtr^^;",
            FilePath = "Test.TcPOU",
            StartLine = 55
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().HaveCountGreaterThan(0);
        issues.Should().AllSatisfy(i => i.Severity.Should().Be(Severity.Critical));
    }

    [Fact]
    public void CheckLogicChange_체인멤버접근_Critical이슈반환()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "ChainedAccess",
            NewCode = "value := obj.subObj.value;",
            FilePath = "Test.TcPOU",
            StartLine = 60
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().HaveCount(1); // obj.subObj 검출
        issues[0].Severity.Should().Be(Severity.Critical);
    }

    [Fact]
    public void CheckLogicChange_NULL체크0사용_이슈없음()
    {
        // Arrange
        var change = new LogicChange
        {
            ChangeType = ChangeType.Added,
            ElementName = "NullCheckZero",
            NewCode = @"
                IF dataPtr <> 0 THEN
                    value := dataPtr^;
                END_IF
            ",
            FilePath = "Test.TcPOU",
            StartLine = 65
        };

        // Act
        var issues = _rule.CheckLogicChange(change);

        // Assert
        issues.Should().BeEmpty();
    }

    #endregion

    #region 데이터 타입 변경 테스트

    [Fact]
    public void CheckDataTypeChange_포인터타입으로변경_Warning이슈반환()
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
                    FieldName = "dataField",
                    OldDataType = "INT",
                    NewDataType = "POINTER TO INT"
                }
            }
        };

        // Act
        var issues = _rule.CheckDataTypeChange(change);

        // Assert
        issues.Should().HaveCount(1);
        issues[0].Severity.Should().Be(Severity.Warning);
        issues[0].Description.Should().Contain("포인터/레퍼런스 타입으로 변경");
    }

    [Fact]
    public void CheckDataTypeChange_레퍼런스타입으로변경_Warning이슈반환()
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
                    FieldName = "refField",
                    OldDataType = "DINT",
                    NewDataType = "REFERENCE TO ST_Data"
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
    public void CheckDataTypeChange_일반타입변경_이슈없음()
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
                    FieldName = "normalField",
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

    #endregion

    #region 변수 변경 테스트

    [Fact]
    public void CheckVariableChange_변수변경_이슈없음()
    {
        // Arrange
        var change = new VariableChange
        {
            ChangeType = ChangeType.Added,
            VariableName = "newPtr",
            NewDataType = "POINTER TO INT",
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
        _rule.RuleId.Should().Be("QA004");
        _rule.RuleName.Should().Be("NULL 포인터 검사 누락");
        _rule.Severity.Should().Be(Severity.Critical);
        _rule.Description.Should().NotBeNullOrEmpty();
    }

    #endregion
}
