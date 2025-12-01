using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TwinCatQA.Domain.Contracts;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.CLI.Utils;

/// <summary>
/// 파일 스캔 유틸리티
///
/// TwinCAT 프로젝트 디렉토리에서 분석 대상 파일(.TcPOU, .TcDUT, .TcGVL)을 스캔합니다.
/// </summary>
public static class FileScanner
{
    /// <summary>
    /// TwinCAT 파일 확장자 목록
    /// </summary>
    private static readonly string[] TwinCATExtensions = { ".TcPOU", ".TcDUT", ".TcGVL" };

    /// <summary>
    /// 지정된 경로에서 모든 TwinCAT 파일을 재귀적으로 검색합니다.
    /// </summary>
    /// <param name="projectPath">프로젝트 루트 경로</param>
    /// <returns>찾은 파일 경로 목록</returns>
    public static List<string> ScanTwinCATFiles(string projectPath)
    {
        if (!Directory.Exists(projectPath))
        {
            throw new DirectoryNotFoundException($"프로젝트 경로가 존재하지 않습니다: {projectPath}");
        }

        var files = new List<string>();

        foreach (var extension in TwinCATExtensions)
        {
            var foundFiles = Directory.GetFiles(projectPath, $"*{extension}", SearchOption.AllDirectories);
            files.AddRange(foundFiles);
        }

        return files;
    }

    /// <summary>
    /// 파일 목록을 파싱하여 ValidationSession을 생성합니다.
    /// </summary>
    /// <param name="projectPath">프로젝트 경로</param>
    /// <param name="parser">파서 서비스</param>
    /// <returns>검증 세션</returns>
    public static ValidationSession CreateValidationSession(string projectPath, IParserService parser)
    {
        var files = ScanTwinCATFiles(projectPath);

        var session = new ValidationSession
        {
            ProjectPath = projectPath,
            SyntaxTrees = new List<SyntaxTree>()
        };

        foreach (var file in files)
        {
            try
            {
                var syntaxTree = parser.ParseFile(file);
                session.SyntaxTrees.Add(syntaxTree);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  파일 파싱 실패: {file}");
                Console.WriteLine($"   오류: {ex.Message}");
            }
        }

        return session;
    }
}
