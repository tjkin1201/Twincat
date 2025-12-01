using System;
using System.Linq;
using Xunit;
using FluentAssertions;
using static TwinCatQA.Infrastructure.Tests.Parsers.ParserTestHelper;

namespace TwinCatQA.Infrastructure.Tests.Parsers
{
    /// <summary>
    /// 실제 TwinCAT 코드 파싱 테스트
    /// </summary>
    public class RealCodeParsingTests
    {
        [Fact]
        public void Parse_모터제어FB_성공()
        {
            // Arrange
            var code = @"
FUNCTION_BLOCK FB_MotorControl
VAR_INPUT
    targetSpeed : REAL;
    enable : BOOL;
END_VAR
VAR_OUTPUT
    currentSpeed : REAL;
    fault : BOOL;
END_VAR
VAR
    rampUpRate : REAL := 10.0;
    rampDownRate : REAL := 20.0;
END_VAR

IF enable THEN
    IF currentSpeed < targetSpeed THEN
        currentSpeed := currentSpeed + rampUpRate * 0.01;
        IF currentSpeed > targetSpeed THEN
            currentSpeed := targetSpeed;
        END_IF
    ELSIF currentSpeed > targetSpeed THEN
        currentSpeed := currentSpeed - rampDownRate * 0.01;
        IF currentSpeed < targetSpeed THEN
            currentSpeed := targetSpeed;
        END_IF
    END_IF
ELSE
    currentSpeed := 0.0;
END_IF
END_FUNCTION_BLOCK
";

            // Act
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
            var program = tree as StructuredTextParser.ProgramContext;
            program.Should().NotBeNull();

            var fbDecl = program!.programUnit(0).functionBlockDeclaration();
            fbDecl.Should().NotBeNull();
            fbDecl!.IDENTIFIER().GetText().Should().Be("FB_MotorControl");

            // 변수 선언 검증
            fbDecl.varDeclaration().Should().HaveCount(3);

            // 문장 검증
            fbDecl.statement().Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public void Parse_컨베이어제어_성공()
        {
            // Arrange
            var code = @"
FUNCTION_BLOCK FB_ConveyorControl
VAR_INPUT
    start : BOOL;
    stop : BOOL;
    emergencyStop : BOOL;
END_VAR
VAR_OUTPUT
    motorRunning : BOOL;
    status : INT;
END_VAR
VAR
    state : INT := 0;
    timer : TON;
END_VAR

// 비상 정지 우선 처리
IF emergencyStop THEN
    motorRunning := FALSE;
    state := 0;
    RETURN;
END_IF

// 상태 머신
CASE state OF
    0: // 대기 상태
        motorRunning := FALSE;
        IF start THEN
            state := 1;
        END_IF

    1: // 시작 상태
        motorRunning := TRUE;
        status := 1;
        IF stop THEN
            state := 2;
        END_IF

    2: // 정지 상태
        motorRunning := FALSE;
        status := 0;
        state := 0;
END_CASE
END_FUNCTION_BLOCK
";

            // Act
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
            var program = tree as StructuredTextParser.ProgramContext;
            program.Should().NotBeNull();

            var fbDecl = program!.programUnit(0).functionBlockDeclaration();
            fbDecl!.IDENTIFIER().GetText().Should().Be("FB_ConveyorControl");
        }

        [Fact]
        public void Parse_PID제어기_성공()
        {
            // Arrange
            var code = @"
FUNCTION_BLOCK FB_PIDController
VAR_INPUT
    setpoint : REAL;
    processValue : REAL;
    Kp : REAL := 1.0;
    Ki : REAL := 0.1;
    Kd : REAL := 0.01;
END_VAR
VAR_OUTPUT
    output : REAL;
END_VAR
VAR
    error : REAL;
    lastError : REAL;
    integral : REAL;
    derivative : REAL;
END_VAR

// PID 계산
error := setpoint - processValue;
integral := integral + error * 0.01; // dt = 10ms
derivative := (error - lastError) / 0.01;

output := Kp * error + Ki * integral + Kd * derivative;

// 적분 와인드업 방지
IF integral > 100.0 THEN
    integral := 100.0;
ELSIF integral < -100.0 THEN
    integral := -100.0;
END_IF

// 출력 제한
IF output > 100.0 THEN
    output := 100.0;
ELSIF output < 0.0 THEN
    output := 0.0;
END_IF

lastError := error;
END_FUNCTION_BLOCK
";

            // Act
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
        }

        [Fact]
        public void Parse_시퀀스제어_성공()
        {
            // Arrange
            var code = @"
PROGRAM PRG_SequenceControl
VAR
    step : INT := 0;
    timer : TON;
    cylinder1 : BOOL;
    cylinder2 : BOOL;
    sensor1 : BOOL;
    sensor2 : BOOL;
END_VAR

CASE step OF
    0: // 초기화
        cylinder1 := FALSE;
        cylinder2 := FALSE;
        IF sensor1 AND sensor2 THEN
            step := 10;
        END_IF

    10: // 실린더1 전진
        cylinder1 := TRUE;
        timer(IN := TRUE, PT := T#2s);
        IF timer.Q THEN
            timer(IN := FALSE);
            step := 20;
        END_IF

    20: // 실린더2 전진
        cylinder2 := TRUE;
        timer(IN := TRUE, PT := T#2s);
        IF timer.Q THEN
            timer(IN := FALSE);
            step := 30;
        END_IF

    30: // 실린더1 후진
        cylinder1 := FALSE;
        timer(IN := TRUE, PT := T#2s);
        IF timer.Q THEN
            timer(IN := FALSE);
            step := 40;
        END_IF

    40: // 실린더2 후진
        cylinder2 := FALSE;
        timer(IN := TRUE, PT := T#2s);
        IF timer.Q THEN
            timer(IN := FALSE);
            step := 0;
        END_IF
END_CASE
END_PROGRAM
";

            // Act
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
            var program = tree as StructuredTextParser.ProgramContext;
            program.Should().NotBeNull();

            var progDecl = program!.programUnit(0).programDeclaration();
            progDecl.Should().NotBeNull();
            progDecl!.IDENTIFIER().GetText().Should().Be("PRG_SequenceControl");
        }

        [Fact]
        public void Parse_데이터로깅_성공()
        {
            // Arrange
            var code = @"
FUNCTION_BLOCK FB_DataLogger
VAR_INPUT
    enable : BOOL;
    sampleRate : TIME := T#100ms;
END_VAR
VAR
    buffer : ARRAY[0..999] OF REAL;
    index : INT := 0;
    timer : TON;
    value : REAL;
END_VAR

IF enable THEN
    timer(IN := TRUE, PT := sampleRate);

    IF timer.Q THEN
        timer(IN := FALSE);

        // 데이터 샘플링
        IF index < 1000 THEN
            buffer[index] := value;
            index := index + 1;
        ELSE
            // 버퍼 가득 참
            index := 0;
        END_IF
    END_IF
ELSE
    timer(IN := FALSE);
    index := 0;
END_IF
END_FUNCTION_BLOCK
";

            // Act
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
        }

        [Fact]
        public void Parse_복수FB_프로그램_성공()
        {
            // Arrange
            var code = @"
TYPE E_State :
(
    IDLE := 0,
    RUNNING := 1,
    ERROR := 2
);
END_TYPE

FUNCTION_BLOCK FB_Motor
VAR_INPUT
    enable : BOOL;
END_VAR
VAR_OUTPUT
    running : BOOL;
END_VAR
END_FUNCTION_BLOCK

PROGRAM Main
VAR
    motor1 : FB_Motor;
    motor2 : FB_Motor;
    state : E_State;
END_VAR

motor1(enable := TRUE);
motor2(enable := FALSE);

IF motor1.running THEN
    state := E_State.RUNNING;
ELSE
    state := E_State.IDLE;
END_IF
END_PROGRAM
";

            // Act
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
            var program = tree as StructuredTextParser.ProgramContext;
            program.Should().NotBeNull();

            // TYPE, FB, PROGRAM 3개의 프로그램 단위
            program!.programUnit().Should().HaveCount(3);
        }

        [Fact]
        public void Parse_수학함수_성공()
        {
            // Arrange
            var code = @"
FUNCTION FC_Average : REAL
VAR_INPUT
    values : ARRAY[0..9] OF REAL;
    count : INT;
END_VAR
VAR
    sum : REAL := 0.0;
    i : INT;
END_VAR

FOR i := 0 TO count - 1 DO
    sum := sum + values[i];
END_FOR

IF count > 0 THEN
    FC_Average := sum / INT_TO_REAL(count);
ELSE
    FC_Average := 0.0;
END_IF
END_FUNCTION
";

            // Act
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
            var program = tree as StructuredTextParser.ProgramContext;
            program.Should().NotBeNull();

            var funcDecl = program!.programUnit(0).functionDeclaration();
            funcDecl.Should().NotBeNull();
            funcDecl!.IDENTIFIER().GetText().Should().Be("FC_Average");
        }

        [Fact]
        public void Parse_안전로직_성공()
        {
            // Arrange
            var code = @"
FUNCTION_BLOCK FB_SafetyMonitor
VAR_INPUT
    emergencyStop : BOOL;
    doorClosed : BOOL;
    lightCurtain : BOOL;
END_VAR
VAR_OUTPUT
    safetyOK : BOOL;
    errorCode : INT;
END_VAR

// 모든 안전 조건 확인
IF NOT emergencyStop AND doorClosed AND lightCurtain THEN
    safetyOK := TRUE;
    errorCode := 0;
ELSE
    safetyOK := FALSE;

    // 에러 코드 결정
    IF NOT emergencyStop THEN
        errorCode := 1; // 비상 정지
    ELSIF NOT doorClosed THEN
        errorCode := 2; // 도어 열림
    ELSIF NOT lightCurtain THEN
        errorCode := 3; // 라이트 커튼 차단
    END_IF
END_IF
END_FUNCTION_BLOCK
";

            // Act
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
        }

        [Fact]
        public void Parse_복잡한상태머신_성공()
        {
            // Arrange
            var code = @"
FUNCTION_BLOCK FB_StateMachine
VAR
    currentState : INT := 0;
    nextState : INT := 0;
    timer : TON;
    counter : INT;
END_VAR

// 상태 전환 로직
CASE currentState OF
    0: // IDLE
        IF counter > 0 THEN
            nextState := 1;
        END_IF

    1: // INIT
        timer(IN := TRUE, PT := T#1s);
        IF timer.Q THEN
            timer(IN := FALSE);
            nextState := 2;
        END_IF

    2: // RUNNING
        counter := counter + 1;
        IF counter >= 100 THEN
            nextState := 3;
        END_IF

    3: // COMPLETE
        counter := 0;
        nextState := 0;
END_CASE

currentState := nextState;
END_FUNCTION_BLOCK
";

            // Act
            var tree = ParseCode(code);

            // Assert
            tree.Should().NotBeNull();
        }
    }
}
