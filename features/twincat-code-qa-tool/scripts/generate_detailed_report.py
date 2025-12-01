#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
TwinCAT QA 상세 리포트 생성기
- 파일별 상세 이슈 목록
- 실제 코드 스니펫 (전후 컨텍스트 포함)
- 구체적인 수정 방법
- HTML 인터랙티브 리포트
"""

import json
import html
from pathlib import Path
from datetime import datetime
from collections import defaultdict

def load_report(json_path: str) -> dict:
    """JSON 리포트 로드"""
    with open(json_path, 'r', encoding='utf-8') as f:
        return json.load(f)

def generate_detailed_html_report(report: dict, project_path: str) -> str:
    """상세 HTML 리포트 생성"""

    # 파일별 이슈 그룹핑
    issues_by_file = defaultdict(list)
    for issue in report['issues']:
        issues_by_file[issue['file']].append(issue)

    # 규칙별 이슈 그룹핑
    issues_by_rule = defaultdict(list)
    for issue in report['issues']:
        issues_by_rule[issue['rule_id']].append(issue)

    # 심각도별 그룹핑
    issues_by_severity = defaultdict(list)
    for issue in report['issues']:
        issues_by_severity[issue['severity']].append(issue)

    s = report['summary']

    html_content = f'''<!DOCTYPE html>
<html lang="ko">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>TwinCAT QA 상세 분석 리포트</title>
    <style>
        :root {{
            --critical-color: #dc3545;
            --warning-color: #ffc107;
            --info-color: #17a2b8;
            --success-color: #28a745;
            --bg-dark: #1a1a2e;
            --bg-card: #16213e;
            --text-primary: #eee;
            --text-secondary: #aaa;
            --border-color: #0f3460;
        }}

        * {{ box-sizing: border-box; margin: 0; padding: 0; }}

        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: var(--bg-dark);
            color: var(--text-primary);
            line-height: 1.6;
        }}

        .container {{
            max-width: 1400px;
            margin: 0 auto;
            padding: 20px;
        }}

        header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 30px;
            border-radius: 10px;
            margin-bottom: 30px;
            box-shadow: 0 4px 15px rgba(0,0,0,0.3);
        }}

        header h1 {{
            font-size: 2em;
            margin-bottom: 10px;
        }}

        header .meta {{
            opacity: 0.9;
            font-size: 0.9em;
        }}

        .summary-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            margin-bottom: 30px;
        }}

        .summary-card {{
            background: var(--bg-card);
            border-radius: 10px;
            padding: 20px;
            text-align: center;
            border: 1px solid var(--border-color);
        }}

        .summary-card.critical {{ border-left: 4px solid var(--critical-color); }}
        .summary-card.warning {{ border-left: 4px solid var(--warning-color); }}
        .summary-card.info {{ border-left: 4px solid var(--info-color); }}

        .summary-card .number {{
            font-size: 2.5em;
            font-weight: bold;
            margin-bottom: 5px;
        }}

        .summary-card.critical .number {{ color: var(--critical-color); }}
        .summary-card.warning .number {{ color: var(--warning-color); }}
        .summary-card.info .number {{ color: var(--info-color); }}

        .tabs {{
            display: flex;
            gap: 10px;
            margin-bottom: 20px;
            flex-wrap: wrap;
        }}

        .tab-btn {{
            padding: 10px 20px;
            background: var(--bg-card);
            border: 1px solid var(--border-color);
            border-radius: 5px;
            color: var(--text-primary);
            cursor: pointer;
            transition: all 0.3s;
        }}

        .tab-btn:hover, .tab-btn.active {{
            background: #667eea;
            border-color: #667eea;
        }}

        .tab-content {{
            display: none;
        }}

        .tab-content.active {{
            display: block;
        }}

        .section {{
            background: var(--bg-card);
            border-radius: 10px;
            padding: 20px;
            margin-bottom: 20px;
            border: 1px solid var(--border-color);
        }}

        .section h2 {{
            color: #667eea;
            margin-bottom: 15px;
            padding-bottom: 10px;
            border-bottom: 1px solid var(--border-color);
        }}

        .section h3 {{
            color: var(--text-primary);
            margin: 15px 0 10px 0;
            font-size: 1.1em;
        }}

        .issue-card {{
            background: rgba(0,0,0,0.2);
            border-radius: 8px;
            padding: 15px;
            margin-bottom: 15px;
            border-left: 4px solid var(--border-color);
        }}

        .issue-card.critical {{ border-left-color: var(--critical-color); }}
        .issue-card.warning {{ border-left-color: var(--warning-color); }}
        .issue-card.info {{ border-left-color: var(--info-color); }}

        .issue-header {{
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 10px;
            flex-wrap: wrap;
            gap: 10px;
        }}

        .issue-title {{
            font-weight: bold;
            display: flex;
            align-items: center;
            gap: 10px;
        }}

        .badge {{
            padding: 3px 10px;
            border-radius: 12px;
            font-size: 0.8em;
            font-weight: bold;
        }}

        .badge.critical {{ background: var(--critical-color); }}
        .badge.warning {{ background: var(--warning-color); color: #000; }}
        .badge.info {{ background: var(--info-color); }}

        .issue-location {{
            color: var(--text-secondary);
            font-size: 0.9em;
        }}

        .issue-message {{
            margin: 10px 0;
            padding: 10px;
            background: rgba(0,0,0,0.3);
            border-radius: 5px;
        }}

        .code-block {{
            background: #0d1117;
            border-radius: 5px;
            padding: 15px;
            margin: 10px 0;
            overflow-x: auto;
            font-family: 'Consolas', 'Monaco', monospace;
            font-size: 0.9em;
            line-height: 1.5;
        }}

        .code-block .line-number {{
            color: #6e7681;
            user-select: none;
            padding-right: 15px;
            min-width: 50px;
            display: inline-block;
            text-align: right;
        }}

        .code-block .highlight {{
            background: rgba(220, 53, 69, 0.3);
            display: block;
            margin: 0 -15px;
            padding: 0 15px;
        }}

        .suggestion {{
            background: rgba(40, 167, 69, 0.1);
            border: 1px solid var(--success-color);
            border-radius: 5px;
            padding: 10px;
            margin-top: 10px;
        }}

        .suggestion-title {{
            color: var(--success-color);
            font-weight: bold;
            margin-bottom: 5px;
        }}

        .file-tree {{
            font-family: monospace;
        }}

        .file-item {{
            padding: 8px 15px;
            border-bottom: 1px solid var(--border-color);
            display: flex;
            justify-content: space-between;
            align-items: center;
            cursor: pointer;
            transition: background 0.2s;
        }}

        .file-item:hover {{
            background: rgba(102, 126, 234, 0.1);
        }}

        .file-name {{
            color: var(--text-primary);
        }}

        .file-issues {{
            display: flex;
            gap: 10px;
        }}

        .issue-count {{
            padding: 2px 8px;
            border-radius: 10px;
            font-size: 0.8em;
        }}

        .issue-count.critical {{ background: var(--critical-color); }}
        .issue-count.warning {{ background: var(--warning-color); color: #000; }}
        .issue-count.info {{ background: var(--info-color); }}

        .rule-table {{
            width: 100%;
            border-collapse: collapse;
        }}

        .rule-table th, .rule-table td {{
            padding: 12px;
            text-align: left;
            border-bottom: 1px solid var(--border-color);
        }}

        .rule-table th {{
            background: rgba(0,0,0,0.3);
            color: #667eea;
        }}

        .rule-table tr:hover {{
            background: rgba(102, 126, 234, 0.1);
        }}

        .filter-bar {{
            display: flex;
            gap: 10px;
            margin-bottom: 20px;
            flex-wrap: wrap;
        }}

        .filter-bar select, .filter-bar input {{
            padding: 8px 12px;
            background: var(--bg-card);
            border: 1px solid var(--border-color);
            border-radius: 5px;
            color: var(--text-primary);
        }}

        .progress-bar {{
            height: 20px;
            background: rgba(0,0,0,0.3);
            border-radius: 10px;
            overflow: hidden;
            display: flex;
        }}

        .progress-segment {{
            height: 100%;
            transition: width 0.3s;
        }}

        .progress-segment.critical {{ background: var(--critical-color); }}
        .progress-segment.warning {{ background: var(--warning-color); }}
        .progress-segment.info {{ background: var(--info-color); }}

        .collapsible {{
            cursor: pointer;
        }}

        .collapsible-content {{
            max-height: 0;
            overflow: hidden;
            transition: max-height 0.3s ease-out;
        }}

        .collapsible-content.expanded {{
            max-height: none;
        }}

        .expand-btn {{
            background: none;
            border: none;
            color: #667eea;
            cursor: pointer;
            padding: 5px 10px;
        }}

        @media (max-width: 768px) {{
            .summary-grid {{
                grid-template-columns: 1fr 1fr;
            }}
        }}
    </style>
</head>
<body>
    <div class="container">
        <header>
            <h1>🔍 TwinCAT QA 상세 분석 리포트</h1>
            <div class="meta">
                <div>📁 프로젝트: {html.escape(report['project_path'])}</div>
                <div>📅 분석 일시: {report['generated_at']}</div>
                <div>📊 총 파일: {s['total_files']}개 | 총 코드: {s['total_lines']:,}줄</div>
            </div>
        </header>

        <!-- 요약 카드 -->
        <div class="summary-grid">
            <div class="summary-card">
                <div class="number">{s['total_issues']:,}</div>
                <div>총 이슈</div>
            </div>
            <div class="summary-card critical">
                <div class="number">{s['critical_count']}</div>
                <div>🔴 Critical</div>
            </div>
            <div class="summary-card warning">
                <div class="number">{s['warning_count']}</div>
                <div>🟡 Warning</div>
            </div>
            <div class="summary-card info">
                <div class="number">{s['info_count']}</div>
                <div>🔵 Info</div>
            </div>
        </div>

        <!-- 이슈 분포 바 -->
        <div class="section">
            <h2>📊 이슈 분포</h2>
            <div class="progress-bar">
                <div class="progress-segment critical" style="width: {s['critical_count']*100/max(s['total_issues'],1):.1f}%"></div>
                <div class="progress-segment warning" style="width: {s['warning_count']*100/max(s['total_issues'],1):.1f}%"></div>
                <div class="progress-segment info" style="width: {s['info_count']*100/max(s['total_issues'],1):.1f}%"></div>
            </div>
            <div style="display: flex; justify-content: space-around; margin-top: 10px; font-size: 0.9em;">
                <span>🔴 Critical: {s['critical_count']*100/max(s['total_issues'],1):.1f}%</span>
                <span>🟡 Warning: {s['warning_count']*100/max(s['total_issues'],1):.1f}%</span>
                <span>🔵 Info: {s['info_count']*100/max(s['total_issues'],1):.1f}%</span>
            </div>
        </div>

        <!-- 탭 네비게이션 -->
        <div class="tabs">
            <button class="tab-btn active" onclick="showTab('critical-issues')">🔴 Critical ({s['critical_count']})</button>
            <button class="tab-btn" onclick="showTab('by-file')">📁 파일별 ({len(issues_by_file)})</button>
            <button class="tab-btn" onclick="showTab('by-rule')">📋 규칙별 ({len(issues_by_rule)})</button>
            <button class="tab-btn" onclick="showTab('all-issues')">📝 전체 목록</button>
            <button class="tab-btn" onclick="showTab('complexity')">⚠️ 복잡도</button>
        </div>

        <!-- Critical Issues 탭 -->
        <div id="critical-issues" class="tab-content active">
            <div class="section">
                <h2>🔴 Critical Issues - 즉시 수정 필요</h2>
                <p style="color: var(--text-secondary); margin-bottom: 20px;">
                    아래 이슈들은 런타임 오류, 데이터 손실, 시스템 불안정을 유발할 수 있습니다.
                </p>
                {generate_critical_issues_html(issues_by_severity.get('Critical', []), project_path)}
            </div>
        </div>

        <!-- 파일별 탭 -->
        <div id="by-file" class="tab-content">
            <div class="section">
                <h2>📁 파일별 이슈 목록</h2>
                <div class="filter-bar">
                    <input type="text" id="file-search" placeholder="파일명 검색..." onkeyup="filterFiles()">
                    <select id="severity-filter" onchange="filterFiles()">
                        <option value="all">모든 심각도</option>
                        <option value="critical">Critical만</option>
                        <option value="warning">Warning만</option>
                        <option value="info">Info만</option>
                    </select>
                </div>
                <div class="file-tree" id="file-list">
                    {generate_file_list_html(issues_by_file)}
                </div>
            </div>
        </div>

        <!-- 규칙별 탭 -->
        <div id="by-rule" class="tab-content">
            <div class="section">
                <h2>📋 규칙별 이슈 통계</h2>
                {generate_rule_details_html(issues_by_rule)}
            </div>
        </div>

        <!-- 전체 목록 탭 -->
        <div id="all-issues" class="tab-content">
            <div class="section">
                <h2>📝 전체 이슈 목록</h2>
                <div class="filter-bar">
                    <select id="all-severity-filter" onchange="filterAllIssues()">
                        <option value="all">모든 심각도</option>
                        <option value="Critical">Critical</option>
                        <option value="Warning">Warning</option>
                        <option value="Info">Info</option>
                    </select>
                    <select id="all-rule-filter" onchange="filterAllIssues()">
                        <option value="all">모든 규칙</option>
                        {generate_rule_options(issues_by_rule)}
                    </select>
                    <input type="text" id="all-search" placeholder="검색..." onkeyup="filterAllIssues()">
                </div>
                <div id="all-issues-list">
                    {generate_all_issues_html(report['issues'][:500])}
                    {f'<p style="color: var(--text-secondary); text-align: center;">... 외 {len(report["issues"]) - 500}개 이슈 (JSON 파일에서 전체 확인 가능)</p>' if len(report['issues']) > 500 else ''}
                </div>
            </div>
        </div>

        <!-- 복잡도 탭 -->
        <div id="complexity" class="tab-content">
            <div class="section">
                <h2>⚠️ 코드 복잡도 분석</h2>
                {generate_complexity_html(report['files'])}
            </div>
        </div>
    </div>

    <script>
        function showTab(tabId) {{
            // 모든 탭 버튼 비활성화
            document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
            // 모든 탭 컨텐츠 숨기기
            document.querySelectorAll('.tab-content').forEach(content => content.classList.remove('active'));

            // 선택된 탭 활성화
            event.target.classList.add('active');
            document.getElementById(tabId).classList.add('active');
        }}

        function filterFiles() {{
            const search = document.getElementById('file-search').value.toLowerCase();
            const severity = document.getElementById('severity-filter').value;

            document.querySelectorAll('.file-item').forEach(item => {{
                const fileName = item.dataset.filename.toLowerCase();
                const hasCritical = item.dataset.critical > 0;
                const hasWarning = item.dataset.warning > 0;
                const hasInfo = item.dataset.info > 0;

                let show = fileName.includes(search);

                if (severity === 'critical') show = show && hasCritical;
                else if (severity === 'warning') show = show && hasWarning;
                else if (severity === 'info') show = show && hasInfo;

                item.style.display = show ? 'flex' : 'none';
            }});
        }}

        function filterAllIssues() {{
            const severity = document.getElementById('all-severity-filter').value;
            const rule = document.getElementById('all-rule-filter').value;
            const search = document.getElementById('all-search').value.toLowerCase();

            document.querySelectorAll('.issue-card').forEach(card => {{
                const cardSeverity = card.dataset.severity;
                const cardRule = card.dataset.rule;
                const cardText = card.textContent.toLowerCase();

                let show = true;
                if (severity !== 'all' && cardSeverity !== severity) show = false;
                if (rule !== 'all' && cardRule !== rule) show = false;
                if (search && !cardText.includes(search)) show = false;

                card.style.display = show ? 'block' : 'none';
            }});
        }}

        function toggleCollapse(id) {{
            const content = document.getElementById(id);
            content.classList.toggle('expanded');
        }}

        function copyCode(btn) {{
            const code = btn.parentElement.querySelector('code').textContent;
            navigator.clipboard.writeText(code);
            btn.textContent = '복사됨!';
            setTimeout(() => btn.textContent = '복사', 2000);
        }}
    </script>
</body>
</html>'''

    return html_content


def generate_critical_issues_html(issues: list, project_path: str) -> str:
    """Critical 이슈 상세 HTML"""
    if not issues:
        return '<p>Critical 이슈가 없습니다. 👍</p>'

    # 규칙별 그룹핑
    by_rule = defaultdict(list)
    for issue in issues:
        by_rule[issue['rule_id']].append(issue)

    html_parts = []

    rule_info = {
        'QA001': {
            'name': '초기화되지 않은 변수',
            'desc': 'REAL, LREAL, 포인터 타입 변수가 초기값 없이 선언되었습니다.',
            'risk': '예측 불가능한 값으로 인한 오동작, 시스템 불안정',
            'fix': '변수 선언 시 초기값을 명시하세요.',
            'example_bad': 'fTemperature : REAL;',
            'example_good': 'fTemperature : REAL := 0.0;'
        },
        'QA002': {
            'name': '위험한 타입 변환',
            'desc': '큰 데이터 타입에서 작은 타입으로 변환 시 데이터 손실 가능성',
            'risk': '오버플로우, 데이터 손실, 정밀도 저하',
            'fix': 'LIMIT 함수로 범위 검증 후 변환하세요.',
            'example_bad': 'nValue := REAL_TO_INT(fInput);',
            'example_good': 'nValue := REAL_TO_INT(LIMIT(-32768.0, fInput, 32767.0));'
        },
        'QA005': {
            'name': 'REAL 직접 비교',
            'desc': '실수형 변수를 등호(=)로 직접 비교하고 있습니다.',
            'risk': '부동소수점 정밀도 문제로 비교 실패',
            'fix': '허용 오차(epsilon)를 사용한 비교로 변경하세요.',
            'example_bad': 'IF fValue = fTarget THEN',
            'example_good': 'IF ABS(fValue - fTarget) < 0.0001 THEN'
        },
        'QA006': {
            'name': '0으로 나누기 가능성',
            'desc': '변수로 나누기를 수행하며, 0 검사가 없습니다.',
            'risk': '런타임 오류로 시스템 정지',
            'fix': '나누기 전 분모가 0인지 확인하세요.',
            'example_bad': 'fResult := fNumerator / fDenominator;',
            'example_good': 'IF fDenominator <> 0.0 THEN\n    fResult := fNumerator / fDenominator;\nEND_IF'
        }
    }

    for rule_id in sorted(by_rule.keys()):
        rule_issues = by_rule[rule_id]
        info = rule_info.get(rule_id, {
            'name': rule_id,
            'desc': '',
            'risk': '',
            'fix': '',
            'example_bad': '',
            'example_good': ''
        })

        html_parts.append(f'''
        <div class="section" style="background: rgba(220,53,69,0.1); border: 1px solid var(--critical-color);">
            <h3 style="color: var(--critical-color);">
                {rule_id}: {info['name']} ({len(rule_issues)}건)
            </h3>
            <p><strong>설명:</strong> {info['desc']}</p>
            <p><strong>위험:</strong> {info['risk']}</p>

            <div class="suggestion">
                <div class="suggestion-title">✅ 수정 방법</div>
                <p>{info['fix']}</p>
                {f"""
                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-top: 10px;">
                    <div>
                        <div style="color: var(--critical-color); font-weight: bold;">❌ 잘못된 코드</div>
                        <div class="code-block"><code>{html.escape(info['example_bad'])}</code></div>
                    </div>
                    <div>
                        <div style="color: var(--success-color); font-weight: bold;">✅ 올바른 코드</div>
                        <div class="code-block"><code>{html.escape(info['example_good'])}</code></div>
                    </div>
                </div>
                """ if info['example_bad'] else ''}
            </div>

            <h4 style="margin-top: 15px;">📍 발생 위치 ({len(rule_issues)}건)</h4>
            <table class="rule-table">
                <tr>
                    <th style="width: 35%;">파일</th>
                    <th style="width: 10%;">라인</th>
                    <th style="width: 55%;">코드</th>
                </tr>
        ''')

        for issue in rule_issues[:30]:  # 상위 30개만
            code = html.escape(issue.get('code', '')[:100])
            file_name = Path(issue['file']).name
            html_parts.append(f'''
                <tr>
                    <td><code>{html.escape(file_name)}</code></td>
                    <td>{issue['line']}</td>
                    <td><code style="font-size: 0.85em;">{code}</code></td>
                </tr>
            ''')

        if len(rule_issues) > 30:
            html_parts.append(f'''
                <tr>
                    <td colspan="3" style="text-align: center; color: var(--text-secondary);">
                        ... 외 {len(rule_issues) - 30}건 (전체 목록은 JSON 참조)
                    </td>
                </tr>
            ''')

        html_parts.append('</table></div>')

    return '\n'.join(html_parts)


def generate_file_list_html(issues_by_file: dict) -> str:
    """파일별 이슈 목록 HTML"""
    html_parts = []

    # 이슈 많은 순으로 정렬
    sorted_files = sorted(issues_by_file.items(),
                         key=lambda x: len([i for i in x[1] if i['severity'] == 'Critical']),
                         reverse=True)

    for file_path, issues in sorted_files:
        critical = len([i for i in issues if i['severity'] == 'Critical'])
        warning = len([i for i in issues if i['severity'] == 'Warning'])
        info = len([i for i in issues if i['severity'] == 'Info'])

        file_name = Path(file_path).name

        html_parts.append(f'''
        <div class="file-item" data-filename="{html.escape(file_name)}"
             data-critical="{critical}" data-warning="{warning}" data-info="{info}"
             onclick="toggleCollapse('file-{hash(file_path)}')">
            <span class="file-name">📄 {html.escape(file_name)}</span>
            <div class="file-issues">
                {f'<span class="issue-count critical">{critical}</span>' if critical else ''}
                {f'<span class="issue-count warning">{warning}</span>' if warning else ''}
                {f'<span class="issue-count info">{info}</span>' if info else ''}
            </div>
        </div>
        <div id="file-{hash(file_path)}" class="collapsible-content">
            <div style="padding: 10px 20px; background: rgba(0,0,0,0.2);">
        ''')

        for issue in issues[:20]:
            severity_class = issue['severity'].lower()
            html_parts.append(f'''
                <div class="issue-card {severity_class}" style="margin: 5px 0;">
                    <div class="issue-header">
                        <span class="badge {severity_class}">{issue['severity']}</span>
                        <span>{issue['rule_id']}</span>
                        <span>라인 {issue['line']}</span>
                    </div>
                    <div>{html.escape(issue['message'])}</div>
                    {f'<div class="code-block"><code>{html.escape(issue.get("code", "")[:150])}</code></div>' if issue.get('code') else ''}
                </div>
            ''')

        if len(issues) > 20:
            html_parts.append(f'<p style="color: var(--text-secondary);">... 외 {len(issues) - 20}건</p>')

        html_parts.append('</div></div>')

    return '\n'.join(html_parts)


def generate_rule_details_html(issues_by_rule: dict) -> str:
    """규칙별 상세 HTML"""
    rule_descriptions = {
        'QA001': ('초기화되지 않은 변수', 'Safety', '변수 선언 시 초기값 미지정'),
        'QA002': ('위험한 타입 변환', 'Safety', '큰 타입에서 작은 타입으로 변환'),
        'QA003': ('대용량 배열', 'Performance', '1000개 이상 요소의 배열'),
        'QA004': ('포인터 변수', 'Safety', '포인터 사용 - NULL 체크 필요'),
        'QA005': ('REAL 직접 비교', 'Safety', '실수형 등호 비교'),
        'QA006': ('0으로 나누기', 'Safety', '분모 검증 없는 나눗셈'),
        'QA007': ('매직 넘버', 'Maintainability', '의미 불명확한 숫자 상수'),
        'QA008': ('과도한 중첩', 'Maintainability', '4단계 이상 중첩'),
        'QA009': ('긴 코드', 'Maintainability', '500줄 초과'),
        'QA010': ('하드코딩된 시간', 'Maintainability', 'T#nnn 형태의 고정 시간값'),
        'QA011': ('빈 예외 처리', 'Safety', '빈 ELSE 블록'),
        'QA012': ('TODO/FIXME', 'Maintainability', '미완료 작업 표시'),
        'QA013': ('주석 처리된 코드', 'Maintainability', '주석으로 비활성화된 코드'),
        'QA014': ('높은 복잡도', 'Maintainability', '순환 복잡도 15 초과'),
        'QA015': ('주석 부족', 'Maintainability', '코드 대비 10% 미만'),
        'QA016': ('명명 규칙', 'Style', '헝가리안 표기법 미준수'),
    }

    html_parts = ['<table class="rule-table">']
    html_parts.append('''
        <tr>
            <th>규칙 ID</th>
            <th>이름</th>
            <th>카테고리</th>
            <th>설명</th>
            <th>건수</th>
        </tr>
    ''')

    for rule_id in sorted(issues_by_rule.keys()):
        issues = issues_by_rule[rule_id]
        severity = issues[0]['severity'] if issues else 'Info'
        severity_class = severity.lower()

        name, category, desc = rule_descriptions.get(rule_id, (rule_id, 'Other', ''))

        html_parts.append(f'''
            <tr>
                <td><span class="badge {severity_class}">{rule_id}</span></td>
                <td>{name}</td>
                <td>{category}</td>
                <td>{desc}</td>
                <td><strong>{len(issues)}</strong></td>
            </tr>
        ''')

    html_parts.append('</table>')
    return '\n'.join(html_parts)


def generate_rule_options(issues_by_rule: dict) -> str:
    """규칙 선택 옵션"""
    options = []
    for rule_id in sorted(issues_by_rule.keys()):
        count = len(issues_by_rule[rule_id])
        options.append(f'<option value="{rule_id}">{rule_id} ({count})</option>')
    return '\n'.join(options)


def generate_all_issues_html(issues: list) -> str:
    """전체 이슈 목록 HTML"""
    html_parts = []

    for issue in issues:
        severity_class = issue['severity'].lower()
        html_parts.append(f'''
        <div class="issue-card {severity_class}"
             data-severity="{issue['severity']}"
             data-rule="{issue['rule_id']}">
            <div class="issue-header">
                <div class="issue-title">
                    <span class="badge {severity_class}">{issue['severity']}</span>
                    <span>{issue['rule_id']}</span>
                </div>
                <span class="issue-location">📁 {html.escape(Path(issue['file']).name)} : {issue['line']}</span>
            </div>
            <div class="issue-message">{html.escape(issue['message'])}</div>
            {f'<div class="code-block"><code>{html.escape(issue.get("code", ""))}</code></div>' if issue.get('code') else ''}
            {f'<div class="suggestion"><div class="suggestion-title">💡 제안</div>{html.escape(issue.get("suggestion", ""))}</div>' if issue.get('suggestion') else ''}
        </div>
        ''')

    return '\n'.join(html_parts)


def generate_complexity_html(files: list) -> str:
    """복잡도 분석 HTML"""
    # 복잡도 높은 순 정렬
    complex_files = sorted([f for f in files if f.get('complexity', 0) > 0],
                          key=lambda x: x.get('complexity', 0), reverse=True)[:30]

    html_parts = ['''
        <p style="color: var(--text-secondary); margin-bottom: 15px;">
            순환 복잡도(Cyclomatic Complexity)가 높은 파일은 테스트와 유지보수가 어렵습니다.
            일반적으로 15 이하를 권장합니다.
        </p>
        <table class="rule-table">
            <tr>
                <th>파일</th>
                <th>타입</th>
                <th>라인수</th>
                <th>복잡도</th>
                <th>이슈수</th>
                <th>상태</th>
            </tr>
    ''']

    for f in complex_files:
        complexity = f.get('complexity', 0)
        if complexity > 50:
            status = '<span style="color: var(--critical-color);">🔴 리팩토링 필요</span>'
        elif complexity > 15:
            status = '<span style="color: var(--warning-color);">🟡 검토 권장</span>'
        else:
            status = '<span style="color: var(--success-color);">🟢 정상</span>'

        html_parts.append(f'''
            <tr>
                <td>{html.escape(f.get('name', ''))}</td>
                <td>{f.get('pou_type', '')}</td>
                <td>{f.get('lines', 0):,}</td>
                <td><strong>{complexity}</strong></td>
                <td>{f.get('issue_count', 0)}</td>
                <td>{status}</td>
            </tr>
        ''')

    html_parts.append('</table>')
    return '\n'.join(html_parts)


def generate_detailed_markdown(report: dict) -> str:
    """상세 Markdown 리포트"""
    s = report['summary']

    md = []
    md.append("# TwinCAT QA 상세 분석 리포트")
    md.append("")
    md.append(f"**프로젝트**: `{report['project_path']}`")
    md.append(f"**분석 일시**: {report['generated_at']}")
    md.append("")

    # 요약
    md.append("## 📊 분석 요약")
    md.append("")
    md.append(f"- **총 파일**: {s['total_files']}개")
    md.append(f"- **총 코드**: {s['total_lines']:,}줄")
    md.append(f"- **총 이슈**: {s['total_issues']:,}개")
    md.append(f"  - 🔴 Critical: {s['critical_count']}개")
    md.append(f"  - 🟡 Warning: {s['warning_count']}개")
    md.append(f"  - 🔵 Info: {s['info_count']}개")
    md.append("")

    # Critical 이슈 상세
    critical_issues = [i for i in report['issues'] if i['severity'] == 'Critical']
    if critical_issues:
        md.append("## 🔴 Critical Issues 상세")
        md.append("")

        by_rule = defaultdict(list)
        for issue in critical_issues:
            by_rule[issue['rule_id']].append(issue)

        for rule_id in sorted(by_rule.keys()):
            issues = by_rule[rule_id]
            md.append(f"### {rule_id} ({len(issues)}건)")
            md.append("")

            for issue in issues[:20]:
                md.append(f"#### 📍 {Path(issue['file']).name}:{issue['line']}")
                md.append("")
                md.append(f"**메시지**: {issue['message']}")
                if issue.get('code'):
                    md.append("")
                    md.append("```iecst")
                    md.append(issue['code'])
                    md.append("```")
                if issue.get('suggestion'):
                    md.append("")
                    md.append(f"**💡 수정 제안**: {issue['suggestion']}")
                md.append("")
                md.append("---")
                md.append("")

            if len(issues) > 20:
                md.append(f"*... 외 {len(issues) - 20}건*")
                md.append("")

    return '\n'.join(md)


if __name__ == "__main__":
    import sys

    # JSON 리포트 경로
    json_path = r"D:\01. Vscode\Twincat\features\twincat-code-qa-tool\output\single_project_qa_report.json"
    project_path = r"D:\00.Comapre\pollux_hcds_ald_mirror_ffff\Src_Diff\PLC\PM1\PM1"

    if len(sys.argv) > 1:
        json_path = sys.argv[1]

    print("상세 리포트 생성 중...")

    # 리포트 로드
    report = load_report(json_path)

    # HTML 리포트 생성
    html_content = generate_detailed_html_report(report, project_path)
    html_path = Path(json_path).parent / "qa_detailed_report.html"
    with open(html_path, 'w', encoding='utf-8') as f:
        f.write(html_content)
    print(f"HTML 리포트: {html_path}")

    # 상세 Markdown 생성
    md_content = generate_detailed_markdown(report)
    md_path = Path(json_path).parent / "qa_detailed_report.md"
    with open(md_path, 'w', encoding='utf-8') as f:
        f.write(md_content)
    print(f"Markdown 리포트: {md_path}")

    print("\n완료!")
