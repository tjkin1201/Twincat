using TwinCatQA.Domain.Models;
using TwinCatQA.Infrastructure.Analysis;
using Xunit;

namespace TwinCatQA.Application.Tests.Analysis;

/// <summary>
/// 심화 분석기 테스트
/// </summary>
public class AdvancedAnalyzersTests
{
    #region 메모리 영역 분석 테스트

    [Fact]
    public void MemoryRegionAnalyzer_DetectsOverlappingAddresses()
    {
        // Arrange
        var analyzer = new MemoryRegionAnalyzer();
        var session = CreateSession(@"
VAR_GLOBAL
    startButton AT %IX0.0 : BOOL;
    stopButton AT %IX0.0 : BOOL;  // 중복!
END_VAR
");

        // Act
        var result = analyzer.Analyze(session);

        // Assert
        Assert.True(result.Conflicts.Any(c => c.ConflictType == MemoryConflictType.Overlap));
    }

    [Fact]
    public void MemoryRegionAnalyzer_DetectsMixedAccess()
    {
        // Arrange
        var analyzer = new MemoryRegionAnalyzer();
        var session = CreateSession(@"
VAR_GLOBAL
    bitVar AT %IX0.0 : BOOL;
    byteVar AT %IB0 : BYTE;  // 비트와 바이트 혼용
END_VAR
");

        // Act
        var result = analyzer.Analyze(session);

        // Assert
        Assert.True(result.Conflicts.Any(c => c.ConflictType == MemoryConflictType.MixedAccess));
    }

    [Fact]
    public void MemoryRegionAnalyzer_CalculatesStatistics()
    {
        // Arrange
        var analyzer = new MemoryRegionAnalyzer();
        var session = CreateSession(@"
VAR_GLOBAL
    input1 AT %IX0.0 : BOOL;
    input2 AT %IX0.1 : BOOL;
    output1 AT %QW0 : WORD;
    memory1 AT %MD100 : DWORD;
END_VAR
");

        // Act
        var result = analyzer.Analyze(session);

        // Assert
        Assert.Equal(4, result.Allocations.Count);
        Assert.True(result.Statistics.Any(s => s.RegionType == MemoryRegionType.Input));
    }

    #endregion

    #region 데드 코드 검출 테스트

    [Fact]
    public void DeadCodeDetector_DetectsAlwaysTrueCondition()
    {
        // Arrange
        var detector = new DeadCodeDetector();
        var session = CreateSession(@"
IF TRUE THEN
    x := 1;
END_IF
");

        // Act
        var results = detector.Detect(session);

        // Assert
        Assert.Contains(results, r => r.Type == DeadCodeType.AlwaysTrueCondition);
    }

    [Fact]
    public void DeadCodeDetector_DetectsAlwaysFalseCondition()
    {
        // Arrange
        var detector = new DeadCodeDetector();
        var session = CreateSession(@"
IF FALSE THEN
    x := 1;  // 절대 실행 안됨
END_IF
");

        // Act
        var results = detector.Detect(session);

        // Assert
        Assert.Contains(results, r => r.Type == DeadCodeType.AlwaysFalseCondition);
    }

    [Fact]
    public void DeadCodeDetector_DetectsUnreachableAfterReturn()
    {
        // Arrange
        var detector = new DeadCodeDetector();
        var session = CreateSession(@"
RETURN;
x := 1;  // 도달 불가
y := 2;
");

        // Act
        var results = detector.Detect(session);

        // Assert
        Assert.Contains(results, r => r.Type == DeadCodeType.UnreachableCode);
    }

    [Fact]
    public void DeadCodeDetector_DetectsCommentedCode()
    {
        // Arrange
        var detector = new DeadCodeDetector();
        var session = CreateSession(@"
// IF oldCondition THEN
x := 1;
(* FOR i := 0 TO 10 DO *)
");

        // Act
        var results = detector.Detect(session);

        // Assert
        Assert.Contains(results, r => r.Type == DeadCodeType.CommentedOutCode);
    }

    #endregion

    #region 배열 경계 검사 테스트

    [Fact]
    public void ArrayBoundsChecker_DetectsConstantOutOfBounds()
    {
        // Arrange
        var checker = new ArrayBoundsChecker();
        var session = CreateSession(@"
VAR
    arr : ARRAY[0..10] OF INT;
END_VAR

arr[15] := 100;  // 범위 초과!
");

        // Act
        var results = checker.Check(session);

        // Assert
        Assert.Contains(results, r => r.ViolationType == ArrayBoundsViolationType.ConstantOutOfBounds);
    }

    [Fact]
    public void ArrayBoundsChecker_DetectsNegativeIndex()
    {
        // Arrange
        var checker = new ArrayBoundsChecker();
        var session = CreateSession(@"
VAR
    arr : ARRAY[0..10] OF INT;
END_VAR

arr[-1] := 100;  // 음수 인덱스!
");

        // Act
        var results = checker.Check(session);

        // Assert
        Assert.Contains(results, r => r.ViolationType == ArrayBoundsViolationType.NegativeIndex ||
                                       r.ViolationType == ArrayBoundsViolationType.ConstantOutOfBounds);
    }

    [Fact]
    public void ArrayBoundsChecker_DetectsLoopIndexOutOfBounds()
    {
        // Arrange
        var checker = new ArrayBoundsChecker();
        var session = CreateSession(@"
VAR
    arr : ARRAY[0..10] OF INT;
    i : INT;
END_VAR

FOR i := 0 TO 15 DO  // 범위 초과!
    arr[i] := 0;
END_FOR
");

        // Act
        var results = checker.Check(session);

        // Assert
        Assert.Contains(results, r => r.ViolationType == ArrayBoundsViolationType.LoopIndexOutOfBounds);
    }

    #endregion

    #region 매직 넘버 검출 테스트

    [Fact]
    public void MagicNumberDetector_DetectsMagicNumbers()
    {
        // Arrange
        var detector = new MagicNumberDetector();
        var session = CreateSession(@"
VAR
    speed : REAL;
END_VAR

speed := 3.14159;  // 매직 넘버
IF count > 42 THEN  // 매직 넘버
    timeout := 5000;  // 매직 넘버
END_IF
");

        // Act
        var results = detector.Detect(session);

        // Assert
        Assert.True(results.Count >= 2);  // 최소 2개 이상 검출
    }

    [Fact]
    public void MagicNumberDetector_IgnoresAllowedNumbers()
    {
        // Arrange
        var detector = new MagicNumberDetector();
        var session = CreateSession(@"
VAR
    x : INT;
END_VAR

x := 0;
x := 1;
x := 100;
");

        // Act
        var results = detector.Detect(session);

        // Assert
        Assert.Empty(results);  // 0, 1, 100은 허용
    }

    [Fact]
    public void MagicNumberDetector_IgnoresConstantDeclarations()
    {
        // Arrange
        var detector = new MagicNumberDetector();
        var session = CreateSession(@"
VAR CONSTANT
    MAX_SPEED : REAL := 150.5;
    TIMEOUT_MS : INT := 5000;
END_VAR
");

        // Act
        var results = detector.Detect(session);

        // Assert
        Assert.Empty(results);  // 상수 선언은 매직 넘버 아님
    }

    #endregion

    #region 순환 복잡도 테스트

    [Fact]
    public void CyclomaticComplexityAnalyzer_CalculatesCorrectComplexity()
    {
        // Arrange
        var analyzer = new CyclomaticComplexityAnalyzer();
        var session = CreateSessionWithFB("FB_Test", @"
IF condition1 THEN
    IF condition2 THEN
        x := 1;
    ELSIF condition3 THEN
        x := 2;
    END_IF
END_IF

FOR i := 0 TO 10 DO
    CASE state OF
        0: x := 0;
        1: x := 1;
        2: x := 2;
    END_CASE
END_FOR
");

        // Act
        var results = analyzer.Analyze(session);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].Complexity > 5);  // 복잡한 코드
    }

    [Fact]
    public void CyclomaticComplexityAnalyzer_GradesComplexity()
    {
        // Arrange
        var analyzer = new CyclomaticComplexityAnalyzer();

        // 단순한 코드
        var simpleSession = CreateSessionWithFB("FB_Simple", "x := 1;");

        // Act
        var results = analyzer.Analyze(simpleSession);

        // Assert
        Assert.Single(results);
        Assert.Equal(ComplexityGrade.A, results[0].Grade);  // 단순 = A등급
    }

    #endregion

    #region Helper Methods

    private ValidationSession CreateSession(string content)
    {
        return new ValidationSession
        {
            ScannedFiles = new List<CodeFile>
            {
                new CodeFile
                {
                    FilePath = "test.TcPOU",
                    Content = content,
                    FunctionBlocks = new List<FunctionBlock>()
                }
            }
        };
    }

    private ValidationSession CreateSessionWithFB(string fbName, string body)
    {
        return new ValidationSession
        {
            ScannedFiles = new List<CodeFile>
            {
                new CodeFile
                {
                    FilePath = "test.TcPOU",
                    Content = $"FUNCTION_BLOCK {fbName}\n{body}\nEND_FUNCTION_BLOCK",
                    FunctionBlocks = new List<FunctionBlock>
                    {
                        new FunctionBlock
                        {
                            Name = fbName,
                            Body = body
                        }
                    }
                }
            }
        };
    }

    #endregion
}
