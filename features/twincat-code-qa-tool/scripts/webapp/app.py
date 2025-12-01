# -*- coding: utf-8 -*-
"""
TwinCAT Code QA 웹 애플리케이션
- 단일 프로젝트 분석
- Source & Target 비교 분석
"""

from flask import Flask, render_template, request, jsonify, send_file
import sys
import os
import json
from datetime import datetime
from pathlib import Path

# 상위 디렉토리의 분석 모듈 임포트
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from analyze_single_project import TwinCATSingleProjectAnalyzer
from analyze_real_project import TwinCATProjectComparer

app = Flask(__name__)
app.config['JSON_AS_ASCII'] = False

# 분석 결과 저장 디렉토리
OUTPUT_DIR = Path(__file__).parent.parent / "output"
OUTPUT_DIR.mkdir(exist_ok=True)


@app.route('/')
def index():
    """메인 페이지"""
    return render_template('index.html')


@app.route('/api/analyze/single', methods=['POST'])
def analyze_single():
    """단일 프로젝트 분석 API"""
    try:
        data = request.get_json()
        project_path = data.get('project_path', '')

        if not project_path:
            return jsonify({'success': False, 'error': '프로젝트 경로를 입력하세요.'})

        if not os.path.exists(project_path):
            return jsonify({'success': False, 'error': f'경로가 존재하지 않습니다: {project_path}'})

        # 분석 실행
        analyzer = TwinCATSingleProjectAnalyzer(project_path)
        report = analyzer.analyze()

        # 결과 저장
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        json_path = OUTPUT_DIR / f"single_analysis_{timestamp}.json"
        with open(json_path, 'w', encoding='utf-8') as f:
            json.dump(report, f, ensure_ascii=False, indent=2, default=str)

        # 요약 정보 생성
        summary = {
            'success': True,
            'analysis_type': 'single',
            'project_path': project_path,
            'timestamp': report['timestamp'],
            'summary': {
                'total_files': report['summary']['total_files'],
                'pou_count': report['summary']['pou_count'],
                'gvl_count': report['summary']['gvl_count'],
                'dut_count': report['summary']['dut_count'],
                'total_lines': report['summary']['total_lines'],
                'total_issues': report['summary']['total_issues'],
                'critical_count': report['summary']['critical_count'],
                'warning_count': report['summary']['warning_count'],
                'info_count': report['summary']['info_count'],
            },
            'issues_by_category': report['issues_by_category'],
            'issues_by_rule': report['issues_by_rule'],
            'critical_issues': report['issues'][:100],  # 상위 100개만
            'high_complexity_files': report['files'][:20],  # 복잡도 높은 파일 20개
            'result_file': str(json_path)
        }

        return jsonify(summary)

    except Exception as e:
        return jsonify({'success': False, 'error': str(e)})


@app.route('/api/analyze/compare', methods=['POST'])
def analyze_compare():
    """Source & Target 비교 분석 API"""
    try:
        data = request.get_json()
        source_path = data.get('source_path', '')
        target_path = data.get('target_path', '')

        if not source_path or not target_path:
            return jsonify({'success': False, 'error': 'Source와 Target 경로를 모두 입력하세요.'})

        if not os.path.exists(source_path):
            return jsonify({'success': False, 'error': f'Source 경로가 존재하지 않습니다: {source_path}'})

        if not os.path.exists(target_path):
            return jsonify({'success': False, 'error': f'Target 경로가 존재하지 않습니다: {target_path}'})

        # 비교 분석 실행
        comparer = TwinCATProjectComparer(source_path, target_path)
        report = comparer.compare()

        # 결과 저장
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        json_path = OUTPUT_DIR / f"compare_analysis_{timestamp}.json"
        with open(json_path, 'w', encoding='utf-8') as f:
            json.dump(report, f, ensure_ascii=False, indent=2, default=str)

        # 요약 정보 생성
        summary = {
            'success': True,
            'analysis_type': 'compare',
            'source_path': source_path,
            'target_path': target_path,
            'timestamp': report['timestamp'],
            'summary': {
                'total_changes': report['summary']['total_changes'],
                'added_files': report['summary']['added_files'],
                'deleted_files': report['summary']['deleted_files'],
                'modified_files': report['summary']['modified_files'],
                'variable_changes': report['summary']['variable_changes'],
                'total_issues': report['summary']['total_issues'],
                'critical_count': report['summary']['critical_count'],
                'warning_count': report['summary']['warning_count'],
                'info_count': report['summary']['info_count'],
            },
            'file_changes': report['file_changes'][:50],  # 변경 파일 50개
            'variable_changes': report['variable_changes'][:30],  # 변수 변경 30개
            'qa_issues': report['qa_issues'][:100],  # QA 이슈 100개
            'result_file': str(json_path)
        }

        return jsonify(summary)

    except Exception as e:
        return jsonify({'success': False, 'error': str(e)})


@app.route('/api/browse', methods=['POST'])
def browse_directory():
    """디렉토리 브라우징 API"""
    try:
        data = request.get_json()
        path = data.get('path', '')

        if not path:
            # 기본 드라이브 목록 반환
            import string
            drives = []
            for letter in string.ascii_uppercase:
                drive = f"{letter}:\\"
                if os.path.exists(drive):
                    drives.append({'name': drive, 'path': drive, 'type': 'drive'})
            return jsonify({'success': True, 'items': drives, 'current': ''})

        if not os.path.exists(path):
            return jsonify({'success': False, 'error': '경로가 존재하지 않습니다.'})

        items = []
        try:
            for item in os.listdir(path):
                item_path = os.path.join(path, item)
                if os.path.isdir(item_path):
                    items.append({
                        'name': item,
                        'path': item_path,
                        'type': 'folder'
                    })
        except PermissionError:
            return jsonify({'success': False, 'error': '접근 권한이 없습니다.'})

        # 정렬
        items.sort(key=lambda x: x['name'].lower())

        # 상위 폴더 추가
        parent = os.path.dirname(path.rstrip('\\'))
        if parent and parent != path:
            items.insert(0, {'name': '..', 'path': parent, 'type': 'parent'})

        return jsonify({'success': True, 'items': items, 'current': path})

    except Exception as e:
        return jsonify({'success': False, 'error': str(e)})


@app.route('/api/history', methods=['GET'])
def get_history():
    """분석 이력 조회"""
    try:
        history = []
        for f in OUTPUT_DIR.glob("*.json"):
            try:
                stat = f.stat()
                history.append({
                    'filename': f.name,
                    'path': str(f),
                    'size': stat.st_size,
                    'modified': datetime.fromtimestamp(stat.st_mtime).isoformat(),
                    'type': 'single' if 'single' in f.name else 'compare'
                })
            except:
                pass

        # 최신순 정렬
        history.sort(key=lambda x: x['modified'], reverse=True)
        return jsonify({'success': True, 'history': history[:20]})

    except Exception as e:
        return jsonify({'success': False, 'error': str(e)})


@app.route('/api/report/<filename>')
def get_report(filename):
    """저장된 리포트 조회"""
    try:
        file_path = OUTPUT_DIR / filename
        if not file_path.exists():
            return jsonify({'success': False, 'error': '파일을 찾을 수 없습니다.'})

        with open(file_path, 'r', encoding='utf-8') as f:
            data = json.load(f)

        return jsonify({'success': True, 'data': data})

    except Exception as e:
        return jsonify({'success': False, 'error': str(e)})


if __name__ == '__main__':
    print("=" * 60)
    print("TwinCAT Code QA 웹 애플리케이션")
    print("=" * 60)
    print("브라우저에서 http://localhost:5000 으로 접속하세요")
    print("=" * 60)
    app.run(debug=True, host='0.0.0.0', port=5000)
