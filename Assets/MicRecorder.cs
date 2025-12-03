using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

[System.Serializable]
public class AskAudioResponse
{
    public string answer;
    public string audio_url;
}

public class MicRecorder : MonoBehaviour
{
    [Header("Server")]
    public string serverBaseUrl = "http://172.19.1.192:8000"; // or 맥 IP
    public string askAudioPath = "http://172.19.1.192:8000/ask_audio";
    public AudioSource audioSource;

    [Header("Recording")]
    public int sampleRate = 16000;
    public int maxRecordSeconds = 10;

    private AudioClip _clip;
    private string _micDevice;
    private bool _isRecording;

    public void StartRecord()
    {
        if (_isRecording) return;

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("마이크 디바이스가 없습니다.");
            return;
        }

        _micDevice = Microphone.devices[0];
        _clip = Microphone.Start(_micDevice, false, maxRecordSeconds, sampleRate);
        _isRecording = true;
        Debug.Log("녹음 시작");
    }

    public void StopRecordAndSend()
    {
        if (!_isRecording) return;

        int position = Microphone.GetPosition(_micDevice);
        Microphone.End(_micDevice);
        _isRecording = false;

        if (position <= 0)
        {
            Debug.LogWarning("녹음된 샘플이 없습니다.");
            return;
        }

        // 필요 길이만 잘라서 새 AudioClip으로 만들기
        int channels = _clip.channels;
        float[] data = new float[position * channels];
        _clip.GetData(data, 0);
        AudioClip trimmed = AudioClip.Create(
            "recorded",
            position,
            channels,
            sampleRate,
            false
        );
        trimmed.SetData(data, 0);

        byte[] wavData = WavUtility.FromAudioClip(trimmed);
        StartCoroutine(SendAudioToServer(wavData));
    }

    private IEnumerator SendAudioToServer(byte[] wavData)
    {
        Debug.Log($"[MicRecorder] POST {askAudioPath}");

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "audio.wav", "audio/wav");

        using (UnityWebRequest req = UnityWebRequest.Post(askAudioPath, form))
        {
            yield return req.SendWebRequest();

            Debug.Log($"[MicRecorder] HTTP result = {req.result}, code = {req.responseCode}, error = {req.error}");

            string body = req.downloadHandler.text;
            Debug.Log($"[MicRecorder] body = {body}");

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("ask_audio error, stop here");
                yield break;
            }

            AskAudioResponse res;
            try
            {
                res = JsonUtility.FromJson<AskAudioResponse>(body);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MicRecorder] JSON parse error: {e}");
                yield break;
            }

            Debug.Log($"[MicRecorder] answer = {res.answer}, audio_url = {res.audio_url}");

            if (string.IsNullOrEmpty(res.audio_url))
            {
                Debug.LogWarning("[MicRecorder] audio_url is empty, not downloading audio");
                yield break;
            }

            // audio_url이 절대경로인지 상대경로인지에 따라 처리
            string audioUrlFull = res.audio_url;
            if (!audioUrlFull.StartsWith("http"))
            {
                // 서버에서 "/audio/xxx.wav" 이렇게만 보내면 여기서 host 붙여줌
                audioUrlFull = serverBaseUrl + res.audio_url; // serverBaseUrl 따로 있다면
            }

            Debug.Log($"[MicRecorder] downloading audio from {audioUrlFull}");

            using (UnityWebRequest audioReq =
                   UnityWebRequestMultimedia.GetAudioClip(audioUrlFull, AudioType.UNKNOWN))
            {
                yield return audioReq.SendWebRequest();

                Debug.Log($"[MicRecorder] audio HTTP result = {audioReq.result}, code = {audioReq.responseCode}, error = {audioReq.error}");

                if (audioReq.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("오디오 다운로드 실패");
                    yield break;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(audioReq);
                if (clip == null)
                {
                    Debug.LogError("DownloadHandlerAudioClip.GetContent result is null");
                    yield break;
                }

                audioSource.clip = clip;
                audioSource.Play();
                Debug.Log("[MicRecorder] audio play!");
            }
        }
    }
}