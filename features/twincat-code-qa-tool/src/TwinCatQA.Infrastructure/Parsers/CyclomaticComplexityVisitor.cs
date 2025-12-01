using System;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Infrastructure.Parsers
{
    /// <summary>
    /// 사이클로매틱 복잡도 계산 Visitor
    ///
    /// McCabe의 사이클로매틱 복잡도를 계산합니다.
    /// 기본 공식: M = E - N + 2P (E=간선, N=노드, P=연결된 컴포넌트)
    /// 단순화 방식: M = 1 + 결정 포인트 개수
    /// </summary>
    /// <remarks>
    /// ANTLR4 Visitor 패턴 사용
    /// StructuredText.g4를 컴파일한 후 StructuredTextBaseVisitor를 상속하여 구현
    ///
    /// 현재는 ANTLR4 생성 파일 없이 스켈레톤만 제공
    /// 실제 구현은 ANTLR4 컴파일 후 주석 해제하여 사용
    /// </remarks>
    public class CyclomaticComplexityVisitor // TODO: ANTLR 생성 후 상속: : StructuredTextBaseVisitor<int>
    {
        #pragma warning disable CS0169 // 필드가 사용되지 않음 (ANTLR 구현 대기 중)
        private int _complexity;
        #pragma warning restore CS0169

        /// <summary>
        /// Function Block의 사이클로매틱 복잡도를 계산합니다.
        /// </summary>
        /// <param name="functionBlock">분석할 Function Block</param>
        /// <returns>복잡도 값 (최소 1)</returns>
        public int Calculate(FunctionBlock functionBlock)
        {
            // TODO: ANTLR4 통합 후 구현
            // 실제 구현:
            // _complexity = 1; // 기본값
            // Visit(functionBlock.AstNode);
            // return _complexity;

            // 현재는 기본값 반환
            return 1;
        }

        // TODO: ANTLR4 생성 파일 추가 후 아래 메서드 주석 해제

        /*
        /// <summary>
        /// IF 문 방문 - 복잡도 +1
        /// </summary>
        public override int VisitIfStatement(StructuredTextParser.IfStatementContext context)
        {
            // IF 문 자체로 +1
            _complexity++;

            // THEN 블록 방문
            Visit(context.statement());

            // 각 ELSIF 절마다 +1
            if (context.expression() != null)
            {
                foreach (var elsif in context.expression().Skip(1)) // 첫 번째는 IF 조건
                {
                    _complexity++; // 각 ELSIF마다 +1
                }
            }

            // ELSE 절은 복잡도를 증가시키지 않음 (기본 경로)
            // 하지만 내부 statement는 방문해야 함

            return base.VisitIfStatement(context);
        }

        /// <summary>
        /// CASE 문 방문 - 각 분기마다 복잡도 +1
        /// </summary>
        public override int VisitCaseStatement(StructuredTextParser.CaseStatementContext context)
        {
            // CASE 문의 각 case 요소마다 +1
            var caseElements = context.caseElement();
            if (caseElements != null)
            {
                _complexity += caseElements.Length;
            }

            // ELSE 절은 복잡도를 증가시키지 않음 (기본 경로)

            return base.VisitCaseStatement(context);
        }

        /// <summary>
        /// FOR 문 방문 - 복잡도 +1
        /// </summary>
        public override int VisitForStatement(StructuredTextParser.ForStatementContext context)
        {
            _complexity++; // FOR 루프 자체로 +1

            return base.VisitForStatement(context);
        }

        /// <summary>
        /// WHILE 문 방문 - 복잡도 +1
        /// </summary>
        public override int VisitWhileStatement(StructuredTextParser.WhileStatementContext context)
        {
            _complexity++; // WHILE 루프 자체로 +1

            return base.VisitWhileStatement(context);
        }

        /// <summary>
        /// REPEAT 문 방문 - 복잡도 +1
        /// </summary>
        public override int VisitRepeatStatement(StructuredTextParser.RepeatStatementContext context)
        {
            _complexity++; // REPEAT 루프 자체로 +1

            return base.VisitRepeatStatement(context);
        }

        /// <summary>
        /// 논리 AND 연산자 방문 - 복잡도 +1
        /// 단, 이는 선택적 구현 (표준 McCabe에서는 제외)
        /// </summary>
        public override int VisitExpression(StructuredTextParser.ExpressionContext context)
        {
            // 논리 연산자 (&&, ||)를 복잡도에 포함할지 선택
            // 현재는 제외 (표준 McCabe 방식)

            // 만약 포함하려면:
            // if (context.op?.Type == StructuredTextParser.AND ||
            //     context.op?.Type == StructuredTextParser.OR)
            // {
            //     _complexity++;
            // }

            return base.VisitExpression(context);
        }

        /// <summary>
        /// EXIT 문 방문 - 복잡도 +1 (선택적)
        /// </summary>
        public override int VisitExitStatement(StructuredTextParser.ExitStatementContext context)
        {
            // EXIT는 루프의 조기 탈출 경로를 생성하므로 +1
            _complexity++;

            return base.VisitExitStatement(context);
        }

        /// <summary>
        /// RETURN 문 방문 - 복잡도 +1 (선택적)
        /// </summary>
        public override int VisitReturnStatement(StructuredTextParser.ReturnStatementContext context)
        {
            // 함수 중간의 RETURN은 조기 반환 경로를 생성하므로 +1
            // 단, 함수 끝의 RETURN은 제외 (기본 경로)
            _complexity++;

            return base.VisitReturnStatement(context);
        }
        */

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
