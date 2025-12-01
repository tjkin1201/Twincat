using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;
using TwinCatQA.Infrastructure.Comparison;
using Xunit;

namespace TwinCatQA.Application.Tests.Comparison;

/// <summary>
/// 폴더 비교 기능 테스트
/// </summary>
public class FolderComparerTests
{
    #region 변수 비교 테스트

    [Fact]
    public void VariableComparer_DetectsAddedVariable()
    {
        // Arrange
        var comparer = new VariableComparer();
        var oldFiles = CreateFiles(@"
VAR_GLOBAL
    nOldVar : INT;
END_VAR");
        var newFiles = CreateFiles(@"
VAR_GLOBAL
    nOldVar : INT;
    nNewVar : REAL;
END_VAR");

        // Act
        var changes = comparer.Compare(oldFiles, newFiles);

        // Assert
        Assert.Contains(changes, c => c.ChangeType == ChangeType.Added && c.VariableName == "nNewVar");
    }

    [Fact]
    public void VariableComparer_DetectsRemovedVariable()
    {
        // Arrange
        var comparer = new VariableComparer();
        var oldFiles = CreateFiles(@"
VAR_GLOBAL
    nVar1 : INT;
    nVar2 : REAL;
END_VAR");
        var newFiles = CreateFiles(@"
VAR_GLOBAL
    nVar1 : INT;
END_VAR");

        // Act
        var changes = comparer.Compare(oldFiles, newFiles);

        // Assert
        Assert.Contains(changes, c => c.ChangeType == ChangeType.Removed && c.VariableName == "nVar2");
    }

    [Fact]
    public void VariableComparer_DetectsModifiedVariable()
    {
        // Arrange
        var comparer = new VariableComparer();
        var oldFiles = CreateFiles(@"
VAR_GLOBAL
    nValue : INT := 100;
END_VAR");
        var newFiles = CreateFiles(@"
VAR_GLOBAL
    nValue : DINT := 200;
END_VAR");

        // Act
        var changes = comparer.Compare(oldFiles, newFiles);

        // Assert
        Assert.Contains(changes, c =>
            c.ChangeType == ChangeType.Modified &&
            c.VariableName == "nValue" &&
            c.OldDataType == "INT" &&
            c.NewDataType == "DINT");
    }

    #endregion

    #region I/O 매핑 비교 테스트

    [Fact]
    public void IOMappingComparer_DetectsAddedMapping()
    {
        // Arrange
        var comparer = new IOMappingComparer();
        var oldFiles = CreateFiles(@"
VAR_GLOBAL
    bInput1 AT %IX0.0 : BOOL;
END_VAR");
        var newFiles = CreateFiles(@"
VAR_GLOBAL
    bInput1 AT %IX0.0 : BOOL;
    bInput2 AT %IX0.1 : BOOL;
END_VAR");

        // Act
        var changes = comparer.Compare(oldFiles, newFiles);

        // Assert
        Assert.Contains(changes, c => c.ChangeType == ChangeType.Added && c.VariableName == "bInput2");
    }

    [Fact]
    public void IOMappingComparer_DetectsAddressChange()
    {
        // Arrange
        var comparer = new IOMappingComparer();
        var oldFiles = CreateFiles(@"
VAR_GLOBAL
    bSensor AT %IX0.0 : BOOL;
END_VAR");
        var newFiles = CreateFiles(@"
VAR_GLOBAL
    bSensor AT %IX1.0 : BOOL;
END_VAR");

        // Act
        var changes = comparer.Compare(oldFiles, newFiles);

        // Assert
        Assert.Contains(changes, c =>
            c.ChangeType == ChangeType.Modified &&
            c.VariableName == "bSensor" &&
            c.OldAddress == "%IX0.0" &&
            c.NewAddress == "%IX1.0");
    }

    #endregion

    #region 로직 비교 테스트

    [Fact]
    public void LogicComparer_DetectsAddedFunctionBlock()
    {
        // Arrange
        var comparer = new LogicComparer();
        var oldFiles = CreateFiles(@"
FUNCTION_BLOCK FB_Old
VAR
END_VAR
END_FUNCTION_BLOCK");
        var newFiles = CreateFiles(@"
FUNCTION_BLOCK FB_Old
VAR
END_VAR
END_FUNCTION_BLOCK

FUNCTION_BLOCK FB_New
VAR
END_VAR
END_FUNCTION_BLOCK");

        // Act
        var changes = comparer.Compare(oldFiles, newFiles);

        // Assert
        Assert.Contains(changes, c => c.ChangeType == ChangeType.Added && c.ElementName == "FB_New");
    }

    [Fact]
    public void LogicComparer_DetectsModifiedLogic()
    {
        // Arrange
        var comparer = new LogicComparer();
        var oldFiles = CreateFiles(@"
FUNCTION_BLOCK FB_Test
VAR
    nValue : INT;
END_VAR
nValue := 100;
END_FUNCTION_BLOCK");
        var newFiles = CreateFiles(@"
FUNCTION_BLOCK FB_Test
VAR
    nValue : INT;
END_VAR
nValue := 200;
IF nValue > 100 THEN
    nValue := 0;
END_IF
END_FUNCTION_BLOCK");

        // Act
        var changes = comparer.Compare(oldFiles, newFiles);

        // Assert
        Assert.Contains(changes, c => c.ChangeType == ChangeType.Modified && c.ElementName == "FB_Test");
    }

    #endregion

    #region 데이터 타입 비교 테스트

    [Fact]
    public void DataTypeComparer_DetectsAddedStruct()
    {
        // Arrange
        var comparer = new DataTypeComparer();
        var oldFiles = CreateFiles(@"
TYPE T_OldStruct :
STRUCT
    nValue : INT;
END_STRUCT
END_TYPE");
        var newFiles = CreateFiles(@"
TYPE T_OldStruct :
STRUCT
    nValue : INT;
END_STRUCT
END_TYPE

TYPE T_NewStruct :
STRUCT
    rValue : REAL;
END_STRUCT
END_TYPE");

        // Act
        var changes = comparer.Compare(oldFiles, newFiles);

        // Assert
        Assert.Contains(changes, c => c.ChangeType == ChangeType.Added && c.TypeName == "T_NewStruct");
    }

    [Fact]
    public void DataTypeComparer_DetectsModifiedStruct()
    {
        // Arrange
        var comparer = new DataTypeComparer();
        var oldFiles = CreateFiles(@"
TYPE T_Data :
STRUCT
    nOld : INT;
END_STRUCT
END_TYPE");
        var newFiles = CreateFiles(@"
TYPE T_Data :
STRUCT
    nOld : INT;
    nNew : REAL;
END_STRUCT
END_TYPE");

        // Act
        var changes = comparer.Compare(oldFiles, newFiles);

        // Assert
        Assert.Contains(changes, c =>
            c.ChangeType == ChangeType.Modified &&
            c.TypeName == "T_Data" &&
            c.FieldChanges.Any(f => f.ChangeType == ChangeType.Added && f.FieldName == "nNew"));
    }

    [Fact]
    public void DataTypeComparer_DetectsEnumChanges()
    {
        // Arrange
        var comparer = new DataTypeComparer();
        var oldFiles = CreateFiles(@"
TYPE E_State : (IDLE, RUNNING, STOPPED);
END_TYPE");
        var newFiles = CreateFiles(@"
TYPE E_State : (IDLE, RUNNING, PAUSED, STOPPED);
END_TYPE");

        // Act
        var changes = comparer.Compare(oldFiles, newFiles);

        // Assert
        Assert.Contains(changes, c =>
            c.ChangeType == ChangeType.Modified &&
            c.TypeName == "E_State" &&
            c.EnumChanges.Any(e => e.ChangeType == ChangeType.Added && e.ValueName == "PAUSED"));
    }

    #endregion

    #region 통합 테스트

    [Fact]
    public void ComparisonResultFormatter_FormatsResultCorrectly()
    {
        // Arrange
        var result = new FolderComparisonResult
        {
            OldFolderPath = "C:\\Old",
            NewFolderPath = "C:\\New",
            VariableChanges = new List<VariableChange>
            {
                new VariableChange
                {
                    ChangeType = ChangeType.Added,
                    VariableName = "nNewVar",
                    NewDataType = "INT",
                    FilePath = "test.TcPOU",
                    Line = 10
                }
            }
        };

        // Act
        var text = ComparisonResultFormatter.FormatAsText(result);

        // Assert
        Assert.Contains("nNewVar", text);
        Assert.Contains("추가", text);
    }

    #endregion

    #region Helper Methods

    private List<CodeFile> CreateFiles(string content)
    {
        return new List<CodeFile>
        {
            new CodeFile
            {
                FilePath = "test.TcPOU",
                Content = content,
                Type = FileType.POU,
                Language = ProgrammingLanguage.ST
            }
        };
    }

    #endregion
}
