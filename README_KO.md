# OverlayPlugin

Advanced Combat Tracker에 사용자 지정 가능한 오버레이를 표시하는 플러그인입니다.

## 다운로드

[Releases 페이지][releases]에서 배포판과 시험판을 다운로드할 수 있습니다.

## 시스템 요구사항

* .NET Framework 4.7.1
* MS Visual C++ Redistributable for Visual Studio 2019

## 사용법

이 플러그인을 사용하려면 ACT의 플러그인 탭에 `OverlayPlugin.dll`을 추가하세요. 다른 파일들도 중요하므로, 이 파일만 옮기면 안됩니다.

ACT에서 `Plugins` 탭으로 이동한 다음 `OverlayPlugin.dll` 탭으로 이동합니다. 거기에 모든 오버레이 관련 설정이 있습니다.

[OBS에 오버레이를 표시하고 싶은 사용자를 위한 가이드](https://overlayplugin.github.io/OverlayPlugin/streamers_ko)도 있습니다.

## 오버레이

* [널리 사용되는 오버레이 목록](https://gist.github.com/ngld/e2217563bbbe1750c0917217f136687d#overlays)
* [나만의 오버레이 만들기 가이드](https://overlayplugin.github.io/OverlayPlugin/devs/)


## 문제 해결

[ACT Discord의 문제 해결을 위한 FAQ](https://gist.github.com/ngld/e2217563bbbe1750c0917217f136687d)를 참고하세요.

## 빌드 방법

이 지침은 플러그인을 수정하려는 경우에만 해당됩니다. 플러그인을 설치하려면 [Releases 페이지][releases]로 이동하세요.

* 설치 프로그램을 통해 Visual Studio 2019와 .NET 데스크톱 구성 요소를 설치합니다. (이전 버전도 동작할 수 있으나 테스트되지 않음)
* git으로 소스 코드를 가져오거나 ZIP으로 소스 코드를 다운로드하고 압축을 풉니다.
* `tools/fetch_deps.py`를 실행합니다.
* `build.bat`를 실행합니다.

완료되면 플러그인 파일 `OverlayPlugin.dll`이 `out` 폴더에 생성됩니다. 빌드 설정에 따라 `Release` 또는 `Debug` 하위 폴더에 있습니다.

## 라이센스

MIT 라이센스. 자세한 내용은 LICENSE.txt를 참고하세요.

[releases]: https://github.com/OverlayPlugin/OverlayPlugin/releases
