from __future__ import annotations

import os
import re
import uuid
from pathlib import Path

import edge_tts
import google.generativeai as genai
from dotenv import load_dotenv


# Load local environment variables when present. Secrets are never committed.
load_dotenv()

WHISPER_MODEL_SIZE = os.getenv("WHISPER_MODEL_SIZE", "small")
WHISPER_DEVICE = os.getenv("WHISPER_DEVICE", "cpu")
WHISPER_COMPUTE = os.getenv("WHISPER_COMPUTE", "int8")

BASE_DIR = Path(__file__).resolve().parent
TTS_DIR = BASE_DIR / "tts_out"
TTS_DIR.mkdir(parents=True, exist_ok=True)

EDGE_TTS_VOICE = os.getenv("EDGE_TTS_VOICE", "ko-KR-SunHiNeural")

_GENAI_API_KEY = os.getenv("GENAI_API_KEY") or os.getenv("GEMINI_API_KEY")
if not _GENAI_API_KEY:
    print("[voice_to_gemini] GEMINI_API_KEY is not configured.")

genai.configure(api_key=_GENAI_API_KEY)
_GEMINI_MODEL_NAME = os.getenv("GEMINI_MODEL_NAME", "gemini-2.5-flash")
_gemini_model = genai.GenerativeModel(_GEMINI_MODEL_NAME)


def clean_reply(text: str) -> str:
    """Normalize generated text before it is spoken inside the VR scene."""
    if not text:
        return ""

    text = re.sub(r"[\u2600-\u27BF\uFE0F\u200D]+", "", text)
    text = re.sub(r"[*_~`]+", "", text)
    text = re.sub(r"[!?！？]{2,}", "!", text)
    text = re.sub(r"[…]{2,}", "…", text)
    return re.sub(r"\s{2,}", " ", text).strip()


def run_gemini(text: str) -> str:
    """Generate a concise explanation suitable for the human-body VR experience."""
    prompt = (
        "너는 VR 인체 탐험 체험에서 사용자에게 장기와 인체 구조를 설명하는 AI다.\n"
        "역할과 규칙:\n"
        "1. 사용자의 질문은 기본적으로 인체 장기나 인체 구조에 관한 것으로 가정한다.\n"
        "2. '위', '간', '심장', '폐'처럼 인체 장기 이름과 동일한 한국어 단어가 나오면 "
        "인체 장기 의미를 우선적으로 가정한다.\n"
        "3. 두 가지 이상으로 해석 가능한 모호한 표현이면 한 문장으로 짧게 의미를 되묻는다.\n"
        "4. 중학생도 이해할 수 있도록 2~4문장으로 차분하게 설명한다.\n"
        "5. 이모지, 특수기호, 과장된 말투, 마크다운 기호는 사용하지 않는다.\n"
        f"사용자 발화: {text}"
    )

    response = _gemini_model.generate_content(prompt)
    answer = clean_reply((response.text or "").strip())

    if answer:
        return answer

    # Keep a simple fallback for cases where the model returns no printable text.
    candidates = getattr(response, "candidates", None) or []
    if candidates:
        try:
            answer = clean_reply(candidates[0].content.parts[0].text)
        except Exception:
            answer = ""

    return answer or "죄송하지만, 지금은 적절한 답변을 생성하지 못했습니다."


async def tts_to_file(text: str) -> str:
    """Generate an MP3 response and return the relative FastAPI static URL."""
    text = (text or "").strip()
    if not text:
        raise ValueError("Cannot generate TTS from empty text.")

    filename = f"{uuid.uuid4().hex}.mp3"
    out_path = TTS_DIR / filename

    communicate = edge_tts.Communicate(text, EDGE_TTS_VOICE)
    await communicate.save(str(out_path))

    return f"/tts/{filename}"


def run_from_text(text: str) -> str:
    """Compatibility wrapper retained from the original prototype."""
    return run_gemini(text)
