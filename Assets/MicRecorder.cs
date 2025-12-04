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
    public string serverBaseUrl = "http://172.19.1.192:8000";
    public string askAudioPath = "http://172.19.1.192:8000/ask_audio";
    public AudioSource audioSource;

    [Header("Recording")]
    public int sampleRate = 16000;
    public int maxRecordSeconds = 10;

    private AudioClip _clip;
    private string _micDevice;
    private bool _isRecording;

    void Start()
    {
        // AudioSource가 없으면 자동으로 추가해주는 안전장치
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

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

        int channels = _clip.channels;
        float[] data = new float[position * channels];
        _clip.GetData(data, 0);
        AudioClip trimmed = AudioClip.Create("recorded", position, channels, sampleRate, false);
        trimmed.SetData(data, 0);

        // WavUtility 클래스가 프로젝트에 있어야 합니다.
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

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[MicRecorder] Error: {req.error}");
                yield break;
            }

            string body = req.downloadHandler.text;
            Debug.Log($"[MicRecorder] body = {body}");

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

            if (string.IsNullOrEmpty(res.audio_url))
            {
                Debug.LogWarning("[MicRecorder] audio_url is empty");
                yield break;
            }

            // URL 조합
            string audioUrlFull = res.audio_url;
            if (!audioUrlFull.StartsWith("http"))
            {
                audioUrlFull = serverBaseUrl + res.audio_url;
            }

            // [중요] URL에 공백 등이 있을 경우를 대비해 인코딩
            audioUrlFull = System.Uri.EscapeUriString(audioUrlFull);

            Debug.Log($"[MicRecorder] downloading audio from {audioUrlFull}");

            // [핵심 수정] AudioType.UNKNOWN 사용
            using (UnityWebRequest audioReq = UnityWebRequestMultimedia.GetAudioClip(audioUrlFull, AudioType.MPEG))
            {
                var handler = (DownloadHandlerAudioClip)audioReq.downloadHandler;
                handler.streamAudio = false;
                handler.compressed = false;

                yield return audioReq.SendWebRequest();

                if (audioReq.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(audioReq.error);
                    yield break;
                }

                AudioClip clip = handler.audioClip;

                Debug.Log($"Clip freq={clip.frequency}, channels={clip.channels}, len={clip.length}");
                Debug.Log($"AudioSource volume={audioSource.volume}, spatialBlend={audioSource.spatialBlend}");

                audioSource.spatialBlend = 0f; // 2D
                audioSource.clip = clip;

                yield return new WaitForEndOfFrame();
                audioSource.Play();
            }
        }
    }
}