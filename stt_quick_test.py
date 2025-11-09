import time
import numpy as np
import sounddevice as sd
from faster_whisper import WhisperModel

SAMPLE_RATE = 16000
CHANNELS = 1
MODEL_SIZE = "small"
DEVICE = "cpu"
COMPUTE_TYPE = "int8"
DEFAULT_INPUT_DEVICE = 1  # 네 이어폰 마이크

def record_audio(seconds=5, device_index=None):
    print(f"🎙️ {seconds}초 녹음 시작 (device={device_index}) ...")
    audio = sd.rec(int(seconds * SAMPLE_RATE),
                   samplerate=SAMPLE_RATE,
                   channels=CHANNELS,
                   dtype="float32",
                   device=device_index)
    sd.wait()
    print("🛑 녹음 종료")
    return audio.flatten()

def transcribe_audio(audio_data):
    print("🧠 Whisper 로딩 중...")
    model = WhisperModel(MODEL_SIZE, device=DEVICE, compute_type=COMPUTE_TYPE)
    print("✅ 모델 준비 완료")

    print("🔎 텍스트 변환 중...")
    # numpy 데이터를 바로 STT에 넘기기
    segments, info = model.transcribe(audio_data, language="ko", beam_size=5)
    text = "".join(seg.text for seg in segments).strip()

    print("\n🗣️ 인식 결과 =====================")
    print(text if text else "(인식된 텍스트 없음)")
    print("=================================")

    # 결과를 stt_result.py에 저장
    with open("stt_result.py", "w", encoding="utf-8") as f:
        f.write("# 자동 생성된 STT 결과 파일\n\n")
        f.write(f"stt_text = '''{text}'''\n")
    print("💾 결과가 stt_result.py 파일에 저장되었습니다!")

    return text

if __name__ == "__main__":
    audio = record_audio(seconds=5, device_index=DEFAULT_INPUT_DEVICE)
    transcribe_audio(audio)
