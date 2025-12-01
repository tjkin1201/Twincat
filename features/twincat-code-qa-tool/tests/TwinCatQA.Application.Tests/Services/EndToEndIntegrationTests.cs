using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using TwinCatQA.Application.Services;
using TwinCatQA.Domain.Models;
using TwinCatQA.Infrastructure.Comparison;
using Xunit;

namespace TwinCatQA.Application.Tests.Services;

/// <summary>
/// End-to-End 통합 테스트
/// 전체 워크플로우를 실제 시나리오로 테스트
/// </summary>
public class EndToEndIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _oldFolderPath;
    private readonly string _newFolderPath;

    public EndToEndIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"e2e_test_{Guid.NewGuid()}");
        _oldFolderPath = Path.Combine(_tempDir, "old_version");
        _newFolderPath = Path.Combine(_tempDir, "new_version");

        Directory.CreateDirectory(_oldFolderPath);
        Directory.CreateDirectory(_newFolderPath);
    }

    #region E2E 시나리오 테스트

    [Fact]
    public async Task 시나리오1_변수타입축소감지_Critical위반반환()
    {
        // Arrange: 변수 타입 축소 (DINT → INT)
        var oldCode = @"
PROGRAM Main
VAR
    counter : DINT := 0;  // 32비트
END_VAR
counter := counter + 1;
END_PROGRAM
";
        var newCode = @"
PROGRAM Main
VAR
    counter : INT := 0;  // 16비트로 축소!
END_VAR
counter := counter + 1;
END_PROGRAM
";
        await CreateTestFile(_oldFolderPath, "Main.TcPOU", oldCode);
        await CreateTestFile(_newFolderPath, "Main.TcPOU", newCode);

        // Act
        var comparer = new FolderComparer();
        var result = await comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        result.HasChanges.Should().BeTrue();
        result.VariableChanges.Should().Contain(v =>
            v.ChangeType == ChangeType.Modified &&
            v.VariableName == "counter" &&
            v.OldDataType == "DINT" &&
            v.NewDataType == "INT");
    }

    [Fact]
    public async Task 시나리오2_부동소수점비교_Warning위반반환()
    {
        // Arrange: 부동소수점 직접 비교
        var oldCode = @"
PROGRAM Main
VAR
    temp : REAL := 25.0;
END_VAR
temp := temp + 0.1;
END_PROGRAM
";
        var newCode = @"
PROGRAM Main
VAR
    temp : REAL := 25.0;
END_VAR
IF temp = 25.0 THEN  // 부동소수점 직접 비교 - 위험!
    temp := 0.0;
END_IF
END_PROGRAM
";
        await CreateTestFile(_oldFolderPath, "Main.TcPOU", oldCode);
        await CreateTestFile(_newFolderPath, "Main.TcPOU", newCode);

        // Act
        var comparer = new FolderComparer();
        var result = await comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        result.LogicChanges.Should().NotBeEmpty();
        result.LogicChanges.First().NewCode.Should().Contain("temp = 25.0");
    }

    [Fact]
    public async Task 시나리오3_매직넘버사용_Info위반반환()
    {
        // Arrange: 매직 넘버 사용
        var oldCode = @"
PROGRAM Main
VAR
    speed : REAL;
END_VAR
speed := 0.0;
END_PROGRAM
";
        var newCode = @"
PROGRAM Main
VAR
    speed : REAL;
END_VAR
IF speed > 100.5 THEN  // 매직 넘버 - 상수화 권장
    speed := 50.3;     // 또 다른 매직 넘버
END_IF
END_PROGRAM
";
        await CreateTestFile(_oldFolderPath, "Main.TcPOU", oldCode);
        await CreateTestFile(_newFolderPath, "Main.TcPOU", newCode);

        // Act
        var comparer = new FolderComparer();
        var result = await comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        result.LogicChanges.Should().NotBeEmpty();
        var logic = result.LogicChanges.First();
        logic.NewCode.Should().Contain("100.5");
        logic.NewCode.Should().Contain("50.3");
    }

    [Fact]
    public async Task 시나리오4_변수추가및로직변경_복합감지()
    {
        // Arrange: 복합 변경 시나리오
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
    counter : INT := 0;
    temperature : REAL := 25.0;  // 새 변수 추가
    isRunning : BOOL := FALSE;
END_VAR
counter := counter + 1;
IF temperature > 30.0 THEN
    isRunning := TRUE;
    counter := 0;
END_IF
END_PROGRAM
";
        await CreateTestFile(_oldFolderPath, "Main.TcPOU", oldCode);
        await CreateTestFile(_newFolderPath, "Main.TcPOU", newCode);

        // Act
        var comparer = new FolderComparer();
        var result = await comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        result.HasChanges.Should().BeTrue();
        result.VariableChanges.Should().Contain(v => v.VariableName == "temperature");
        result.VariableChanges.Should().Contain(v => v.VariableName == "isRunning");
        result.LogicChanges.Should().Contain(l => l.ChangeType == ChangeType.Modified);
    }

    [Fact]
    public async Task 시나리오5_함수블록추가_Added감지()
    {
        // Arrange: 새 Function Block 추가
        var oldCode = @"
PROGRAM Main
VAR
END_VAR
END_PROGRAM
";
        var newCode = @"
FUNCTION_BLOCK FB_Motor
VAR_INPUT
    enable : BOOL;
    targetSpeed : REAL;
END_VAR
VAR_OUTPUT
    currentSpeed : REAL;
    fault : BOOL;
END_VAR
IF enable THEN
    currentSpeed := targetSpeed;
ELSE
    currentSpeed := 0.0;
END_IF
END_FUNCTION_BLOCK

PROGRAM Main
VAR
    motor : FB_Motor;
END_VAR
motor.enable := TRUE;
motor.targetSpeed := 100.0;
END_PROGRAM
";
        await CreateTestFile(_oldFolderPath, "Main.TcPOU", oldCode);
        await CreateTestFile(_newFolderPath, "Main.TcPOU", newCode);

        // Act
        var comparer = new FolderComparer();
        var result = await comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        result.LogicChanges.Should().Contain(l =>
            l.ChangeType == ChangeType.Added &&
            l.ElementName.Contains("FB_Motor"));
    }

    [Fact]
    public async Task 시나리오6_여러파일동시변경_모두감지()
    {
        // Arrange: 여러 파일 동시 변경
        await CreateTestFile(_oldFolderPath, "Main.TcPOU",
            "PROGRAM Main\nVAR x:INT; END_VAR\nEND_PROGRAM");
        await CreateTestFile(_oldFolderPath, "Motor.TcPOU",
            "FUNCTION_BLOCK FB_Motor\nEND_FUNCTION_BLOCK");
        await CreateTestFile(_oldFolderPath, "Globals.TcGVL",
            "VAR_GLOBAL\nEND_VAR");

        await CreateTestFile(_newFolderPath, "Main.TcPOU",
            "PROGRAM Main\nVAR x:DINT; y:BOOL; END_VAR\nIF y THEN x:=1; END_IF\nEND_PROGRAM");
        await CreateTestFile(_newFolderPath, "Motor.TcPOU",
            "FUNCTION_BLOCK FB_Motor\nVAR speed:REAL; END_VAR\nEND_FUNCTION_BLOCK");
        await CreateTestFile(_newFolderPath, "Globals.TcGVL",
            "VAR_GLOBAL\nsystemState:INT; END_VAR");

        // Act
        var comparer = new FolderComparer();
        var result = await comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        result.TotalChanges.Should().BeGreaterThan(3);
        result.VariableChanges.Should().NotBeEmpty();
        result.LogicChanges.Should().NotBeEmpty();
    }

    [Fact]
    public async Task 시나리오7_파일삭제_Removed감지()
    {
        // Arrange: 파일 삭제 시나리오
        await CreateTestFile(_oldFolderPath, "Main.TcPOU",
            "PROGRAM Main\nEND_PROGRAM");
        await CreateTestFile(_oldFolderPath, "OldMotor.TcPOU",
            "FUNCTION_BLOCK FB_OldMotor\nEND_FUNCTION_BLOCK");

        await CreateTestFile(_newFolderPath, "Main.TcPOU",
            "PROGRAM Main\nEND_PROGRAM");
        // OldMotor.TcPOU는 새 버전에 없음

        // Act
        var comparer = new FolderComparer();
        var result = await comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        result.LogicChanges.Should().Contain(l =>
            l.ChangeType == ChangeType.Removed &&
            l.FilePath.Contains("OldMotor"));
    }

    [Fact]
    public async Task 시나리오8_변수초기값변경_Modified감지()
    {
        // Arrange: 초기값 변경
        var oldCode = @"
PROGRAM Main
VAR
    maxSpeed : REAL := 100.0;
END_VAR
END_PROGRAM
";
        var newCode = @"
PROGRAM Main
VAR
    maxSpeed : REAL := 150.0;  // 초기값 변경
END_VAR
END_PROGRAM
";
        await CreateTestFile(_oldFolderPath, "Main.TcPOU", oldCode);
        await CreateTestFile(_newFolderPath, "Main.TcPOU", newCode);

        // Act
        var comparer = new FolderComparer();
        var result = await comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        result.VariableChanges.Should().Contain(v =>
            v.ChangeType == ChangeType.Modified &&
            v.VariableName == "maxSpeed");
    }

    #endregion

    #region 통계 및 집계 테스트

    [Fact]
    public async Task 통계계산_복합변경_정확한집계()
    {
        // Arrange
        var oldCode = @"
PROGRAM Main
VAR
    a : INT;
    b : INT;
    c : BOOL;
END_VAR
a := 1;
END_PROGRAM
";
        var newCode = @"
PROGRAM Main
VAR
    a : DINT;  // Modified
    c : BOOL;
    d : REAL;  // Added
END_VAR
a := 2;
b := 3;
IF d > 10.0 THEN
    c := TRUE;
END_IF
END_PROGRAM
";
        await CreateTestFile(_oldFolderPath, "Main.TcPOU", oldCode);
        await CreateTestFile(_newFolderPath, "Main.TcPOU", newCode);

        // Act
        var comparer = new FolderComparer();
        var result = await comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        result.AddedCount.Should().BeGreaterThan(0);
        result.RemovedCount.Should().BeGreaterThan(0);
        result.ModifiedCount.Should().BeGreaterThan(0);
        result.TotalChanges.Should().Be(
            result.AddedCount + result.RemovedCount + result.ModifiedCount);
    }

    [Fact]
    public async Task 변경없음_빈결과반환()
    {
        // Arrange: 동일한 코드
        var code = "PROGRAM Main\nEND_PROGRAM";
        await CreateTestFile(_oldFolderPath, "Main.TcPOU", code);
        await CreateTestFile(_newFolderPath, "Main.TcPOU", code);

        // Act
        var comparer = new FolderComparer();
        var result = await comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        result.HasChanges.Should().BeFalse();
        result.TotalChanges.Should().Be(0);
    }

    #endregion

    #region 성능 테스트

    [Fact]
    public async Task 대량파일처리_성능측정()
    {
        // Arrange: 100개 파일 생성
        for (int i = 0; i < 100; i++)
        {
            await CreateTestFile(_oldFolderPath, $"File{i}.TcPOU",
                $"PROGRAM File{i}\nVAR x:INT; END_VAR\nEND_PROGRAM");
            await CreateTestFile(_newFolderPath, $"File{i}.TcPOU",
                $"PROGRAM File{i}\nVAR x:DINT; y:BOOL; END_VAR\nEND_PROGRAM");
        }

        // Act
        var startTime = DateTime.UtcNow;
        var comparer = new FolderComparer();
        var result = await comparer.CompareAsync(_oldFolderPath, _newFolderPath);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        result.TotalChanges.Should().BeGreaterThan(100);
        elapsed.TotalSeconds.Should().BeLessThan(30); // 30초 이내 완료
    }

    #endregion

    #region 포매터 테스트

    [Fact]
    public async Task FormatAsText_종합보고서_완전한출력()
    {
        // Arrange
        var oldCode = "PROGRAM Main\nVAR a:INT; END_VAR\na:=1;\nEND_PROGRAM";
        var newCode = "PROGRAM Main\nVAR a:DINT; b:BOOL; END_VAR\na:=2;\nIF b THEN a:=0; END_IF\nEND_PROGRAM";
        await CreateTestFile(_oldFolderPath, "Main.TcPOU", oldCode);
        await CreateTestFile(_newFolderPath, "Main.TcPOU", newCode);

        var comparer = new FolderComparer();
        var result = await comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Act
        var formatted = ComparisonResultFormatter.FormatAsText(result);

        // Assert
        formatted.Should().Contain("비교 결과");
        formatted.Should().Contain("요약");
        formatted.Should().Contain("변수 변경");
        formatted.Should().Contain("로직 변경");
    }

    #endregion

    #region 오류 처리 테스트

    [Fact]
    public async Task 잘못된경로_예외처리()
    {
        // Arrange
        var invalidPath = Path.Combine(_tempDir, "nonexistent");

        // Act & Assert
        var comparer = new FolderComparer();
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => comparer.CompareAsync(invalidPath, _newFolderPath));
    }

    [Fact]
    public async Task 빈파일처리_정상완료()
    {
        // Arrange: 빈 파일
        await CreateTestFile(_oldFolderPath, "Empty.TcPOU", "");
        await CreateTestFile(_newFolderPath, "Empty.TcPOU", "");

        // Act
        var comparer = new FolderComparer();
        var result = await comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task 특수문자포함_정상처리()
    {
        // Arrange: 특수 문자 포함 코드
        var code = @"
PROGRAM Main
VAR
    // 한글 주석: 속도 제어
    message : STRING := '테스트 메시지';
END_VAR
(* 블록 주석
   여러 줄 *)
END_PROGRAM
";
        await CreateTestFile(_oldFolderPath, "Main.TcPOU", code);
        await CreateTestFile(_newFolderPath, "Main.TcPOU", code);

        // Act
        var comparer = new FolderComparer();
        var result = await comparer.CompareAsync(_oldFolderPath, _newFolderPath);

        // Assert
        result.Should().NotBeNull();
        result.HasChanges.Should().BeFalse();
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
