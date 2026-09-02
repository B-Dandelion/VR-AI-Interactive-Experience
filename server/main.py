from __future__ import annotations

import os
import tempfile
from typing import Optional

from fastapi import FastAPI, File, UploadFile
from fastapi.staticfiles import StaticFiles
from faster_whisper import WhisperModel
from pydantic import BaseModel

from voice_to_gemini import (
    TTS_DIR,
    WHISPER_COMPUTE,
    WHISPER_DEVICE,
    WHISPER_MODEL_SIZE,
    run_gemini,
    tts_to_file,
)


app = FastAPI(
    title="VR AI Voice Server",
    description="Voice interaction backend for the Unity/Meta Quest VR prototype.",
)

# Edge-TTS output is served back to the Unity client through /tts/<filename>.
app.mount("/tts", StaticFiles(directory=str(TTS_DIR)), name="tts")

print("[server] Loading faster-whisper model...")
wmodel = WhisperModel(
    WHISPER_MODEL_SIZE,
    device=WHISPER_DEVICE,
    compute_type=WHISPER_COMPUTE,
)
print("[server] faster-whisper ready")


class AskRequest(BaseModel):
    text: str


class AskResponse(BaseModel):
    answer: str


class AskAudioResponse(BaseModel):
    answer: str
    audio_url: str


@app.post("/ask_audio", response_model=AskAudioResponse)
async def ask_audio(file: UploadFile = File(...)) -> AskAudioResponse:
    """Process one Unity voice request end-to-end.

    Unity sends a WAV file as multipart/form-data. The server transcribes it,
    generates a short response, creates TTS audio, and returns the text plus
    the relative URL of the generated audio file.
    """

    tmp_path: Optional[str] = None

    try:
        with tempfile.NamedTemporaryFile(delete=False, suffix=".wav") as tmp:
            data = await file.read()
            tmp.write(data)
            tmp_path = tmp.name
        print(f"[ask_audio] received {len(data)} bytes")
    except Exception as exc:
        print("[ask_audio] failed to save uploaded audio:", repr(exc))
        return AskAudioResponse(
            answer="업로드된 음성을 처리하는 중 오류가 발생했습니다.",
            audio_url="",
        )

    try:
        try:
            segments, _ = wmodel.transcribe(
                tmp_path,
                language="ko",
                beam_size=5,
                vad_filter=True,
                vad_parameters={"min_silence_duration_ms": 250},
                condition_on_previous_text=False,
            )
            text = "".join(segment.text for segment in segments).strip()
            print(f"[ask_audio] transcript={text!r}")
        except Exception as exc:
            print("[ask_audio] STT failed:", repr(exc))
            return AskAudioResponse(
                answer="음성을 텍스트로 변환하는 중 오류가 발생했습니다.",
                audio_url="",
            )

        if not text:
            return AskAudioResponse(
                answer="음성에서 텍스트를 인식하지 못했습니다.",
                audio_url="",
            )

        try:
            answer = run_gemini(text)
        except Exception as exc:
            print("[ask_audio] Gemini request failed:", repr(exc))
            return AskAudioResponse(
                answer="답변을 생성하는 중 오류가 발생했습니다.",
                audio_url="",
            )

        try:
            audio_url = await tts_to_file(answer)
        except Exception as exc:
            print("[ask_audio] TTS failed:", repr(exc))
            return AskAudioResponse(
                answer="음성 답변을 생성하는 중 오류가 발생했습니다.",
                audio_url="",
            )

        return AskAudioResponse(answer=answer, audio_url=audio_url)

    finally:
        # The original prototype left temporary uploads behind during debugging.
        # The portfolio version cleans them after each request.
        if tmp_path and os.path.exists(tmp_path):
            try:
                os.remove(tmp_path)
            except OSError as exc:
                print("[ask_audio] temporary file cleanup failed:", repr(exc))


@app.post("/ask", response_model=AskResponse)
async def ask(req: AskRequest) -> AskResponse:
    """Text-only endpoint used for quick server-side integration tests."""
    return AskResponse(answer=run_gemini(req.text))


@app.get("/")
def root() -> dict[str, str]:
    return {"message": "VR AI voice server is running"}


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host="0.0.0.0", port=8000)
