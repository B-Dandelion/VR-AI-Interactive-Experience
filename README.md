# VR AI Interactive Experience

> **Unity/C# 기반 Meta Quest VR 콘텐츠와 음성 AI 서버를 end-to-end로 연결한 팀 프로젝트**

세종대학교 AR/VR/MR 수업에서 진행한 4인 팀 프로젝트입니다. 사용자가 VR 환경에서 인체 소화기관을 탐험하며 음성으로 질문하면, Unity 클라이언트가 녹음한 음성을 FastAPI 서버로 전송하고 **STT → LLM → TTS** 파이프라인을 거쳐 생성된 답변을 다시 VR에서 음성으로 재생합니다.

## Project Overview

- **기간**: 2025-2학기
- **형태**: 4인 팀 프로젝트
- **플랫폼**: Meta Quest 2 / Quest 3
- **Unity**: 2023.2.6f1
- **핵심 목표**: VR 입력과 외부 AI 서비스를 하나의 음성 상호작용 흐름으로 통합
- **현재 상태**: 포트폴리오용 소스 정리 완료 중 / 로컬 실행 및 Quest 재검증 예정

## System Flow

```text
Meta Quest / Unity
        │
        │ Controller input
        ▼
Unity Microphone Recording
        │
        │ AudioClip → 16-bit PCM WAV
        ▼
UnityWebRequest (multipart/form-data)
        │
        ▼
FastAPI  POST /ask_audio
        │
        ├─ faster-whisper : Korean STT
        ├─ Gemini         : response generation
        └─ Edge TTS       : MP3 generation
        │
        ▼
{ answer, audio_url }
        │
        ▼
Unity downloads and plays generated audio
```

## My Contribution

본 프로젝트에서 **Unity/C# 기반 음성 입력·서버 통신 기능과 AI 서버 파이프라인 전반**을 담당했습니다.

### Unity / C#

- Meta Quest 컨트롤러 입력과 마이크 녹음 동작 연결
- `AudioClip` 녹음 데이터 추출 및 16-bit PCM WAV 변환
- `UnityWebRequest` 기반 음성 파일 업로드 및 서버 응답 처리
- JSON 응답 파싱, 생성 음성 다운로드 및 `AudioSource` 재생
- Listening → Thinking → Speaking 상태 UI와 오류 상태 연결
- XR Interaction Toolkit 기반 텔레포트 및 스테이지 이동 상태 로직 구현
- 이동 안내 음성과 AI 답변 음성이 겹치지 않도록 오디오 상태 제어

### Python / AI Server

- FastAPI 기반 음성 요청 API 구현
- faster-whisper 기반 한국어 STT 파이프라인 구성
- Gemini API 기반 인체 설명 응답 생성
- Edge TTS 기반 음성 응답 파일 생성 및 정적 URL 제공
- Unity 클라이언트 ↔ 서버 간 end-to-end 연동 및 통합 디버깅

> 그래픽 에셋 배치, 환경 디자인 및 이동 버그 방지를 위한 일부 레벨 콜라이더 구성은 다른 팀원이 담당했습니다.

## Repository Structure

```text
VR-AI-Interactive-Experience/
├─ Assets/                 # Unity scenes, assets and C# scripts
├─ Packages/               # Unity package manifest
├─ ProjectSettings/        # Unity project settings
├─ server/                 # Cleaned FastAPI / STT / Gemini / TTS backend
│  ├─ main.py
│  ├─ voice_to_gemini.py
│  ├─ requirements.txt
│  ├─ .env.example
│  └─ README.md
├─ .gitignore
└─ README.md
```

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
`Python` · `FastAPI` · `faster-whisper` · `Gemini API` · `Edge TTS`

## Server

공개용 서버 코드는 [`server/`](./server) 디렉터리에 정리했습니다. 원래 private 서버 저장소의 최종 `Jin` 브랜치를 기준으로 핵심 파이프라인을 복원하되, 다음 항목은 공개 저장소에서 제외했습니다.

- API key 및 `.env` 파일
- Python virtual environment
- 생성된 TTS MP3 파일
- 로컬 테스트 산출물
- private 서버 저장소의 기존 Git history

실행 방법은 [`server/README.md`](./server/README.md)를 참고합니다.

## What I Learned

이 프로젝트에서는 생성형 AI API 자체보다 **VR 클라이언트와 외부 AI 시스템을 실제 사용자 인터랙션으로 연결하는 과정**에 집중했습니다. VR 입력 → 오디오 데이터 처리 → 네트워크 요청 → STT/LLM/TTS 처리 → VR 음성 재생까지 이어지는 전체 흐름을 구현하며 Unity 런타임과 Python 서버 사이의 상태 관리와 통합 디버깅을 경험했습니다.

## Repository Note

이 프로젝트는 세종대학교 AR/VR/MR 수업에서 조교가 제공한 **Unity XR 기본 템플릿**을 기반으로 시작했습니다. 기본 XR 프로젝트 설정 위에 팀 콘텐츠를 구현했으며, 본 저장소는 포트폴리오 제출을 위해 프로젝트 구조와 본인 담당 코드를 정리한 버전입니다.

원본 팀 프로젝트 저장소: `leejaewook-dev/VR-Project---sejong`

---

### Restoration Status

- [x] 팀 최종 Unity 소스 복원
- [x] private 서버 최종 브랜치 기반 공개용 서버 코드 정리
- [x] secret / generated file 제외 규칙 정리
- [ ] Unity 2023.2.6f1 로컬 실행 확인
- [ ] FastAPI + STT + Gemini + TTS 재실행 확인
- [ ] Quest 또는 XR 환경에서 end-to-end 동작 재검증
- [ ] 데모 영상 / GIF 및 시스템 구조도 추가
