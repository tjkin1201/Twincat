#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
TwinCAT 단일 프로젝트 QA 분석 스크립트
코드 품질, 잠재적 버그, 코딩 표준 검사
"""

import os
import re
import sys
from pathlib import Path
from dataclasses import dataclass, field
from typing import List, Dict, Optional, Tuple
from datetime import datetime
from collections import defaultdict
import json

@dataclass
class QAIssue:
    """QA 이슈"""
    rule_id: str
    severity: str  # Critical, Warning, Info
    category: str  # Safety, Performance, Maintainability, Style
    file_path: str
    line: int
    message: str
    code_snippet: str = ""
    suggestion: str = ""

@dataclass
class FileStats:
    """파일 통계"""
    file_path: str
    file_type: str  # POU, GVL, DUT
    pou_type: str = ""  # PROGRAM, FUNCTION_BLOCK, FUNCTION
    name: str = ""
    lines_of_code: int = 0
    lines_of_comment: int = 0
    variable_count: int = 0
    complexity: int = 0  # 순환 복잡도 추정
    issues: List[QAIssue] = field(default_factory=list)

class TwinCATSingleProjectAnalyzer:
    """TwinCAT 단일 프로젝트 분석기"""

    def __init__(self, project_path: str):
        self.project_path = Path(project_path)
        self.files: List[FileStats] = []
        self.qa_issues: List[QAIssue] = []
        self.global_vars: Dict[str, Dict] = {}
        self.functions: Dict[str, Dict] = {}

    def analyze(self) -> Dict:
        """전체 분석 실행"""
        print(f"{'='*60}")
        print(f"TwinCAT 프로젝트 QA 분석")
        print(f"{'='*60}")
        print(f"분석 대상: {self.project_path}")
        print(f"분석 시작: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        print()

        # 1. 파일 수집
        self._collect_files()

        # 2. 각 파일 분석
        self._analyze_files()

        # 3. 전역 분석 (크로스 파일)
        self._global_analysis()

        # 4. 리포트 생성
        return self._generate_report()

    def _collect_files(self):
        """분석할 파일 수집"""
        print("[1/4] 파일 수집 중...")

        extensions = {'.TcPOU': 'POU', '.TcGVL': 'GVL', '.TcDUT': 'DUT'}

        for ext, file_type in extensions.items():
            for file_path in self.project_path.rglob(f'*{ext}'):
                self.files.append(FileStats(
                    file_path=str(file_path.relative_to(self.project_path)),
                    file_type=file_type
                ))

        print(f"  - TcPOU: {len([f for f in self.files if f.file_type == 'POU'])}개")
        print(f"  - TcGVL: {len([f for f in self.files if f.file_type == 'GVL'])}개")
        print(f"  - TcDUT: {len([f for f in self.files if f.file_type == 'DUT'])}개")
        print(f"  - 총: {len(self.files)}개")
        print()

    def _analyze_files(self):
        """각 파일 분석"""
        print("[2/4] 파일별 분석 중...")

        for i, file_stat in enumerate(self.files):
            full_path = self.project_path / file_stat.file_path
            try:
                content = full_path.read_text(encoding='utf-8', errors='ignore')

                # 기본 정보 추출
                self._extract_file_info(file_stat, content)

                # QA 규칙 적용
                self._apply_qa_rules(file_stat, content)

            except Exception as e:
                print(f"    경고: {file_stat.file_path} 분석 실패 - {e}")

            # 진행률 표시
            if (i + 1) % 20 == 0 or i == len(self.files) - 1:
                print(f"  진행: {i+1}/{len(self.files)} ({(i+1)*100//len(self.files)}%)")

        print()

    def _extract_file_info(self, file_stat: FileStats, content: str):
        """파일 정보 추출"""
        # POU 타입 및 이름 추출
        if file_stat.file_type == 'POU':
            if match := re.search(r'<POU\s+Name="([^"]+)"[^>]*>', content):
                file_stat.name = match.group(1)

            declaration = self._extract_section(content, 'Declaration')
            if 'PROGRAM' in declaration:
                file_stat.pou_type = 'PROGRAM'
            elif 'FUNCTION_BLOCK' in declaration:
                file_stat.pou_type = 'FUNCTION_BLOCK'
            elif 'FUNCTION' in declaration:
                file_stat.pou_type = 'FUNCTION'

        elif file_stat.file_type == 'GVL':
            if match := re.search(r'<GVL\s+Name="([^"]+)"', content):
                file_stat.name = match.group(1)

        elif file_stat.file_type == 'DUT':
            if match := re.search(r'<DUT\s+Name="([^"]+)"', content):
                file_stat.name = match.group(1)

        # 코드 라인 수
        st_code = self._extract_section(content, 'ST')
        declaration = self._extract_section(content, 'Declaration')
        all_code = declaration + '\n' + st_code

        lines = all_code.split('\n')
        file_stat.lines_of_code = len([l for l in lines if l.strip() and not l.strip().startswith('//')])
        file_stat.lines_of_comment = len([l for l in lines if l.strip().startswith('//')])

        # 변수 수
        file_stat.variable_count = len(re.findall(r'^\s*\w+\s*:\s*\w+', all_code, re.MULTILINE))

        # 순환 복잡도 추정 (분기문 수)
        file_stat.complexity = len(re.findall(r'\b(IF|ELSIF|CASE|FOR|WHILE|REPEAT)\b', st_code, re.IGNORECASE))

    def _apply_qa_rules(self, file_stat: FileStats, content: str):
        """QA 규칙 적용"""
        declaration = self._extract_section(content, 'Declaration')
        st_code = self._extract_section(content, 'ST')

        # 선언부 분석
        self._check_declaration_rules(file_stat, declaration)

        # 구현부 분석
        self._check_implementation_rules(file_stat, st_code)

        # 전체 코드 분석
        self._check_general_rules(file_stat, declaration + '\n' + st_code)

    def _check_declaration_rules(self, file_stat: FileStats, declaration: str):
        """선언부 QA 규칙"""
        lines = declaration.split('\n')

        for line_num, line in enumerate(lines, 1):
            # QA001: 초기화되지 않은 변수 (Critical 타입만)
            if self._is_uninitialized_critical_var(line):
                self._add_issue(file_stat, QAIssue(
                    rule_id="QA001",
                    severity="Critical",
                    category="Safety",
                    file_path=file_stat.file_path,
                    line=line_num,
                    message="초기화되지 않은 중요 변수 (REAL/LREAL/포인터)",
                    code_snippet=line.strip(),
                    suggestion="선언 시 초기값을 명시하세요: var : TYPE := 초기값;"
                ))

            # QA003: 배열 선언 검사
            if self._is_large_array(line):
                self._add_issue(file_stat, QAIssue(
                    rule_id="QA003",
                    severity="Warning",
                    category="Performance",
                    file_path=file_stat.file_path,
                    line=line_num,
                    message="대용량 배열 선언 감지",
                    code_snippet=line.strip(),
                    suggestion="메모리 사용량을 검토하세요"
                ))

            # QA004: 포인터 변수
            if re.search(r':\s*POINTER\s+TO\b', line, re.IGNORECASE):
                self._add_issue(file_stat, QAIssue(
                    rule_id="QA004",
                    severity="Warning",
                    category="Safety",
                    file_path=file_stat.file_path,
                    line=line_num,
                    message="포인터 변수 사용 - NULL 체크 필수",
                    code_snippet=line.strip(),
                    suggestion="사용 전 반드시 NULL 체크를 수행하세요"
                ))

            # QA016: 명명 규칙 검사
            naming_issue = self._check_naming(line, file_stat.file_type)
            if naming_issue:
                self._add_issue(file_stat, QAIssue(
                    rule_id="QA016",
                    severity="Info",
                    category="Style",
                    file_path=file_stat.file_path,
                    line=line_num,
                    message=f"명명 규칙: {naming_issue}",
                    code_snippet=line.strip(),
                    suggestion="헝가리안 표기법 또는 프로젝트 명명 규칙을 따르세요"
                ))

    def _check_implementation_rules(self, file_stat: FileStats, st_code: str):
        """구현부 QA 규칙"""
        lines = st_code.split('\n')

        # 중첩 깊이 추적
        nesting_depth = 0
        max_nesting = 0

        for line_num, line in enumerate(lines, 1):
            # 중첩 깊이 계산
            nesting_depth += len(re.findall(r'\b(IF|FOR|WHILE|CASE|REPEAT)\b', line, re.IGNORECASE))
            nesting_depth -= len(re.findall(r'\b(END_IF|END_FOR|END_WHILE|END_CASE|UNTIL)\b', line, re.IGNORECASE))
            max_nesting = max(max_nesting, nesting_depth)

            # QA002: 타입 축소 변환
            type_issue = self._check_type_narrowing(line)
            if type_issue:
                self._add_issue(file_stat, QAIssue(
                    rule_id="QA002",
                    severity="Critical",
                    category="Safety",
                    file_path=file_stat.file_path,
                    line=line_num,
                    message=f"위험한 타입 변환: {type_issue}",
                    code_snippet=line.strip(),
                    suggestion="LIMIT 함수로 범위 검증 후 변환하세요"
                ))

            # QA005: REAL 직접 비교
            if self._check_real_comparison(line):
                self._add_issue(file_stat, QAIssue(
                    rule_id="QA005",
                    severity="Critical",
                    category="Safety",
                    file_path=file_stat.file_path,
                    line=line_num,
                    message="실수형(REAL/LREAL) 직접 등호 비교",
                    code_snippet=line.strip(),
                    suggestion="ABS(a - b) < epsilon 형태로 비교하세요"
                ))

            # QA006: 0으로 나누기 가능성
            if self._check_division_by_zero(line):
                self._add_issue(file_stat, QAIssue(
                    rule_id="QA006",
                    severity="Critical",
                    category="Safety",
                    file_path=file_stat.file_path,
                    line=line_num,
                    message="0으로 나누기 가능성",
                    code_snippet=line.strip(),
                    suggestion="나누기 전 분모가 0이 아닌지 확인하세요"
                ))

            # QA007: 매직 넘버
            magic = self._check_magic_number(line)
            if magic:
                self._add_issue(file_stat, QAIssue(
                    rule_id="QA007",
                    severity="Warning",
                    category="Maintainability",
                    file_path=file_stat.file_path,
                    line=line_num,
                    message=f"매직 넘버 사용: {magic}",
                    code_snippet=line.strip(),
                    suggestion="상수(CONSTANT)로 정의하세요"
                ))

            # QA010: 하드코딩된 시간값
            if self._check_hardcoded_time(line):
                self._add_issue(file_stat, QAIssue(
                    rule_id="QA010",
                    severity="Warning",
                    category="Maintainability",
                    file_path=file_stat.file_path,
                    line=line_num,
                    message="하드코딩된 시간값",
                    code_snippet=line.strip(),
                    suggestion="파라미터 또는 상수로 정의하세요"
                ))

            # QA011: 빈 예외 처리
            if re.search(r'\bELSE\s*;', line, re.IGNORECASE):
                self._add_issue(file_stat, QAIssue(
                    rule_id="QA011",
                    severity="Warning",
                    category="Safety",
                    file_path=file_stat.file_path,
                    line=line_num,
                    message="빈 ELSE 블록",
                    code_snippet=line.strip(),
                    suggestion="예외 처리 로직을 추가하거나 주석으로 의도를 명시하세요"
                ))

            # QA012: TODO/FIXME 주석
            if re.search(r'(//|/\*)\s*(TODO|FIXME|XXX|HACK)', line, re.IGNORECASE):
                self._add_issue(file_stat, QAIssue(
                    rule_id="QA012",
                    severity="Info",
                    category="Maintainability",
                    file_path=file_stat.file_path,
                    line=line_num,
                    message="미완료 작업 표시 발견",
                    code_snippet=line.strip(),
                    suggestion="릴리스 전 해결이 필요합니다"
                ))

            # QA013: 주석 처리된 코드
            if self._is_commented_code(line):
                self._add_issue(file_stat, QAIssue(
                    rule_id="QA013",
                    severity="Info",
                    category="Maintainability",
                    file_path=file_stat.file_path,
                    line=line_num,
                    message="주석 처리된 코드",
                    code_snippet=line.strip()[:80],
                    suggestion="불필요한 코드는 삭제하세요 (버전 관리 시스템 활용)"
                ))

        # QA008: 과도한 중첩
        if max_nesting > 4:
            self._add_issue(file_stat, QAIssue(
                rule_id="QA008",
                severity="Warning",
                category="Maintainability",
                file_path=file_stat.file_path,
                line=0,
                message=f"과도한 중첩 깊이: {max_nesting}단계",
                suggestion="함수 분리 또는 early return 패턴을 사용하세요"
            ))

    def _check_general_rules(self, file_stat: FileStats, all_code: str):
        """전체 코드 규칙"""
        # QA009: 긴 함수/프로그램
        if file_stat.lines_of_code > 500:
            self._add_issue(file_stat, QAIssue(
                rule_id="QA009",
                severity="Warning",
                category="Maintainability",
                file_path=file_stat.file_path,
                line=0,
                message=f"코드가 너무 깁니다: {file_stat.lines_of_code}줄",
                suggestion="500줄 이하로 분리를 권장합니다"
            ))

        # QA014: 높은 순환 복잡도
        if file_stat.complexity > 15:
            self._add_issue(file_stat, QAIssue(
                rule_id="QA014",
                severity="Warning",
                category="Maintainability",
                file_path=file_stat.file_path,
                line=0,
                message=f"높은 순환 복잡도: {file_stat.complexity}",
                suggestion="함수를 더 작은 단위로 분리하세요"
            ))

        # QA015: 주석 부족
        if file_stat.lines_of_code > 50 and file_stat.lines_of_comment < file_stat.lines_of_code * 0.1:
            self._add_issue(file_stat, QAIssue(
                rule_id="QA015",
                severity="Info",
                category="Maintainability",
                file_path=file_stat.file_path,
                line=0,
                message="주석이 부족합니다 (코드 대비 10% 미만)",
                suggestion="복잡한 로직에 주석을 추가하세요"
            ))

    def _global_analysis(self):
        """전역 분석"""
        print("[3/4] 전역 분석 중...")

        # 미사용 전역 변수 검사 등
        # (현재는 기본 분석만 수행)

        total_issues = len(self.qa_issues)
        critical = len([i for i in self.qa_issues if i.severity == 'Critical'])
        warning = len([i for i in self.qa_issues if i.severity == 'Warning'])
        info = len([i for i in self.qa_issues if i.severity == 'Info'])

        print(f"  - 총 이슈: {total_issues}개")
        print(f"  - Critical: {critical}개")
        print(f"  - Warning: {warning}개")
        print(f"  - Info: {info}개")
        print()

    def _generate_report(self) -> Dict:
        """리포트 생성"""
        print("[4/4] 리포트 생성 중...")

        # 카테고리별 집계
        by_category = defaultdict(list)
        for issue in self.qa_issues:
            by_category[issue.category].append(issue)

        # 파일별 집계
        by_file = defaultdict(list)
        for issue in self.qa_issues:
            by_file[issue.file_path].append(issue)

        # 규칙별 집계
        by_rule = defaultdict(list)
        for issue in self.qa_issues:
            by_rule[issue.rule_id].append(issue)

        report = {
            "generated_at": datetime.now().isoformat(),
            "project_path": str(self.project_path),
            "summary": {
                "total_files": len(self.files),
                "total_pou": len([f for f in self.files if f.file_type == 'POU']),
                "total_gvl": len([f for f in self.files if f.file_type == 'GVL']),
                "total_dut": len([f for f in self.files if f.file_type == 'DUT']),
                "total_lines": sum(f.lines_of_code for f in self.files),
                "total_issues": len(self.qa_issues),
                "critical_count": len([i for i in self.qa_issues if i.severity == 'Critical']),
                "warning_count": len([i for i in self.qa_issues if i.severity == 'Warning']),
                "info_count": len([i for i in self.qa_issues if i.severity == 'Info']),
                "by_category": {k: len(v) for k, v in by_category.items()},
            },
            "files": [
                {
                    "path": f.file_path,
                    "type": f.file_type,
                    "pou_type": f.pou_type,
                    "name": f.name,
                    "lines": f.lines_of_code,
                    "complexity": f.complexity,
                    "issue_count": len([i for i in self.qa_issues if i.file_path == f.file_path])
                }
                for f in self.files
            ],
            "issues_by_rule": {
                rule_id: {
                    "count": len(issues),
                    "severity": issues[0].severity if issues else "",
                    "category": issues[0].category if issues else "",
                }
                for rule_id, issues in sorted(by_rule.items())
            },
            "issues": [
                {
                    "rule_id": i.rule_id,
                    "severity": i.severity,
                    "category": i.category,
                    "file": i.file_path,
                    "line": i.line,
                    "message": i.message,
                    "code": i.code_snippet,
                    "suggestion": i.suggestion
                }
                for i in self.qa_issues
            ]
        }

        return report

    # === Helper Methods ===

    def _extract_section(self, content: str, section: str) -> str:
        """XML에서 섹션 추출"""
        pattern = rf'<{section}><!\[CDATA\[(.*?)\]\]></{section}>'
        matches = re.findall(pattern, content, re.DOTALL)
        return '\n'.join(matches)

    def _add_issue(self, file_stat: FileStats, issue: QAIssue):
        """이슈 추가"""
        self.qa_issues.append(issue)
        file_stat.issues.append(issue)

    def _is_uninitialized_critical_var(self, line: str) -> bool:
        """중요 타입의 초기화되지 않은 변수"""
        pattern = r'^\s*\w+\s*:\s*(REAL|LREAL|POINTER)\b(?!.*:=)'
        return bool(re.search(pattern, line, re.IGNORECASE))

    def _is_large_array(self, line: str) -> bool:
        """대용량 배열"""
        match = re.search(r'ARRAY\s*\[\s*(\d+)\s*\.\.\s*(\d+)\s*\]', line, re.IGNORECASE)
        if match:
            size = int(match.group(2)) - int(match.group(1)) + 1
            return size > 1000
        return False

    def _check_naming(self, line: str, file_type: str) -> Optional[str]:
        """명명 규칙 검사"""
        match = re.search(r'^\s*(\w+)\s*:\s*(\w+)', line)
        if match:
            var_name = match.group(1)
            var_type = match.group(2).upper()

            # 상수는 예외
            if var_name.isupper():
                return None

            # 헝가리안 표기법 검사
            prefixes = {
                'BOOL': 'b', 'INT': 'n', 'DINT': 'n', 'REAL': 'f', 'LREAL': 'f',
                'STRING': 's', 'WORD': 'w', 'DWORD': 'dw', 'BYTE': 'by',
                'POINTER': 'p', 'ARRAY': 'a', 'TIME': 't', 'TON': 'ton', 'TOF': 'tof'
            }

            expected_prefix = prefixes.get(var_type)
            if expected_prefix and not var_name.lower().startswith(expected_prefix):
                # FB, FC 같은 접두사는 허용
                if not re.match(r'^(fb|fc|st|e|i|o|io)[A-Z_]', var_name, re.IGNORECASE):
                    return f"'{var_name}'에 타입 접두사 '{expected_prefix}' 권장"
        return None

    def _check_type_narrowing(self, line: str) -> Optional[str]:
        """타입 축소 검사"""
        patterns = [
            (r'DINT_TO_INT\s*\(', 'DINT→INT'),
            (r'LINT_TO_DINT\s*\(', 'LINT→DINT'),
            (r'LINT_TO_INT\s*\(', 'LINT→INT'),
            (r'LREAL_TO_REAL\s*\(', 'LREAL→REAL'),
            (r'REAL_TO_INT\s*\(', 'REAL→INT'),
            (r'REAL_TO_DINT\s*\(', 'REAL→DINT'),
            (r'LREAL_TO_INT\s*\(', 'LREAL→INT'),
            (r'DWORD_TO_WORD\s*\(', 'DWORD→WORD'),
            (r'DWORD_TO_BYTE\s*\(', 'DWORD→BYTE'),
        ]
        for pattern, desc in patterns:
            if re.search(pattern, line, re.IGNORECASE):
                return desc
        return None

    def _check_real_comparison(self, line: str) -> bool:
        """REAL 직접 비교"""
        # := 할당이 아닌 = 비교에서 REAL 변수 사용
        if ':=' in line:
            return False
        # f, r 접두사 변수와 = 비교
        pattern = r'\b[fr]\w+\s*=\s*[fr]\w+\b|\b[fr]\w+\s*=\s*\d+\.\d+'
        return bool(re.search(pattern, line, re.IGNORECASE))

    def _check_division_by_zero(self, line: str) -> bool:
        """0으로 나누기 가능성"""
        # 변수로 나누는 경우
        if re.search(r'/\s*[a-zA-Z_]\w*\s*[;)]', line):
            # 직전에 0 체크가 없으면 위험
            return True
        return False

    def _check_magic_number(self, line: str) -> Optional[str]:
        """매직 넘버"""
        # 주석 제외
        code = re.sub(r'//.*$', '', line)
        code = re.sub(r'\(\*.*?\*\)', '', code)

        # 배열 인덱스, 시간값 제외
        code = re.sub(r'\[\s*\d+\s*\]', '', code)
        code = re.sub(r'T#\d+\w+', '', code)
        code = re.sub(r'#\d+', '', code)  # 16#FF 등

        # 2자리 이상 숫자
        match = re.search(r'(?<![:\w#])\b(\d{3,})\b', code)
        if match:
            return match.group(1)
        return None

    def _check_hardcoded_time(self, line: str) -> bool:
        """하드코딩된 시간"""
        return bool(re.search(r'T#\d+(?:ms|s|m|h)', line, re.IGNORECASE))

    def _is_commented_code(self, line: str) -> bool:
        """주석 처리된 코드"""
        if line.strip().startswith('//'):
            code_part = line.strip()[2:].strip()
            # ST 키워드나 할당문이 있으면 코드로 판단
            if re.search(r':=|;\s*$|\bIF\b|\bFOR\b|\bWHILE\b|\bEND_', code_part, re.IGNORECASE):
                return True
        return False


def generate_markdown_report(report: Dict) -> str:
    """Markdown 리포트 생성"""
    md = []
    md.append("# TwinCAT 프로젝트 QA 분석 리포트")
    md.append("")
    md.append(f"**분석 일시**: {report['generated_at']}")
    md.append(f"**프로젝트 경로**: `{report['project_path']}`")
    md.append("")

    # 요약
    s = report['summary']
    md.append("## 📊 프로젝트 요약")
    md.append("")
    md.append(f"| 항목 | 값 |")
    md.append(f"|------|-----|")
    md.append(f"| 총 파일 수 | {s['total_files']}개 |")
    md.append(f"| POU (프로그램/FB/함수) | {s['total_pou']}개 |")
    md.append(f"| GVL (전역 변수) | {s['total_gvl']}개 |")
    md.append(f"| DUT (데이터 타입) | {s['total_dut']}개 |")
    md.append(f"| 총 코드 라인 | {s['total_lines']:,}줄 |")
    md.append(f"| **총 QA 이슈** | **{s['total_issues']}개** |")
    md.append(f"| 🔴 Critical | {s['critical_count']}개 |")
    md.append(f"| 🟡 Warning | {s['warning_count']}개 |")
    md.append(f"| 🔵 Info | {s['info_count']}개 |")
    md.append("")

    # 카테고리별 이슈
    md.append("## 📈 카테고리별 이슈")
    md.append("")
    md.append("| 카테고리 | 건수 | 설명 |")
    md.append("|----------|------|------|")
    categories = {
        'Safety': '안전 - 잠재적 버그, 런타임 오류',
        'Performance': '성능 - 메모리, 실행 속도',
        'Maintainability': '유지보수 - 가독성, 복잡도',
        'Style': '스타일 - 명명 규칙, 코딩 표준'
    }
    for cat, desc in categories.items():
        count = s.get('by_category', {}).get(cat, 0)
        md.append(f"| {cat} | {count}개 | {desc} |")
    md.append("")

    # Critical 이슈
    critical_issues = [i for i in report['issues'] if i['severity'] == 'Critical']
    if critical_issues:
        md.append("## 🔴 Critical Issues (즉시 검토 필요)")
        md.append("")
        md.append("| 파일 | 라인 | 규칙 | 메시지 |")
        md.append("|------|------|------|--------|")
        for issue in critical_issues[:50]:
            file_name = Path(issue['file']).name
            md.append(f"| {file_name} | {issue['line']} | {issue['rule_id']} | {issue['message'][:50]} |")
        if len(critical_issues) > 50:
            md.append(f"| ... | | | *외 {len(critical_issues) - 50}개* |")
        md.append("")

    # 규칙별 통계
    md.append("## 📋 규칙별 이슈 통계")
    md.append("")
    md.append("| 규칙 ID | 심각도 | 카테고리 | 건수 |")
    md.append("|---------|--------|----------|------|")
    for rule_id, data in sorted(report['issues_by_rule'].items()):
        severity_icon = {'Critical': '🔴', 'Warning': '🟡', 'Info': '🔵'}.get(data['severity'], '')
        md.append(f"| {rule_id} | {severity_icon} {data['severity']} | {data['category']} | {data['count']}개 |")
    md.append("")

    # 복잡도 높은 파일
    complex_files = sorted([f for f in report['files'] if f['complexity'] > 10],
                          key=lambda x: x['complexity'], reverse=True)[:10]
    if complex_files:
        md.append("## ⚠️ 복잡도 높은 파일 (Top 10)")
        md.append("")
        md.append("| 파일 | 타입 | 라인수 | 복잡도 | 이슈수 |")
        md.append("|------|------|--------|--------|--------|")
        for f in complex_files:
            md.append(f"| {f['name']} | {f['pou_type']} | {f['lines']} | {f['complexity']} | {f['issue_count']} |")
        md.append("")

    return '\n'.join(md)


if __name__ == "__main__":
    # 경로 설정 (명령줄 인자 또는 기본값)
    if len(sys.argv) > 1:
        PROJECT_PATH = sys.argv[1]
    else:
        PROJECT_PATH = r"D:\00.Comapre\pollux_hcds_ald_mirror_ffff\Src_Diff\PLC\PM1\PM1"

    # 분석 실행
    analyzer = TwinCATSingleProjectAnalyzer(PROJECT_PATH)
    report = analyzer.analyze()

    # 출력 디렉토리
    output_dir = Path(r"D:\01. Vscode\Twincat\features\twincat-code-qa-tool\output")
    output_dir.mkdir(exist_ok=True)

    # JSON 저장
    json_path = output_dir / "single_project_qa_report.json"
    with open(json_path, 'w', encoding='utf-8') as f:
        json.dump(report, f, ensure_ascii=False, indent=2)
    print(f"\nJSON 리포트: {json_path}")

    # Markdown 저장
    md_content = generate_markdown_report(report)
    md_path = output_dir / "single_project_qa_report.md"
    with open(md_path, 'w', encoding='utf-8') as f:
        f.write(md_content)
    print(f"Markdown 리포트: {md_path}")

    # 요약 출력
    print("\n" + "="*60)
    print("분석 완료!")
    print("="*60)
    s = report['summary']
    print(f"총 파일: {s['total_files']}개 ({s['total_lines']:,}줄)")
    print(f"총 QA 이슈: {s['total_issues']}개")
    print(f"  🔴 Critical: {s['critical_count']}개")
    print(f"  🟡 Warning: {s['warning_count']}개")
    print(f"  🔵 Info: {s['info_count']}개")
