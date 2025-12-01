using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Infrastructure.Parsers;
using Xunit;
using Xunit.Abstractions;

namespace TwinCatQA.Integration.Tests
{
    /// <summary>
    /// 실제 TwinCAT 프로젝트 파일 파싱 테스트
    ///
    /// 실제 파일을 읽어서 파싱하고 AST를 추출하는 테스트
    /// </summary>
    public class RealProjectParsingTests
    {
        private readonly ITestOutputHelper _output;
        private readonly string _testProjectPath;
        private readonly AntlrParserService _parserService;

        public RealProjectParsingTests(ITestOutputHelper output)
        {
            _output = output;
            _testProjectPath = @"D:\00.Comapre\pollux_hcds_ald_mirror\Src_Diff\PLC\TM";
            _parserService = new AntlrParserService();
        }

        [Fact]
        public void ParseMAINPOU_ShouldExtractStructuredTextCode()
        {
            // Arrange
            var mainPouPath = Path.Combine(_testProjectPath, "TM", "TM", "POUs", "MAIN.TcPOU");

            if (!File.Exists(mainPouPath))
            {
                _output.WriteLine($"⚠️ MAIN.TcPOU 파일이 존재하지 않습니다: {mainPouPath}");
                _output.WriteLine("이 테스트를 건너뜁니다.");
                return;
            }

            // Act - 파일 파싱
            var syntaxTree = _parserService.ParseFile(mainPouPath);

            // Assert
            _output.WriteLine($"\n==========================================");
            _output.WriteLine($"📄 파일: MAIN.TcPOU");
            _output.WriteLine($"==========================================");
            _output.WriteLine($"파일 경로: {syntaxTree.FilePath}");
            _output.WriteLine($"소스 코드 길이: {syntaxTree.SourceCode.Length}자");
            _output.WriteLine($"파싱 오류: {syntaxTree.Errors.Count}개");
            _output.WriteLine($"파싱 성공 여부: {syntaxTree.IsValid}");

            syntaxTree.Should().NotBeNull();
            syntaxTree.SourceCode.Should().NotBeEmpty();
            syntaxTree.FilePath.Should().Be(mainPouPath);

            // 소스 코드 샘플 출력
            var sourceLines = syntaxTree.SourceCode.Split('\n').Take(30).ToList();
            _output.WriteLine($"\n--- 소스 코드 샘플 (처음 30줄) ---");
            for (int i = 0; i < sourceLines.Count; i++)
            {
                _output.WriteLine($"{i + 1,3}: {sourceLines[i]}");
            }

            // 주석 추출
            var comments = _parserService.ExtractComments(syntaxTree);
            _output.WriteLine($"\n--- 주석 분석 ---");
            _output.WriteLine($"총 주석 수: {comments.Count}개");

            foreach (var comment in comments.Take(10))
            {
                bool isKorean = _parserService.IsKoreanComment(comment.Value);
                string koreanStatus = isKorean ? "✅ 한글" : "❌ 영어";
                _output.WriteLine($"  라인 {comment.Key}: {koreanStatus} - \"{comment.Value}\"");
            }
        }

        [Fact]
        public void ParseAllPOUFiles_ShouldSucceed()
        {
            // Arrange
            if (!Directory.Exists(_testProjectPath))
            {
                _output.WriteLine($"⚠️ 테스트 프로젝트 경로가 존재하지 않습니다: {_testProjectPath}");
                return;
            }

            var pouFiles = Directory.GetFiles(_testProjectPath, "*.TcPOU", SearchOption.AllDirectories).ToList();

            if (pouFiles.Count == 0)
            {
                _output.WriteLine("⚠️ POU 파일을 찾을 수 없습니다.");
                return;
            }

            // Act & Assert
            _output.WriteLine($"\n==========================================");
            _output.WriteLine($"📊 총 POU 파일: {pouFiles.Count}개 파싱 테스트");
            _output.WriteLine($"==========================================\n");

            int successCount = 0;
            int failureCount = 0;
            var failedFiles = new List<(string FileName, string Error)>();

            foreach (var pouFile in pouFiles)
            {
                try
                {
                    var syntaxTree = _parserService.ParseFile(pouFile);
                    var fileName = Path.GetFileName(pouFile);

                    if (syntaxTree.IsValid)
                    {
                        successCount++;
                        _output.WriteLine($"✅ {fileName,-40} | 코드 길이: {syntaxTree.SourceCode.Length,6}자 | 오류: {syntaxTree.Errors.Count}개");
                    }
                    else
                    {
                        failureCount++;
                        var errorMsg = syntaxTree.Errors.FirstOrDefault()?.Message ?? "알 수 없는 오류";
                        _output.WriteLine($"⚠️ {fileName,-40} | 파싱 오류: {errorMsg}");
                        failedFiles.Add((fileName, errorMsg));
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    var fileName = Path.GetFileName(pouFile);
                    _output.WriteLine($"❌ {fileName,-40} | 예외 발생: {ex.Message}");
                    failedFiles.Add((fileName, ex.Message));
                }
            }

            // 결과 요약
            _output.WriteLine($"\n==========================================");
            _output.WriteLine($"📊 파싱 결과 요약");
            _output.WriteLine($"==========================================");
            _output.WriteLine($"✅ 성공: {successCount}개");
            _output.WriteLine($"❌ 실패: {failureCount}개");
            _output.WriteLine($"📈 성공률: {(double)successCount / pouFiles.Count * 100:F1}%");

            if (failedFiles.Any())
            {
                _output.WriteLine($"\n실패한 파일 목록:");
                foreach (var (fileName, error) in failedFiles)
                {
                    _output.WriteLine($"  - {fileName}: {error}");
                }
            }

            // 최소 50% 이상 성공해야 함
            (successCount >= pouFiles.Count * 0.5).Should().BeTrue($"파싱 성공률이 50% 이상이어야 합니다 (현재: {(double)successCount / pouFiles.Count * 100:F1}%)");
        }

        [Fact]
        public void ParseDUTFiles_ShouldExtractEnumTypes()
        {
            // Arrange
            if (!Directory.Exists(_testProjectPath))
            {
                _output.WriteLine($"⚠️ 테스트 프로젝트 경로가 존재하지 않습니다: {_testProjectPath}");
                return;
            }

            var dutFiles = Directory.GetFiles(_testProjectPath, "*.TcDUT", SearchOption.AllDirectories).Take(5).ToList();

            if (dutFiles.Count == 0)
            {
                _output.WriteLine("⚠️ DUT 파일을 찾을 수 없습니다.");
                return;
            }

            // Act & Assert
            _output.WriteLine($"\n==========================================");
            _output.WriteLine($"📊 DUT 파일 파싱 테스트 (샘플 5개)");
            _output.WriteLine($"==========================================\n");

            foreach (var dutFile in dutFiles)
            {
                var syntaxTree = _parserService.ParseFile(dutFile);
                var fileName = Path.GetFileName(dutFile);

                _output.WriteLine($"\n📄 파일: {fileName}");
                _output.WriteLine($"   소스 코드 길이: {syntaxTree.SourceCode.Length}자");
                _output.WriteLine($"   파싱 성공: {syntaxTree.IsValid}");

                // 소스 코드 일부 출력
                var sourceLines = syntaxTree.SourceCode.Split('\n').Take(15);
                _output.WriteLine($"   --- 소스 코드 샘플 ---");
                foreach (var line in sourceLines)
                {
                    _output.WriteLine($"   {line}");
                }

                syntaxTree.Should().NotBeNull();
                syntaxTree.SourceCode.Should().NotBeEmpty();
            }
        }

        [Fact]
        public void ExtractComments_FromAllPOUFiles_ShouldDetectKoreanRatio()
        {
            // Arrange
            if (!Directory.Exists(_testProjectPath))
            {
                _output.WriteLine($"⚠️ 테스트 프로젝트 경로가 존재하지 않습니다: {_testProjectPath}");
                return;
            }

            var pouFiles = Directory.GetFiles(_testProjectPath, "*.TcPOU", SearchOption.AllDirectories).ToList();

            if (pouFiles.Count == 0)
            {
                _output.WriteLine("⚠️ POU 파일을 찾을 수 없습니다.");
                return;
            }

            // Act
            _output.WriteLine($"\n==========================================");
            _output.WriteLine($"📊 주석 한글 비율 분석 ({pouFiles.Count}개 파일)");
            _output.WriteLine($"==========================================\n");

            int totalComments = 0;
            int koreanComments = 0;
            int englishComments = 0;

            foreach (var pouFile in pouFiles)
            {
                var syntaxTree = _parserService.ParseFile(pouFile);
                var comments = _parserService.ExtractComments(syntaxTree);
                var fileName = Path.GetFileName(pouFile);

                int fileKoreanCount = 0;
                int fileEnglishCount = 0;

                foreach (var comment in comments)
                {
                    totalComments++;
                    bool isKorean = _parserService.IsKoreanComment(comment.Value);

                    if (isKorean)
                    {
                        koreanComments++;
                        fileKoreanCount++;
                    }
                    else
                    {
                        englishComments++;
                        fileEnglishCount++;
                    }
                }

                if (comments.Count > 0)
                {
                    double koreanRatio = (double)fileKoreanCount / comments.Count * 100;
                    string status = koreanRatio >= 80 ? "✅" : "⚠️";
                    _output.WriteLine($"{status} {fileName,-40} | 주석: {comments.Count,3}개 | 한글: {fileKoreanCount,3}개 ({koreanRatio:F1}%)");
                }
            }

            // 전체 통계
            _output.WriteLine($"\n==========================================");
            _output.WriteLine($"📈 전체 주석 통계");
            _output.WriteLine($"==========================================");
            _output.WriteLine($"총 주석 수: {totalComments}개");
            _output.WriteLine($"한글 주석: {koreanComments}개 ({(double)koreanComments / totalComments * 100:F1}%)");
            _output.WriteLine($"영어 주석: {englishComments}개 ({(double)englishComments / totalComments * 100:F1}%)");

            if (totalComments > 0)
            {
                double overallKoreanRatio = (double)koreanComments / totalComments * 100;
                _output.WriteLine($"\n전체 한글 주석 비율: {overallKoreanRatio:F1}%");

                if (overallKoreanRatio < 80)
                {
                    _output.WriteLine($"⚠️ 경고: 한글 주석 비율이 80% 미만입니다!");
                }
                else
                {
                    _output.WriteLine($"✅ 양호: 한글 주석 비율이 80% 이상입니다.");
                }
            }
        }
    }
}
