using System;
using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Infrastructure.Parsers
{
    /// <summary>
    /// 사이클로매틱 복잡도 계산기
    ///
    /// McCabe의 사이클로매틱 복잡도를 정규식 기반으로 계산합니다.
    /// 공식: M = 1 + 결정 포인트 개수
    /// </summary>
    /// <remarks>
    /// 결정 포인트:
    /// - IF, ELSIF 문
    /// - FOR, WHILE, REPEAT 루프
    /// - CASE 문의 각 분기
    /// - AND, OR 논리 연산자 (조건 복잡도)
    /// - EXIT, RETURN (조기 탈출)
    /// - __TRY/__CATCH 예외 처리
    /// </remarks>
    public class CyclomaticComplexityVisitor
    {
        // 정규식 패턴 (컴파일됨)
        private static readonly Regex IfPattern = new(@"\bIF\b(?!\s*$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ElsifPattern = new(@"\bELSIF\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ForPattern = new(@"\bFOR\b\s+\w+\s*:=", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex WhilePattern = new(@"\bWHILE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RepeatPattern = new(@"\bREPEAT\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CaseOfPattern = new(@"\bCASE\b.*?\bOF\b", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex CaseBranchPattern = new(@"^\s*(\d+|[\w_]+)\s*:", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex AndPattern = new(@"\bAND\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex OrPattern = new(@"\bOR\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ExitPattern = new(@"\bEXIT\b\s*;", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ReturnPattern = new(@"\bRETURN\b\s*;", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex TryCatchPattern = new(@"\b__CATCH\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // 주석 제거용 패턴
        private static readonly Regex LineCommentPattern = new(@"//.*$", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex BlockCommentPattern = new(@"\(\*.*?\*\)", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Function Block의 사이클로매틱 복잡도를 계산합니다.
        /// </summary>
        /// <param name="functionBlock">분석할 Function Block</param>
        /// <returns>복잡도 값 (최소 1)</returns>
        public int Calculate(FunctionBlock functionBlock)
        {
            if (functionBlock == null || string.IsNullOrEmpty(functionBlock.Body))
            {
                return 1;
            }

            return CalculateFromCode(functionBlock.Body);
        }

        /// <summary>
        /// 소스 코드 문자열에서 사이클로매틱 복잡도를 계산합니다.
        /// </summary>
        /// <param name="code">분석할 ST 코드</param>
        /// <returns>복잡도 값 (최소 1)</returns>
        public int CalculateFromCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return 1;
            }

            // 주석 제거 (주석 내 키워드가 복잡도에 영향을 주지 않도록)
            var cleanCode = RemoveComments(code);

            int complexity = 1; // 기본 복잡도

            // 1. 조건문 (IF, ELSIF)
            complexity += IfPattern.Matches(cleanCode).Count;
            complexity += ElsifPattern.Matches(cleanCode).Count;

            // 2. 반복문 (FOR, WHILE, REPEAT)
            complexity += ForPattern.Matches(cleanCode).Count;
            complexity += WhilePattern.Matches(cleanCode).Count;
            complexity += RepeatPattern.Matches(cleanCode).Count;

            // 3. CASE 문 분기 (각 분기마다 +1)
            complexity += CountCaseBranches(cleanCode);

            // 4. 논리 연산자 (AND, OR) - 조건 복잡도
            complexity += AndPattern.Matches(cleanCode).Count;
            complexity += OrPattern.Matches(cleanCode).Count;

            // 5. 조기 탈출 (EXIT, RETURN)
            complexity += ExitPattern.Matches(cleanCode).Count;
            complexity += ReturnPattern.Matches(cleanCode).Count;

            // 6. 예외 처리 (__TRY/__CATCH)
            complexity += TryCatchPattern.Matches(cleanCode).Count;

            return complexity;
        }

        /// <summary>
        /// 소스 코드에서 주석을 제거합니다.
        /// </summary>
        private string RemoveComments(string code)
        {
            // 블록 주석 제거 (* ... *)
            var result = BlockCommentPattern.Replace(code, " ");
            // 라인 주석 제거 //
            result = LineCommentPattern.Replace(result, "");
            return result;
        }

        /// <summary>
        /// CASE 문의 분기 개수를 계산합니다.
        /// </summary>
        private int CountCaseBranches(string code)
        {
            int branchCount = 0;

            // CASE ... OF ... END_CASE 블록 찾기
            var caseMatches = CaseOfPattern.Matches(code);
            if (caseMatches.Count == 0)
            {
                return 0;
            }

            // 각 CASE 블록 내에서 분기 개수 세기
            // CASE 블록 시작 위치 찾기
            var casePositions = new System.Collections.Generic.List<int>();
            var endCasePattern = new Regex(@"\bEND_CASE\b", RegexOptions.IgnoreCase);

            foreach (Match caseMatch in caseMatches)
            {
                int startPos = caseMatch.Index + caseMatch.Length;
                var endMatch = endCasePattern.Match(code, startPos);

                if (endMatch.Success)
                {
                    // CASE와 END_CASE 사이의 코드 추출
                    string caseBlock = code.Substring(startPos, endMatch.Index - startPos);

                    // 분기 패턴 매칭 (숫자: 또는 식별자:)
                    branchCount += CaseBranchPattern.Matches(caseBlock).Count;
                }
            }

            return branchCount;
        }

        // ----------------------------------------------------------------------------
        // 복잡도 계산 가이드라인
        // ----------------------------------------------------------------------------
        //
        // McCabe의 사이클로매틱 복잡도:
        //
        // 1. 기본값: 1 (단일 경로)
        //
        // 2. 결정 포인트마다 +1:
        //    - IF 문: +1
        //    - ELSIF 절: 각각 +1
        //    - CASE 문: 각 case 요소마다 +1
        //    - FOR 문: +1
        //    - WHILE 문: +1
        //    - REPEAT 문: +1
        //
        // 3. 선택적 +1:
        //    - EXIT 문: +1 (조기 탈출)
        //    - RETURN 문: +1 (중간 반환, 단 마지막 RETURN 제외)
        //    - 논리 AND/OR: +1 (엄격한 계산 시)
        //
        // 4. 복잡도 임계값:
        //    - 1-10: 단순 (Good)
        //    - 11-15: 보통 (Medium warning)
        //    - 16-20: 복잡 (High warning)
        //    - 21+: 매우 복잡 (Critical, 리팩토링 필수)
        //
        // 5. 예시:
        //
        //    FUNCTION_BLOCK FB_Example
        //    VAR
        //        state : INT;
        //        alarm : BOOL;
        //    END_VAR
        //
        //    IF state = 0 THEN          // +1 (IF)
        //        state := 1;
        //    ELSIF state = 1 THEN       // +1 (ELSIF)
        //        state := 2;
        //    ELSIF state = 2 THEN       // +1 (ELSIF)
        //        state := 0;
        //    END_IF;
        //
        //    IF alarm THEN              // +1 (IF)
        //        RETURN;                // +1 (조기 반환)
        //    END_IF;
        //
        //    // 총 복잡도 = 1 (기본) + 1 (IF) + 2 (ELSIF×2) + 1 (IF) + 1 (RETURN) = 6
        //
        // ----------------------------------------------------------------------------

        /// <summary>
        /// 복잡도를 기반으로 품질 등급을 반환합니다.
        /// </summary>
        /// <param name="complexity">복잡도 값</param>
        /// <returns>품질 등급</returns>
        public static QualityGrade GetQualityGrade(int complexity)
        {
            if (complexity <= 10)
                return QualityGrade.Good;

            if (complexity <= 15)
                return QualityGrade.Fair;

            if (complexity <= 20)
                return QualityGrade.Poor;

            return QualityGrade.Poor; // 21 이상은 매우 복잡
        }

        /// <summary>
        /// 복잡도를 기반으로 위반 심각도를 반환합니다.
        /// </summary>
        /// <param name="complexity">복잡도 값</param>
        /// <param name="mediumThreshold">Medium 임계값 (기본 10)</param>
        /// <param name="highThreshold">High 임계값 (기본 15)</param>
        /// <param name="criticalThreshold">Critical 임계값 (기본 20)</param>
        /// <returns>위반 심각도 (임계값 미만이면 null)</returns>
        public static ViolationSeverity? GetViolationSeverity(
            int complexity,
            int mediumThreshold = 10,
            int highThreshold = 15,
            int criticalThreshold = 20)
        {
            if (complexity > criticalThreshold)
                return ViolationSeverity.Critical;

            if (complexity > highThreshold)
                return ViolationSeverity.High;

            if (complexity > mediumThreshold)
                return ViolationSeverity.Medium;

            // 임계값 이하면 위반 없음
            return null;
        }
    }
}
