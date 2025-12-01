# 🚀 빠른 시작 가이드: TwinCAT Folder Comparison Tool

**작성일**: 2025-11-24
**버전**: 0.1.0 (Alpha)
**목적**: 5분 안에 도구 실행하고 사용하기

> ⚠️ **현재 상태**: 폴더 비교 기능만 완전히 동작합니다. 코드 품질 분석 기능은 개발 중입니다.

---

## ⚡ 3가지 실행 방법

### 방법 1: CLI (명령줄) - 가장 빠름 ⚡

```bash
cd D:\01. Vscode\Twincat\features\twincat-code-qa-tool\src\TwinCatQA.CLI

dotnet run -- compare \
  --source "C:\TwinCAT\프로젝트_V1" \
  --target "C:\TwinCAT\프로젝트_V2"
```

### 방법 2: UI (그래픽) - 가장 직관적 👁️

```bash
cd D:\01. Vscode\Twincat\features\twincat-code-qa-tool\src\TwinCatQA.UI

dotnet run
```

그 후 화면에서:
1. Source/Target 폴더 선택 (Browse 버튼)
2. Start Compare 클릭
3. 결과 확인

### 방법 3: 빌드 후 실행 - 가장 빠른 속도 🚄

```bash
cd D:\01. Vscode\Twincat\features\twincat-code-qa-tool
dotnet build --configuration Release

# CLI 실행
.\src\TwinCatQA.CLI\bin\Release\net9.0\TwinCatQA.CLI.exe compare --source "경로1" --target "경로2"

# UI 실행
.\src\TwinCatQA.UI\bin\Release\net8.0-windows\TwinCatQA.UI.exe
```

---

## 📋 필수 조건

- **.NET 8.0 SDK** 이상
- **Windows 10/11** (UI 사용 시)

---

## 🎯 기본 사용 예제

### 예제 1: 모든 변경 사항 비교

```bash
dotnet run -- compare \
  --source "D:\Projects\MachineControl_V1.0" \
  --target "D:\Projects\MachineControl_V2.0"

# RazorLight (HTML 리포트)
Install-Package RazorLight -Version 2.3.0

# iText 7 Community (PDF 리포트)
Install-Package itext7 -Version 8.0.0

# System.Text.Json (JSON 직렬화)
# .NET Framework 4.8: Install-Package System.Text.Json -Version 7.0.0
# .NET 6+: 내장됨

# LINQ to XML (TwinCAT XML 파싱)
# .NET Framework 4.8: 내장됨
# .NET 6+: 내장됨
```

### 1.3 프로젝트 구조 생성

```
TwinCatQA/
├── TwinCatQA.sln
├── TwinCatQA/                      # VSIX 프로젝트
│   ├── source.extension.vsixmanifest
│   ├── TwinCatQAPackage.cs
│   └── ToolWindows/
│       └── QualityWindow.cs
├── TwinCatQA.Domain/              # 도메인 모델
│   ├── Models/
│   │   ├── CodeFile.cs
│   │   ├── Violation.cs
│   │   └── ValidationSession.cs
│   └── Contracts/
│       ├── IValidationRule.cs
│       └── IValidationEngine.cs
├── TwinCatQA.Infrastructure/      # 인프라스트럭처
│   ├── Parsers/
│   │   ├── AntlrParserService.cs
│   │   └── Grammars/
│   │       └── StructuredText.g4  # ANTLR4 문법
│   ├── Git/
│   │   └── LibGit2Service.cs
│   └── Storage/
│       └── FileSystemRepository.cs
├── TwinCatQA.Application/         # 응용 계층
│   ├── ValidationEngine.cs
│   ├── ReportGenerator.cs
│   └── Rules/
│       ├── KoreanCommentRule.cs
│       ├── CyclomaticComplexityRule.cs
│       └── NamingConventionRule.cs
└── TwinCatQA.Tests/               # 단위 테스트
    ├── ParserTests.cs
    ├── RuleTests.cs
    └── IntegrationTests.cs
```

---

## 2. ANTLR4 문법 파일 생성

### 2.1 StructuredText.g4 작성

**파일**: `TwinCatQA.Infrastructure/Parsers/Grammars/StructuredText.g4`

```antlr
grammar StructuredText;

// 프로그램 최상위 규칙
program
    : declaration* implementation* EOF
    ;

// 선언부
declaration
    : varDeclaration
    | functionBlockDeclaration
    | dataTypeDeclaration
    ;

varDeclaration
    : 'VAR' varDeclList 'END_VAR'
    | 'VAR_INPUT' varDeclList 'END_VAR'
    | 'VAR_OUTPUT' varDeclList 'END_VAR'
    ;

varDeclList
    : varDecl (',' varDecl)*
    ;

varDecl
    : IDENTIFIER ':' dataType (',' IDENTIFIER ':' dataType)* ';'
    ;

functionBlockDeclaration
    : 'FUNCTION_BLOCK' IDENTIFIER declaration* 'END_FUNCTION_BLOCK'
    ;

dataTypeDeclaration
    : 'TYPE' IDENTIFIER ':' structType 'END_TYPE'
    ;

structType
    : 'STRUCT' varDeclList 'END_STRUCT'
    | '(' enumValue (',' enumValue)* ')'  // ENUM
    ;

enumValue
    : IDENTIFIER ('=' INTEGER_LITERAL)?
    ;

// 구현부
implementation
    : statement*
    ;

statement
    : assignmentStatement
    | ifStatement
    | caseStatement
    | forStatement
    | whileStatement
    | repeatStatement
    | returnStatement
    | ';'
    ;

assignmentStatement
    : variable ':=' expression ';'
    ;

ifStatement
    : 'IF' expression 'THEN' statement*
      ('ELSIF' expression 'THEN' statement*)*
      ('ELSE' statement*)?
      'END_IF'
    ;

caseStatement
    : 'CASE' expression 'OF'
      caseElement+
      ('ELSE' statement*)?
      'END_CASE'
    ;

caseElement
    : constantExpression (',' constantExpression)* ':' statement*
    ;

forStatement
    : 'FOR' IDENTIFIER ':=' expression 'TO' expression ('BY' expression)? 'DO'
      statement*
      'END_FOR'
    ;

whileStatement
    : 'WHILE' expression 'DO'
      statement*
      'END_WHILE'
    ;

repeatStatement
    : 'REPEAT'
      statement*
      'UNTIL' expression
      'END_REPEAT'
    ;

returnStatement
    : 'RETURN' ';'
    ;

// 표현식
expression
    : literal
    | variable
    | functionCall
    | '(' expression ')'
    | expression op=('*'|'/'|'MOD') expression
    | expression op=('+'|'-') expression
    | expression op=('<'|'<='|'>'|'>='|'='|'<>') expression
    | expression op=('AND'|'&') expression
    | expression op=('OR') expression
    | expression op=('XOR') expression
    | 'NOT' expression
    ;

constantExpression
    : literal
    | IDENTIFIER
    ;

variable
    : IDENTIFIER ('.' IDENTIFIER)* ('[' expression ']')*
    ;

functionCall
    : IDENTIFIER '(' (expression (',' expression)*)? ')'
    ;

// 데이터 타입
dataType
    : primitiveType
    | IDENTIFIER  // 사용자 정의 타입
    | 'ARRAY' '[' INTEGER_LITERAL '..' INTEGER_LITERAL ']' 'OF' dataType
    ;

primitiveType
    : 'BOOL' | 'BYTE' | 'WORD' | 'DWORD' | 'LWORD'
    | 'SINT' | 'USINT' | 'INT' | 'UINT' | 'DINT' | 'UDINT' | 'LINT' | 'ULINT'
    | 'REAL' | 'LREAL'
    | 'STRING'
    | 'TIME' | 'DATE' | 'TIME_OF_DAY' | 'DATE_AND_TIME'
    ;

// 리터럴
literal
    : INTEGER_LITERAL
    | REAL_LITERAL
    | STRING_LITERAL
    | BOOLEAN_LITERAL
    ;

// 토큰 정의
IDENTIFIER : [a-zA-Z_][a-zA-Z0-9_]* ;
INTEGER_LITERAL : [0-9]+ ;
REAL_LITERAL : [0-9]+ '.' [0-9]+ ;
STRING_LITERAL : '\'' (~'\'')* '\'' ;
BOOLEAN_LITERAL : 'TRUE' | 'FALSE' ;

// 주석
COMMENT : '(*' .*? '*)' -> skip ;
LINE_COMMENT : '//' ~[\r\n]* -> skip ;

// 공백
WS : [ \t\r\n]+ -> skip ;
```

### 2.2 ANTLR4 컴파일

```bash
# ANTLR4 JAR 다운로드 (최신 버전)
# https://www.antlr.org/download.html

# 문법 파일 컴파일 (C# 타겟)
cd TwinCatQA.Infrastructure/Parsers/Grammars
java -jar antlr-4.11.1-complete.jar -Dlanguage=CSharp StructuredText.g4

# 생성된 파일:
#   - StructuredTextLexer.cs
#   - StructuredTextParser.cs
#   - StructuredTextVisitor.cs
#   - StructuredTextBaseVisitor.cs
```

---

## 3. 첫 번째 검증 규칙 구현

### 3.1 KoreanCommentRule.cs

**파일**: `TwinCatQA.Application/Rules/KoreanCommentRule.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TwinCatQA.Domain.Contracts;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Application.Rules
{
    /// <summary>
    /// 원칙 1: 한글 우선 - 주석이 한글로 작성되었는지 검증
    /// </summary>
    public class KoreanCommentRule : IValidationRule
    {
        private double _requiredKoreanRatio = 0.95;  // 기본값: 95%

        public string RuleId => "FR-2-KOREAN-COMMENT";
        public string RuleName => "한글 주석 검증";
        public string Description => "모든 주석이 한글로 작성되었는지 확인합니다.";
        public ConstitutionPrinciple RelatedPrinciple => ConstitutionPrinciple.KoreanFirst;
        public ViolationSeverity DefaultSeverity => ViolationSeverity.High;
        public bool IsEnabled { get; set; } = true;
        public ProgrammingLanguage[] SupportedLanguages => new[] { ProgrammingLanguage.ST };

        public void Configure(Dictionary<string, object> parameters)
        {
            if (parameters.TryGetValue("required_korean_ratio", out var ratio))
            {
                _requiredKoreanRatio = Convert.ToDouble(ratio);
            }
        }

        public IEnumerable<Violation> Validate(CodeFile file)
        {
            var violations = new List<Violation>();

            // AST에서 주석 추출 (IParserService 사용)
            var comments = ExtractComments(file.Ast);

            if (comments.Count == 0)
            {
                // 주석이 없으면 위반
                violations.Add(new Violation
                {
                    RuleId = RuleId,
                    RuleName = RuleName,
                    RelatedPrinciple = RelatedPrinciple,
                    Severity = ViolationSeverity.Medium,
                    FilePath = file.FilePath,
                    Line = 1,
                    Column = 0,
                    Message = "주석이 전혀 없습니다. 코드의 목적과 동작을 설명하는 한글 주석을 추가하세요.",
                    Suggestion = "Function Block 상단에 목적 설명 주석을 추가하세요.",
                    DocumentationUrl = "file:///memory/constitution.md#원칙-1"
                });
            }
            else
            {
                // 한글 주석 비율 계산
                int koreanCommentCount = 0;

                foreach (var (line, content) in comments)
                {
                    if (IsKoreanComment(content))
                    {
                        koreanCommentCount++;
                    }
                    else
                    {
                        // 비한글 주석 위반
                        violations.Add(new Violation
                        {
                            RuleId = RuleId,
                            RuleName = RuleName,
                            RelatedPrinciple = RelatedPrinciple,
                            Severity = DefaultSeverity,
                            FilePath = file.FilePath,
                            Line = line,
                            Column = 0,
                            CodeSnippet = content,
                            Message = $"주석이 한글이 아닙니다: \"{content.Trim()}\"",
                            Suggestion = "주석을 한글로 작성하세요.",
                            DocumentationUrl = "file:///memory/constitution.md#원칙-1"
                        });
                    }
                }

                double koreanRatio = (double)koreanCommentCount / comments.Count;

                if (koreanRatio < _requiredKoreanRatio)
                {
                    violations.Add(new Violation
                    {
                        RuleId = RuleId,
                        RuleName = RuleName,
                        RelatedPrinciple = RelatedPrinciple,
                        Severity = ViolationSeverity.Medium,
                        FilePath = file.FilePath,
                        Line = 1,
                        Column = 0,
                        Message = $"한글 주석 비율이 {koreanRatio:P1}로 목표({_requiredKoreanRatio:P1})에 미달합니다.",
                        Suggestion = "영어 주석을 한글로 번역하세요."
                    });
                }
            }

            return violations;
        }

        private Dictionary<int, string> ExtractComments(SyntaxTree ast)
        {
            // ANTLR4 토큰 스트림에서 COMMENT 토큰 추출
            // 실제 구현은 IParserService에 위임
            // 여기서는 간략화
            return new Dictionary<int, string>();
        }

        private bool IsKoreanComment(string comment)
        {
            // 한글 유니코드 범위: U+AC00 ~ U+D7A3 (가-힣)
            var koreanPattern = @"[\uAC00-\uD7A3]";
            var koreanMatches = Regex.Matches(comment, koreanPattern);

            // 주석에 한글이 10글자 이상 포함되어 있으면 한글 주석으로 간주
            return koreanMatches.Count >= 10;
        }
    }
}
```

---

## 4. Tool Window 생성

### 4.1 QualityWindow.cs

**파일**: `TwinCatQA/ToolWindows/QualityWindow.cs`

```csharp
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace TwinCatQA.ToolWindows
{
    [Guid("a3f2e1d4-5b6c-7a8b-9c0d-1e2f3a4b5c6d")]
    public class QualityWindow : ToolWindowPane
    {
        public QualityWindow() : base(null)
        {
            this.Caption = "TwinCAT 품질 검증";

            // WPF UserControl 생성
            this.Content = new QualityWindowControl();
        }
    }
}
```

### 4.2 QualityWindowControl.xaml

**파일**: `TwinCatQA/ToolWindows/QualityWindowControl.xaml`

```xml
<UserControl x:Class="TwinCatQA.ToolWindows.QualityWindowControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             mc:Ignorable="d"
             d:DesignHeight="450" d:DesignWidth="800">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 도구 모음 -->
        <ToolBar Grid.Row="0">
            <Button x:Name="RunValidationButton" Content="전체 검증" Click="RunValidation_Click"/>
            <Button x:Name="RunIncrementalButton" Content="증분 검증" Click="RunIncremental_Click"/>
            <Separator/>
            <Button x:Name="OpenReportButton" Content="리포트 열기" Click="OpenReport_Click"/>
            <Button x:Name="SettingsButton" Content="설정" Click="Settings_Click"/>
        </ToolBar>

        <!-- 검증 결과 표시 -->
        <TabControl Grid.Row="1">
            <TabItem Header="위반 사항">
                <DataGrid x:Name="ViolationsGrid"
                          AutoGenerateColumns="False"
                          IsReadOnly="True"
                          SelectionMode="Single"
                          MouseDoubleClick="ViolationsGrid_DoubleClick">
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="심각도" Binding="{Binding Severity}" Width="80"/>
                        <DataGridTextColumn Header="규칙" Binding="{Binding RuleName}" Width="150"/>
                        <DataGridTextColumn Header="파일" Binding="{Binding FilePath}" Width="200"/>
                        <DataGridTextColumn Header="라인" Binding="{Binding Line}" Width="60"/>
                        <DataGridTextColumn Header="설명" Binding="{Binding Message}" Width="*"/>
                    </DataGrid.Columns>
                </DataGrid>
            </TabItem>

            <TabItem Header="품질 점수">
                <StackPanel Margin="20">
                    <TextBlock Text="전체 품질 점수" FontSize="16" FontWeight="Bold"/>
                    <TextBlock x:Name="QualityScoreText" Text="--" FontSize="48" Margin="0,10,0,20"/>

                    <TextBlock Text="헌장 준수율" FontSize="16" FontWeight="Bold" Margin="0,20,0,10"/>
                    <ListView x:Name="ConstitutionComplianceList">
                        <ListView.View>
                            <GridView>
                                <GridViewColumn Header="원칙" Width="200"/>
                                <GridViewColumn Header="준수율" Width="100"/>
                            </GridView>
                        </ListView.View>
                    </ListView>
                </StackPanel>
            </TabItem>
        </TabControl>

        <!-- 상태 표시줄 -->
        <StatusBar Grid.Row="2">
            <StatusBarItem>
                <TextBlock x:Name="StatusText" Text="준비됨"/>
            </StatusBarItem>
        </StatusBar>
    </Grid>
</UserControl>
```

---

## 5. 첫 검증 실행

### 5.1 디버그 실행

```bash
# Visual Studio에서 F5 누르기
# → 새로운 Visual Studio 실험적 인스턴스가 실행됨

# TwinCAT 프로젝트 열기
# → View 메뉴 → "TwinCAT 품질 검증" 선택
# → Tool Window 표시됨

# "전체 검증" 버튼 클릭
# → 검증 시작
# → 위반 사항 목록 표시
```

### 5.2 수동 테스트

```bash
# 테스트용 TwinCAT 프로젝트 생성
# POUs/FB_TestController.TcPOU 파일:

FUNCTION_BLOCK FB_TestController
VAR_INPUT
    temp : REAL;  // Temperature input
END_VAR
VAR
    alarm : BOOL;
END_VAR

(* This is a test function block *)  // 영어 주석 → 위반
IF temp > 100.0 THEN
    alarm := TRUE;
END_IF;
END_FUNCTION_BLOCK
```

**예상 결과**:
- 위반 1: "주석이 한글이 아닙니다: (* This is a test function block *)"
- 위반 2: "주석이 한글이 아닙니다: // Temperature input"

---

## 6. 설정 파일 생성

### 6.1 .twincat-qa/config.yaml

```yaml
# TwinCAT QA 도구 설정 파일

global:
  enabled: true
  report_format:
    - html
    - pdf
  auto_open_report: true
  output_dir: .twincat-qa/reports
  retention_months: 12

rules:
  korean_comment:
    enabled: true
    severity: high
    required_korean_ratio: 0.95

  cyclomatic_complexity:
    enabled: true
    severity: medium
    medium_threshold: 10
    high_threshold: 15

  naming_convention:
    enabled: true
    severity: high

git:
  pre_commit:
    enabled: false  # 첫 실행 시 비활성화
    block_on_critical: true
    incremental_validation: true

report:
  include_charts: true
  include_code_snippets: true
```

---

## 7. 다음 단계

### 7.1 추가 규칙 구현
- [ ] `CyclomaticComplexityRule` (FR-1)
- [ ] `NamingConventionRule` (FR-7)
- [ ] `StateMachineValidationRule` (FR-6)

### 7.2 리포트 생성 기능
- [ ] Razor 템플릿 작성
- [ ] HTML 리포트 생성
- [ ] PDF 변환

### 7.3 Git 통합
- [ ] LibGit2Sharp 통합
- [ ] Pre-commit hook 설치
- [ ] 증분 검증 구현

---

## 8. 문제 해결 (Troubleshooting)

### Q: ANTLR4 파싱 오류 발생
**A**: `StructuredText.g4` 문법 파일에 오타가 없는지 확인하고, 테스트 코드를 간소화하세요.

### Q: Visual Studio 확장이 로드되지 않음
**A**: `source.extension.vsixmanifest`에서 지원하는 VS 버전을 확인하세요.

### Q: Git 훅이 작동하지 않음
**A**: Git Bash가 설치되어 있는지 확인하고, 훅 스크립트 실행 권한을 체크하세요.

---

**빠른 시작 가이드 완료**: 2025-11-20
**다음 문서**: plan.md (최종 구현 계획서)
