import os
from dotenv import load_dotenv
import google.generativeai as genai
from stt_result import stt_text

# .env.stt 파일 로드
load_dotenv(".env.stt")

# 환경변수에서 API 키 가져오기
api_key = os.getenv("GEMINI_API_KEY")

if not api_key:
    raise ValueError("❌ GEMINI_API_KEY를 찾을 수 없습니다. .env.stt 파일을 확인하세요!")

# Gemini 모델 설정
genai.configure(api_key=api_key)
model = genai.GenerativeModel("models/gemini-2.5-flash")

# STT 결과를 Gemini에 전달
print("🤖 Gemini 응답 생성 중...\n")
response = model.generate_content(stt_text)
print(response.text)
