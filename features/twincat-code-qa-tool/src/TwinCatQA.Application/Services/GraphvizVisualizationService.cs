using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Application.Services;

/// <summary>
/// Graphviz 기반 의존성 그래프 시각화 서비스
///
/// DOT 형식의 그래프를 SVG, PNG 등의 이미지로 변환합니다.
/// Graphviz가 설치되지 않은 경우 DOT 파일만 생성합니다.
/// </summary>
public class GraphvizVisualizationService
{
    private readonly ILogger<GraphvizVisualizationService> _logger;
    private const string GRAPHVIZ_DOT_COMMAND = "dot";

    public GraphvizVisualizationService(ILogger<GraphvizVisualizationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Graphviz 설치 여부 확인
    /// </summary>
    public bool IsGraphvizInstalled()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = GRAPHVIZ_DOT_COMMAND,
                    Arguments = "-V",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit(2000);

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// DOT 그래프를 SVG로 변환
    /// </summary>
    /// <param name="dotContent">DOT 형식 그래프 내용</param>
    /// <param name="outputPath">출력 SVG 파일 경로</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>생성된 SVG 파일 경로 (실패 시 null)</returns>
    public async Task<string?> ConvertToSvgAsync(
        string dotContent,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dotContent))
        {
            throw new ArgumentException("DOT 내용이 비어있습니다.", nameof(dotContent));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("출력 경로가 비어있습니다.", nameof(outputPath));
        }

        // DOT 파일 먼저 저장
        string dotFilePath = Path.ChangeExtension(outputPath, ".dot");
        await File.WriteAllTextAsync(dotFilePath, dotContent, Encoding.UTF8, cancellationToken);

        _logger.LogInformation($"DOT 파일 저장: {dotFilePath}");

        // Graphviz가 설치되지 않은 경우
        if (!IsGraphvizInstalled())
        {
            _logger.LogWarning("Graphviz가 설치되지 않았습니다. DOT 파일만 생성되었습니다.");
            _logger.LogWarning("SVG 변환을 위해서는 Graphviz를 설치하세요: https://graphviz.org/download/");
            return null;
        }

        try
        {
            // DOT → SVG 변환
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = GRAPHVIZ_DOT_COMMAND,
                    Arguments = $"-Tsvg \"{dotFilePath}\" -o \"{outputPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            _logger.LogInformation($"Graphviz 실행: {process.StartInfo.FileName} {process.StartInfo.Arguments}");

            process.Start();

            string stdout = await process.StandardOutput.ReadToEndAsync();
            string stderr = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                _logger.LogError($"Graphviz 변환 실패 (ExitCode: {process.ExitCode})");
                _logger.LogError($"stderr: {stderr}");
                return null;
            }

            if (File.Exists(outputPath))
            {
                _logger.LogInformation($"SVG 파일 생성 완료: {outputPath}");
                return outputPath;
            }

            _logger.LogWarning("SVG 파일이 생성되지 않았습니다.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Graphviz 변환 중 예외 발생");
            return null;
        }
    }

    /// <summary>
    /// 의존성 그래프를 향상된 DOT 형식으로 변환 (스타일 적용)
    /// </summary>
    /// <param name="graph">의존성 그래프</param>
    /// <param name="title">그래프 제목</param>
    /// <returns>스타일이 적용된 DOT 형식 문자열</returns>
    public string GenerateStyledDotGraph(DependencyGraph graph, string title = "Dependency Graph")
    {
        var sb = new StringBuilder();

        // 그래프 헤더
        sb.AppendLine($"digraph \"{title}\" {{");
        sb.AppendLine("    // 그래프 설정");
        sb.AppendLine("    rankdir=LR;");  // 좌우 방향
        sb.AppendLine("    bgcolor=\"#FFFFFF\";");
        sb.AppendLine("    fontname=\"맑은 고딕\";");
        sb.AppendLine("    fontsize=14;");
        sb.AppendLine("    labelloc=\"t\";");
        sb.AppendLine($"    label=\"{EscapeDotString(title)}\";");
        sb.AppendLine();

        // 노드 기본 스타일
        sb.AppendLine("    // 노드 기본 스타일");
        sb.AppendLine("    node [");
        sb.AppendLine("        shape=box,");
        sb.AppendLine("        style=\"rounded,filled\",");
        sb.AppendLine("        fontname=\"맑은 고딕\",");
        sb.AppendLine("        fontsize=11");
        sb.AppendLine("    ];");
        sb.AppendLine();

        // 엣지 기본 스타일
        sb.AppendLine("    // 엣지 기본 스타일");
        sb.AppendLine("    edge [");
        sb.AppendLine("        fontname=\"맑은 고딕\",");
        sb.AppendLine("        fontsize=9,");
        sb.AppendLine("        color=\"#666666\"");
        sb.AppendLine("    ];");
        sb.AppendLine();

        // 노드 정의 (타입별 색상 구분)
        sb.AppendLine("    // 노드 정의");
        foreach (var node in graph.Nodes)
        {
            string color = GetNodeColor(node.NodeType);
            string shape = GetNodeShape(node.NodeType);

            sb.AppendLine($"    \"{EscapeDotString(node.Id)}\" [");
            sb.AppendLine($"        label=\"{EscapeDotString(node.Id)}\",");  // Id를 레이블로 사용
            sb.AppendLine($"        fillcolor=\"{color}\",");
            sb.AppendLine($"        shape={shape}");
            sb.AppendLine("    ];");
        }
        sb.AppendLine();

        // 엣지 정의 (의존성 타입별 스타일 구분)
        sb.AppendLine("    // 엣지 정의");
        foreach (var edge in graph.Edges)
        {
            string edgeStyle = GetEdgeStyle(edge.Type);
            string edgeLabel = GetEdgeLabel(edge.Type);

            sb.AppendLine($"    \"{EscapeDotString(edge.From)}\" -> \"{EscapeDotString(edge.To)}\" [");
            sb.AppendLine($"        label=\"{edgeLabel}\",");
            sb.AppendLine($"        {edgeStyle}");
            sb.AppendLine("    ];");
        }

        // 그래프 푸터
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// DOT 문자열 이스케이프
    /// </summary>
    private string EscapeDotString(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "");
    }

    /// <summary>
    /// 노드 타입에 따른 색상 반환
    /// </summary>
    private string GetNodeColor(string nodeType)
    {
        return nodeType.ToUpper() switch
        {
            "PROGRAM" => "#B3E5FC",        // 연한 파랑
            "FUNCTION_BLOCK" => "#C8E6C9", // 연한 초록
            "FUNCTION" => "#FFF9C4",       // 연한 노랑
            "INTERFACE" => "#F8BBD0",      // 연한 분홍
            "UNKNOWN" => "#E0E0E0",        // 회색
            _ => "#FFFFFF"
        };
    }

    /// <summary>
    /// 노드 타입에 따른 모양 반환
    /// </summary>
    private string GetNodeShape(string nodeType)
    {
        return nodeType.ToUpper() switch
        {
            "PROGRAM" => "box",
            "FUNCTION_BLOCK" => "component",
            "FUNCTION" => "ellipse",
            "INTERFACE" => "diamond",
            _ => "box"
        };
    }

    /// <summary>
    /// 의존성 타입에 따른 엣지 스타일 반환
    /// </summary>
    private string GetEdgeStyle(DependencyType dependencyType)
    {
        return dependencyType switch
        {
            DependencyType.FunctionCall => "style=solid, color=\"#2196F3\"",
            DependencyType.Inheritance => "style=dashed, color=\"#4CAF50\", arrowhead=onormal",
            DependencyType.InterfaceImplementation => "style=dotted, color=\"#E91E63\", arrowhead=onormal",
            DependencyType.VariableReference => "style=solid, color=\"#9E9E9E\", arrowhead=vee",
            _ => "style=solid, color=\"#666666\""
        };
    }

    /// <summary>
    /// 의존성 타입에 따른 엣지 레이블 반환
    /// </summary>
    private string GetEdgeLabel(DependencyType dependencyType)
    {
        return dependencyType switch
        {
            DependencyType.FunctionCall => "호출",
            DependencyType.Inheritance => "상속",
            DependencyType.InterfaceImplementation => "구현",
            DependencyType.VariableReference => "참조",
            _ => ""
        };
    }
}
