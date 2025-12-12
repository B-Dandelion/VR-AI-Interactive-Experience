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
    [Header("UI 연결")]
    public VoiceStatusUI statusUI;

    [Header("Server")]
    public string serverBaseUrl = "http://192.168.0.XX:8000";
    public string askAudioPath = "http://192.168.0.XX:8000/ask_audio";
    public AudioSource audioSource;

    [Header("Recording")]
    public int sampleRate = 16000;
    public int maxRecordSeconds = 10;

    [Range(1f, 20f)]
    public float gainMultiplier = 5.0f;

    private AudioClip _clip;
    private string _micDevice;
    private bool _isRecording;

    void Start()
    {
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        if (statusUI != null) statusUI.HideImmediate();
    }

    public void StartRecord()
    {
        StopAllSounds();
        if (_isRecording) return;
        if (Microphone.devices.Length == 0)
        {
            if (statusUI != null) statusUI.ShowError("No Mic");
            return;
        }

        _micDevice = Microphone.devices[0];
        _clip = Microphone.Start(_micDevice, false, maxRecordSeconds, sampleRate);
        _isRecording = true;

        if (statusUI != null) statusUI.ShowRecording();
    }

    public void StopRecordAndSend()
    {
        if (!_isRecording) return;

        int position = Microphone.GetPosition(_micDevice);
        Microphone.End(_micDevice);
        _isRecording = false;

        if (position <= 0)
        {
            if (statusUI != null) statusUI.ShowError("No Sound");
            return;
        }

        // 증폭 및 변환
        int channels = _clip.channels;
        float[] data = new float[position * channels];
        _clip.GetData(data, 0);

        for (int i = 0; i < data.Length; i++)
        {
            data[i] = Mathf.Clamp(data[i] * gainMultiplier, -1f, 1f);
        }

        AudioClip trimmed = AudioClip.Create("recorded", position, channels, sampleRate, false);
        trimmed.SetData(data, 0);

        byte[] wavData = WavUtility.FromAudioClip(trimmed);
        StartCoroutine(SendAudioToServer(wavData));
    }

    private IEnumerator SendAudioToServer(byte[] wavData)
    {
        if (statusUI != null) statusUI.ShowProcessing();

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "audio.wav", "audio/wav");

        using (UnityWebRequest req = UnityWebRequest.Post(askAudioPath, form))
        {
            req.SetRequestHeader("ngrok-skip-browser-warning", "true");
            req.timeout = 60; // 타임아웃 설정 (60초)

            yield return req.SendWebRequest();

            // ★ [에러 처리 1] 네트워크/서버 에러 (404, 500, Timeout 등)
            if (req.result != UnityWebRequest.Result.Success)
            {
                string errorMsg = $"Server Error ({req.responseCode})";
                Debug.LogError(errorMsg + " : " + req.error);

                if (statusUI != null) statusUI.ShowError(errorMsg); // 4초 후 꺼짐
                yield break;
            }

            // 성공 데이터 파싱
            string body = req.downloadHandler.text;
            AskAudioResponse res = null;
            try
            {
                res = JsonUtility.FromJson<AskAudioResponse>(body);
            }
            catch (System.Exception)
            {
                if (statusUI != null) statusUI.ShowError("JSON Error");
                yield break;
            }

            if (res == null || string.IsNullOrEmpty(res.audio_url))
            {
                if (statusUI != null) statusUI.ShowError("Empty URL");
                yield break;
            }

            string audioUrlFull = res.audio_url;
            if (!audioUrlFull.StartsWith("http"))
            {
                audioUrlFull = serverBaseUrl + res.audio_url;
            }
            audioUrlFull = System.Uri.EscapeUriString(audioUrlFull);

            yield return StartCoroutine(DownloadAndPlayAudio(audioUrlFull));
        }
    }
    private void StopAllSounds()
    {
        // 1. AI 마이크(MicRecorder) 끄기
        MicRecorder mic = FindObjectOfType<MicRecorder>();
        if (mic != null)
        {
            if (mic.audioSource != null && mic.audioSource.isPlaying) mic.audioSource.Stop();
            if (mic.statusUI != null) mic.statusUI.HideImmediate();
        }

        // 2. 맵에 있는 텔레포트 패드들 소리 끄기
        XRTeleportPad_CC[] allPads = FindObjectsOfType<XRTeleportPad_CC>();
        foreach (var pad in allPads)
        {
            AudioSource padAudio = pad.GetComponent<AudioSource>();
            if (padAudio != null && padAudio.isPlaying)
            {
                padAudio.Stop();
            }
        }
    }
    private IEnumerator DownloadAndPlayAudio(string url)
    {
        using (UnityWebRequest audioReq = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            ((DownloadHandlerAudioClip)audioReq.downloadHandler).streamAudio = false;

            yield return audioReq.SendWebRequest();

            // ★ [에러 처리 2] 오디오 다운로드 실패
            if (audioReq.result != UnityWebRequest.Result.Success)
            {
                if (statusUI != null) statusUI.ShowError("Audio Error");
                yield break;
            }

            AudioClip clip = ((DownloadHandlerAudioClip)audioReq.downloadHandler).audioClip;
            StopAllSounds();
            // 재생 시작
            audioSource.spatialBlend = 0f;
            audioSource.clip = clip;
            audioSource.Play();

            if (statusUI != null) statusUI.ShowPlaying();

            // 오디오 끝날 때까지 대기
            if (clip != null)
            {
                yield return new WaitForSeconds(clip.length);
            }

            // ★ [성공 처리] 재생 끝난 후 4초 대기 -> 사라짐
            if (statusUI != null) statusUI.HideDelayed(4.0f);
        }
    }
}