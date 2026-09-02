# Voice AI Server

FastAPI backend used by the Unity/Meta Quest prototype.

## Request flow

```text
Unity microphone input
  -> PCM/WAV encoding
  -> POST /ask_audio
  -> faster-whisper STT
  -> Gemini response generation
  -> Edge TTS MP3
  -> { answer, audio_url }
  -> Unity downloads and plays the generated audio
```

## Local setup

```bash
cd server
python -m venv .venv
```

Activate the virtual environment, then install dependencies:

```bash
pip install -r requirements.txt
```

Copy `.env.example` to `.env` and set your own Gemini API key.

```bash
uvicorn main:app --host 0.0.0.0 --port 8000
```

For a Quest device on the same local network, configure the Unity client to use the development PC's LAN address, for example:

```text
http://<PC-LAN-IP>:8000/ask_audio
```

## Endpoints

- `POST /ask_audio` — accepts a WAV upload and returns generated text plus a relative TTS audio URL.
- `POST /ask` — text-only integration test endpoint.
- `GET /` — health/status response.
- `/tts/*` — generated TTS audio served as static files.

## Notes

This directory is a cleaned portfolio version reconstructed from the project's final `Jin` server branch. Secrets, virtual environments, generated MP3 files, and the original private server repository history are intentionally excluded.
