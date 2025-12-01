using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;
using TwinCatQA.Infrastructure.Comparison;
using Xunit;

namespace TwinCatQA.Application.Tests.Services;

/// <summary>
/// FolderComparer 통합 테스트
/// </summary>
public class FolderComparerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _oldFolderPath;
    private readonly string _newFolderPath;
    private readonly FolderComparer _comparer;

    public FolderComparerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"qa_test_{Guid.NewGuid()}");
        _oldFolderPath = Path.Combine(_tempDir, "old");
        _newFolderPath = Path.Combine(_tempDir, "new");

        Directory.CreateDirectory(_oldFolderPath);
        Directory.CreateDirectory(_newFolderPath);

        _comparer = new FolderComparer();
    }

    #region 기본 비교 테스트

    [Fact]
    public async Task CompareAsync_빈폴더비교_변경사항없음()
    {
        // Act
        var result = await _comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        result.Should().NotBeNull();
        result.HasChanges.Should().BeFalse();
        result.TotalChanges.Should().Be(0);
        result.OldFolderPath.Should().Be(_oldFolderPath);
        result.NewFolderPath.Should().Be(_newFolderPath);
    }

    [Fact]
    public async Task CompareAsync_존재하지않는폴더_예외발생()
    {
        // Arrange
        var invalidPath = Path.Combine(_tempDir, "nonexistent");

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _comparer.CompareAsync(invalidPath, _newFolderPath));
    }

    #endregion

    #region 변수 변경 테스트

    [Fact]
    public async Task CompareAsync_변수추가감지_정상반환()
    {
        // Arrange
        var oldCode = @"
PROGRAM Main
VAR
    counter : INT := 0;
END_VAR
END_PROGRAM
";
        var newCode = @"
PROGRAM Main
VAR
    counter : INT := 0;
    newVariable : DINT := 100;
END_VAR
END_PROGRAM
";
        await CreateTestFile(_oldFolderPath, "Main.TcPOU", oldCode);
        await CreateTestFile(_newFolderPath, "Main.TcPOU", newCode);

        // Act
        var result = await _comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        result.VariableChanges.Should().NotBeEmpty();
        result.VariableChanges.Should().Contain(v =>
            v.ChangeType == ChangeType.Added &&
            v.VariableName == "newVariable");
    }

    [Fact]
    public async Task CompareAsync_변수삭제감지_정상반환()
    {
        // Arrange
        var oldCode = @"
PROGRAM Main
VAR
    counter : INT := 0;
    oldVariable : BOOL := TRUE;
END_VAR
END_PROGRAM
";
        var newCode = @"
PROGRAM Main
VAR
    counter : INT := 0;
END_VAR
END_PROGRAM
";
        await CreateTestFile(_oldFolderPath, "Main.TcPOU", oldCode);
        await CreateTestFile(_newFolderPath, "Main.TcPOU", newCode);

        // Act
        var result = await _comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        result.VariableChanges.Should().Contain(v =>
            v.ChangeType == ChangeType.Removed &&
            v.VariableName == "oldVariable");
    }

    [Fact]
    public async Task CompareAsync_변수타입변경감지_Modified반환()
    {
        // Arrange
        var oldCode = @"
PROGRAM Main
VAR
    counter : DINT := 0;
END_VAR
END_PROGRAM
";
        var newCode = @"
PROGRAM Main
VAR
    counter : INT := 0;
END_VAR
END_PROGRAM
";
        await CreateTestFile(_oldFolderPath, "Main.TcPOU", oldCode);
        await CreateTestFile(_newFolderPath, "Main.TcPOU", newCode);

        // Act
        var result = await _comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        result.VariableChanges.Should().Contain(v =>
            v.ChangeType == ChangeType.Modified &&
            v.VariableName == "counter" &&
            v.OldDataType == "DINT" &&
            v.NewDataType == "INT");
    }

    #endregion

    #region 로직 변경 테스트

    [Fact]
    public async Task CompareAsync_함수블록추가_Added반환()
    {
        // Arrange
        var oldCode = @"
PROGRAM Main
VAR
END_VAR
counter := counter + 1;
END_PROGRAM
";
        var newCode = @"
FUNCTION_BLOCK FB_NewMotor
VAR_INPUT
    enable : BOOL;
END_VAR
IF enable THEN
END_IF
END_FUNCTION_BLOCK

PROGRAM Main
VAR
END_VAR
counter := counter + 1;
END_PROGRAM
";
        await CreateTestFile(_oldFolderPath, "Main.TcPOU", oldCode);
        await CreateTestFile(_newFolderPath, "Main.TcPOU", newCode);

        // Act
        var result = await _comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        result.LogicChanges.Should().Contain(l =>
            l.ChangeType == ChangeType.Added &&
            l.ElementName.Contains("FB_NewMotor"));
    }

    [Fact]
    public async Task CompareAsync_로직수정감지_Modified반환()
    {
        // Arrange
        var oldCode = @"
PROGRAM Main
VAR
    counter : INT;
END_VAR
counter := counter + 1;
END_PROGRAM
";
        var newCode = @"
PROGRAM Main
VAR
    counter : INT;
END_VAR
counter := counter + 2;
IF counter > 100 THEN
    counter := 0;
END_IF
END_PROGRAM
";
        await CreateTestFile(_oldFolderPath, "Main.TcPOU", oldCode);
        await CreateTestFile(_newFolderPath, "Main.TcPOU", newCode);

        // Act
        var result = await _comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        result.LogicChanges.Should().Contain(l =>
            l.ChangeType == ChangeType.Modified &&
            l.ElementName == "Main");
        result.LogicChanges.First().ChangedLineCount.Should().BeGreaterThan(0);
    }

    #endregion

    #region 복합 변경 테스트

    [Fact]
    public async Task CompareAsync_복합변경_모든타입감지()
    {
        // Arrange
        var oldCode = @"
PROGRAM Main
VAR
    counter : INT := 0;
END_VAR
counter := counter + 1;
END_PROGRAM
";
        var newCode = @"
PROGRAM Main
VAR
    counter : DINT := 0;
    temperature : REAL := 25.0;
END_VAR
counter := counter + 2;
IF temperature > 30.0 THEN
    counter := 0;
END_IF
END_PROGRAM
";
        await CreateTestFile(_oldFolderPath, "Main.TcPOU", oldCode);
        await CreateTestFile(_newFolderPath, "Main.TcPOU", newCode);

        // Act
        var result = await _comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        result.HasChanges.Should().BeTrue();
        result.TotalChanges.Should().BeGreaterThan(0);
        result.VariableChanges.Should().NotBeEmpty();
        result.LogicChanges.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CompareAsync_여러파일변경_모두감지()
    {
        // Arrange
        await CreateTestFile(_oldFolderPath, "Main.TcPOU", "PROGRAM Main\nEND_PROGRAM");
        await CreateTestFile(_oldFolderPath, "Motor.TcPOU", "FUNCTION_BLOCK FB_Motor\nEND_FUNCTION_BLOCK");

        await CreateTestFile(_newFolderPath, "Main.TcPOU", "PROGRAM Main\nVAR x:INT; END_VAR\nEND_PROGRAM");
        await CreateTestFile(_newFolderPath, "Motor.TcPOU", "FUNCTION_BLOCK FB_Motor\nVAR speed:REAL; END_VAR\nEND_FUNCTION_BLOCK");

        // Act
        var result = await _comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        result.VariableChanges.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    #endregion

    #region 통계 테스트

    [Fact]
    public async Task TotalChanges_모든변경합계_정확계산()
    {
        // Arrange
        var oldCode = @"
PROGRAM Main
VAR
    a : INT;
    b : INT;
END_VAR
a := 1;
END_PROGRAM
";
        var newCode = @"
PROGRAM Main
VAR
    a : DINT;
    c : INT;
END_VAR
a := 2;
b := 3;
END_PROGRAM
";
        await CreateTestFile(_oldFolderPath, "Main.TcPOU", oldCode);
        await CreateTestFile(_newFolderPath, "Main.TcPOU", newCode);

        // Act
        var result = await _comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        var expectedTotal = result.VariableChanges.Count +
                          result.LogicChanges.Count +
                          result.IOMappingChanges.Count +
                          result.DataTypeChanges.Count;
        result.TotalChanges.Should().Be(expectedTotal);
    }

    [Fact]
    public async Task AddedCount_추가항목수_정확계산()
    {
        // Arrange
        var oldCode = "PROGRAM Main\nVAR\nEND_VAR\nEND_PROGRAM";
        var newCode = @"
PROGRAM Main
VAR
    newVar1 : INT;
    newVar2 : BOOL;
END_VAR
END_PROGRAM
";
        await CreateTestFile(_oldFolderPath, "Main.TcPOU", oldCode);
        await CreateTestFile(_newFolderPath, "Main.TcPOU", newCode);

        // Act
        var result = await _comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        result.AddedCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task ComparedAt_비교시간기록_최근시간()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);
        await CreateTestFile(_oldFolderPath, "Main.TcPOU", "PROGRAM Main\nEND_PROGRAM");
        await CreateTestFile(_newFolderPath, "Main.TcPOU", "PROGRAM Main\nEND_PROGRAM");

        // Act
        var result = await _comparer.CompareAsync(_oldFolderPath, _newFolderPath);
        var after = DateTime.UtcNow.AddSeconds(1);

        // Assert
        result.ComparedAt.Should().BeAfter(before);
        result.ComparedAt.Should().BeBefore(after);
    }

    #endregion

    #region 옵션 테스트

    [Fact]
    public async Task CompareAsync_변수비교비활성화_변수변경없음()
    {
        // Arrange
        var oldCode = "PROGRAM Main\nVAR a:INT; END_VAR\nEND_PROGRAM";
        var newCode = "PROGRAM Main\nVAR a:DINT; b:INT; END_VAR\nEND_PROGRAM";
        await CreateTestFile(_oldFolderPath, "Main.TcPOU", oldCode);
        await CreateTestFile(_newFolderPath, "Main.TcPOU", newCode);

        var options = new CompareOptions
        {
            IncludeVariables = false,
            IncludeLogic = true
        };

        // Act
        var result = await _comparer.CompareAsync(_oldFolderPath, _newFolderPath, options);

        // Assert
        result.VariableChanges.Should().BeEmpty();
    }

    #endregion

    #region 형식 테스트

    [Fact]
    public void FormatAsText_변경사항있음_포맷된문자열반환()
    {
        // Arrange
        var result = new FolderComparisonResult
        {
            OldFolderPath = _oldFolderPath,
            NewFolderPath = _newFolderPath,
            VariableChanges = new List<VariableChange>
            {
                new()
                {
                    ChangeType = ChangeType.Added,
                    VariableName = "testVar",
                    NewDataType = "INT",
                    FilePath = "test.TcPOU",
                    Line = 10
                }
            }
        };

        // Act
        var text = ComparisonResultFormatter.FormatAsText(result);

        // Assert
        text.Should().Contain("비교 결과");
        text.Should().Contain("변수 변경");
        text.Should().Contain("testVar");
        text.Should().Contain("추가");
    }

    [Fact]
    public void FormatAsText_변경사항없음_빈결과메시지()
    {
        // Arrange
        var result = new FolderComparisonResult
        {
            OldFolderPath = _oldFolderPath,
            NewFolderPath = _newFolderPath
        };

        // Act
        var text = ComparisonResultFormatter.FormatAsText(result);

        // Assert
        text.Should().Contain("변경 사항 없음");
    }

    #endregion

    #region 헬퍼 메서드

    private async Task CreateTestFile(string folderPath, string fileName, string content)
    {
        var filePath = Path.Combine(folderPath, fileName);
        await File.WriteAllTextAsync(filePath, content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch
            {
                // 테스트 환경에서 파일 삭제 실패는 무시
            }
        }
    }

    #endregion
}
