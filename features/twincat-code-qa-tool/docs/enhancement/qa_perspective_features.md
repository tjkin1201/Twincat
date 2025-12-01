# QA 관점 기능 설계 - 휴먼 에러 방지

**작성일**: 2025-11-24
**목표**: **개발자 코드의 휴먼 에러를 줄이고, 상세한 분석 및 코멘트 제공**

---

## 🎯 핵심 목표

> **"QA 관점에서 개발자들이 개발한 코드에 대해 상세히 분석하고 코멘트하여 휴먼 에러를 줄이는 것이 최대 목표"**

### 기존 기능 vs QA 강화 기능

| 기존 기능 | QA 강화 기능 |
|----------|-------------|
| 변경 사항 감지 | **왜 위험한지** 설명 |
| 변경 부분 표시 | **잠재적 버그** 지적 |
| 영향도 분석 | **테스트 체크리스트** 제공 |
| 변경 이유 추론 | **베스트 프랙티스** 가이드 |

---

## 🛡️ 휴먼 에러 방지 기능 설계

### 1. 자동 코드 리뷰 시스템

#### 기능 개요
변경된 코드를 TwinCAT ST 베스트 프랙티스와 안전 규칙에 따라 자동 검증

#### 검증 카테고리

##### 🔴 Critical (심각한 에러 가능성)
```csharp
public class CriticalRuleChecker
{
    public List<QAIssue> CheckCriticalIssues(CodeChange change)
    {
        var issues = new List<QAIssue>();

        // 1. 타입 불일치로 인한 오버플로우
        if (IsTypeNarrowing(change))
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Critical,
                Category = "타입 안전성",
                Title = "타입 축소로 인한 데이터 손실 가능",
                Description = $"{change.OldType} → {change.NewType} 변경으로 값 범위 초과 가능",
                Location = $"{change.FilePath}:{change.Line}",
                Recommendation = @"
                    ❌ 위험: DINT (32bit) → INT (16bit) 변경 시 값 손실
                    ✅ 해결:
                    1. 값 범위 확인 후 변경
                    2. 또는 DINT 유지
                    3. 변환 시 범위 체크 추가:
                       IF value > 32767 OR value < -32768 THEN
                           // 에러 처리
                       END_IF
                ",
                Examples = new[]
                {
                    "변경 전: counter : DINT := 50000;",
                    "변경 후: counter : INT := 50000; // ❌ 오버플로우!"
                }
            });
        }

        // 2. 초기화되지 않은 변수 사용
        if (IsUninitializedVariableUsed(change))
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Critical,
                Category = "초기화 누락",
                Title = "초기화되지 않은 변수 사용",
                Description = "변수가 초기값 없이 사용되어 예측 불가능한 동작 발생 가능",
                Location = $"{change.FilePath}:{change.Line}",
                Recommendation = @"
                    ❌ 위험: VAR enabled : BOOL; END_VAR
                            IF enabled THEN ... // enabled 값이 불확실

                    ✅ 해결: VAR enabled : BOOL := FALSE; END_VAR
                "
            });
        }

        // 3. 배열 범위 초과
        if (IsArrayBoundsUnchecked(change))
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Critical,
                Category = "배열 안전성",
                Title = "배열 인덱스 범위 검증 누락",
                Description = "배열 접근 시 인덱스 범위를 확인하지 않아 런타임 에러 가능",
                Location = $"{change.FilePath}:{change.Line}",
                Recommendation = @"
                    ❌ 위험: value := dataArray[index]; // index 검증 없음

                    ✅ 해결:
                    IF index >= 1 AND index <= UPPER_BOUND(dataArray, 1) THEN
                        value := dataArray[index];
                    ELSE
                        // 에러 처리
                        value := 0;
                        errorFlag := TRUE;
                    END_IF
                "
            });
        }

        // 4. NULL 또는 유효성 체크 누락
        if (IsNullCheckMissing(change))
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Critical,
                Category = "포인터 안전성",
                Title = "포인터 유효성 검증 누락",
                Description = "포인터 사용 전 NULL 체크가 없어 시스템 크래시 가능",
                Location = $"{change.FilePath}:{change.Line}",
                Recommendation = @"
                    ❌ 위험: ptr^.value := 100; // ptr이 NULL일 수 있음

                    ✅ 해결:
                    IF ptr <> 0 THEN
                        ptr^.value := 100;
                    ELSE
                        // 에러 처리
                    END_IF
                "
            });
        }

        return issues;
    }
}
```

##### 🟡 Warning (잠재적 버그)
```csharp
public class WarningRuleChecker
{
    public List<QAIssue> CheckWarnings(CodeChange change)
    {
        var issues = new List<QAIssue>();

        // 1. 부동소수점 비교
        if (IsFloatingPointComparison(change))
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Warning,
                Category = "부동소수점 연산",
                Title = "부동소수점 직접 비교 (정밀도 문제)",
                Description = "REAL/LREAL 타입을 = 또는 <> 로 직접 비교 시 오차로 인한 오동작 가능",
                Recommendation = @"
                    ❌ 위험: IF temperature = 25.0 THEN ...

                    ✅ 해결:
                    CONST EPSILON : REAL := 0.001;
                    IF ABS(temperature - 25.0) < EPSILON THEN ...
                "
            });
        }

        // 2. 타이머 값 하드코딩
        if (IsTimerValueHardcoded(change))
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Warning,
                Category = "유지보수성",
                Title = "타이머 값 하드코딩 (조정 어려움)",
                Description = "타이머 값을 직접 입력하면 나중에 변경이 어렵고 실수 유발",
                Recommendation = @"
                    ❌ 나쁨: timer(IN:=start, PT:=T#1500ms);

                    ✅ 좋음:
                    VAR CONSTANT
                        TIMEOUT_MOTOR_START : TIME := T#1500ms;
                    END_VAR
                    timer(IN:=start, PT:=TIMEOUT_MOTOR_START);
                "
            });
        }

        // 3. 매직 넘버 사용
        if (IsMagicNumber(change))
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Warning,
                Category = "가독성",
                Title = "매직 넘버 사용 (의미 불명확)",
                Description = "숫자 리터럴의 의미를 알 수 없어 이해와 유지보수가 어려움",
                Recommendation = @"
                    ❌ 나쁨: IF speed > 1500 THEN ...

                    ✅ 좋음:
                    VAR CONSTANT
                        MAX_SAFE_SPEED : INT := 1500; // rpm
                    END_VAR
                    IF speed > MAX_SAFE_SPEED THEN ...
                "
            });
        }

        // 4. 깊은 중첩 (가독성)
        if (IsDeepNesting(change, threshold: 4))
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Warning,
                Category = "복잡도",
                Title = "과도한 중첩 (가독성 저하)",
                Description = $"중첩 레벨 {change.NestingLevel}은 이해하기 어렵고 실수 유발",
                Recommendation = @"
                    ❌ 나쁨: IF ... THEN
                                IF ... THEN
                                    IF ... THEN
                                        IF ... THEN ...

                    ✅ 좋음: Early return 또는 상태 머신 사용
                    IF NOT condition1 THEN RETURN; END_IF
                    IF NOT condition2 THEN RETURN; END_IF
                    // 실제 로직
                "
            });
        }

        return issues;
    }
}
```

##### 🟢 Info (개선 권장)
```csharp
public class InfoRuleChecker
{
    public List<QAIssue> CheckImprovements(CodeChange change)
    {
        var issues = new List<QAIssue>();

        // 1. 주석 부족
        if (IsCommentMissing(change))
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Info,
                Category = "문서화",
                Title = "복잡한 로직에 주석 권장",
                Description = "수식이나 로직이 복잡하여 나중에 이해하기 어려울 수 있음",
                Recommendation = @"
                    ✅ 권장:
                    // 온도 보정: 센서 오차 ±2도 고려
                    // 공식: corrected = raw * 0.98 + offset
                    correctedTemp := rawTemp * 0.98 + 2.0;
                "
            });
        }

        // 2. 변수명 개선
        if (IsPoorVariableName(change))
        {
            issues.Add(new QAIssue
            {
                Severity = Severity.Info,
                Category = "네이밍",
                Title = "변수명 개선 권장",
                Description = "변수명이 의미를 명확히 전달하지 못함",
                Recommendation = @"
                    ❌ 나쁨: temp, cnt, flg
                    ✅ 좋음: motorTemperature, errorCount, isMotorEnabled
                "
            });
        }

        return issues;
    }
}
```

---

### 2. QA 체크리스트 자동 생성

#### 기능 개요
변경 사항을 분석하여 테스터가 확인해야 할 항목을 자동 생성

```csharp
public class QAChecklistGenerator
{
    public QAChecklist GenerateChecklist(ComparisonResult result)
    {
        var checklist = new QAChecklist();

        // 변수 타입 변경 → 경계값 테스트
        foreach (var change in result.VariableChanges.Where(c => c.OldDataType != c.NewDataType))
        {
            checklist.AddItem(new ChecklistItem
            {
                Category = "경계값 테스트",
                Priority = Priority.High,
                Description = $"{change.VariableName} 타입 변경 테스트",
                TestCases = new[]
                {
                    $"✓ 최소값 테스트: {GetMinValue(change.NewDataType)}",
                    $"✓ 최대값 테스트: {GetMaxValue(change.NewDataType)}",
                    $"✓ 최소값-1 테스트 (언더플로우 확인)",
                    $"✓ 최대값+1 테스트 (오버플로우 확인)",
                    $"✓ 기존 사용 케이스 재검증"
                },
                Rationale = $"타입 변경({change.OldDataType} → {change.NewDataType})으로 값 범위가 변경되어 기존 값이 유효하지 않을 수 있음"
            });
        }

        // 로직 변경 → 시나리오 테스트
        foreach (var change in result.LogicChanges)
        {
            checklist.AddItem(new ChecklistItem
            {
                Category = "기능 테스트",
                Priority = Priority.High,
                Description = $"{change.ElementName} 로직 변경 검증",
                TestCases = new[]
                {
                    "✓ 정상 동작 시나리오 (Happy Path)",
                    "✓ 예외 상황 처리 (비정상 입력)",
                    "✓ 경계 조건 (Edge Cases)",
                    "✓ 동시 호출 테스트 (Race Condition)",
                    "✓ 이전 버전과 동작 비교"
                },
                Rationale = "로직 변경으로 인한 의도하지 않은 부작용 확인 필요"
            });
        }

        // I/O 매핑 변경 → 하드웨어 테스트
        foreach (var change in result.IOMappingChanges)
        {
            checklist.AddItem(new ChecklistItem
            {
                Category = "하드웨어 테스트",
                Priority = Priority.Critical,
                Description = $"{change.VariableName} I/O 주소 변경 검증",
                TestCases = new[]
                {
                    $"✓ 물리적 연결 확인: {change.NewAddress}",
                    "✓ 신호 읽기 테스트 (Input인 경우)",
                    "✓ 신호 쓰기 테스트 (Output인 경우)",
                    "✓ 신호 품질 확인 (노이즈, 지연)",
                    "✓ 배선 도면과 일치 확인"
                },
                Rationale = "I/O 주소 변경으로 잘못된 하드웨어 제어 가능성"
            });
        }

        return checklist;
    }
}

public class ChecklistItem
{
    public string Category { get; init; }
    public Priority Priority { get; init; }
    public string Description { get; init; }
    public string[] TestCases { get; init; }
    public string Rationale { get; init; }
}
```

---

### 3. 일반적인 실수 패턴 데이터베이스

#### TwinCAT ST 휴먼 에러 패턴 (50+ 규칙)

```csharp
public static class CommonMistakePatterns
{
    public static readonly Pattern[] Patterns = new[]
    {
        // 1. 타이머 재사용 실수
        new Pattern
        {
            Name = "타이머 재사용 오류",
            Regex = @"(\w+)\s*\(\s*IN\s*:=.*?\);.*?\1\s*\(\s*IN\s*:=",
            Severity = Severity.Critical,
            Description = "동일한 타이머 인스턴스를 여러 곳에서 사용",
            WhyDangerous = "타이머는 상태를 가지므로 재사용 시 예상치 못한 동작",
            CorrectWay = @"
                ❌ 잘못됨:
                timer1(IN:=startA, PT:=T#1s);
                timer1(IN:=startB, PT:=T#2s); // 같은 타이머!

                ✅ 올바름:
                timerA(IN:=startA, PT:=T#1s);
                timerB(IN:=startB, PT:=T#2s); // 별도 타이머
            "
        },

        // 2. CASE 문에 ELSE 누락
        new Pattern
        {
            Name = "CASE 문 ELSE 누락",
            Regex = @"CASE\s+\w+\s+OF.*?END_CASE",
            CheckLogic = code => !code.Contains("ELSE"),
            Severity = Severity.Warning,
            Description = "CASE 문에 ELSE 절이 없어 예상치 못한 값 처리 안됨",
            WhyDangerous = "열거형 외 값이 들어오면 아무 동작도 하지 않음",
            CorrectWay = @"
                ❌ 위험:
                CASE state OF
                    1: doA();
                    2: doB();
                END_CASE // 3이 들어오면?

                ✅ 안전:
                CASE state OF
                    1: doA();
                    2: doB();
                ELSE
                    errorFlag := TRUE;
                END_CASE
            "
        },

        // 3. FB_EXIT 없는 조건부 RETURN
        new Pattern
        {
            Name = "조건부 RETURN 시 정리 누락",
            Regex = @"IF.*?THEN\s+RETURN;\s+END_IF",
            Severity = Severity.Warning,
            Description = "조건부 RETURN 전에 리소스 정리가 없을 수 있음",
            WhyDangerous = "메모리 누수, 파일 미닫힘 등",
            CorrectWay = @"
                ❌ 위험:
                IF error THEN
                    RETURN; // 리소스 정리 안함
                END_IF

                ✅ 안전:
                IF error THEN
                    CleanupResources();
                    RETURN;
                END_IF
            "
        },

        // 4. 비트 연산자 우선순위 실수
        new Pattern
        {
            Name = "비트 연산자 우선순위 혼동",
            Regex = @"\w+\s+AND\s+\w+\s+OR\s+\w+",
            Severity = Severity.Warning,
            Description = "AND/OR 혼용 시 우선순위 명시 필요",
            WhyDangerous = "의도와 다른 결과 (AND가 OR보다 먼저)",
            CorrectWay = @"
                ❌ 혼동 가능:
                IF a AND b OR c THEN ... // (a AND b) OR c

                ✅ 명확함:
                IF (a AND b) OR c THEN ...
                또는
                IF a AND (b OR c) THEN ...
            "
        },

        // 5. 시간 단위 혼동
        new Pattern
        {
            Name = "시간 단위 혼동",
            Regex = @"T#\d+(?!ms|s|m|h|d)",
            Severity = Severity.Critical,
            Description = "시간 리터럴에 단위 누락",
            WhyDangerous = "기본 단위가 ms라 예상과 다른 타이밍",
            CorrectWay = @"
                ❌ 위험: PT := T#1000; // 1000ms? 1000s?
                ✅ 명확: PT := T#1000ms; 또는 PT := T#1s;
            "
        },

        // ... 45개 더 ...
    };
}
```

---

### 4. 코드 리뷰 코멘트 자동 생성

#### 기능 개요
GitHub PR 스타일의 인라인 코멘트 자동 생성

```csharp
public class CodeReviewCommentGenerator
{
    public List<ReviewComment> GenerateComments(ComparisonResult result)
    {
        var comments = new List<ReviewComment>();

        foreach (var change in result.LogicChanges)
        {
            var issues = _ruleChecker.CheckAll(change);

            foreach (var issue in issues)
            {
                comments.Add(new ReviewComment
                {
                    FilePath = issue.Location.Split(':')[0],
                    Line = int.Parse(issue.Location.Split(':')[1]),
                    Severity = issue.Severity,
                    Author = "TwinCAT QA Bot",
                    Timestamp = DateTime.Now,
                    Comment = FormatComment(issue)
                });
            }
        }

        return comments;
    }

    private string FormatComment(QAIssue issue)
    {
        return $@"
## {GetEmojiForSeverity(issue.Severity)} {issue.Title}

**카테고리**: {issue.Category}
**심각도**: {issue.Severity}

### 문제점
{issue.Description}

### 왜 위험한가?
{issue.WhyDangerous}

### 권장 해결 방법
{issue.Recommendation}

### 예시
```iecst
{string.Join("\n", issue.Examples)}
```

### 참고 문서
- [TwinCAT 베스트 프랙티스](링크)
- [IEC 61131-3 표준](링크)

---
*이 코멘트는 TwinCAT QA Tool에 의해 자동 생성되었습니다.*
*문제가 해결되면 이 코멘트를 'Resolved'로 표시하세요.*
";
    }

    private string GetEmojiForSeverity(Severity s) => s switch
    {
        Severity.Critical => "🔴",
        Severity.Warning => "🟡",
        Severity.Info => "🟢",
        _ => "ℹ️"
    };
}
```

#### UI에 표시
```xml
<!-- Code Review 탭 추가 -->
<TabItem Header="Code Review (QA)">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- 요약 -->
        <Border Grid.Row="0" Background="#FFF3CD" Padding="10" Margin="10">
            <StackPanel>
                <TextBlock Text="QA 자동 리뷰 결과" FontWeight="Bold" FontSize="16"/>
                <StackPanel Orientation="Horizontal" Margin="0,10,0,0">
                    <StackPanel Orientation="Horizontal" Margin="0,0,20,0">
                        <Ellipse Width="12" Height="12" Fill="#DC3545" Margin="0,0,5,0"/>
                        <TextBlock Text="{Binding CriticalIssueCount}"/>
                        <TextBlock Text=" Critical" Margin="5,0,0,0"/>
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Margin="0,0,20,0">
                        <Ellipse Width="12" Height="12" Fill="#FFC107" Margin="0,0,5,0"/>
                        <TextBlock Text="{Binding WarningCount}"/>
                        <TextBlock Text=" Warning" Margin="5,0,0,0"/>
                    </StackPanel>
                    <StackPanel Orientation="Horizontal">
                        <Ellipse Width="12" Height="12" Fill="#28A745" Margin="0,0,5,0"/>
                        <TextBlock Text="{Binding InfoCount}"/>
                        <TextBlock Text=" Info" Margin="5,0,0,0"/>
                    </StackPanel>
                </StackPanel>
            </StackPanel>
        </Border>

        <!-- 코멘트 리스트 -->
        <DataGrid Grid.Row="1" ItemsSource="{Binding ReviewComments}" Margin="10">
            <DataGrid.Columns>
                <DataGridTextColumn Header="심각도" Width="80">
                    <DataGridTextColumn.Binding>
                        <Binding Path="Severity" Converter="{StaticResource SeverityToEmojiConverter}"/>
                    </DataGridTextColumn.Binding>
                </DataGridTextColumn>
                <DataGridTextColumn Header="파일" Binding="{Binding FilePath}" Width="200"/>
                <DataGridTextColumn Header="라인" Binding="{Binding Line}" Width="60"/>
                <DataGridTextColumn Header="제목" Binding="{Binding Issue.Title}" Width="*"/>
                <DataGridTextColumn Header="카테고리" Binding="{Binding Issue.Category}" Width="120"/>

                <!-- 상세 보기 버튼 -->
                <DataGridTemplateColumn Header="" Width="80">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <Button Content="상세"
                                    Command="{Binding DataContext.ShowDetailCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                    CommandParameter="{Binding}"/>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>
    </Grid>
</TabItem>
```

---

### 5. HTML 리포트 생성 (QA 관점)

#### 리포트 구조
```html
<!DOCTYPE html>
<html>
<head>
    <title>TwinCAT QA 리뷰 리포트</title>
    <style>
        .critical { background: #dc3545; color: white; }
        .warning { background: #ffc107; }
        .info { background: #28a745; color: white; }
        .code { background: #f8f9fa; padding: 10px; font-family: monospace; }
    </style>
</head>
<body>
    <h1>TwinCAT 코드 QA 리뷰 리포트</h1>

    <section class="summary">
        <h2>요약</h2>
        <ul>
            <li>🔴 Critical: 3건 - <strong>즉시 수정 필요</strong></li>
            <li>🟡 Warning: 7건 - 검토 후 수정 권장</li>
            <li>🟢 Info: 5건 - 개선 권장</li>
        </ul>
    </section>

    <section class="checklist">
        <h2>✅ QA 테스트 체크리스트</h2>
        <h3>경계값 테스트 (우선순위: High)</h3>
        <ul>
            <li><input type="checkbox"/> motorSpeed 타입 변경 테스트
                <ul>
                    <li>최소값: -32768</li>
                    <li>최대값: 32767</li>
                    <li>오버플로우 확인</li>
                </ul>
            </li>
        </ul>
    </section>

    <section class="issues">
        <h2>🔍 발견된 이슈</h2>

        <div class="issue critical">
            <h3>🔴 [Critical] 타입 축소로 인한 데이터 손실 가능</h3>
            <p><strong>위치</strong>: FB_MotorControl.TcPOU:45</p>
            <p><strong>카테고리</strong>: 타입 안전성</p>

            <h4>문제점</h4>
            <p>DINT → INT 변경으로 값 범위 초과 가능</p>

            <h4>변경 내용</h4>
            <div class="code">
                <pre>
- motorSpeed : DINT := 50000;
+ motorSpeed : INT := 50000; // ❌ 오버플로우!
                </pre>
            </div>

            <h4>권장 해결 방법</h4>
            <div class="code">
                <pre>
IF speed > 32767 OR speed < -32768 THEN
    // 에러 처리
    errorFlag := TRUE;
ELSE
    motorSpeed := INT_TO_DINT(speed);
END_IF
                </pre>
            </div>
        </div>
    </section>

    <section class="metrics">
        <h2>📊 코드 품질 지표</h2>
        <table>
            <tr>
                <th>지표</th>
                <th>값</th>
                <th>상태</th>
            </tr>
            <tr>
                <td>Critical 이슈 밀도</td>
                <td>0.1 / 100 LOC</td>
                <td>⚠️ 개선 필요</td>
            </tr>
            <tr>
                <td>초기화 누락률</td>
                <td>5%</td>
                <td>✅ 양호</td>
            </tr>
            <tr>
                <td>주석 커버리지</td>
                <td>75%</td>
                <td>✅ 우수</td>
            </tr>
        </table>
    </section>
</body>
</html>
```

---

## 🎯 우선순위별 QA 기능 통합

### 1순위 (Side-by-Side Diff) + QA
- 변경 라인에 **인라인 QA 코멘트** 표시
- 위험한 변경 부분 **빨간색 강조**
- 마우스 오버 시 **권장 사항 툴팁**

### 2순위 (Impact Analysis) + QA
- 영향 받는 코드의 **휴먼 에러 가능성** 평가
- 테스트 **우선순위 자동 지정**
- 위험도 높은 변경은 **Critical 마크**

### 3순위 (Reason Inference) + QA
- 변경 이유 + **왜 위험한지** 함께 표시
- 베스트 프랙티스 **위반 여부** 자동 감지

---

## 📊 QA 메트릭 대시보드

```csharp
public class QAMetrics
{
    public int TotalIssues { get; set; }
    public int CriticalIssues { get; set; }
    public int WarningIssues { get; set; }
    public int InfoIssues { get; set; }

    public double IssuesPerHundredLOC { get; set; }
    public double InitializationCoverage { get; set; }  // 초기화율
    public double CommentCoverage { get; set; }          // 주석 커버리지
    public double TypeSafetyCoverage { get; set; }       // 타입 안전성
    public double ErrorHandlingCoverage { get; set; }    // 에러 처리율

    public List<TopIssue> TopIssues { get; set; }        // 가장 많이 발생하는 이슈
}
```

---

## 🚀 구현 로드맵 (QA 기능 포함)

### Phase 0-A + QA (2주)
- Lexer 구현
- **기본 규칙 20개 구현** (Critical만)

### Phase 5 + QA (4주)
- Side-by-Side Diff
- **인라인 QA 코멘트 표시**
- **휴먼 에러 하이라이트**

### Phase 1 + QA (1주)
- I/O Mapping 이유 분석
- **I/O 관련 안전 규칙 추가**

### Phase 0-B/C/D + QA (6주)
- Parser 완성
- **고급 규칙 30개 추가** (Warning, Info)

### Phase 4 + QA (6-8주)
- Impact Analysis
- **영향도 기반 테스트 우선순위**
- **QA 체크리스트 자동 생성**

### Phase 3 + QA (2-3주)
- Reason Inference
- **베스트 프랙티스 가이드 통합**

### Phase 6 + QA (3주)
- 통합 및 최적화
- **HTML 리포트 생성**
- **QA 메트릭 대시보드**

---

**문서 버전**: 1.0
**최종 업데이트**: 2025-11-24
**목표**: 휴먼 에러를 사전에 방지하여 코드 품질 향상 및 안전성 확보
