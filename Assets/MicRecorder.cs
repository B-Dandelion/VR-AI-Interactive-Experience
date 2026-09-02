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
            req.timeout = 60;

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                string errorMsg = $"Server Error ({req.responseCode})";
                Debug.LogError(errorMsg + " : " + req.error);
                if (statusUI != null) statusUI.ShowError(errorMsg);
                yield break;
            }

            AskAudioResponse response = null;
            try
            {
                response = JsonUtility.FromJson<AskAudioResponse>(req.downloadHandler.text);
            }
            catch (System.Exception)
            {
                if (statusUI != null) statusUI.ShowError("JSON Error");
                yield break;
            }

            if (response == null || string.IsNullOrEmpty(response.audio_url))
            {
                if (statusUI != null) statusUI.ShowError("Empty URL");
                yield break;
            }

            string audioUrl = response.audio_url;
            if (!audioUrl.StartsWith("http"))
            {
                audioUrl = serverBaseUrl + audioUrl;
            }

            audioUrl = System.Uri.EscapeUriString(audioUrl);
            yield return StartCoroutine(DownloadAndPlayAudio(audioUrl));
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
