#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
TwinCAT 프로젝트 QA 분석 스크립트
실제 프로젝트 비교 및 QA 이슈 검출
"""

import os
import re
import hashlib
import xml.etree.ElementTree as ET
from pathlib import Path
from dataclasses import dataclass, field
from typing import List, Dict, Tuple, Optional
from datetime import datetime
import json

@dataclass
class QAIssue:
    """QA 이슈"""
    rule_id: str
    severity: str  # Critical, Warning, Info
    file_path: str
    line: int
    message: str
    code_snippet: str = ""
    suggestion: str = ""

@dataclass
class FileChange:
    """파일 변경 정보"""
    file_path: str
    change_type: str  # Added, Deleted, Modified
    old_size: int = 0
    new_size: int = 0
    changes: List[str] = field(default_factory=list)

@dataclass
class VariableChange:
    """변수 변경 정보"""
    file_path: str
    var_name: str
    change_type: str  # Added, Deleted, TypeChanged, InitialValueChanged
    old_type: str = ""
    new_type: str = ""
    old_value: str = ""
    new_value: str = ""

class TwinCATQAAnalyzer:
    """TwinCAT 프로젝트 QA 분석기"""

    def __init__(self, old_path: str, new_path: str):
        self.old_path = Path(old_path)
        self.new_path = Path(new_path)
        self.file_changes: List[FileChange] = []
        self.variable_changes: List[VariableChange] = []
        self.qa_issues: List[QAIssue] = []

    def analyze(self) -> Dict:
        """전체 분석 실행"""
        print(f"[분석 시작] {datetime.now()}")
        print(f"  이전 버전: {self.old_path}")
        print(f"  새 버전: {self.new_path}")
        print()

        # 1. 파일 변경 감지
        self._detect_file_changes()

        # 2. 변수 변경 분석
        self._analyze_variable_changes()

        # 3. QA 규칙 적용
        self._apply_qa_rules()

        # 4. 결과 반환
        return self._generate_report()

    def _detect_file_changes(self):
        """파일 변경 감지"""
        print("[1/4] 파일 변경 감지 중...")

        extensions = {'.TcPOU', '.TcGVL', '.TcDUT', '.plcproj'}
        old_files = self._get_files(self.old_path, extensions)
        new_files = self._get_files(self.new_path, extensions)

        old_rel = {self._relative_path(f, self.old_path): f for f in old_files}
        new_rel = {self._relative_path(f, self.new_path): f for f in new_files}

        # 추가된 파일
        for rel_path in set(new_rel.keys()) - set(old_rel.keys()):
            self.file_changes.append(FileChange(
                file_path=rel_path,
                change_type="Added",
                new_size=new_rel[rel_path].stat().st_size
            ))

        # 삭제된 파일
        for rel_path in set(old_rel.keys()) - set(new_rel.keys()):
            self.file_changes.append(FileChange(
                file_path=rel_path,
                change_type="Deleted",
                old_size=old_rel[rel_path].stat().st_size
            ))

        # 수정된 파일
        for rel_path in set(old_rel.keys()) & set(new_rel.keys()):
            old_hash = self._file_hash(old_rel[rel_path])
            new_hash = self._file_hash(new_rel[rel_path])
            if old_hash != new_hash:
                self.file_changes.append(FileChange(
                    file_path=rel_path,
                    change_type="Modified",
                    old_size=old_rel[rel_path].stat().st_size,
                    new_size=new_rel[rel_path].stat().st_size
                ))

        print(f"  - 추가: {len([f for f in self.file_changes if f.change_type == 'Added'])}개")
        print(f"  - 삭제: {len([f for f in self.file_changes if f.change_type == 'Deleted'])}개")
        print(f"  - 수정: {len([f for f in self.file_changes if f.change_type == 'Modified'])}개")
        print()

    def _analyze_variable_changes(self):
        """변수 변경 분석"""
        print("[2/4] 변수 변경 분석 중...")

        for fc in self.file_changes:
            if fc.change_type == "Modified":
                old_file = self.old_path / fc.file_path
                new_file = self.new_path / fc.file_path

                old_vars = self._extract_variables(old_file)
                new_vars = self._extract_variables(new_file)

                # 추가된 변수
                for var_name in set(new_vars.keys()) - set(old_vars.keys()):
                    self.variable_changes.append(VariableChange(
                        file_path=fc.file_path,
                        var_name=var_name,
                        change_type="Added",
                        new_type=new_vars[var_name].get('type', ''),
                        new_value=new_vars[var_name].get('value', '')
                    ))

                # 삭제된 변수
                for var_name in set(old_vars.keys()) - set(new_vars.keys()):
                    self.variable_changes.append(VariableChange(
                        file_path=fc.file_path,
                        var_name=var_name,
                        change_type="Deleted",
                        old_type=old_vars[var_name].get('type', ''),
                        old_value=old_vars[var_name].get('value', '')
                    ))

                # 변경된 변수
                for var_name in set(old_vars.keys()) & set(new_vars.keys()):
                    old_var = old_vars[var_name]
                    new_var = new_vars[var_name]

                    if old_var.get('type') != new_var.get('type'):
                        self.variable_changes.append(VariableChange(
                            file_path=fc.file_path,
                            var_name=var_name,
                            change_type="TypeChanged",
                            old_type=old_var.get('type', ''),
                            new_type=new_var.get('type', '')
                        ))
                    elif old_var.get('value') != new_var.get('value'):
                        self.variable_changes.append(VariableChange(
                            file_path=fc.file_path,
                            var_name=var_name,
                            change_type="InitialValueChanged",
                            old_value=old_var.get('value', ''),
                            new_value=new_var.get('value', '')
                        ))

        print(f"  - 변수 추가: {len([v for v in self.variable_changes if v.change_type == 'Added'])}개")
        print(f"  - 변수 삭제: {len([v for v in self.variable_changes if v.change_type == 'Deleted'])}개")
        print(f"  - 타입 변경: {len([v for v in self.variable_changes if v.change_type == 'TypeChanged'])}개")
        print(f"  - 초기값 변경: {len([v for v in self.variable_changes if v.change_type == 'InitialValueChanged'])}개")
        print()

    def _apply_qa_rules(self):
        """QA 규칙 적용"""
        print("[3/4] QA 규칙 적용 중...")

        # 변경된 파일에 대해 QA 규칙 적용
        for fc in self.file_changes:
            if fc.change_type in ("Added", "Modified"):
                new_file = self.new_path / fc.file_path
                self._check_qa_rules(new_file, fc.file_path)

        # 변수 변경에 대한 QA 검사
        self._check_variable_qa_rules()

        critical = len([i for i in self.qa_issues if i.severity == 'Critical'])
        warning = len([i for i in self.qa_issues if i.severity == 'Warning'])
        info = len([i for i in self.qa_issues if i.severity == 'Info'])

        print(f"  - Critical: {critical}개")
        print(f"  - Warning: {warning}개")
        print(f"  - Info: {info}개")
        print()

    def _check_qa_rules(self, file_path: Path, rel_path: str):
        """파일에 QA 규칙 적용"""
        try:
            content = file_path.read_text(encoding='utf-8', errors='ignore')

            # XML에서 ST 코드 추출
            st_code = self._extract_st_code(content)

            if not st_code:
                return

            lines = st_code.split('\n')

            for line_num, line in enumerate(lines, 1):
                # QA001: 초기화되지 않은 변수
                if self._check_uninitialized_var(line):
                    self.qa_issues.append(QAIssue(
                        rule_id="QA001",
                        severity="Critical",
                        file_path=rel_path,
                        line=line_num,
                        message="초기화되지 않은 변수가 사용될 수 있습니다",
                        code_snippet=line.strip(),
                        suggestion="변수 선언 시 초기값을 명시하세요"
                    ))

                # QA002: 위험한 타입 변환
                type_issue = self._check_type_narrowing(line)
                if type_issue:
                    self.qa_issues.append(QAIssue(
                        rule_id="QA002",
                        severity="Critical",
                        file_path=rel_path,
                        line=line_num,
                        message=f"위험한 타입 변환: {type_issue}",
                        code_snippet=line.strip(),
                        suggestion="LIMIT 함수를 사용하여 안전하게 변환하세요"
                    ))

                # QA005: REAL 직접 비교
                if self._check_real_comparison(line):
                    self.qa_issues.append(QAIssue(
                        rule_id="QA005",
                        severity="Critical",
                        file_path=rel_path,
                        line=line_num,
                        message="실수형(REAL/LREAL) 직접 비교 감지",
                        code_snippet=line.strip(),
                        suggestion="허용 오차(epsilon)를 사용한 비교로 변경하세요"
                    ))

                # QA007: 매직 넘버 사용
                magic = self._check_magic_number(line)
                if magic:
                    self.qa_issues.append(QAIssue(
                        rule_id="QA007",
                        severity="Warning",
                        file_path=rel_path,
                        line=line_num,
                        message=f"매직 넘버 사용: {magic}",
                        code_snippet=line.strip(),
                        suggestion="상수(CONSTANT)로 정의하여 사용하세요"
                    ))

                # QA010: 하드코딩된 타이머/카운터 값
                if self._check_hardcoded_time(line):
                    self.qa_issues.append(QAIssue(
                        rule_id="QA010",
                        severity="Warning",
                        file_path=rel_path,
                        line=line_num,
                        message="하드코딩된 시간/카운터 값 감지",
                        code_snippet=line.strip(),
                        suggestion="파라미터 또는 상수로 정의하세요"
                    ))

                # QA016: 명명 규칙 위반
                naming = self._check_naming_convention(line)
                if naming:
                    self.qa_issues.append(QAIssue(
                        rule_id="QA016",
                        severity="Info",
                        file_path=rel_path,
                        line=line_num,
                        message=f"명명 규칙 위반: {naming}",
                        code_snippet=line.strip(),
                        suggestion="TwinCAT 명명 규칙을 따르세요"
                    ))

        except Exception as e:
            print(f"    경고: {rel_path} 분석 실패 - {e}")

    def _check_variable_qa_rules(self):
        """변수 변경에 대한 QA 규칙"""
        for vc in self.variable_changes:
            # 위험한 타입 축소
            if vc.change_type == "TypeChanged":
                if self._is_type_narrowing(vc.old_type, vc.new_type):
                    self.qa_issues.append(QAIssue(
                        rule_id="QA002",
                        severity="Critical",
                        file_path=vc.file_path,
                        line=0,
                        message=f"변수 '{vc.var_name}'의 타입이 {vc.old_type}에서 {vc.new_type}로 축소됨",
                        suggestion="데이터 손실 가능성이 있습니다. 검토가 필요합니다."
                    ))

    def _check_uninitialized_var(self, line: str) -> bool:
        """초기화되지 않은 변수 검사"""
        # VAR 선언에서 := 가 없는 경우
        pattern = r'^\s*(\w+)\s*:\s*(INT|DINT|REAL|LREAL|BOOL|STRING|WORD|DWORD)\s*;'
        return bool(re.search(pattern, line, re.IGNORECASE))

    def _check_type_narrowing(self, line: str) -> Optional[str]:
        """타입 축소 변환 검사"""
        patterns = [
            (r'DINT_TO_INT\s*\(', 'DINT→INT'),
            (r'LINT_TO_DINT\s*\(', 'LINT→DINT'),
            (r'LREAL_TO_REAL\s*\(', 'LREAL→REAL'),
            (r'REAL_TO_INT\s*\(', 'REAL→INT'),
            (r'DWORD_TO_WORD\s*\(', 'DWORD→WORD'),
        ]
        for pattern, desc in patterns:
            if re.search(pattern, line, re.IGNORECASE):
                return desc
        return None

    def _check_real_comparison(self, line: str) -> bool:
        """REAL 직접 비교 검사"""
        # 변수 타입 추론이 어려우므로 패턴 기반 검사
        pattern = r'\b(rReal|fValue|fTemp|fSpeed|rSpeed|fPos|rPos)\w*\s*[<>=]\s*\d+\.?\d*'
        return bool(re.search(pattern, line, re.IGNORECASE))

    def _check_magic_number(self, line: str) -> Optional[str]:
        """매직 넘버 검사"""
        # 주석 제외
        code_part = re.sub(r'//.*$', '', line)
        code_part = re.sub(r'\(\*.*?\*\)', '', code_part)

        # 0, 1, -1, TRUE, FALSE 등 일반적인 값 제외
        pattern = r'(?<![:\w])\b(\d{2,}(?:\.\d+)?)\b(?!\s*\])'
        match = re.search(pattern, code_part)
        if match:
            num = match.group(1)
            # T#, TIME# 등은 제외
            if not re.search(r'[T#]' + num, code_part):
                return num
        return None

    def _check_hardcoded_time(self, line: str) -> bool:
        """하드코딩된 시간값 검사"""
        pattern = r'T#\d+(?:ms|s|m|h)'
        return bool(re.search(pattern, line, re.IGNORECASE))

    def _check_naming_convention(self, line: str) -> Optional[str]:
        """명명 규칙 검사"""
        # 변수명이 소문자로만 시작하는 경우 (헝가리안 표기법 미사용)
        match = re.search(r'^\s*(\w+)\s*:\s*\w+', line)
        if match:
            var_name = match.group(1)
            # FB_, FC_, ST_, E_ 등 접두사가 없는 경우
            if not re.match(r'^(fb|fc|st|e|n|r|b|s|a|p|i|o|io)[A-Z_]', var_name, re.IGNORECASE):
                if re.match(r'^[a-z]{2,}[A-Z]', var_name):
                    return None  # camelCase는 허용
                if len(var_name) > 2 and var_name.islower():
                    return f"'{var_name}' - 접두사 없음"
        return None

    def _is_type_narrowing(self, old_type: str, new_type: str) -> bool:
        """타입 축소 여부 확인"""
        type_sizes = {
            'LINT': 64, 'LREAL': 64, 'LWORD': 64,
            'DINT': 32, 'REAL': 32, 'DWORD': 32, 'UDINT': 32,
            'INT': 16, 'WORD': 16, 'UINT': 16,
            'SINT': 8, 'BYTE': 8, 'USINT': 8,
            'BOOL': 1
        }
        old_size = type_sizes.get(old_type.upper(), 0)
        new_size = type_sizes.get(new_type.upper(), 0)
        return old_size > new_size > 0

    def _extract_variables(self, file_path: Path) -> Dict[str, Dict]:
        """파일에서 변수 추출"""
        variables = {}
        try:
            content = file_path.read_text(encoding='utf-8', errors='ignore')
            st_code = self._extract_declaration(content)

            if not st_code:
                return variables

            # VAR 블록 파싱
            var_pattern = r'^\s*(\w+)\s*:\s*(\w+(?:\s*\[\d+\.\.\d+\])?)\s*(?::=\s*(.+?))?;'
            for match in re.finditer(var_pattern, st_code, re.MULTILINE):
                var_name = match.group(1)
                var_type = match.group(2)
                var_value = match.group(3) if match.group(3) else ''
                variables[var_name] = {
                    'type': var_type.strip(),
                    'value': var_value.strip()
                }
        except Exception:
            pass
        return variables

    def _extract_st_code(self, content: str) -> str:
        """XML에서 ST 코드 추출"""
        # <ST><![CDATA[...]]></ST>
        pattern = r'<ST><!\[CDATA\[(.*?)\]\]></ST>'
        matches = re.findall(pattern, content, re.DOTALL)
        return '\n'.join(matches)

    def _extract_declaration(self, content: str) -> str:
        """XML에서 선언부 추출"""
        pattern = r'<Declaration><!\[CDATA\[(.*?)\]\]></Declaration>'
        matches = re.findall(pattern, content, re.DOTALL)
        return '\n'.join(matches)

    def _get_files(self, base_path: Path, extensions: set) -> List[Path]:
        """지정된 확장자의 파일 목록"""
        files = []
        for ext in extensions:
            files.extend(base_path.rglob(f'*{ext}'))
        return files

    def _relative_path(self, file_path: Path, base_path: Path) -> str:
        """상대 경로 계산"""
        return str(file_path.relative_to(base_path))

    def _file_hash(self, file_path: Path) -> str:
        """파일 해시 계산"""
        return hashlib.md5(file_path.read_bytes()).hexdigest()

    def _generate_report(self) -> Dict:
        """리포트 생성"""
        print("[4/4] 리포트 생성 중...")

        report = {
            "generated_at": datetime.now().isoformat(),
            "source_folder": str(self.old_path),
            "target_folder": str(self.new_path),
            "summary": {
                "total_files_changed": len(self.file_changes),
                "files_added": len([f for f in self.file_changes if f.change_type == 'Added']),
                "files_deleted": len([f for f in self.file_changes if f.change_type == 'Deleted']),
                "files_modified": len([f for f in self.file_changes if f.change_type == 'Modified']),
                "total_variable_changes": len(self.variable_changes),
                "total_qa_issues": len(self.qa_issues),
                "critical_issues": len([i for i in self.qa_issues if i.severity == 'Critical']),
                "warning_issues": len([i for i in self.qa_issues if i.severity == 'Warning']),
                "info_issues": len([i for i in self.qa_issues if i.severity == 'Info']),
            },
            "file_changes": [
                {
                    "path": fc.file_path,
                    "type": fc.change_type,
                    "old_size": fc.old_size,
                    "new_size": fc.new_size
                }
                for fc in self.file_changes
            ],
            "variable_changes": [
                {
                    "file": vc.file_path,
                    "name": vc.var_name,
                    "type": vc.change_type,
                    "old_type": vc.old_type,
                    "new_type": vc.new_type,
                    "old_value": vc.old_value,
                    "new_value": vc.new_value
                }
                for vc in self.variable_changes
            ],
            "qa_issues": [
                {
                    "rule_id": issue.rule_id,
                    "severity": issue.severity,
                    "file": issue.file_path,
                    "line": issue.line,
                    "message": issue.message,
                    "code": issue.code_snippet,
                    "suggestion": issue.suggestion
                }
                for issue in self.qa_issues
            ]
        }

        return report


def generate_markdown_report(report: Dict) -> str:
    """Markdown 리포트 생성"""
    md = []
    md.append("# TwinCAT Code QA Report")
    md.append("")
    md.append(f"**분석 일시**: {report['generated_at']}")
    md.append(f"**이전 버전**: `{report['source_folder']}`")
    md.append(f"**새 버전**: `{report['target_folder']}`")
    md.append("")

    # 요약
    s = report['summary']
    md.append("## 📊 요약")
    md.append("")
    md.append(f"| 항목 | 값 |")
    md.append(f"|------|-----|")
    md.append(f"| 총 파일 변경 | {s['total_files_changed']}개 |")
    md.append(f"| 파일 추가 | {s['files_added']}개 |")
    md.append(f"| 파일 삭제 | {s['files_deleted']}개 |")
    md.append(f"| 파일 수정 | {s['files_modified']}개 |")
    md.append(f"| 변수 변경 | {s['total_variable_changes']}개 |")
    md.append(f"| **QA 이슈 총계** | **{s['total_qa_issues']}개** |")
    md.append(f"| 🔴 Critical | {s['critical_issues']}개 |")
    md.append(f"| 🟡 Warning | {s['warning_issues']}개 |")
    md.append(f"| 🔵 Info | {s['info_issues']}개 |")
    md.append("")

    # Critical 이슈
    critical_issues = [i for i in report['qa_issues'] if i['severity'] == 'Critical']
    if critical_issues:
        md.append("## 🔴 Critical Issues")
        md.append("")
        md.append("| 파일 | 라인 | 규칙 | 메시지 |")
        md.append("|------|------|------|--------|")
        for issue in critical_issues[:30]:  # 상위 30개
            file_name = Path(issue['file']).name
            md.append(f"| {file_name} | {issue['line']} | {issue['rule_id']} | {issue['message']} |")
        if len(critical_issues) > 30:
            md.append(f"| ... | ... | ... | *외 {len(critical_issues) - 30}개* |")
        md.append("")

    # Warning 이슈
    warning_issues = [i for i in report['qa_issues'] if i['severity'] == 'Warning']
    if warning_issues:
        md.append("## 🟡 Warning Issues")
        md.append("")
        md.append("| 파일 | 라인 | 규칙 | 메시지 |")
        md.append("|------|------|------|--------|")
        for issue in warning_issues[:30]:
            file_name = Path(issue['file']).name
            md.append(f"| {file_name} | {issue['line']} | {issue['rule_id']} | {issue['message']} |")
        if len(warning_issues) > 30:
            md.append(f"| ... | ... | ... | *외 {len(warning_issues) - 30}개* |")
        md.append("")

    # Info 이슈
    info_issues = [i for i in report['qa_issues'] if i['severity'] == 'Info']
    if info_issues:
        md.append("## 🔵 Info Issues")
        md.append("")
        md.append(f"총 {len(info_issues)}개의 Info 이슈가 발견되었습니다.")
        md.append("")

    # 파일 변경 목록
    md.append("## 📁 파일 변경 목록")
    md.append("")

    for fc in report['file_changes']:
        icon = {"Added": "➕", "Deleted": "➖", "Modified": "📝"}.get(fc['type'], "?")
        md.append(f"- {icon} `{fc['path']}` ({fc['type']})")
    md.append("")

    # 변수 변경 (타입 변경만)
    type_changes = [v for v in report['variable_changes'] if v['type'] == 'TypeChanged']
    if type_changes:
        md.append("## ⚠️ 변수 타입 변경")
        md.append("")
        md.append("| 파일 | 변수명 | 이전 타입 | 새 타입 |")
        md.append("|------|--------|-----------|---------|")
        for vc in type_changes[:20]:
            file_name = Path(vc['file']).name
            md.append(f"| {file_name} | {vc['name']} | {vc['old_type']} | {vc['new_type']} |")
        md.append("")

    return '\n'.join(md)


if __name__ == "__main__":
    import sys

    # 경로 설정
    OLD_PATH = r"D:\00.Comapre\pollux_hcds_ald_mirror\Src_Diff\PLC\PM1\PM1"
    NEW_PATH = r"D:\00.Comapre\pollux_hcds_ald_mirror_ffff\Src_Diff\PLC\PM1\PM1"

    # 분석 실행
    analyzer = TwinCATQAAnalyzer(OLD_PATH, NEW_PATH)
    report = analyzer.analyze()

    # JSON 저장
    output_dir = Path(r"D:\01. Vscode\Twincat\features\twincat-code-qa-tool\output")
    output_dir.mkdir(exist_ok=True)

    json_path = output_dir / "qa_report.json"
    with open(json_path, 'w', encoding='utf-8') as f:
        json.dump(report, f, ensure_ascii=False, indent=2)
    print(f"\nJSON 리포트 저장: {json_path}")

    # Markdown 저장
    md_content = generate_markdown_report(report)
    md_path = output_dir / "qa_report.md"
    with open(md_path, 'w', encoding='utf-8') as f:
        f.write(md_content)
    print(f"Markdown 리포트 저장: {md_path}")

    # 콘솔 출력
    print("\n" + "="*60)
    print("분석 완료!")
    print("="*60)
    print(f"총 파일 변경: {report['summary']['total_files_changed']}개")
    print(f"총 QA 이슈: {report['summary']['total_qa_issues']}개")
    print(f"  - Critical: {report['summary']['critical_issues']}개")
    print(f"  - Warning: {report['summary']['warning_issues']}개")
    print(f"  - Info: {report['summary']['info_issues']}개")
