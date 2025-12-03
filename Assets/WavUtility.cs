using System;
using System.IO;
using UnityEngine;

/// <summary>
///
/// </summary>
public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        if (clip == null)
            throw new ArgumentNullException(nameof(clip));

        int channels = clip.channels;
        int sampleCount = clip.samples * channels;

        float[] samples = new float[sampleCount];
        clip.GetData(samples, 0);

        // float(-1~1)�� 16-bit PCM���� ��ȯ
        short[] intData = new short[sampleCount];
        byte[] bytesData = new byte[sampleCount * 2];

        const float rescaleFactor = 32767f;
        for (int i = 0; i < sampleCount; i++)
        {
            intData[i] = (short)(Mathf.Clamp(samples[i], -1f, 1f) * rescaleFactor);
            byte[] byteArr = BitConverter.GetBytes(intData[i]);
            bytesData[i * 2] = byteArr[0];
            bytesData[i * 2 + 1] = byteArr[1];
        }

        // WAV ��� + ������ ���̱�
        byte[] wav = WriteWavHeader(bytesData, clip.frequency, channels);
        return wav;
    }

    private static byte[] WriteWavHeader(byte[] pcmData, int sampleRate, int channels)
    {
        int subChunk1 = 16;      // PCM
        int bitsPerSample = 16;
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        int blockAlign = channels * bitsPerSample / 8;
        int subChunk2 = pcmData.Length;
        int chunkSize = 4 + (8 + subChunk1) + (8 + subChunk2);

        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            // RIFF header
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(chunkSize);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // fmt subchunk
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(subChunk1);
            writer.Write((short)1); // PCM
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)blockAlign);
            writer.Write((short)bitsPerSample);

            // data subchunk
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(subChunk2);
            writer.Write(pcmData);

            writer.Flush();
            return ms.ToArray();
        }
    }
}
