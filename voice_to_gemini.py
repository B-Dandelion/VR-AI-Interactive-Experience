import os
import time
import numpy as np
import sounddevice as sd
from dotenv import load_dotenv
from faster_whisper import WhisperModel
import google.generativeai as genai

# ========= 설정 =========
SAMPLE_RATE = 16000
CHANNELS = 1
RECORD_SEC = 5                 # 말할 시간 (초)
INPUT_DEVICE = 1               # 마이크 인덱스 (예: 1 = AB13X USB Audio, 필요시 15로 변경)
WHISPER_MODEL_SIZE = "small"   # tiny/base/small/medium/large-v3
WHISPER_DEVICE = "cpu"         # GPU 있으면 "cuda"
WHISPER_COMPUTE = "int8"       # GPU면 "float16"
GEMINI_MODEL_ID = "models/gemini-2.5-flash"  # 네 계정에서 사용 가능 모델

# ========= 유틸 =========
def record_audio(seconds=RECORD_SEC, device_index=INPUT_DEVICE):
    print(f"\n🎙️ {seconds}초 녹음 시작 (device={device_index}) ...")
    audio = sd.rec(int(seconds * SAMPLE_RATE),
                   samplerate=SAMPLE_RATE,
                   channels=CHANNELS,
                   dtype="float32",
                   device=device_index)
    sd.wait()
    print("🛑 녹음 종료")
    return audio.reshape(-1)  # 1D PCM float32

def transcribe_audio(model: WhisperModel, audio_1d: np.ndarray) -> str:
    print("🧠 STT 변환 중...")
    segments, _ = model.transcribe(
        audio_1d,
        language="ko",               # 자동감지는 None
        beam_size=5,
        vad_filter=True,
        vad_parameters=dict(min_silence_duration_ms=250),
        condition_on_previous_text=False,
    )
    text = "".join(s.text for s in segments).strip()
    if text:
        print(f"🗣️ 인식 결과: {text}")
    else:
        print("🗣️ (인식된 텍스트 없음)")
    return text

def ask_gemini(model, prompt: str) -> str:
    print("🤖 Gemini 응답 생성 중...\n")
    resp = model.generate_content(prompt)
    return (resp.text or "").strip()

def save_stt_result(text: str, path="stt_result.py"):
    with open(path, "w", encoding="utf-8") as f:
        f.write("# 자동 생성된 STT 결과 파일\n\n")
        f.write(f"stt_text = '''{text}'''\n")
    print(f"💾 STT 결과 저장: {path}")

# ========= 메인 =========
def main():
    # 1) 키 로드 (.env.stt 파일 사용)
    load_dotenv(".env.stt")
    api_key = os.getenv("GEMINI_API_KEY")
    if not api_key:
        raise ValueError("❌ GEMINI_API_KEY를 찾을 수 없습니다. .env.stt 파일을 확인하세요!")

    # 2) Gemini 준비 (REST 고정: 일부 환경 gRPC 이슈 회피)
    genai.configure(api_key=api_key, transport="rest")
    gmodel = genai.GenerativeModel(GEMINI_MODEL_ID)

    # 3) Whisper 준비 (1회 로드)
    print("📦 Whisper 모델 로딩 중...")
    wmodel = WhisperModel(WHISPER_MODEL_SIZE, device=WHISPER_DEVICE, compute_type=WHISPER_COMPUTE)
    print("✅ Whisper 준비 완료!")

    print("\n====== 음성 → STT → Gemini 통합 ======")
    print("Enter: 녹음 시작(고정 길이) / q + Enter: 종료")

    try:
        while True:
            cmd = input("\n▶ Enter를 누르면 녹음합니다 (종료: q): ").strip().lower()
            if cmd == "q":
                print("종료합니다.")
                break

            # 4) 녹음
            audio = record_audio()

            # 5) STT
            text = transcribe_audio(wmodel, audio)
            if not text:
                continue

            # (선택) STT 결과 보관
            save_stt_result(text)

            # 6) Gemini 응답
            reply = ask_gemini(gmodel, text)
            if reply:
                print("\n===== Gemini =====")
                print(reply)
                print("==================")
            else:
                print("⚠️ Gemini 응답이 비어있습니다.")

            # 약간의 쿨다운 (선택)
            time.sleep(0.2)

    except KeyboardInterrupt:
        print("\n🛑 사용자 중지")

if __name__ == "__main__":
    main()
