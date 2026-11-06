using UnityEngine;

/// <summary>Retro sound effects synthesised at runtime (no audio files needed).</summary>
public static class SoundFX
{
    const int Rate = 44100;

    public static AudioClip Flap, Point, Hit, Die, Swoosh;
    static bool built;

    enum Wave { Square, Triangle, Noise }

    public static void Build()
    {
        if (built) return;
        built = true;

        Flap = Clip("flap", Render(0.13f, 320f, 760f, 0.28f, Wave.Square, 1.2f));
        Point = Clip("point", Join(
            Render(0.07f, 988f, 988f, 0.26f, Wave.Square, 0.3f),
            Render(0.22f, 1319f, 1319f, 0.26f, Wave.Square, 1.4f)));
        Hit = Clip("hit", Mix(
            Render(0.18f, 0f, 0f, 0.5f, Wave.Noise, 2f),
            Render(0.18f, 220f, 60f, 0.35f, Wave.Square, 1.5f)));
        Die = Clip("die", Render(0.55f, 520f, 90f, 0.3f, Wave.Triangle, 0.8f));
        Swoosh = Clip("swoosh", Bell(Render(0.22f, 0f, 0f, 0.25f, Wave.Noise, 0f)));
    }

    static float[] Render(float duration, float f0, float f1, float volume, Wave wave, float decay)
    {
        int n = Mathf.Max(1, (int)(duration * Rate));
        var data = new float[n];
        var rnd = new System.Random(1234 + n);
        float phase = 0f, lp = 0f;
        float attack = Rate * 0.004f;

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float f = Mathf.Lerp(f0, f1, t);
            phase += f / Rate;
            phase -= Mathf.Floor(phase);

            float s;
            switch (wave)
            {
                case Wave.Square: s = phase < 0.5f ? 1f : -1f; break;
                case Wave.Triangle: s = 4f * Mathf.Abs(phase - 0.5f) - 1f; break;
                default:
                    float noise = (float)(rnd.NextDouble() * 2.0 - 1.0);
                    lp = lp * 0.5f + noise * 0.5f;
                    s = lp;
                    break;
            }

            float env = Mathf.Pow(1f - t, decay) * Mathf.Min(1f, i / attack);
            data[i] = s * env * volume;
        }
        return data;
    }

    static float[] Join(float[] a, float[] b)
    {
        var r = new float[a.Length + b.Length];
        System.Array.Copy(a, 0, r, 0, a.Length);
        System.Array.Copy(b, 0, r, a.Length, b.Length);
        return r;
    }

    static float[] Mix(float[] a, float[] b)
    {
        var r = new float[Mathf.Max(a.Length, b.Length)];
        for (int i = 0; i < r.Length; i++)
            r[i] = (i < a.Length ? a[i] : 0f) + (i < b.Length ? b[i] : 0f);
        return r;
    }

    static float[] Bell(float[] d)
    {
        for (int i = 0; i < d.Length; i++) d[i] *= Mathf.Sin(Mathf.PI * i / d.Length);
        return d;
    }

    static AudioClip Clip(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
