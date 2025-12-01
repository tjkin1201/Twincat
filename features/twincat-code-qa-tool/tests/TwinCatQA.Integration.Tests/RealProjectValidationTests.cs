using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using TwinCatQA.Application.Services;
using TwinCatQA.Domain.Contracts;
using TwinCatQA.Domain.Models;
using TwinCatQA.Infrastructure.Parsers;
using Xunit;
using Xunit.Abstractions;

namespace TwinCatQA.Integration.Tests
{
    /// <summary>
    /// 실제 TwinCAT 프로젝트를 대상으로 한 통합 테스트
    ///
    /// 테스트 대상: D:\00.Comapre\pollux_hcds_ald_mirror\Src_Diff\PLC\TM
    /// - 34개 파일 (20 DUT, 6 GVL, 4 POU)
    /// </summary>
    public class RealProjectValidationTests
    {
        private readonly ITestOutputHelper _output;
        private readonly string _testProjectPath;

        public RealProjectValidationTests(ITestOutputHelper output)
        {
            _output = output;
            _testProjectPath = @"D:\00.Comapre\pollux_hcds_ald_mirror\Src_Diff\PLC\TM";
        }

        [Fact(Skip = "실제 프로젝트 경로가 존재할 때만 실행")]
        public void ScanRealProject_ShouldFindAllTwinCATFiles()
        {
            // Arrange - 프로젝트 경로 확인
            if (!Directory.Exists(_testProjectPath))
            {
                _output.WriteLine($"테스트 프로젝트 경로가 존재하지 않습니다: {_testProjectPath}");
                return;
            }

            // Act - 파일 스캔
            var files = Directory.GetFiles(_testProjectPath, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".TcPOU") || f.EndsWith(".TcDUT") || f.EndsWith(".TcGVL"))
                .ToList();

            // Assert
            _output.WriteLine($"발견된 파일 수: {files.Count}개");
            files.Should().NotBeEmpty("TwinCAT 파일이 최소 1개 이상 존재해야 함");

            // 파일 타입별 분류
            var pouFiles = files.Where(f => f.EndsWith(".TcPOU")).ToList();
            var dutFiles = files.Where(f => f.EndsWith(".TcDUT")).ToList();
            var gvlFiles = files.Where(f => f.EndsWith(".TcGVL")).ToList();

            _output.WriteLine($"POU 파일: {pouFiles.Count}개");
            _output.WriteLine($"DUT 파일: {dutFiles.Count}개");
            _output.WriteLine($"GVL 파일: {gvlFiles.Count}개");

            foreach (var file in files.Take(10))
            {
                _output.WriteLine($"  - {Path.GetFileName(file)}");
            }
        }

        [Fact(Skip = "실제 프로젝트 경로가 존재할 때만 실행")]
        public void ParseRealMainPOU_ShouldExtractCodeSuccessfully()
        {
            // Arrange
            var mainPouPath = Path.Combine(_testProjectPath, "TM", "TM", "POUs", "MAIN.TcPOU");

            if (!File.Exists(mainPouPath))
            {
                _output.WriteLine($"MAIN.TcPOU 파일이 존재하지 않습니다: {mainPouPath}");
                return;
            }

            var parserService = new AntlrParserService();

            // Act - 파일 파싱
            var syntaxTree = parserService.ParseFile(mainPouPath);

            // Assert
            _output.WriteLine($"파일 경로: {syntaxTree.FilePath}");
            _output.WriteLine($"소스 코드 길이: {syntaxTree.SourceCode.Length}자");
            _output.WriteLine($"파싱 오류: {syntaxTree.Errors.Count}개");

            syntaxTree.Should().NotBeNull();
            syntaxTree.SourceCode.Should().NotBeEmpty();

            // 소스 코드 일부 출력
            var sourceLines = syntaxTree.SourceCode.Split('\n').Take(20);
            _output.WriteLine("\n--- 소스 코드 샘플 (처음 20줄) ---");
            foreach (var line in sourceLines)
            {
                _output.WriteLine(line);
            }
        }

        [Fact(Skip = "실제 프로젝트 경로가 존재할 때만 실행")]
        public void ValidateRealProject_KoreanCommentRule_ShouldDetectViolations()
        {
            // Arrange
            var mainPouPath = Path.Combine(_testProjectPath, "TM", "TM", "POUs", "MAIN.TcPOU");

            if (!File.Exists(mainPouPath))
            {
                _output.WriteLine($"MAIN.TcPOU 파일이 존재하지 않습니다: {mainPouPath}");
                return;
            }

            // 실제 검증 엔진은 구현이 필요하므로, 여기서는 파서만 테스트
            var parserService = new AntlrParserService();
            var syntaxTree = parserService.ParseFile(mainPouPath);

            // Act - 주석 추출
            var comments = parserService.ExtractComments(syntaxTree);

            // Assert
            _output.WriteLine($"\n발견된 주석: {comments.Count}개");

            foreach (var comment in comments)
            {
                _output.WriteLine($"라인 {comment.Key}: {comment.Value}");

                // 한글 포함 여부 확인
                bool isKorean = parserService.IsKoreanComment(comment.Value);
                _output.WriteLine($"  → 한글 주석 여부: {isKorean}");
            }

            comments.Should().NotBeNull();
        }

        [Fact(Skip = "실제 프로젝트 경로가 존재할 때만 실행")]
        public void FullValidation_RealProject_ShouldGenerateQualityReport()
        {
            // Arrange - 프로젝트 경로 확인
            if (!Directory.Exists(_testProjectPath))
            {
                _output.WriteLine($"테스트 프로젝트 경로가 존재하지 않습니다: {_testProjectPath}");
                return;
            }

            // 검증 엔진 초기화 (실제 구현 필요)
            // var validationEngine = new DefaultValidationEngine(...);

            // Act - 전체 검증 실행
            // var session = validationEngine.StartSession(_testProjectPath, ValidationMode.Full);
            // validationEngine.ScanFiles(session);
            // validationEngine.ParseFiles(session);
            // validationEngine.RunValidation(session);
            // validationEngine.CalculateQualityScores(session);

            // Assert
            _output.WriteLine("전체 검증 프로세스 테스트 (구현 예정)");

            // session.ScannedFiles.Should().HaveCountGreaterThan(0);
            // session.QualityScore.Should().BeGreaterOrEqualTo(0).And.BeLessThanOrEqualTo(100);
        }

        [Fact]
        public void ManualValidation_RealProject_OutputFileList()
        {
            // Arrange - 실제 경로 확인
            if (!Directory.Exists(_testProjectPath))
            {
                _output.WriteLine($"⚠️ 테스트 프로젝트 경로가 존재하지 않습니다: {_testProjectPath}");
                _output.WriteLine("이 테스트를 실행하려면 경로를 확인해주세요.");
                return;
            }

            // Act - 파일 수집
            var allFiles = Directory.GetFiles(_testProjectPath, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".TcPOU") || f.EndsWith(".TcDUT") || f.EndsWith(".TcGVL"))
                .ToList();

            // Assert
            _output.WriteLine($"\n==========================================");
            _output.WriteLine($"📁 프로젝트 경로: {_testProjectPath}");
            _output.WriteLine($"📊 총 발견된 파일: {allFiles.Count}개");
            _output.WriteLine($"==========================================\n");

            // 파일 타입별 분류
            var pouFiles = allFiles.Where(f => f.EndsWith(".TcPOU")).ToList();
            var dutFiles = allFiles.Where(f => f.EndsWith(".TcDUT")).ToList();
            var gvlFiles = allFiles.Where(f => f.EndsWith(".TcGVL")).ToList();

            _output.WriteLine($"📌 POU 파일 (Program Organization Unit): {pouFiles.Count}개");
            foreach (var file in pouFiles)
            {
                _output.WriteLine($"   - {Path.GetFileName(file)}");
            }

            _output.WriteLine($"\n📌 DUT 파일 (Data Unit Type): {dutFiles.Count}개");
            foreach (var file in dutFiles.Take(5))
            {
                _output.WriteLine($"   - {Path.GetFileName(file)}");
            }
            if (dutFiles.Count > 5)
            {
                _output.WriteLine($"   ... 외 {dutFiles.Count - 5}개");
            }

            _output.WriteLine($"\n📌 GVL 파일 (Global Variable List): {gvlFiles.Count}개");
            foreach (var file in gvlFiles)
            {
                _output.WriteLine($"   - {Path.GetFileName(file)}");
            }

            // 검증 성공
            allFiles.Should().NotBeEmpty("최소 1개 이상의 TwinCAT 파일이 발견되어야 합니다");
        }
    }
}
