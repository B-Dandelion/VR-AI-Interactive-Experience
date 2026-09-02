# VR AI Interactive Experience

> **Unity/C# 기반 Meta Quest VR 콘텐츠에 음성 AI 상호작용을 연결한 팀 프로젝트**

세종대학교 AR/VR/MR 수업에서 진행한 팀 프로젝트입니다. 사용자가 VR 환경에서 인체 소화기관을 탐험하면서 음성으로 질문하면, Unity 클라이언트가 음성을 서버로 전송하고 STT → LLM → TTS 파이프라인을 거쳐 생성된 답변을 다시 VR에서 음성으로 재생합니다.

## Project Overview

- **기간**: 2025-2학기
- **형태**: 4인 팀 프로젝트
- **플랫폼**: Meta Quest 2 / Quest 3
- **Unity**: 2023.2.6f1
- **핵심 목표**: VR 콘텐츠와 외부 AI 서비스 사이의 실시간 음성 상호작용 구현

### Interaction Flow

```text
Meta Quest / Unity
  ↓  Controller input
Microphone recording
  ↓  PCM → WAV
HTTP audio upload
  ↓
Python API Server
  ↓
faster-whisper (STT)
  ↓
Gemini 2.5 Flash (response generation)
  ↓
TTS
  ↓  generated audio
Unity audio playback
```

## My Contribution

본 프로젝트에서 **Unity/C# 기반 음성 입력·서버 통신 기능과 AI 서버 파이프라인 전반을 담당**했습니다.

### Unity / C#

- Meta Quest 컨트롤러 입력과 마이크 녹음 동작 연결
- `AudioClip` 녹음 데이터 추출 및 WAV 변환
- `UnityWebRequest` 기반 음성 파일 업로드 및 서버 응답 처리
- JSON 응답 파싱, 생성 음성 다운로드 및 `AudioSource` 재생
- Listening → Thinking → Speaking 상태 UI 연결 및 오류 처리
- XR Interaction Toolkit 기반 텔레포트 및 스테이지 이동 상태 로직 구현
- 이동/AI 안내 음성이 겹치지 않도록 오디오 상태 제어

### AI / Server Integration

- Python 기반 음성 처리 파이프라인 구성
- faster-whisper를 이용한 한국어 STT
- Gemini API를 이용한 질의응답 생성
- TTS 결과를 Unity 클라이언트가 재생할 수 있도록 연동
- Unity 클라이언트 ↔ AI 서버 end-to-end 통신 및 통합 디버깅

> 그래픽 에셋 배치, 환경 디자인 및 이동 버그 방지를 위한 일부 레벨 콜라이더 구성은 다른 팀원이 담당했습니다.

## Key C# Implementations

| File | Responsibility |
| --- | --- |
| `Assets/MicRecorder.cs` | 마이크 녹음, 오디오 전처리, HTTP 요청, 서버 응답 및 생성 음성 재생 |
| `Assets/MicRecoderInput.cs` | XR Input Action과 녹음 시작/종료 연결 |
| `Assets/WavUtility.cs` | Unity `AudioClip` 데이터를 16-bit PCM WAV로 직렬화 |
| `Assets/XRTP.cs` | XR 텔레포트 및 스테이지/오디오 상태 제어 |
| `Assets/nscript/TeleportManager.cs` | 현재 스테이지 복귀 및 전체 초기 위치 이동 로직 |
| `Assets/nscript/VoiceStatusUI.cs` | 녹음·처리·재생·오류 상태 UI 관리 |

## Tech Stack

**Client / XR**  
`Unity 2023.2.6f1` · `C#` · `XR Interaction Toolkit 2.5.4` · `OpenXR` · `Oculus XR` · `URP`

**AI / Server**  
`Python` · `FastAPI` · `faster-whisper` · `Gemini API` · `TTS`

## What I Learned

이 프로젝트에서는 생성형 AI API 자체보다, **VR 클라이언트와 외부 AI 시스템을 하나의 사용자 경험으로 연결하는 과정**에 집중했습니다. 특히 VR 입력 → 오디오 데이터 처리 → 네트워크 요청 → 비동기 응답 → VR 재생까지 이어지는 흐름을 구현하며 Unity 런타임과 서버 사이의 상태 관리와 통합 디버깅을 경험했습니다.

## Repository Note

이 프로젝트는 세종대학교 AR/VR/MR 수업에서 조교가 제공한 **Unity XR 기본 템플릿을 기반으로 시작**했습니다. 기본 XR 프로젝트 설정을 토대로 팀 콘텐츠를 개발했으며, 본 저장소는 포트폴리오 제출을 위해 프로젝트 구조와 구현 내용을 정리하는 버전입니다.

원본 팀 프로젝트 저장소: `leejaewook-dev/VR-Project---sejong`

---

### Portfolio refurbishment

현재 저장소는 기존 수업 프로젝트를 다시 실행 가능한 상태로 복구하고, 코드 및 문서를 취업용 포트폴리오 형태로 정리하는 중입니다. 최종 제출 전 실행 영상, 시스템 구조도 및 실행 방법을 추가할 예정입니다.
