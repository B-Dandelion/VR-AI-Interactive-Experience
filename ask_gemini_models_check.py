import os
from dotenv import load_dotenv
import google.generativeai as genai

load_dotenv(".env.stt")
genai.configure(api_key=os.getenv("GEMINI_API_KEY"), transport="rest")  # REST 강제

print("== generateContent 지원 모델 ==")
for m in genai.list_models():
    if "generateContent" in getattr(m, "supported_generation_methods", []):
        print("-", m.name)
