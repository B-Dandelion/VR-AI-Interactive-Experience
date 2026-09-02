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
    [Header("UI")]
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

    [Header("Portfolio Demo Mode")]
    [Tooltip("When enabled, the A-button flow is demonstrated without a running server or microphone.")]
    public bool portfolioDemoMode = true;

    [Tooltip("Keeps the recorded demo visibly distinguishable from a live server call.")]
    public bool showDemoLabel = true;

    [TextArea(1, 3)]
    public string demoQuestion = "위는 어떤 역할을 해?";

    [TextArea(2, 5)]
    public string demoAnswer = "위는 음식물을 잠시 저장하고, 위산과 소화효소로 음식물을 분해해 소장으로 보내는 기관입니다.";

    public float demoRecognizedDelay = 0.7f;
    public float demoThinkingSeconds = 1.5f;
    public float demoAnswerHoldSeconds = 5.0f;

    [Tooltip("Optional prerecorded TTS. Leave empty for a text-only demo.")]
    public AudioClip demoResponseAudio;

    private AudioClip _clip;
    private string _micDevice;
    private bool _isRecording;
    private Coroutine _demoRoutine;

    void Start()
    {
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        if (statusUI != null) statusUI.HideImmediate();
    }

    public void StartRecord()
    {
        StopAllSounds();
        if (_isRecording) return;

        // Demo mode deliberately avoids external dependencies so the portfolio
        // interaction can be captured reliably from the restored Unity project.
        if (portfolioDemoMode)
        {
            _isRecording = true;
            if (_demoRoutine != null)
            {
                StopCoroutine(_demoRoutine);
                _demoRoutine = null;
            }
            if (statusUI != null) statusUI.ShowRecording();
            return;
        }

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

        if (portfolioDemoMode)
        {
            _isRecording = false;
            if (_demoRoutine != null) StopCoroutine(_demoRoutine);
            _demoRoutine = StartCoroutine(RunPortfolioDemo());
            return;
        }

        int position = Microphone.GetPosition(_micDevice);
        Microphone.End(_micDevice);
        _isRecording = false;

        if (position <= 0)
        {
            if (statusUI != null) statusUI.ShowError("No Sound");
            return;
        }

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

    private IEnumerator RunPortfolioDemo()
    {
        string prefix = showDemoLabel ? "[DEMO]\n" : "";

        if (statusUI != null)
        {
            statusUI.ShowMessage($"{prefix}질문 인식\n\"{demoQuestion}\"", Color.white);
        }

        yield return new WaitForSeconds(demoRecognizedDelay);

        if (statusUI != null) statusUI.ShowProcessing();
        yield return new WaitForSeconds(demoThinkingSeconds);

        if (statusUI != null)
        {
            statusUI.ShowMessage($"{prefix}AI 답변\n{demoAnswer}", Color.white);
        }

        float holdSeconds = demoAnswerHoldSeconds;
        if (demoResponseAudio != null && audioSource != null)
        {
            audioSource.spatialBlend = 0f;
            audioSource.clip = demoResponseAudio;
            audioSource.Play();
            holdSeconds = Mathf.Max(holdSeconds, demoResponseAudio.length);
        }

        yield return new WaitForSeconds(holdSeconds);

        if (statusUI != null) statusUI.HideImmediate();
        _demoRoutine = null;
    }

    private IEnumerator SendAudioToServer(byte[] wavData)
    {
        if (statusUI != null) statusUI.ShowProcessing();

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "audio.wav", "audio/wav");

        using (UnityWebRequest req = UnityWebRequest.Post(askAudioPath, form))
        {
            req.SetRequestHeader("ngrok-skip-browser-warning", "true");
            req.timeout = 60;

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                string errorMsg = $"Server Error ({req.responseCode})";
                Debug.LogError(errorMsg + " : " + req.error);

                if (statusUI != null) statusUI.ShowError(errorMsg);
                yield break;
            }

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
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
        if (statusUI != null) statusUI.HideImmediate();

        XRTeleportPad_CC[] allPads = FindObjectsByType<XRTeleportPad_CC>(FindObjectsSortMode.None);
        foreach (var pad in allPads)
        {
            AudioSource padAudio = pad.GetComponent<AudioSource>();
            if (padAudio != null && padAudio.isPlaying) padAudio.Stop();
        }
    }

    private IEnumerator DownloadAndPlayAudio(string url)
    {
        using (UnityWebRequest audioReq = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            ((DownloadHandlerAudioClip)audioReq.downloadHandler).streamAudio = false;

            yield return audioReq.SendWebRequest();

            if (audioReq.result != UnityWebRequest.Result.Success)
            {
                if (statusUI != null) statusUI.ShowError("Audio Error");
                yield break;
            }

            AudioClip clip = ((DownloadHandlerAudioClip)audioReq.downloadHandler).audioClip;
            StopAllSounds();

            audioSource.spatialBlend = 0f;
            audioSource.clip = clip;
            audioSource.Play();

            if (statusUI != null) statusUI.ShowPlaying();

            if (clip != null)
            {
                yield return new WaitForSeconds(clip.length);
            }

            if (statusUI != null) statusUI.HideDelayed(4.0f);
        }
    }
}
