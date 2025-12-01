# 리포트 생성 예시

TwinCAT QA Tool의 다양한 리포트 생성 방법을 설명합니다.

## 목차

1. [Markdown 리포트 생성](#markdown-리포트-생성)
2. [JSON 리포트 생성](#json-리포트-생성)
3. [CI/CD 연동](#cicd-연동)
4. [통합 예시](#통합-예시)

---

## Markdown 리포트 생성

GitHub PR이나 이슈에 첨부하기 적합한 Markdown 형식의 리포트를 생성합니다.

### 기본 사용법

```csharp
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Infrastructure.Reports;

// QA 보고서 생성 (실제로는 QA 분석기가 생성)
var report = new QAReport
{
    GeneratedAt = DateTime.Now,
    SourceFolder = @"C:\Projects\MyProject\Source",
    TargetFolder = @"C:\Projects\MyProject\Target",
    TotalChanges = 10,
    CriticalCount = 2,
    WarningCount = 3,
    InfoCount = 5,
    Issues = new List<QAIssue>
    {
        new QAIssue
        {
            Severity = Severity.Critical,
            RuleId = "SAFETY-001",
            Category = "타입 안전성",
            Title = "ANY 타입 사용 금지",
            Description = "ANY 타입은 타입 안전성을 해칩니다",
            FilePath = @"C:\Projects\MyProject\MAIN.TcPOU",
            Line = 42,
            WhyDangerous = "런타임 타입 변환 오류 가능",
            Recommendation = "구체적인 타입(INT, REAL 등)을 사용하세요",
            OldCodeSnippet = "VAR\n    myVar : ANY;\nEND_VAR",
            NewCodeSnippet = "VAR\n    myVar : INT;\nEND_VAR"
        }
    }
};

// Markdown 생성기 생성
var markdownGenerator = new MarkdownReportGenerator();

// 문자열로 생성
string markdown = markdownGenerator.Generate(report);
Console.WriteLine(markdown);

// 파일로 저장
string filePath = markdownGenerator.GenerateToFile(report, @"C:\Reports\qa_report.md");
Console.WriteLine($"리포트 저장됨: {filePath}");
```

### 자동 경로 생성

출력 경로를 지정하지 않으면 자동으로 `reports` 폴더에 타임스탬프가 포함된 파일명으로 저장됩니다.

```csharp
var markdownGenerator = new MarkdownReportGenerator();

// 자동 경로 생성: reports/qa_report_20231125_143022.md
string filePath = markdownGenerator.GenerateToFile(report);
```

### Markdown 출력 예시

```markdown
# 🔍 TwinCAT Code QA Report

**생성 시각**: 2023-11-25 14:30:22
**원본 폴더**: `C:\Projects\MyProject\Source`
**대상 폴더**: `C:\Projects\MyProject\Target`

---

## 📊 Summary

⚠️ **심각한 이슈가 발견되었습니다!**

| 항목 | 개수 |
|------|------|
| 총 변경 사항 | 10 |
| 총 이슈 | 10 |
| 🔴 Critical | 2 |
| 🟡 Warning | 3 |
| 🔵 Info | 5 |

## 🔴 Critical Issues

총 **2개**의 Critical 이슈가 발견되었습니다.

### 🔴 [SAFETY-001] ANY 타입 사용 금지

**파일**: `C:\Projects\MyProject\MAIN.TcPOU`
**위치**: 라인 42
**카테고리**: 타입 안전성

**설명**: ANY 타입은 타입 안전성을 해칩니다

**⚠️ 왜 위험한가요?**

> 런타임 타입 변환 오류 가능

**변경 전 코드**:
\```iecst
VAR
    myVar : ANY;
END_VAR
\```

**변경 후 코드**:
\```iecst
VAR
    myVar : INT;
END_VAR
\```

**✅ 권장 해결 방법**:

구체적인 타입(INT, REAL 등)을 사용하세요

---
```

---

## JSON 리포트 생성

CI/CD 파이프라인이나 자동화 도구와 연동하기 적합한 JSON 형식의 리포트를 생성합니다.

### 기본 사용법

```csharp
using TwinCatQA.Infrastructure.Reports;

// JSON 생성기 생성 (들여쓰기 적용)
var jsonGenerator = new JsonReportGenerator(prettyPrint: true);

// 문자열로 생성
string json = jsonGenerator.Generate(report);
Console.WriteLine(json);

// 파일로 저장
string filePath = jsonGenerator.GenerateToFile(report, @"C:\Reports\qa_report.json");
Console.WriteLine($"리포트 저장됨: {filePath}");
```

### 스트림으로 출력

```csharp
var jsonGenerator = new JsonReportGenerator();

using var fileStream = File.Create(@"C:\Reports\qa_report.json");
await jsonGenerator.GenerateToStreamAsync(report, fileStream);
```

### 요약 정보만 생성

전체 이슈 정보 없이 요약만 필요한 경우:

```csharp
var jsonGenerator = new JsonReportGenerator();

string summaryJson = jsonGenerator.GenerateSummary(report);
Console.WriteLine(summaryJson);
```

### JSON 출력 예시

```json
{
  "metadata": {
    "generatedAt": "2023-11-25T14:30:22.1234567+09:00",
    "generator": "TwinCAT Code QA Tool - JsonReportGenerator",
    "version": "1.0.0"
  },
  "project": {
    "sourceFolder": "C:\\Projects\\MyProject\\Source",
    "targetFolder": "C:\\Projects\\MyProject\\Target",
    "totalChanges": 10
  },
  "summary": {
    "totalIssues": 10,
    "hasCriticalIssues": true,
    "severityCounts": {
      "critical": 2,
      "warning": 3,
      "info": 5
    }
  },
  "statistics": {
    "byCategory": [
      {
        "category": "타입 안전성",
        "count": 4,
        "percentage": 40.0
      }
    ],
    "byRule": [
      {
        "ruleId": "SAFETY-001",
        "count": 2
      }
    ],
    "byFile": [
      {
        "filePath": "C:\\Projects\\MyProject\\MAIN.TcPOU",
        "fileName": "MAIN.TcPOU",
        "totalIssues": 5,
        "criticalCount": 2,
        "warningCount": 2,
        "infoCount": 1
      }
    ]
  },
  "issues": [
    {
      "ruleId": "SAFETY-001",
      "severity": "Critical",
      "category": "타입 안전성",
      "title": "ANY 타입 사용 금지",
      "description": "ANY 타입은 타입 안전성을 해칩니다",
      "location": {
        "filePath": "C:\\Projects\\MyProject\\MAIN.TcPOU",
        "fileName": "MAIN.TcPOU",
        "line": 42,
        "locationString": "C:\\Projects\\MyProject\\MAIN.TcPOU:42"
      },
      "details": {
        "whyDangerous": "런타임 타입 변환 오류 가능",
        "recommendation": "구체적인 타입(INT, REAL 등)을 사용하세요",
        "oldCodeSnippet": "VAR\n    myVar : ANY;\nEND_VAR",
        "newCodeSnippet": "VAR\n    myVar : INT;\nEND_VAR",
        "examples": []
      }
    }
  ]
}
```

---

## CI/CD 연동

다양한 CI/CD 도구와 연동하기 위한 포맷터를 제공합니다.

### GitHub Actions

```csharp
using TwinCatQA.Infrastructure.Reports;

// GitHub Actions 어노테이션 형식으로 변환
string annotations = CICDFormatter.ToGitHubActionsAnnotations(report);
Console.WriteLine(annotations);

// 파일로 저장
File.WriteAllText("github_annotations.txt", annotations);
```

**출력 예시:**

```
::error file=C:\Projects\MyProject\MAIN.TcPOU,line=42::[SAFETY-001] ANY 타입 사용 금지
::warning file=C:\Projects\MyProject\FB_Motor.TcPOU,line=15::[NAMING-001] FB_ 접두사 누락
::notice file=C:\Projects\MyProject\MAIN.TcPOU,line=5::[DOC-001] 한글 주석 권장
```

**GitHub Actions 워크플로우 예시:**

```yaml
name: TwinCAT QA Check

on: [push, pull_request]

jobs:
  qa-analysis:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3

      - name: Run TwinCAT QA Tool
        run: |
          dotnet run --project TwinCatQA.CLI -- analyze --source ./src --output report.json

      - name: Generate GitHub Annotations
        run: |
          dotnet run --project TwinCatQA.CLI -- format --input report.json --format github-actions --output annotations.txt
          cat annotations.txt

      - name: Upload Report
        uses: actions/upload-artifact@v3
        with:
          name: qa-report
          path: report.*
```

### Azure DevOps

```csharp
using TwinCatQA.Infrastructure.Reports;

// Azure DevOps 로그 형식으로 변환
string azureLog = CICDFormatter.ToAzureDevOpsLog(report);
Console.WriteLine(azureLog);
```

**출력 예시:**

```
##vso[task.logissue type=error;sourcepath=C:\Projects\MyProject\MAIN.TcPOU;linenumber=42][SAFETY-001] ANY 타입 사용 금지
##vso[task.logissue type=warning;sourcepath=C:\Projects\MyProject\FB_Motor.TcPOU;linenumber=15][NAMING-001] FB_ 접두사 누락
##vso[task.logissue type=info;sourcepath=C:\Projects\MyProject\MAIN.TcPOU;linenumber=5][DOC-001] 한글 주석 권장
```

**Azure Pipeline 예시:**

```yaml
trigger:
  - main

pool:
  vmImage: 'windows-latest'

steps:
- task: DotNetCoreCLI@2
  displayName: 'Run TwinCAT QA Tool'
  inputs:
    command: 'run'
    projects: 'TwinCatQA.CLI'
    arguments: 'analyze --source ./src --output $(Build.ArtifactStagingDirectory)/report.json'

- task: PowerShell@2
  displayName: 'Generate Azure DevOps Logs'
  inputs:
    targetType: 'inline'
    script: |
      dotnet run --project TwinCatQA.CLI -- format --input $(Build.ArtifactStagingDirectory)/report.json --format azure-devops
```

### Jenkins (JUnit XML)

```csharp
using TwinCatQA.Infrastructure.Reports;

// JUnit XML 형식으로 변환
string junitXml = CICDFormatter.ToJUnitXml(report);
File.WriteAllText("test-results.xml", junitXml);
```

**출력 예시:**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<testsuite name="TwinCAT QA Analysis" tests="10" failures="5" errors="0" skipped="5" timestamp="2023-11-25T14:30:22.1234567+09:00">
  <testcase classname="MAIN" name="[SAFETY-001] ANY 타입 사용 금지" time="0">
    <failure message="ANY 타입 사용 금지" type="Critical">
      <![CDATA[
File: C:\Projects\MyProject\MAIN.TcPOU
Line: 42
Category: 타입 안전성
Description: ANY 타입은 타입 안전성을 해칩니다
Why Dangerous: 런타임 타입 변환 오류 가능
Recommendation: 구체적인 타입(INT, REAL 등)을 사용하세요
      ]]>
    </failure>
  </testcase>
</testsuite>
```

**Jenkinsfile 예시:**

```groovy
pipeline {
    agent any

    stages {
        stage('QA Analysis') {
            steps {
                bat 'dotnet run --project TwinCatQA.CLI -- analyze --source ./src --output report.json'
                bat 'dotnet run --project TwinCatQA.CLI -- format --input report.json --format junit --output test-results.xml'
            }
        }
    }

    post {
        always {
            junit 'test-results.xml'
            archiveArtifacts artifacts: 'report.*', fingerprint: true
        }
    }
}
```

---

## 통합 예시

모든 형식의 리포트를 한 번에 생성하는 통합 예시입니다.

```csharp
using TwinCatQA.Domain.Models.QA;
using TwinCatQA.Infrastructure.Reports;

public class ReportGenerationService
{
    private readonly MarkdownReportGenerator _markdownGenerator;
    private readonly JsonReportGenerator _jsonGenerator;

    public ReportGenerationService()
    {
        _markdownGenerator = new MarkdownReportGenerator();
        _jsonGenerator = new JsonReportGenerator(prettyPrint: true);
    }

    /// <summary>
    /// 모든 형식의 리포트 생성
    /// </summary>
    public void GenerateAllReports(QAReport report, string outputDirectory)
    {
        // 출력 디렉토리 생성
        Directory.CreateDirectory(outputDirectory);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var baseName = $"qa_report_{timestamp}";

        // 1. Markdown 리포트
        var mdPath = Path.Combine(outputDirectory, $"{baseName}.md");
        _markdownGenerator.GenerateToFile(report, mdPath);
        Console.WriteLine($"✅ Markdown 리포트 생성: {mdPath}");

        // 2. JSON 리포트
        var jsonPath = Path.Combine(outputDirectory, $"{baseName}.json");
        _jsonGenerator.GenerateToFile(report, jsonPath);
        Console.WriteLine($"✅ JSON 리포트 생성: {jsonPath}");

        // 3. JSON 요약
        var summaryPath = Path.Combine(outputDirectory, $"{baseName}_summary.json");
        var summary = _jsonGenerator.GenerateSummary(report);
        File.WriteAllText(summaryPath, summary);
        Console.WriteLine($"✅ JSON 요약 생성: {summaryPath}");

        // 4. GitHub Actions 어노테이션
        var githubPath = Path.Combine(outputDirectory, $"{baseName}_github.txt");
        var githubAnnotations = CICDFormatter.ToGitHubActionsAnnotations(report);
        File.WriteAllText(githubPath, githubAnnotations);
        Console.WriteLine($"✅ GitHub Actions 어노테이션 생성: {githubPath}");

        // 5. Azure DevOps 로그
        var azurePath = Path.Combine(outputDirectory, $"{baseName}_azure.txt");
        var azureLog = CICDFormatter.ToAzureDevOpsLog(report);
        File.WriteAllText(azurePath, azureLog);
        Console.WriteLine($"✅ Azure DevOps 로그 생성: {azurePath}");

        // 6. JUnit XML
        var junitPath = Path.Combine(outputDirectory, $"{baseName}_junit.xml");
        var junitXml = CICDFormatter.ToJUnitXml(report);
        File.WriteAllText(junitPath, junitXml);
        Console.WriteLine($"✅ JUnit XML 생성: {junitPath}");

        Console.WriteLine($"\n📊 총 6개의 리포트가 생성되었습니다: {outputDirectory}");
    }
}

// 사용 예시
var service = new ReportGenerationService();
service.GenerateAllReports(report, @"C:\Reports\QA");
```

**실행 결과:**

```
✅ Markdown 리포트 생성: C:\Reports\QA\qa_report_20231125_143022.md
✅ JSON 리포트 생성: C:\Reports\QA\qa_report_20231125_143022.json
✅ JSON 요약 생성: C:\Reports\QA\qa_report_20231125_143022_summary.json
✅ GitHub Actions 어노테이션 생성: C:\Reports\QA\qa_report_20231125_143022_github.txt
✅ Azure DevOps 로그 생성: C:\Reports\QA\qa_report_20231125_143022_azure.txt
✅ JUnit XML 생성: C:\Reports\QA\qa_report_20231125_143022_junit.xml

📊 총 6개의 리포트가 생성되었습니다: C:\Reports\QA
```

---

## 참고

- **MarkdownReportGenerator**: GitHub PR/Issue에 첨부하기 적합
- **JsonReportGenerator**: CI/CD 파이프라인 연동 및 자동화 처리에 적합
- **CICDFormatter**: 다양한 CI/CD 도구(GitHub Actions, Azure DevOps, Jenkins)와 연동

모든 리포트 생성기는 한글을 완벽하게 지원하며, UTF-8 인코딩을 사용합니다.
