# Commands.png 아이콘 생성 가이드

## 개요

Visual Studio Extension에서 사용할 명령어 아이콘 이미지를 생성하는 방법을 설명합니다.

## 요구사항

- **파일명**: Commands.png
- **크기**: 64x16 픽셀
- **형식**: PNG (24비트 또는 32비트 RGBA)
- **레이아웃**: 4개의 16x16 아이콘을 가로로 배열

```
[Icon 1][Icon 2][Icon 3][Icon 4]
 16x16   16x16   16x16   16x16
<------------ 64px ------------>
```

## 아이콘 순서

1. **bmpAnalyze** (0-15px): 현재 파일 분석
   - 권장: 돋보기, 문서 + 체크마크

2. **bmpAnalyzeProject** (16-31px): 전체 프로젝트 분석
   - 권장: 폴더 + 돋보기, 다중 문서

3. **bmpToolWindow** (32-47px): 도구 창 표시
   - 권장: 창 아이콘, 패널 아이콘

4. **bmpSettings** (48-63px): 설정
   - 권장: 톱니바퀴, 슬라이더

## 방법 1: Visual Studio Image Library 사용 (권장)

Visual Studio 설치 시 제공되는 Image Library를 활용합니다.

### 위치
```
C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\VS2022ImageLibrary\VS2022ImageLibrary.zip
```

### 단계
1. VS2022ImageLibrary.zip 압축 해제
2. `Actions\png_f\DevEnv\16\` 폴더 이동
3. 적절한 아이콘 선택:
   - `SearchDocument_16x.png` (분석)
   - `SearchFolder_16x.png` (프로젝트 분석)
   - `Window_16x.png` (도구 창)
   - `Settings_16x.png` (설정)

4. 이미지 편집 도구로 4개 아이콘을 64x16 이미지로 결합

## 방법 2: PowerShell 스크립트로 자동 생성

### 스크립트: create-icons.ps1

```powershell
# TwinCAT QA 명령어 아이콘 생성 스크립트
# 64x16 크기의 Commands.png 생성

param(
    [string]$OutputPath = "D:\01. Vscode\Twincat\features\twincat-code-qa-tool\src\TwinCatQA.VsExtension\Resources\Commands.png",
    [string]$VSImageLibPath = "C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\VS2022ImageLibrary\VS2022ImageLibrary"
)

# .NET 이미지 처리 로드
Add-Type -AssemblyName System.Drawing

# 아이콘 배열
$icons = @(
    "$VSImageLibPath\Actions\png_f\DevEnv\16\SearchDocument_16x.png",
    "$VSImageLibPath\Actions\png_f\DevEnv\16\SearchFolder_16x.png",
    "$VSImageLibPath\Actions\png_f\DevEnv\16\Window_16x.png",
    "$VSImageLibPath\Actions\png_f\DevEnv\16\Settings_16x.png"
)

# 64x16 비트맵 생성
$bitmap = New-Object System.Drawing.Bitmap(64, 16)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# 투명 배경
$graphics.Clear([System.Drawing.Color]::Transparent)

# 각 아이콘을 위치에 배치
$x = 0
foreach ($iconPath in $icons) {
    if (Test-Path $iconPath) {
        $icon = [System.Drawing.Image]::FromFile($iconPath)
        $graphics.DrawImage($icon, $x, 0, 16, 16)
        $icon.Dispose()
    } else {
        Write-Warning "아이콘을 찾을 수 없음: $iconPath"
        # 기본 사각형 그리기
        $graphics.DrawRectangle([System.Drawing.Pens]::Gray, $x, 0, 15, 15)
    }
    $x += 16
}

# 저장
$bitmap.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bitmap.Dispose()
$graphics.Dispose()

Write-Host "아이콘 생성 완료: $OutputPath"
```

### 실행
```powershell
cd "D:\01. Vscode\Twincat\features\twincat-code-qa-tool\docs"
.\create-icons.ps1
```

## 방법 3: 온라인 도구 사용

### Photopea (무료 온라인 포토샵)
1. https://www.photopea.com/ 접속
2. 파일 > 새로 만들기 > 64x16 픽셀
3. 각 16x16 영역에 아이콘 배치
4. 파일 > 내보내기 > PNG

### Figma (무료)
1. https://www.figma.com/ 접속
2. 64x16 프레임 생성
3. 아이콘 라이브러리에서 아이콘 추가
4. Export as PNG

## 방법 4: 간단한 대체 아이콘 생성

아이콘 없이 단색 사각형으로 플레이스홀더 생성:

```python
# Python + PIL 사용
from PIL import Image, ImageDraw, ImageFont

# 64x16 이미지 생성
img = Image.new('RGBA', (64, 16), (0, 0, 0, 0))
draw = ImageDraw.Draw(img)

# 4개의 구별되는 색상 사각형
colors = [(100, 149, 237), (46, 139, 87), (255, 165, 0), (220, 20, 60)]
for i, color in enumerate(colors):
    x = i * 16
    draw.rectangle([x, 0, x + 15, 15], fill=color)

img.save('Commands.png')
print('Commands.png 생성 완료')
```

실행:
```bash
python create_icons.py
```

## 방법 5: VSCT 수정하여 KnownMonikers 사용 (아이콘 파일 불필요)

Commands.png 파일이 없을 경우, VSCT 파일을 수정하여 Visual Studio의 내장 아이콘을 사용할 수 있습니다.

### TwinCatQAPackage.vsct 수정

```xml
<!-- 기존 아이콘 참조 대신 -->
<Button guid="guidTwinCatQAPackageCmdSet" id="AnalyzeCurrentFileCommandId" ...>
  <!-- 기존: <Icon guid="guidImages" id="bmpAnalyze"/> -->

  <!-- 변경: KnownMoniker 사용 -->
  <Icon guid="ImageCatalogGuid" id="DocumentSearch"/>
  <CommandFlag>IconIsMoniker</CommandFlag>
  ...
</Button>
```

### 권장 KnownMonikers

| 명령어 | KnownMoniker | 설명 |
|--------|--------------|------|
| 현재 파일 분석 | `DocumentSearch` | 문서 검색 아이콘 |
| 전체 프로젝트 분석 | `SearchFolder` | 폴더 검색 아이콘 |
| 도구 창 표시 | `ToolWindow` | 도구 창 아이콘 |
| 설정 | `Settings` | 설정 아이콘 |

### 전체 수정 예시

```xml
<Buttons>
  <Button guid="guidTwinCatQAPackageCmdSet" id="AnalyzeCurrentFileCommandId" priority="0x0100" type="Button">
    <Parent guid="guidTwinCatQAPackageCmdSet" id="TwinCatQASubMenuGroup"/>
    <Icon guid="ImageCatalogGuid" id="DocumentSearch"/>
    <CommandFlag>IconIsMoniker</CommandFlag>
    <Strings>
      <ButtonText>현재 파일 분석</ButtonText>
    </Strings>
  </Button>

  <Button guid="guidTwinCatQAPackageCmdSet" id="AnalyzeProjectCommandId" priority="0x0101" type="Button">
    <Parent guid="guidTwinCatQAPackageCmdSet" id="TwinCatQASubMenuGroup"/>
    <Icon guid="ImageCatalogGuid" id="SearchFolder"/>
    <CommandFlag>IconIsMoniker</CommandFlag>
    <Strings>
      <ButtonText>전체 프로젝트 분석</ButtonText>
    </Strings>
  </Button>

  <Button guid="guidTwinCatQAPackageCmdSet" id="ShowToolWindowCommandId" priority="0x0102" type="Button">
    <Parent guid="guidTwinCatQAPackageCmdSet" id="TwinCatQASubMenuGroup"/>
    <Icon guid="ImageCatalogGuid" id="ToolWindow"/>
    <CommandFlag>IconIsMoniker</CommandFlag>
    <Strings>
      <ButtonText>QA 결과 창 표시</ButtonText>
    </Strings>
  </Button>

  <Button guid="guidTwinCatQAPackageCmdSet" id="SettingsCommandId" priority="0x0103" type="Button">
    <Parent guid="guidTwinCatQAPackageCmdSet" id="TwinCatQASubMenuGroup"/>
    <Icon guid="ImageCatalogGuid" id="Settings"/>
    <CommandFlag>IconIsMoniker</CommandFlag>
    <Strings>
      <ButtonText>설정...</ButtonText>
    </Strings>
  </Button>
</Buttons>

<!-- Bitmaps 섹션 제거 또는 주석 처리 -->
<!--
<Bitmaps>
  <Bitmap guid="guidImages" href="Resources\Commands.png" usedList="bmpAnalyze, bmpAnalyzeProject, bmpToolWindow, bmpSettings"/>
</Bitmaps>
-->
```

## 검증

생성된 이미지를 확인하세요:

```powershell
# 파일 존재 확인
Test-Path "D:\01. Vscode\Twincat\features\twincat-code-qa-tool\src\TwinCatQA.VsExtension\Resources\Commands.png"

# 이미지 정보 확인
Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Image]::FromFile("D:\01. Vscode\Twincat\features\twincat-code-qa-tool\src\TwinCatQA.VsExtension\Resources\Commands.png")
Write-Host "크기: $($img.Width)x$($img.Height)"
$img.Dispose()
```

예상 출력:
```
크기: 64x16
```

## 권장 사항

1. **개발 단계**: KnownMonikers 사용 (가장 빠르고 간단)
2. **배포 단계**: Visual Studio Image Library의 공식 아이콘 사용
3. **브랜딩 필요 시**: 커스텀 아이콘 디자인 및 생성

## 추가 리소스

- [Visual Studio Image Service](https://docs.microsoft.com/visualstudio/extensibility/image-service-and-catalog)
- [KnownMonikers 전체 목록](https://docs.microsoft.com/dotnet/api/microsoft.visualstudio.imaging.knownmonikers)
- [VS Image Library 다운로드](https://marketplace.visualstudio.com/items?itemName=VisualStudioClient.MicrosoftVisualStudio2022ImageLibrary)
