# TwinCAT 코드 비교 도구 - 우선순위별 상세 설계안

**작성일**: 2025-11-24
**버전**: 1.0
**기반 문서**: [requirements_specification.md](requirements_specification.md)

---

## 📐 전체 아키텍처 개요

### 계층 구조 (Clean Architecture 유지)

```
┌─────────────────────────────────────────────────────────┐
│                   Presentation Layer                     │
│  ┌──────────────┬──────────────┬─────────────────────┐  │
│  │ FolderComp   │ SideBySide   │ ImpactAnalysis     │  │
│  │ Window       │ DiffViewer   │ HeatmapView        │  │
│  └──────────────┴──────────────┴─────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                          ↓ MVVM
┌─────────────────────────────────────────────────────────┐
│                   Application Layer                      │
│  ┌──────────────┬──────────────┬─────────────────────┐  │
│  │ DiffService  │ ImpactAnal   │ ReasonInference    │  │
│  │              │ yzer         │ Service            │  │
│  └──────────────┴──────────────┴─────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                          ↓ Interfaces
┌─────────────────────────────────────────────────────────┐
│                   Infrastructure Layer                   │
│  ┌──────────────┬──────────────┬─────────────────────┐  │
│  │ DiffEngine   │ CallGraph    │ PatternMatcher     │  │
│  │ (DiffPlex)   │ Builder      │ (NLP)              │  │
│  └──────────────┴──────────────┴─────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                          ↓ Uses
┌─────────────────────────────────────────────────────────┐
│                     Domain Layer                         │
│  ┌──────────────┬──────────────┬─────────────────────┐  │
│  │ DiffResult   │ ImpactGraph  │ ChangeReason       │  │
│  │ Models       │ Models       │ Models             │  │
│  └──────────────┴──────────────┴─────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

---

## 🎯 1순위: Side-by-Side Diff Viewer

### 아키텍처 설계

#### 모듈 구성

```
TwinCatQA.Domain
├── Models/
│   ├── DiffModels/
│   │   ├── DiffResult.cs              # Diff 결과 모델
│   │   ├── DiffLine.cs                # 라인별 변경 정보
│   │   ├── DiffHunk.cs                # 변경 블록
│   │   └── SyntaxToken.cs             # 문법 토큰
│   └── Services/
│       └── IDiffService.cs            # Diff 서비스 인터페이스

TwinCatQA.Application
├── Services/
│   ├── DiffService.cs                 # Diff 오케스트레이션
│   └── SyntaxHighlightService.cs     # 문법 강조 서비스

TwinCatQA.Infrastructure
├── Diff/
│   ├── DiffEngine.cs                  # DiffPlex 래퍼
│   ├── MyersDiffAlgorithm.cs          # Myers 알고리즘
│   └── PatienceDiffAlgorithm.cs       # Patience 알고리즘
├── Syntax/
│   ├── STSyntaxHighlighter.cs         # ST 문법 강조
│   └── ANTLR4/
│       ├── STLexer.g4                 # ST Lexer 문법
│       └── STParser.g4                # ST Parser 문법

TwinCatQA.UI
├── Views/
│   ├── SideBySideDiffWindow.xaml      # Side-by-Side 창
│   └── Controls/
│       ├── DiffTextEditor.xaml        # 커스텀 텍스트 편집기
│       └── LineNumberMargin.xaml      # 라인 번호 표시
├── ViewModels/
│   └── SideBySideDiffViewModel.cs     # ViewModel
└── Converters/
    ├── DiffLineToColorConverter.cs    # 변경 유형 → 색상
    └── SyntaxTokenToColorConverter.cs # 토큰 → 색상
```

#### 클래스 다이어그램

```csharp
// Domain Layer
namespace TwinCatQA.Domain.Models;

public enum DiffChangeType
{
    Unchanged,    // 변경 없음
    Added,        // 추가
    Deleted,      // 삭제
    Modified      // 수정
}

public class DiffLine
{
    public int? OldLineNumber { get; init; }
    public int? NewLineNumber { get; init; }
    public string Content { get; init; } = string.Empty;
    public DiffChangeType ChangeType { get; init; }
    public List<SyntaxToken> Tokens { get; init; } = new();
}

public class DiffHunk
{
    public int OldStartLine { get; init; }
    public int NewStartLine { get; init; }
    public List<DiffLine> Lines { get; init; } = new();
    public bool IsCollapsed { get; set; } = false;
}

public class DiffResult
{
    public string OldFilePath { get; init; } = string.Empty;
    public string NewFilePath { get; init; } = string.Empty;
    public List<DiffHunk> Hunks { get; init; } = new();
    public int TotalAdded { get; init; }
    public int TotalDeleted { get; init; }
    public int TotalModified { get; init; }
}

public class SyntaxToken
{
    public string Text { get; init; } = string.Empty;
    public TokenType Type { get; init; }
    public int StartColumn { get; init; }
    public int EndColumn { get; init; }
}

public enum TokenType
{
    Keyword,      // IF, THEN, VAR, END_VAR 등
    Identifier,   // 변수명, 함수명
    Operator,     // +, -, :=, AND, OR
    Literal,      // 숫자, 문자열
    Comment,      // // 주석, (* 주석 *)
    Whitespace
}

// Application Layer
namespace TwinCatQA.Application.Services;

public class DiffService : IDiffService
{
    private readonly IDiffEngine _diffEngine;
    private readonly ISyntaxHighlightService _syntaxService;

    public DiffResult ComputeDiff(string oldContent, string newContent)
    {
        // 1. DiffEngine으로 라인별 비교
        var rawDiff = _diffEngine.Diff(oldContent, newContent);

        // 2. Hunk 단위로 그룹화
        var hunks = GroupIntoHunks(rawDiff);

        // 3. 각 라인에 문법 강조 적용
        foreach (var hunk in hunks)
        {
            foreach (var line in hunk.Lines)
            {
                line.Tokens = _syntaxService.Tokenize(line.Content);
            }
        }

        return new DiffResult { Hunks = hunks };
    }
}

// Infrastructure Layer
namespace TwinCatQA.Infrastructure.Diff;

public class DiffEngine : IDiffEngine
{
    private readonly DiffPlex.Differ _differ;

    public RawDiffResult Diff(string oldText, string newText)
    {
        var result = _differ.CreateLineDiffs(oldText, newText, false);
        return ConvertToRawDiffResult(result);
    }
}

namespace TwinCatQA.Infrastructure.Syntax;

public class STSyntaxHighlighter : ISyntaxHighlightService
{
    // ANTLR4 생성 파서 사용
    private readonly STLexer _lexer;

    public List<SyntaxToken> Tokenize(string code)
    {
        var inputStream = new AntlrInputStream(code);
        _lexer.SetInputStream(inputStream);

        var tokens = new List<SyntaxToken>();
        IToken token;
        while ((token = _lexer.NextToken()).Type != TokenConstants.EOF)
        {
            tokens.Add(new SyntaxToken
            {
                Text = token.Text,
                Type = MapTokenType(token.Type),
                StartColumn = token.Column,
                EndColumn = token.Column + token.Text.Length
            });
        }
        return tokens;
    }
}
```

#### UI 설계 (XAML)

```xml
<!-- SideBySideDiffWindow.xaml -->
<Window x:Class="TwinCatQA.UI.Views.SideBySideDiffWindow"
        Title="Side-by-Side Diff Viewer"
        Height="800" Width="1400">

    <Window.Resources>
        <converters:DiffLineToColorConverter x:Key="DiffColorConverter"/>
        <converters:SyntaxTokenToColorConverter x:Key="SyntaxColorConverter"/>
    </Window.Resources>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>   <!-- 툴바 -->
            <RowDefinition Height="*"/>      <!-- Diff 뷰 -->
            <RowDefinition Height="Auto"/>   <!-- 상태바 -->
        </Grid.RowDefinitions>

        <!-- 툴바 -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="10">
            <TextBlock Text="File:" VerticalAlignment="Center" Margin="0,0,10,0"/>
            <TextBlock Text="{Binding FileName}" FontWeight="Bold"/>
            <Separator Width="20"/>
            <CheckBox Content="변경 부분만 표시" IsChecked="{Binding ShowChangedOnly}"/>
            <Button Content="전체 펼치기" Command="{Binding ExpandAllCommand}" Margin="10,0"/>
            <Button Content="전체 접기" Command="{Binding CollapseAllCommand}" Margin="10,0"/>
        </StackPanel>

        <!-- Side-by-Side 뷰 -->
        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>    <!-- Old 버전 -->
                <ColumnDefinition Width="3"/>    <!-- Splitter -->
                <ColumnDefinition Width="*"/>    <!-- New 버전 -->
            </Grid.ColumnDefinitions>

            <!-- Old 버전 -->
            <Border Grid.Column="0" BorderBrush="#DEE2E6" BorderThickness="1">
                <ScrollViewer x:Name="OldScrollViewer"
                              VerticalScrollBarVisibility="Auto"
                              ScrollChanged="OnScrollChanged">
                    <ItemsControl ItemsSource="{Binding DiffResult.Hunks}">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <!-- Hunk 헤더 -->
                                <StackPanel>
                                    <Border Background="#E8F4F8" Padding="5">
                                        <TextBlock Text="{Binding HeaderText}"/>
                                    </Border>

                                    <!-- 라인들 -->
                                    <ItemsControl ItemsSource="{Binding Lines}"
                                                  Visibility="{Binding IsCollapsed,
                                                               Converter={StaticResource InverseBoolToVis}}">
                                        <ItemsControl.ItemTemplate>
                                            <DataTemplate>
                                                <Grid Background="{Binding ChangeType,
                                                                   Converter={StaticResource DiffColorConverter}}">
                                                    <Grid.ColumnDefinitions>
                                                        <ColumnDefinition Width="50"/>  <!-- 라인 번호 -->
                                                        <ColumnDefinition Width="*"/>   <!-- 코드 -->
                                                    </Grid.ColumnDefinitions>

                                                    <!-- 라인 번호 -->
                                                    <TextBlock Grid.Column="0"
                                                              Text="{Binding OldLineNumber}"
                                                              Foreground="#7F8C8D"
                                                              Padding="5"
                                                              TextAlignment="Right"/>

                                                    <!-- 코드 (문법 강조) -->
                                                    <ItemsControl Grid.Column="1"
                                                                 ItemsSource="{Binding Tokens}">
                                                        <ItemsControl.ItemsPanel>
                                                            <ItemsPanelTemplate>
                                                                <StackPanel Orientation="Horizontal"/>
                                                            </ItemsPanelTemplate>
                                                        </ItemsControl.ItemsPanel>
                                                        <ItemsControl.ItemTemplate>
                                                            <DataTemplate>
                                                                <TextBlock Text="{Binding Text}"
                                                                          Foreground="{Binding Type,
                                                                                      Converter={StaticResource SyntaxColorConverter}}"
                                                                          FontFamily="Consolas"/>
                                                            </DataTemplate>
                                                        </ItemsControl.ItemTemplate>
                                                    </ItemsControl>
                                                </Grid>
                                            </DataTemplate>
                                        </ItemsControl.ItemTemplate>
                                    </ItemsControl>
                                </StackPanel>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </ScrollViewer>
            </Border>

            <!-- Splitter -->
            <GridSplitter Grid.Column="1" HorizontalAlignment="Stretch" Background="#95A5A6"/>

            <!-- New 버전 (동일한 구조) -->
            <Border Grid.Column="2" BorderBrush="#DEE2E6" BorderThickness="1">
                <ScrollViewer x:Name="NewScrollViewer"
                              VerticalScrollBarVisibility="Auto"
                              ScrollChanged="OnScrollChanged">
                    <!-- Old와 동일한 구조 -->
                </ScrollViewer>
            </Border>
        </Grid>

        <!-- 상태바 -->
        <Border Grid.Row="2" Background="#34495E" Padding="10,5">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="+" Foreground="#27AE60" Margin="5,0"/>
                <TextBlock Text="{Binding TotalAdded}" Foreground="#27AE60" Margin="0,0,15,0"/>
                <TextBlock Text="-" Foreground="#E74C3C" Margin="5,0"/>
                <TextBlock Text="{Binding TotalDeleted}" Foreground="#E74C3C" Margin="0,0,15,0"/>
                <TextBlock Text="~" Foreground="#F39C12" Margin="5,0"/>
                <TextBlock Text="{Binding TotalModified}" Foreground="#F39C12"/>
            </StackPanel>
        </Border>
    </Grid>
</Window>
```

#### ViewModel 구현

```csharp
public class SideBySideDiffViewModel : ViewModelBase
{
    private readonly IDiffService _diffService;
    private DiffResult _diffResult;
    private bool _showChangedOnly;

    public DiffResult DiffResult
    {
        get => _diffResult;
        set => SetProperty(ref _diffResult, value);
    }

    public bool ShowChangedOnly
    {
        get => _showChangedOnly;
        set
        {
            if (SetProperty(ref _showChangedOnly, value))
            {
                UpdateVisibility();
            }
        }
    }

    public ICommand ExpandAllCommand { get; }
    public ICommand CollapseAllCommand { get; }

    public async Task LoadDiffAsync(string oldPath, string newPath)
    {
        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        DiffResult = _diffService.ComputeDiff(oldContent, newContent);
    }

    private void UpdateVisibility()
    {
        foreach (var hunk in DiffResult.Hunks)
        {
            // 변경이 없는 hunk는 접기
            if (ShowChangedOnly && hunk.Lines.All(l => l.ChangeType == DiffChangeType.Unchanged))
            {
                hunk.IsCollapsed = true;
            }
        }
    }
}
```

### 구현 단계 (Step-by-Step)

#### Step 1: DiffPlex 통합 (1주)
1. NuGet 패키지 설치
   ```bash
   dotnet add package DiffPlex
   ```
2. `DiffEngine` 클래스 구현
3. 단위 테스트 작성
   ```csharp
   [Test]
   public void Diff_Should_DetectAddedLines()
   {
       var old = "VAR\n  counter : INT;\nEND_VAR";
       var new = "VAR\n  counter : INT;\n  enabled : BOOL;\nEND_VAR";

       var result = _diffEngine.Diff(old, new);

       Assert.That(result.TotalAdded, Is.EqualTo(1));
   }
   ```

#### Step 2: ANTLR4 문법 정의 (1주)
1. ST Lexer 문법 작성 (`STLexer.g4`)
   ```antlr
   lexer grammar STLexer;

   // Keywords
   PROGRAM: 'PROGRAM';
   VAR: 'VAR';
   END_VAR: 'END_VAR';
   IF: 'IF';
   THEN: 'THEN';
   ELSE: 'ELSE';
   END_IF: 'END_IF';
   // ... 더 많은 키워드

   // Operators
   ASSIGN: ':=';
   PLUS: '+';
   MINUS: '-';
   // ... 더 많은 연산자

   // Literals
   IDENTIFIER: [a-zA-Z_][a-zA-Z0-9_]*;
   NUMBER: [0-9]+('.'[0-9]+)?;
   STRING: '\'' (~['])* '\'';

   // Comments
   LINE_COMMENT: '//' ~[\r\n]* -> skip;
   BLOCK_COMMENT: '(*' .*? '*)' -> skip;

   // Whitespace
   WS: [ \t\r\n]+ -> skip;
   ```

2. ANTLR4 도구로 C# 코드 생성
   ```bash
   antlr4 -Dlanguage=CSharp STLexer.g4
   ```

3. `STSyntaxHighlighter` 구현 및 테스트

#### Step 3: UI 컴포넌트 개발 (1-2주)
1. `SideBySideDiffWindow.xaml` 레이아웃 구현
2. 스크롤 동기화 로직
   ```csharp
   private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
   {
       if (sender == OldScrollViewer)
       {
           NewScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
       }
       else
       {
           OldScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
       }
   }
   ```
3. 접기/펼치기 애니메이션
4. 색상 컨버터 구현

#### Step 4: 통합 및 테스트 (1주)
1. `FolderComparisonWindow`에서 Side-by-Side 뷰 호출
   ```csharp
   private void OnLogicChangeDoubleClick(object sender, MouseButtonEventArgs e)
   {
       if (sender is DataGrid grid && grid.SelectedItem is LogicChange change)
       {
           var diffWindow = new SideBySideDiffWindow
           {
               DataContext = new SideBySideDiffViewModel(_diffService)
           };

           diffWindow.ViewModel.LoadDiffAsync(
               change.OldFilePath,
               change.NewFilePath
           );

           diffWindow.Show();
       }
   }
   ```

2. 실제 TwinCAT 프로젝트로 테스트
3. 성능 최적화 (대용량 파일 처리)

---

## 🔍 2순위: Impact Analysis with Heatmap

### 아키텍처 설계

#### 모듈 구성

```
TwinCatQA.Domain
├── Models/
│   ├── ImpactModels/
│   │   ├── ImpactGraph.cs            # 영향도 그래프
│   │   ├── ImpactNode.cs             # 노드 (파일/함수/변수)
│   │   ├── ImpactEdge.cs             # 엣지 (의존성 관계)
│   │   ├── ImpactLevel.cs            # 영향도 레벨
│   │   └── RiskAssessment.cs         # 위험도 평가
│   └── Services/
│       └── IImpactAnalyzer.cs

TwinCatQA.Application
├── Services/
│   ├── ImpactAnalysisService.cs      # 영향도 분석 오케스트레이션
│   └── RiskEvaluationService.cs      # 위험도 평가

TwinCatQA.Infrastructure
├── StaticAnalysis/
│   ├── CallGraphBuilder.cs           # 호출 그래프 생성
│   ├── DataFlowAnalyzer.cs           # 데이터 흐름 분석
│   ├── TypeDependencyTracker.cs      # 타입 의존성 추적
│   └── ASTVisitor/
│       ├── FunctionCallVisitor.cs    # 함수 호출 탐색
│       └── VariableRefVisitor.cs     # 변수 참조 탐색

TwinCatQA.UI
├── Views/
│   └── ImpactAnalysisWindow.xaml     # 영향도 분석 창
├── ViewModels/
│   └── ImpactAnalysisViewModel.cs
└── Controls/
    ├── HeatmapTreeView.xaml          # 히트맵 트리뷰
    └── ImpactDetailPanel.xaml        # 상세 정보 패널
```

#### 클래스 다이어그램

```csharp
// Domain Layer
namespace TwinCatQA.Domain.Models;

public enum ImpactLevel
{
    None,      // 영향 없음 (회색)
    Low,       // 낮음 (노란색)
    Medium,    // 중간 (주황색)
    High       // 높음 (빨간색)
}

public enum RiskLevel
{
    Info,      // 정보성 (ℹ️)
    Warning,   // 경고 (⚠️)
    Critical   // 위험 (⛔)
}

public class ImpactNode
{
    public string Id { get; init; } = string.Empty;        // "FB_MotorControl:Speed"
    public string FilePath { get; init; } = string.Empty;
    public int Line { get; init; }
    public NodeType Type { get; init; }                    // File, Function, Variable
    public ImpactLevel ImpactLevel { get; set; }
    public List<ImpactEdge> OutgoingEdges { get; init; } = new();
    public List<ImpactEdge> IncomingEdges { get; init; } = new();
}

public enum NodeType
{
    File,
    Function,
    FunctionBlock,
    Variable,
    DataType
}

public class ImpactEdge
{
    public ImpactNode From { get; init; }
    public ImpactNode To { get; init; }
    public EdgeType Type { get; init; }
    public int Weight { get; init; } = 1;  // 호출 빈도 등
}

public enum EdgeType
{
    FunctionCall,      // 함수 호출
    VariableReference, // 변수 참조
    TypeDependency,    // 타입 의존성
    Inheritance        // 상속 (FB EXTENDS)
}

public class ImpactGraph
{
    public Dictionary<string, ImpactNode> Nodes { get; init; } = new();
    public List<ImpactEdge> Edges { get; init; } = new();

    public void AddNode(ImpactNode node) => Nodes[node.Id] = node;

    public void AddEdge(ImpactEdge edge)
    {
        Edges.Add(edge);
        edge.From.OutgoingEdges.Add(edge);
        edge.To.IncomingEdges.Add(edge);
    }

    // BFS로 영향도 계산
    public void PropagateImpact(ImpactNode changedNode)
    {
        var queue = new Queue<(ImpactNode node, int distance)>();
        queue.Enqueue((changedNode, 0));

        changedNode.ImpactLevel = ImpactLevel.High;

        while (queue.Count > 0)
        {
            var (node, distance) = queue.Dequeue();

            foreach (var edge in node.IncomingEdges)  // 역방향 추적
            {
                var caller = edge.From;
                var newLevel = CalculateImpactLevel(distance + 1);

                if (newLevel > caller.ImpactLevel)
                {
                    caller.ImpactLevel = newLevel;
                    queue.Enqueue((caller, distance + 1));
                }
            }
        }
    }

    private ImpactLevel CalculateImpactLevel(int distance)
    {
        return distance switch
        {
            0 => ImpactLevel.High,
            1 => ImpactLevel.High,
            2 => ImpactLevel.Medium,
            3 => ImpactLevel.Low,
            _ => ImpactLevel.None
        };
    }
}

public class RiskAssessment
{
    public ImpactNode Node { get; init; }
    public RiskLevel Level { get; init; }
    public string Reason { get; init; } = string.Empty;
    public List<string> Recommendations { get; init; } = new();
}

// Application Layer
namespace TwinCatQA.Application.Services;

public class ImpactAnalysisService : IImpactAnalyzer
{
    private readonly ICallGraphBuilder _callGraphBuilder;
    private readonly IDataFlowAnalyzer _dataFlowAnalyzer;
    private readonly ITypeDependencyTracker _typeTracker;
    private readonly IRiskEvaluationService _riskEvaluator;

    public async Task<ImpactGraph> AnalyzeImpactAsync(
        List<VariableChange> variableChanges,
        List<LogicChange> logicChanges,
        List<DataTypeChange> dataTypeChanges,
        List<CodeFile> allFiles)
    {
        // 1. 호출 그래프 생성
        var callGraph = await _callGraphBuilder.BuildAsync(allFiles);

        // 2. 변경 노드 식별
        var changedNodes = IdentifyChangedNodes(
            variableChanges, logicChanges, dataTypeChanges, callGraph);

        // 3. 영향도 전파
        foreach (var node in changedNodes)
        {
            callGraph.PropagateImpact(node);
        }

        // 4. 위험도 평가
        foreach (var node in callGraph.Nodes.Values.Where(n => n.ImpactLevel != ImpactLevel.None))
        {
            var assessment = _riskEvaluator.Evaluate(node);
            node.RiskAssessment = assessment;
        }

        return callGraph;
    }
}

public class RiskEvaluationService : IRiskEvaluationService
{
    public RiskAssessment Evaluate(ImpactNode node)
    {
        // 타입 변경 → Critical
        if (node.Type == NodeType.Variable && IsTypeChanged(node))
        {
            return new RiskAssessment
            {
                Node = node,
                Level = RiskLevel.Critical,
                Reason = "타입 변경으로 인한 컴파일 오류 가능성",
                Recommendations = new List<string>
                {
                    "모든 참조 위치에서 타입 호환성 확인 필요",
                    "단위 테스트 실행 권장"
                }
            };
        }

        // 로직 변경 → Warning
        if (node.Type == NodeType.Function && IsLogicChanged(node))
        {
            return new RiskAssessment
            {
                Node = node,
                Level = RiskLevel.Warning,
                Reason = "함수 동작 변경으로 인한 논리 오류 가능성",
                Recommendations = new List<string>
                {
                    "호출하는 모든 위치에서 동작 검증 필요",
                    "통합 테스트 권장"
                }
            };
        }

        // 변수명만 변경 → Info
        return new RiskAssessment
        {
            Node = node,
            Level = RiskLevel.Info,
            Reason = "영향이 있으나 안전한 변경",
            Recommendations = new List<string> { "리뷰 후 승인 가능" }
        };
    }
}

// Infrastructure Layer
namespace TwinCatQA.Infrastructure.StaticAnalysis;

public class CallGraphBuilder : ICallGraphBuilder
{
    private readonly IParserService _parser;

    public async Task<ImpactGraph> BuildAsync(List<CodeFile> files)
    {
        var graph = new ImpactGraph();

        // 1단계: 모든 함수/FB 노드 생성
        foreach (var file in files)
        {
            var ast = await _parser.ParseAsync(file.Content);
            var visitor = new FunctionCallVisitor();
            visitor.Visit(ast);

            foreach (var function in visitor.Functions)
            {
                graph.AddNode(new ImpactNode
                {
                    Id = $"{file.FilePath}:{function.Name}",
                    FilePath = file.FilePath,
                    Line = function.Line,
                    Type = NodeType.Function
                });
            }
        }

        // 2단계: 호출 관계 엣지 생성
        foreach (var file in files)
        {
            var ast = await _parser.ParseAsync(file.Content);
            var visitor = new FunctionCallVisitor();
            visitor.Visit(ast);

            foreach (var call in visitor.FunctionCalls)
            {
                var caller = graph.Nodes[$"{file.FilePath}:{call.CallerName}"];
                var callee = graph.Nodes.Values.FirstOrDefault(n => n.Id.EndsWith($":{call.CalleeName}"));

                if (callee != null)
                {
                    graph.AddEdge(new ImpactEdge
                    {
                        From = caller,
                        To = callee,
                        Type = EdgeType.FunctionCall
                    });
                }
            }
        }

        return graph;
    }
}

// ANTLR4 Visitor 예시
public class FunctionCallVisitor : STParserBaseVisitor<object>
{
    public List<FunctionInfo> Functions { get; } = new();
    public List<CallInfo> FunctionCalls { get; } = new();

    public override object VisitFunctionDeclaration(STParser.FunctionDeclarationContext context)
    {
        Functions.Add(new FunctionInfo
        {
            Name = context.IDENTIFIER().GetText(),
            Line = context.Start.Line
        });

        return base.VisitFunctionDeclaration(context);
    }

    public override object VisitFunctionCallExpression(STParser.FunctionCallExpressionContext context)
    {
        FunctionCalls.Add(new CallInfo
        {
            CallerName = GetCurrentFunctionName(),
            CalleeName = context.IDENTIFIER().GetText(),
            Line = context.Start.Line
        });

        return base.VisitFunctionCallExpression(context);
    }
}
```

#### UI 설계 (XAML)

```xml
<!-- ImpactAnalysisWindow.xaml -->
<Window x:Class="TwinCatQA.UI.Views.ImpactAnalysisWindow"
        Title="Impact Analysis - Heatmap View"
        Height="700" Width="1200">

    <Window.Resources>
        <converters:ImpactLevelToColorConverter x:Key="ImpactColorConverter"/>
        <converters:RiskLevelToIconConverter x:Key="RiskIconConverter"/>
    </Window.Resources>

    <Grid Margin="15">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="400"/>  <!-- 트리뷰 -->
            <ColumnDefinition Width="*"/>    <!-- 상세 정보 -->
        </Grid.ColumnDefinitions>

        <!-- 프로젝트 트리 + 히트맵 -->
        <Border Grid.Column="0" BorderBrush="#DEE2E6" BorderThickness="1" CornerRadius="5">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>

                <!-- 헤더 -->
                <Border Grid.Row="0" Background="#2C3E50" Padding="10">
                    <TextBlock Text="Project Files (Heatmap)" Foreground="White" FontWeight="SemiBold"/>
                </Border>

                <!-- 히트맵 트리뷰 -->
                <TreeView Grid.Row="1" ItemsSource="{Binding RootNodes}" Margin="5">
                    <TreeView.ItemTemplate>
                        <HierarchicalDataTemplate ItemsSource="{Binding Children}">
                            <StackPanel Orientation="Horizontal">
                                <!-- 파일 아이콘 -->
                                <TextBlock Text="📄" Margin="0,0,5,0"/>

                                <!-- 파일명 -->
                                <TextBlock Text="{Binding Name}" VerticalAlignment="Center"/>

                                <!-- 히트맵 색상 인디케이터 -->
                                <Border Width="20" Height="20"
                                        Background="{Binding ImpactLevel, Converter={StaticResource ImpactColorConverter}}"
                                        CornerRadius="10"
                                        Margin="10,0,0,0"/>
                            </StackPanel>
                        </HierarchicalDataTemplate>
                    </TreeView.ItemTemplate>
                </TreeView>

                <!-- 범례 -->
                <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="10">
                    <TextBlock Text="범례:" FontWeight="SemiBold" Margin="0,0,10,0"/>
                    <Ellipse Width="15" Height="15" Fill="#E74C3C" Margin="5,0"/>
                    <TextBlock Text="High" Margin="0,0,10,0"/>
                    <Ellipse Width="15" Height="15" Fill="#F39C12" Margin="5,0"/>
                    <TextBlock Text="Medium" Margin="0,0,10,0"/>
                    <Ellipse Width="15" Height="15" Fill="#F1C40F" Margin="5,0"/>
                    <TextBlock Text="Low" Margin="0,0,10,0"/>
                </StackPanel>
            </Grid>
        </Border>

        <!-- 상세 정보 패널 -->
        <Border Grid.Column="1" BorderBrush="#DEE2E6" BorderThickness="1" CornerRadius="5" Margin="10,0,0,0">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>

                <!-- 헤더 -->
                <Border Grid.Row="0" Background="#34495E" Padding="10">
                    <TextBlock Text="Impact Details" Foreground="White" FontWeight="SemiBold"/>
                </Border>

                <!-- 위험도 평가 -->
                <Border Grid.Row="1" Background="#E8F4F8" Padding="15" Margin="10">
                    <StackPanel>
                        <StackPanel Orientation="Horizontal">
                            <TextBlock Text="{Binding SelectedNode.RiskAssessment.Level,
                                              Converter={StaticResource RiskIconConverter}}"
                                      FontSize="24" Margin="0,0,10,0"/>
                            <TextBlock Text="{Binding SelectedNode.Id}"
                                      FontSize="16" FontWeight="Bold" VerticalAlignment="Center"/>
                        </StackPanel>

                        <TextBlock Text="{Binding SelectedNode.RiskAssessment.Reason}"
                                  Margin="0,10,0,0" TextWrapping="Wrap"/>
                    </StackPanel>
                </Border>

                <!-- 영향 받는 위치 리스트 -->
                <Border Grid.Row="2" Margin="10">
                    <Grid>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="*"/>
                        </Grid.RowDefinitions>

                        <TextBlock Grid.Row="0" Text="영향 받는 위치"
                                  FontWeight="SemiBold" Margin="0,0,0,10"/>

                        <DataGrid Grid.Row="1" ItemsSource="{Binding SelectedNode.IncomingEdges}"
                                 AutoGenerateColumns="False">
                            <DataGrid.Columns>
                                <DataGridTextColumn Header="위치" Binding="{Binding From.FilePath}" Width="*"/>
                                <DataGridTextColumn Header="라인" Binding="{Binding From.Line}" Width="60"/>
                                <DataGridTextColumn Header="위험도"
                                                   Binding="{Binding From.RiskAssessment.Level}" Width="80"/>
                            </DataGrid.Columns>
                        </DataGrid>
                    </Grid>
                </Border>
            </Grid>
        </Border>
    </Grid>
</Window>
```

### 구현 단계 (Step-by-Step)

#### Step 1: Call Graph 기반 구축 (2주)
1. ANTLR4 파서로 AST 생성
2. `FunctionCallVisitor` 구현하여 모든 함수 호출 수집
3. `ImpactGraph` 모델 구현
4. 단위 테스트

#### Step 2: 영향도 전파 알고리즘 (1주)
1. BFS 기반 전파 로직 구현
2. 거리에 따른 영향도 레벨 계산
3. 성능 테스트 (대규모 프로젝트)

#### Step 3: 위험도 평가 (1주)
1. 타입 변경 감지 로직
2. 로직 변경 감지 로직
3. 위험도 평가 규칙 엔진
4. 테스트 권장 사항 생성

#### Step 4: 히트맵 UI 구현 (1주)
1. TreeView 히트맵 색상 바인딩
2. 상세 정보 패널 구현
3. Export 기능 (HTML/PDF 리포트)

---

## 🧠 3순위: Change Reason Inference

### 아키텍처 설계

#### 모듈 구성

```
TwinCatQA.Domain
├── Models/
│   ├── ReasonModels/
│   │   ├── ChangeReason.cs           # 변경 이유
│   │   ├── ReasonCategory.cs         # 카테고리 (4가지)
│   │   ├── ConfidenceLevel.cs        # 신뢰도
│   │   └── AIAnalysisResult.cs       # AI 분석 결과

TwinCatQA.Application
├── Services/
│   ├── ReasonInferenceService.cs     # 추론 오케스트레이션
│   ├── CommentAnalyzer.cs            # 주석 분석
│   ├── VariableNameAnalyzer.cs       # 변수명 분석
│   └── LogicPatternMatcher.cs        # 로직 패턴 매칭

TwinCatQA.Infrastructure
├── NLP/
│   ├── KeywordExtractor.cs           # 키워드 추출
│   └── PatternRules/
│       ├── BugFixPatterns.cs         # 버그 수정 패턴
│       ├── FeaturePatterns.cs        # 기능 추가 패턴
│       └── OptimizationPatterns.cs   # 최적화 패턴
└── AI/
    ├── OpenAIClient.cs               # OpenAI API
    └── PromptBuilder.cs              # 프롬프트 생성
```

#### 클래스 다이어그램

```csharp
// Domain Layer
public enum ReasonCategory
{
    NewFeature,        // 🆕 기능 추가
    BugFix,            // 🐛 버그 수정
    Performance,       // ⚡ 성능 최적화
    Refactoring        // 🔧 리팩토링
}

public enum ConfidenceLevel
{
    Certain,    // ✅ 확실
    Probable,   // ⚠️ 추정
    Uncertain   // ❓ 불확실
}

public class ChangeReason
{
    public string ChangeId { get; init; } = string.Empty;
    public ReasonCategory Category { get; init; }
    public ConfidenceLevel Confidence { get; init; }
    public string Explanation { get; init; } = string.Empty;
    public List<string> Evidence { get; init; } = new();  // 증거 (주석, 변수명 등)
}

// Application Layer
public class ReasonInferenceService
{
    private readonly ICommentAnalyzer _commentAnalyzer;
    private readonly IVariableNameAnalyzer _variableAnalyzer;
    private readonly ILogicPatternMatcher _patternMatcher;
    private readonly IAIClient _aiClient;

    public async Task<ChangeReason> InferReasonAsync(
        LogicChange change,
        bool useAI = false)
    {
        // 1. 주석 분석
        var commentEvidence = _commentAnalyzer.Analyze(
            change.OldContent, change.NewContent);

        // 2. 변수명 분석
        var variableEvidence = _variableAnalyzer.Analyze(
            change.OldContent, change.NewContent);

        // 3. 로직 패턴 매칭
        var patternEvidence = _patternMatcher.Match(
            change.OldContent, change.NewContent);

        // 4. 규칙 기반 추론
        var reason = InferFromEvidence(
            commentEvidence, variableEvidence, patternEvidence);

        // 5. (옵션) AI 분석
        if (useAI && reason.Confidence == ConfidenceLevel.Uncertain)
        {
            reason = await EnhanceWithAI(change, reason);
        }

        return reason;
    }

    private ChangeReason InferFromEvidence(
        CommentEvidence comments,
        VariableEvidence variables,
        PatternEvidence patterns)
    {
        // 주석에 명확한 키워드가 있으면 확실
        if (comments.HasKeyword("BUG", "FIX", "수정"))
        {
            return new ChangeReason
            {
                Category = ReasonCategory.BugFix,
                Confidence = ConfidenceLevel.Certain,
                Explanation = "주석에 버그 수정 명시",
                Evidence = comments.ExtractedComments
            };
        }

        // 변수명 패턴으로 추정
        if (variables.HasPattern("temp -> criticalTemp"))
        {
            return new ChangeReason
            {
                Category = ReasonCategory.NewFeature,
                Confidence = ConfidenceLevel.Probable,
                Explanation = "변수명 변경으로 안전성 강화 추정",
                Evidence = new List<string> { variables.Pattern }
            };
        }

        // 패턴 매칭으로 추정
        if (patterns.Matches(OptimizationPatterns.RemoveRedundantCalculation))
        {
            return new ChangeReason
            {
                Category = ReasonCategory.Performance,
                Confidence = ConfidenceLevel.Probable,
                Explanation = "불필요한 연산 제거로 최적화 추정",
                Evidence = new List<string> { patterns.MatchedPattern }
            };
        }

        // 증거 부족
        return new ChangeReason
        {
            Category = ReasonCategory.Refactoring,
            Confidence = ConfidenceLevel.Uncertain,
            Explanation = "충분한 정보 없음",
            Evidence = new List<string>()
        };
    }

    private async Task<ChangeReason> EnhanceWithAI(
        LogicChange change,
        ChangeReason baseReason)
    {
        var prompt = _promptBuilder.Build(change.OldContent, change.NewContent);
        var aiResult = await _aiClient.AnalyzeAsync(prompt);

        return new ChangeReason
        {
            Category = aiResult.Category,
            Confidence = ConfidenceLevel.Certain,
            Explanation = aiResult.Explanation,
            Evidence = baseReason.Evidence.Concat(new[] { "AI 분석 결과" }).ToList()
        };
    }
}

// Infrastructure Layer - Pattern Rules
public static class BugFixPatterns
{
    public static readonly PatternRule[] Rules = new[]
    {
        // IF 조건 강화
        new PatternRule
        {
            Name = "조건 강화",
            OldPattern = @"IF\s+(\w+)\s+THEN",
            NewPattern = @"IF\s+\1\s+AND\s+(\w+)\s+THEN",
            Category = ReasonCategory.BugFix,
            Explanation = "조건 추가로 예외 처리 강화"
        },

        // 타이머 값 변경
        new PatternRule
        {
            Name = "타이머 조정",
            OldPattern = @"T#(\d+)ms",
            NewPattern = @"T#(\d+)ms",  // 값이 달라야 함
            Category = ReasonCategory.BugFix,
            Explanation = "타이머 값 조정으로 타이밍 이슈 해결"
        },

        // 타입 확장 (오버플로 방지)
        new PatternRule
        {
            Name = "타입 확장",
            OldPattern = @":\s*INT",
            NewPattern = @":\s*DINT",
            Category = ReasonCategory.BugFix,
            Explanation = "INT → DINT 변경으로 오버플로 방지"
        }
    };
}

public static class FeaturePatterns
{
    public static readonly PatternRule[] Rules = new[]
    {
        // CASE 분기 추가
        new PatternRule
        {
            Name = "분기 추가",
            OldPattern = @"CASE\s+\w+\s+OF(.*?)END_CASE",
            NewPattern = @"CASE\s+\w+\s+OF(.*?)(\d+:\s*.*?)(.*?)END_CASE",
            Category = ReasonCategory.NewFeature,
            Explanation = "CASE 분기 추가로 기능 확장"
        }
    };
}

// AI Integration
public class OpenAIClient : IAIClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public async Task<AIAnalysisResult> AnalyzeAsync(string prompt)
    {
        var request = new
        {
            model = "gpt-4",
            messages = new[]
            {
                new { role = "system", content = "당신은 TwinCAT Structured Text 코드 분석 전문가입니다." },
                new { role = "user", content = prompt }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            "https://api.openai.com/v1/chat/completions",
            request);

        var result = await response.Content.ReadFromJsonAsync<OpenAIResponse>();

        return ParseAIResponse(result.Choices[0].Message.Content);
    }
}

public class PromptBuilder
{
    public string Build(string oldCode, string newCode)
    {
        return $@"
다음 TwinCAT ST 코드의 변경 사항을 분석하고, 변경 이유를 추론하세요.

**변경 전 코드:**
```
{oldCode}
```

**변경 후 코드:**
```
{newCode}
```

**분석 요청:**
1. 변경 카테고리를 다음 중 하나로 분류: 기능 추가, 버그 수정, 성능 최적화, 리팩토링
2. 변경 이유를 1-2문장으로 설명
3. 주요 변경 사항 나열

**출력 형식 (JSON):**
{{
  ""category"": ""기능 추가"",
  ""explanation"": ""초기화 상태 확인 로직 추가로 안전성 강화"",
  ""changes"": [""initialized 변수 추가"", ""IF 조건 강화""]
}}
";
    }
}
```

### 구현 단계 (Step-by-Step)

#### Step 1: 규칙 기반 추론 (1주)
1. 주석 키워드 추출 (`TODO`, `FIXME`, `BUG`, `수정` 등)
2. 변수명 패턴 분석
3. 로직 패턴 규칙 정의 (20-30개 규칙)

#### Step 2: AI 통합 (선택, 1주)
1. OpenAI API 또는 Azure OpenAI 통합
2. 프롬프트 엔지니어링
3. 결과 파싱 및 신뢰도 평가

#### Step 3: UI 통합 (1주)
1. Logic Changes 탭에 "변경 이유" 컬럼 추가
2. 신뢰도 아이콘 표시
3. AI On/Off 토글 버튼

---

## ⚠️ 설계 검토 결과 및 개선사항

### 검토 방법론
1. ✅ 요구사항 명확성 검증
2. ✅ 설계 완성도 평가
3. ✅ 일관성 확인
4. ⚠️ 누락 사항 식별
5. 💡 개선 기회 발굴

### 주요 발견 사항

#### 🔴 Critical: ANTLR4 파서 구현 기간 과소평가
**문제**: Phase 0에서 완전한 ST 파서를 2-3주에 구현하는 것은 비현실적

**분석**:
- IEC 61131-3 ST 언어는 복잡한 문법 (함수, FB, 인터페이스, 포인터 등)
- 전체 문법 정의: 4-6주 소요 예상
- 기존 코드에는 skeleton만 존재 (`.g4` 파일 없음)

**해결책**: **단계적 파서 구현 전략**

```
Phase 0-A: Lexer 구현 (2주)
├─ 키워드, 연산자, 리터럴 토크나이징
├─ 주석 처리
└─ 1순위(Diff Viewer)에 충분

Phase 0-B: 기본 Parser (2주)
├─ 변수 선언 (VAR ... END_VAR)
├─ 할당문 (:=)
└─ 기본 표현식

Phase 0-C: 제어 구조 Parser (2주)
├─ IF/THEN/ELSE
├─ CASE/OF
├─ FOR/WHILE
└─ 3순위(Reason Inference)에 활용

Phase 0-D: 고급 Parser (2주)
├─ 함수/FB 선언 및 호출
├─ 타입 정의 (STRUCT, ENUM)
└─ 2순위(Impact Analysis)에 필수
```

**권장 순서**: Lexer → 1순위 구현 → Parser 완성 → 2순위 구현

#### 🟡 Warning: 영향도 분석 복잡도 과소평가
**문제**: Call Graph 구축이 예상보다 복잡 (4-5주 → 실제 6-8주)

**복잡도 요인**:
- 300개 파일 × 평균 10개 함수 = 3,000개 노드
- 크로스 파일 참조 해결 (Symbol Table 필요)
- 전역/지역 스코프 관리
- 간접 호출 추적 (함수 포인터)

**MVP 접근**:
```csharp
// MVP: 단순화된 버전 (3주)
public class SimplifiedImpactAnalyzer
{
    // 1. 함수 호출만 추적 (변수/타입 제외)
    // 2. 단일 파일 내부만 분석
    // 3. 직접 호출자만 (1단계)

    public ImpactGraph BuildSimple(List<CodeFile> files)
    {
        // Regex 기반 함수 호출 추출
        // 1단계 영향도만 계산
    }
}

// Full: 완전한 버전 (추가 3-5주)
public class FullImpactAnalyzer
{
    // 1. 변수 참조 추적
    // 2. 크로스 파일 분석
    // 3. N단계 영향도 전파
}
```

#### 🟢 Info: AI 통합 보안 고려사항 추가 필요
**추가 설계 요소**:

```csharp
// AI 설정 모델
public class AIConfiguration
{
    public bool Enabled { get; set; } = false;
    public AIProvider Provider { get; set; }  // OpenAI, Azure, Ollama
    public string ApiKey { get; set; }
    public string ApiEndpoint { get; set; }  // 커스텀 엔드포인트

    // 보안 설정
    public bool AllowCodeUpload { get; set; } = false;  // 사용자 동의 필요
    public int MaxTokensPerRequest { get; set; } = 1000;
    public int MaxRequestsPerDay { get; set; } = 100;

    // 비용 관리
    public decimal MaxDailyCost { get; set; } = 5.0m;  // USD
}

// 로컬 LLM 옵션 (프라이버시 보장)
public class OllamaClient : IAIClient
{
    // Ollama 로컬 서버 연동
    // 비용 없음, 오프라인 가능, 코드 외부 전송 없음
}
```

---

## 🚀 성능 최적화 전략

### 대용량 파일 처리 시나리오
- 300개 파일 × 500라인 = **150,000 라인**
- Side-by-Side Diff: 수만 개 UI 항목 렌더링
- Impact Analysis: 수천 개 노드 그래프

### 최적화 기법

#### 1. UI 가상화 (Virtualization)
```xml
<!-- DataGrid 가상화 -->
<DataGrid VirtualizingPanel.IsVirtualizing="True"
          VirtualizingPanel.VirtualizationMode="Recycling"
          EnableRowVirtualization="True"
          EnableColumnVirtualization="True">
    <!-- 화면에 보이는 항목만 렌더링 -->
</DataGrid>

<!-- ItemsControl 가상화 -->
<ItemsControl VirtualizingPanel.IsVirtualizing="True"
              VirtualizingPanel.ScrollUnit="Pixel">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <VirtualizingStackPanel/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
</ItemsControl>
```

#### 2. 스트림 기반 파일 처리
```csharp
public class StreamingDiffEngine
{
    public async Task<DiffResult> DiffLargeFilesAsync(
        string oldPath,
        string newPath)
    {
        // 전체 파일을 메모리에 로드하지 않음
        using var oldStream = File.OpenRead(oldPath);
        using var newStream = File.OpenRead(newPath);

        var hunks = new List<DiffHunk>();
        var buffer = new char[4096];

        // 청크 단위로 처리
        while (await oldStream.ReadAsync(buffer, 0, buffer.Length) > 0)
        {
            // 라인별 비교
            // 변경 부분만 메모리 유지
        }

        return new DiffResult { Hunks = hunks };
    }
}
```

#### 3. 백그라운드 작업 및 진행률 표시
```csharp
public class SideBySideDiffViewModel : ViewModelBase
{
    private bool _isLoading;
    private int _progress;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public int Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    public async Task LoadDiffAsync(string oldPath, string newPath)
    {
        IsLoading = true;
        Progress = 0;

        try
        {
            var progress = new Progress<int>(p => Progress = p);

            // 백그라운드 스레드에서 실행
            DiffResult = await Task.Run(() =>
                _diffService.ComputeDiffWithProgress(
                    oldPath, newPath, progress));
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

#### 4. 지연 로딩 (Lazy Loading)
```csharp
public class DiffHunk
{
    private List<DiffLine> _lines;

    // 접혀있을 때는 로드 안함
    public List<DiffLine> Lines
    {
        get
        {
            if (_lines == null && !IsCollapsed)
            {
                _lines = LoadLinesFromSource();
            }
            return _lines;
        }
    }

    public bool IsCollapsed { get; set; } = true;
}
```

#### 5. 캐싱 전략
```csharp
public class CachedDiffService : IDiffService
{
    private readonly MemoryCache _cache = new MemoryCache(
        new MemoryCacheOptions { SizeLimit = 100 });

    public DiffResult ComputeDiff(string oldContent, string newContent)
    {
        // 캐시 키 생성 (해시)
        var cacheKey = $"{ComputeHash(oldContent)}_{ComputeHash(newContent)}";

        if (_cache.TryGetValue(cacheKey, out DiffResult cached))
        {
            return cached;
        }

        var result = _diffEngine.Diff(oldContent, newContent);

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            Size = 1,
            SlidingExpiration = TimeSpan.FromMinutes(30)
        });

        return result;
    }
}
```

---

## 🔒 보안 및 에러 처리 전략

### 에러 처리 계층

#### 1. Domain Layer: 커스텀 예외
```csharp
public class DiffException : Exception
{
    public DiffException(string message, Exception inner)
        : base(message, inner) { }
}

public class ParsingException : Exception
{
    public string FilePath { get; init; }
    public int Line { get; init; }

    public ParsingException(string message, string filePath, int line)
        : base($"{message} at {filePath}:{line}")
    {
        FilePath = filePath;
        Line = line;
    }
}
```

#### 2. Application Layer: 로깅 및 복구
```csharp
public class DiffService : IDiffService
{
    private readonly ILogger<DiffService> _logger;
    private readonly IDiffEngine _diffEngine;

    public DiffResult ComputeDiff(string oldContent, string newContent)
    {
        try
        {
            _logger.LogInformation("Starting diff computation...");

            var result = _diffEngine.Diff(oldContent, newContent);

            _logger.LogInformation(
                "Diff completed: +{Added}, -{Deleted}, ~{Modified}",
                result.TotalAdded,
                result.TotalDeleted,
                result.TotalModified);

            return result;
        }
        catch (OutOfMemoryException ex)
        {
            _logger.LogError(ex, "Out of memory during diff");
            throw new DiffException(
                "파일이 너무 커서 비교할 수 없습니다. 파일 크기를 줄여주세요.",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during diff");
            throw new DiffException(
                "코드 비교 중 오류가 발생했습니다.",
                ex);
        }
    }
}
```

#### 3. UI Layer: 사용자 친화적 메시지
```csharp
public class SideBySideDiffViewModel : ViewModelBase
{
    public async Task LoadDiffAsync(string oldPath, string newPath)
    {
        try
        {
            IsLoading = true;
            DiffResult = await _diffService.ComputeDiffAsync(oldPath, newPath);
        }
        catch (DiffException ex)
        {
            // 사용자에게 친화적인 에러 메시지
            await _dialogService.ShowErrorAsync(
                "비교 오류",
                ex.Message,
                "확인");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in ViewModel");

            await _dialogService.ShowErrorAsync(
                "알 수 없는 오류",
                "예상치 못한 오류가 발생했습니다. 로그를 확인하세요.",
                "확인");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### 보안 고려사항

#### 1. 파일 경로 검증
```csharp
public class FileValidator
{
    private readonly string[] _allowedExtensions =
        { ".TcPOU", ".TcGVL", ".TcDUT", ".TcIO" };

    public bool IsValidTwinCatFile(string path)
    {
        // 1. 경로 순회 공격 방지
        var fullPath = Path.GetFullPath(path);
        if (!fullPath.StartsWith(Path.GetFullPath(_basePath)))
        {
            throw new SecurityException("경로 순회 공격 감지");
        }

        // 2. 확장자 확인
        var ext = Path.GetExtension(path);
        if (!_allowedExtensions.Contains(ext))
        {
            throw new ArgumentException($"허용되지 않은 파일 형식: {ext}");
        }

        // 3. 파일 크기 제한 (10MB)
        var fileInfo = new FileInfo(path);
        if (fileInfo.Length > 10 * 1024 * 1024)
        {
            throw new ArgumentException("파일 크기가 너무 큽니다 (최대 10MB)");
        }

        return true;
    }
}
```

#### 2. AI API 키 안전한 저장
```csharp
public class SecureConfigurationService
{
    public void SaveAIConfiguration(AIConfiguration config)
    {
        // Windows DPAPI로 암호화
        var apiKeyBytes = Encoding.UTF8.GetBytes(config.ApiKey);
        var encryptedBytes = ProtectedData.Protect(
            apiKeyBytes,
            null,
            DataProtectionScope.CurrentUser);

        var secureConfig = new
        {
            config.Enabled,
            config.Provider,
            ApiKey = Convert.ToBase64String(encryptedBytes)
        };

        File.WriteAllText("config.json", JsonSerializer.Serialize(secureConfig));
    }
}
```

---

## 🧪 테스트 전략

### 테스트 피라미드

```
        ╱╲  E2E Tests (5%)
       ╱  ╲  - UI 전체 시나리오
      ╱────╲  - 실제 TwinCAT 프로젝트
     ╱      ╲
    ╱────────╲ Integration Tests (25%)
   ╱          ╲ - Service 통합 테스트
  ╱────────────╲ - Parser + Analyzer
 ╱              ╲
╱────────────────╲ Unit Tests (70%)
                   - 알고리즘 로직
                   - 비즈니스 규칙
```

### 1. 단위 테스트 (Unit Tests)

```csharp
// DiffEngine 테스트
public class DiffEngineTests
{
    private readonly DiffEngine _sut;

    [Fact]
    public void Diff_Should_DetectAddedLines()
    {
        // Arrange
        var oldCode = "VAR\n  counter : INT;\nEND_VAR";
        var newCode = "VAR\n  counter : INT;\n  enabled : BOOL;\nEND_VAR";

        // Act
        var result = _sut.Diff(oldCode, newCode);

        // Assert
        Assert.Equal(1, result.TotalAdded);
        Assert.Contains(result.Hunks[0].Lines,
            line => line.Content.Contains("enabled") &&
                    line.ChangeType == DiffChangeType.Added);
    }

    [Fact]
    public void Diff_Should_DetectModifiedLines()
    {
        var oldCode = "speed : INT := 100;";
        var newCode = "speed : REAL := 100.0;";

        var result = _sut.Diff(oldCode, newCode);

        Assert.Equal(1, result.TotalModified);
    }

    [Theory]
    [InlineData("", "")]  // 빈 파일
    [InlineData("VAR END_VAR", "VAR END_VAR")]  // 동일 파일
    public void Diff_Should_ReturnEmpty_WhenNoChanges(string old, string new)
    {
        var result = _sut.Diff(old, new);

        Assert.Equal(0, result.TotalAdded);
        Assert.Equal(0, result.TotalDeleted);
    }
}

// STSyntaxHighlighter 테스트
public class STSyntaxHighlighterTests
{
    [Fact]
    public void Tokenize_Should_IdentifyKeywords()
    {
        var code = "IF enabled THEN speed := 100; END_IF";

        var tokens = _sut.Tokenize(code);

        Assert.Contains(tokens, t => t.Text == "IF" && t.Type == TokenType.Keyword);
        Assert.Contains(tokens, t => t.Text == "THEN" && t.Type == TokenType.Keyword);
        Assert.Contains(tokens, t => t.Text == ":=" && t.Type == TokenType.Operator);
    }
}
```

### 2. 통합 테스트 (Integration Tests)

```csharp
public class DiffServiceIntegrationTests
{
    private readonly DiffService _service;
    private readonly DiffEngine _diffEngine;
    private readonly STSyntaxHighlighter _highlighter;

    public DiffServiceIntegrationTests()
    {
        _diffEngine = new DiffEngine();
        _highlighter = new STSyntaxHighlighter();
        _service = new DiffService(_diffEngine, _highlighter);
    }

    [Fact]
    public async Task ComputeDiff_Should_IncludeSyntaxHighlighting()
    {
        // Arrange
        var oldFile = await File.ReadAllTextAsync("TestData/OldMotorControl.TcPOU");
        var newFile = await File.ReadAllTextAsync("TestData/NewMotorControl.TcPOU");

        // Act
        var result = _service.ComputeDiff(oldFile, newFile);

        // Assert
        Assert.NotEmpty(result.Hunks);
        Assert.All(result.Hunks, hunk =>
            Assert.All(hunk.Lines, line =>
                Assert.NotEmpty(line.Tokens)  // 문법 강조 토큰 존재
            )
        );
    }
}

public class ImpactAnalysisIntegrationTests
{
    [Fact]
    public async Task AnalyzeImpact_Should_TrackFunctionCalls()
    {
        // Arrange
        var files = LoadTestProject();  // 10개 파일
        var changes = new List<VariableChange>
        {
            new() { VariableName = "speed", OldDataType = "INT", NewDataType = "REAL" }
        };

        // Act
        var graph = await _analyzer.AnalyzeImpactAsync(
            changes, new(), new(), files);

        // Assert
        Assert.NotEmpty(graph.Nodes);
        var speedNode = graph.Nodes.Values.First(n => n.Id.Contains("speed"));
        Assert.Equal(ImpactLevel.High, speedNode.ImpactLevel);
        Assert.NotEmpty(speedNode.IncomingEdges);  // 참조하는 곳이 있음
    }
}
```

### 3. E2E 테스트 (UI Tests)

```csharp
public class SideBySideDiffE2ETests : IClassFixture<WpfAppFixture>
{
    [WpfFact]
    public async Task UserCanViewDiff_WhenDoubleClickingLogicChange()
    {
        // Arrange
        var mainWindow = _fixture.LaunchApp();
        await mainWindow.LoadTestComparisonAsync();

        // Act
        var logicChangesGrid = mainWindow.FindDataGrid("LogicChangesGrid");
        logicChangesGrid.DoubleClickRow(0);

        // Assert
        var diffWindow = _fixture.FindWindow<SideBySideDiffWindow>();
        Assert.NotNull(diffWindow);
        Assert.True(diffWindow.IsVisible);

        var oldCode = diffWindow.FindTextBlock("OldCodeViewer");
        var newCode = diffWindow.FindTextBlock("NewCodeViewer");
        Assert.NotEmpty(oldCode.Text);
        Assert.NotEmpty(newCode.Text);
    }
}
```

### 테스트 데이터 관리

```
tests/
├── TestData/
│   ├── RealProjects/
│   │   ├── Small/          # 10개 파일
│   │   ├── Medium/         # 50개 파일
│   │   └── Large/          # 300개 파일
│   ├── EdgeCases/
│   │   ├── EmptyFile.TcPOU
│   │   ├── HugeFile.TcPOU  # 10,000 라인
│   │   ├── SpecialChars.TcPOU
│   │   └── Comments.TcPOU
│   └── Snapshots/
│       └── ExpectedDiffs/  # Golden master 테스트
```

---

## 🔗 기존 코드 통합 가이드

### FolderComparisonWindow 확장

#### 1. DI 컨테이너 설정
```csharp
// App.xaml.cs
public partial class App : Application
{
    private ServiceProvider _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();

        // 기존 서비스
        services.AddSingleton<IFolderComparer, FolderComparer>();
        services.AddSingleton<IVariableComparer, VariableComparer>();

        // 새 서비스 추가
        services.AddSingleton<IDiffEngine, DiffEngine>();
        services.AddSingleton<ISyntaxHighlightService, STSyntaxHighlighter>();
        services.AddSingleton<IDiffService, DiffService>();

        services.AddSingleton<ICallGraphBuilder, CallGraphBuilder>();
        services.AddSingleton<IImpactAnalyzer, ImpactAnalysisService>();

        services.AddSingleton<IReasonInferenceService, ReasonInferenceService>();

        // ViewModels
        services.AddTransient<FolderComparisonViewModel>();
        services.AddTransient<SideBySideDiffViewModel>();
        services.AddTransient<ImpactAnalysisViewModel>();

        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = new FolderComparisonWindow
        {
            DataContext = _serviceProvider.GetRequiredService<FolderComparisonViewModel>()
        };
        mainWindow.Show();
    }
}
```

#### 2. FolderComparisonWindow.xaml 확장
```xml
<!-- 기존 탭 유지 -->
<TabItem Header="Summary">...</TabItem>
<TabItem Header="Variable Changes">...</TabItem>
<TabItem Header="I/O Mapping Changes">...</TabItem>
<TabItem Header="Data Type Changes">...</TabItem>

<!-- Logic Changes 탭에 버튼 추가 -->
<TabItem Header="Logic Changes">
    <Grid Margin="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- 툴바 추가 -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,10">
            <Button Content="📊 영향도 분석"
                    Command="{Binding ShowImpactAnalysisCommand}"
                    Margin="0,0,10,0"/>
            <Button Content="🧠 변경 이유 분석"
                    Command="{Binding InferChangeReasonsCommand}"/>
        </StackPanel>

        <!-- 기존 DataGrid -->
        <DataGrid Grid.Row="1"
                  ItemsSource="{Binding LogicChanges}"
                  MouseDoubleClick="LogicChangesGrid_MouseDoubleClick">
            <DataGrid.Columns>
                <DataGridTextColumn Header="Change Type" Binding="{Binding ChangeType}"/>
                <DataGridTextColumn Header="Element" Binding="{Binding ElementName}"/>
                <DataGridTextColumn Header="File" Binding="{Binding FilePath}"/>
                <!-- 새 컬럼: 변경 이유 -->
                <DataGridTextColumn Header="추론된 이유" Binding="{Binding InferredReason}"/>
                <DataGridTextColumn Header="신뢰도" Binding="{Binding Confidence}"/>
            </DataGrid.Columns>
        </DataGrid>
    </Grid>
</TabItem>
```

#### 3. FolderComparisonViewModel 확장
```csharp
public class FolderComparisonViewModel : ViewModelBase
{
    private readonly IDiffService _diffService;
    private readonly IImpactAnalyzer _impactAnalyzer;
    private readonly IReasonInferenceService _reasonService;

    public ICommand ShowImpactAnalysisCommand { get; }
    public ICommand InferChangeReasonsCommand { get; }

    public FolderComparisonViewModel(
        IFolderComparer folderComparer,
        IDiffService diffService,
        IImpactAnalyzer impactAnalyzer,
        IReasonInferenceService reasonService)
    {
        // 기존 코드...

        _diffService = diffService;
        _impactAnalyzer = impactAnalyzer;
        _reasonService = reasonService;

        ShowImpactAnalysisCommand = new RelayCommand(ShowImpactAnalysis);
        InferChangeReasonsCommand = new RelayCommand(InferChangeReasons);
    }

    private async void ShowImpactAnalysis()
    {
        var impactWindow = new ImpactAnalysisWindow
        {
            DataContext = new ImpactAnalysisViewModel(_impactAnalyzer)
        };

        await impactWindow.ViewModel.AnalyzeAsync(
            VariableChanges,
            LogicChanges,
            DataTypeChanges,
            _allFiles);

        impactWindow.Show();
    }

    private async void InferChangeReasons()
    {
        foreach (var change in LogicChanges)
        {
            var reason = await _reasonService.InferReasonAsync(change);
            change.InferredReason = reason.Explanation;
            change.Confidence = reason.Confidence.ToString();
        }

        OnPropertyChanged(nameof(LogicChanges));
    }
}
```

#### 4. Code-Behind에서 Diff Viewer 호출
```csharp
// FolderComparisonWindow.xaml.cs
private void LogicChangesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    if (sender is DataGrid grid && grid.SelectedItem is LogicChange change)
    {
        var diffWindow = new SideBySideDiffWindow
        {
            DataContext = new SideBySideDiffViewModel(_diffService)
        };

        _ = diffWindow.ViewModel.LoadDiffAsync(
            change.OldFilePath,
            change.NewFilePath);

        diffWindow.Show();
    }
}
```

---

## 📅 수정된 전체 구현 로드맵

### ⚠️ 로드맵 수정 근거
1. **ANTLR4 파서**: 2-3주 → 8주 (단계적 구현)
2. **영향도 분석**: 4-5주 → 6-8주 (복잡도 반영)
3. **성능 최적화**: 통합 단계에 포함 → 별도 1주 추가
4. **테스트 및 문서**: 각 Phase마다 반영

### Phase 0-A: Lexer 구현 (2주) - **최우선**
- [ ] STLexer.g4 문법 정의
  - [ ] 키워드 (PROGRAM, VAR, IF, CASE 등)
  - [ ] 연산자 (+, -, :=, AND, OR 등)
  - [ ] 리터럴 (숫자, 문자열, 시간)
  - [ ] 주석 (// 및 (* *))
- [ ] ANTLR4 C# 코드 생성
- [ ] STSyntaxHighlighter 구현
- [ ] 단위 테스트 (20개 케이스)

### Phase 5: 1순위 - Side-by-Side Diff (4주)
> Lexer만 있어도 구현 가능 (문법 강조)

- [ ] **Week 1**: DiffPlex 통합
  - [ ] NuGet 패키지 설치
  - [ ] DiffEngine 구현
  - [ ] Hunk 그룹화 로직
  - [ ] 단위 테스트
- [ ] **Week 2**: UI 개발
  - [ ] XAML 레이아웃 (Side-by-Side)
  - [ ] 스크롤 동기화
  - [ ] 접기/펼치기 로직
  - [ ] 색상 컨버터
- [ ] **Week 3**: 문법 강조 통합
  - [ ] Lexer 토큰 → UI 바인딩
  - [ ] 색상 스키마 정의
  - [ ] 성능 테스트 (대용량 파일)
- [ ] **Week 4**: 통합 및 최적화
  - [ ] FolderComparisonWindow 통합
  - [ ] 가상화 적용
  - [ ] E2E 테스트
  - [ ] 버그 수정

### Phase 1: 5순위 - I/O Mapping 이유 (1주)
- [ ] I/O 주소 변경 패턴 정의
- [ ] Regex 기반 하드웨어 감지
- [ ] UI에 이유 표시
- [ ] 테스트

### Phase 0-B/C/D: Parser 완성 (6주)
> 2순위(영향도 분석)에 필수

- [ ] **Week 1-2**: 기본 Parser (Phase 0-B)
  - [ ] 변수 선언
  - [ ] 할당문
  - [ ] 표현식
- [ ] **Week 3-4**: 제어 구조 (Phase 0-C)
  - [ ] IF/CASE/FOR
  - [ ] WHILE/REPEAT
- [ ] **Week 5-6**: 고급 Parser (Phase 0-D)
  - [ ] 함수/FB 선언
  - [ ] 호출 표현식
  - [ ] STRUCT/ENUM

### Phase 4: 2순위 - 영향도 분석 (6-8주)
- [ ] **Week 1-2**: MVP Call Graph
  - [ ] 함수 호출만 추적
  - [ ] 단일 파일 분석
  - [ ] 1단계 영향도
- [ ] **Week 3-4**: Full Call Graph
  - [ ] 변수 참조 추적
  - [ ] 크로스 파일 분석
  - [ ] N단계 전파 알고리즘
- [ ] **Week 5-6**: 위험도 평가
  - [ ] 타입 불일치 감지
  - [ ] 위험도 분류 로직
  - [ ] 테스트 권장 생성
- [ ] **Week 7-8**: 히트맵 UI
  - [ ] TreeView 구현
  - [ ] 상세 패널
  - [ ] Export 기능
  - [ ] 통합 테스트

### Phase 3: 3순위 - 변경 이유 추론 (2-3주)
- [ ] **Week 1**: 규칙 기반 추론
  - [ ] 주석 키워드 분석
  - [ ] 변수명 패턴
  - [ ] 로직 패턴 (20-30개 규칙)
- [ ] **Week 2**: (옵션) AI 통합
  - [ ] OpenAI/Azure API
  - [ ] Ollama 로컬 LLM
  - [ ] 프롬프트 엔지니어링
  - [ ] 보안 설정
- [ ] **Week 3**: UI 통합
  - [ ] 컬럼 추가
  - [ ] 신뢰도 아이콘
  - [ ] AI 토글
  - [ ] 테스트

### Phase 6: 통합 및 최적화 (3주)
- [ ] **Week 1**: 통합 테스트
  - [ ] 모든 기능 연동
  - [ ] 실제 프로젝트 (300 파일) 테스트
  - [ ] 버그 수정
- [ ] **Week 2**: 성능 최적화
  - [ ] 프로파일링
  - [ ] 병목 제거
  - [ ] 메모리 최적화
- [ ] **Week 3**: 문서 및 릴리스
  - [ ] 사용자 가이드
  - [ ] API 문서
  - [ ] 릴리스 노트
  - [ ] 패키징

---

## 📊 수정된 예상 소요 기간

| Phase | 기간 | 누적 | 변경사항 |
|-------|------|------|----------|
| Phase 0-A: Lexer | 2주 | 2주 | 새로 분리 |
| Phase 5: 1순위 (Diff) | 4주 | 6주 | **우선 구현** |
| Phase 1: 5순위 (I/O) | 1주 | 7주 | 유지 |
| Phase 0-B/C/D: Parser | 6주 | 13주 | 확대 (2-3주→6주) |
| Phase 4: 2순위 (Impact) | 6-8주 | 21주 | 확대 (4-5주→6-8주) |
| Phase 3: 3순위 (Reason) | 2-3주 | 24주 | 유지 |
| Phase 6: 통합 + 최적화 | 3주 | **27주** | 확대 (2주→3주) |

**총 예상 기간**: **27주 (약 6.5개월)** ← 기존 19주(4.5개월)에서 조정

### 조정 근거
- ✅ **ANTLR4 파서**: 단계적 구현으로 현실화 (2-3주 → 8주)
- ✅ **영향도 분석**: 복잡도 반영 (4-5주 → 6-8주)
- ✅ **성능 최적화**: 별도 시간 할당 (+1주)
- ✅ **테스트/문서**: 각 Phase에 포함
- ✅ **우선순위 재조정**: Lexer → 1순위 → Parser → 2순위

---

## 🎯 우선순위별 기대 효과

### 1순위 (Side-by-Side Diff)
- **즉시 사용 가능**: 구현 직후 코드 리뷰에 바로 활용
- **가시성 향상**: 변경 사항을 한눈에 파악
- **문서화**: HTML/PDF 리포트로 변경 이력 보존

### 2순위 (Impact Analysis)
- **리스크 관리**: 변경의 영향 범위를 사전에 파악
- **테스트 계획**: 어느 부분을 테스트해야 할지 명확
- **승인 결정**: 위험도 기반으로 변경 승인/거부 판단

### 3순위 (Reason Inference)
- **코드 리뷰 효율화**: 변경 이유를 자동으로 파악
- **학습**: 코드 변경 패턴을 팀원과 공유
- **문서화**: 변경 이유를 자동으로 기록

---

## 🔧 기술 스택 요약

| 계층 | 기술 | 용도 |
|------|------|------|
| **Diff Engine** | DiffPlex | Myers/Patience 알고리즘 |
| **Parser** | ANTLR4 | ST 문법 파싱 및 AST 생성 |
| **UI Framework** | WPF (.NET 8/9) | MVVM 아키텍처 |
| **Static Analysis** | 자체 구현 (AST 기반) | Call Graph, Data Flow |
| **NLP** | Regex + 규칙 엔진 | 주석/변수명 분석 |
| **AI (옵션)** | OpenAI API / Azure OpenAI / Ollama | 변경 이유 추론 |
| **Testing** | xUnit / WpfFact | 단위/통합/E2E 테스트 |
| **Logging** | Microsoft.Extensions.Logging | 구조화된 로깅 |
| **Caching** | System.Runtime.Caching | 성능 최적화 |
| **Security** | Windows DPAPI | API 키 암호화 |

---

## 📝 다음 단계

### 즉시 착수 가능
1. ✅ **Phase 0-A 시작**: STLexer.g4 문법 정의 (2주)
   - ANTLR4 개발 환경 설정
   - ST 키워드, 연산자, 리터럴 토큰 정의
   - 단위 테스트 작성

2. ✅ **Phase 5 준비**: DiffPlex 및 UI 프로토타입 (병행 가능)
   - NuGet 패키지 설치
   - 간단한 Diff 프로토타입
   - XAML 레이아웃 초안

### 승인 필요
3. **사용자 검토**: 수정된 설계안 피드백
   - 27주(6.5개월) 일정 승인
   - 우선순위 재조정 승인 (Lexer → 1순위 → Parser → 2순위)
   - 추가된 섹션 검토 (성능, 보안, 테스트)

4. **리소스 할당**: 개발 인력 및 일정 조율

---

## 📊 문서 개정 이력

| 버전 | 날짜 | 변경사항 |
|------|------|----------|
| 1.0 | 2025-11-24 | 초안 작성 (요구사항 기반 설계) |
| 2.0 | 2025-11-24 | 검토 결과 반영 (성능/보안/테스트/통합 가이드 추가, 로드맵 수정) |

---

**문서 버전**: 2.0
**최종 업데이트**: 2025-11-24
**작성자**: 설계 검토 프로세스
**관련 문서**: [requirements_specification.md](requirements_specification.md)
