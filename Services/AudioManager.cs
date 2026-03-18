using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Threading.Tasks;

namespace CockroachPet;

/// <summary>
/// 音效管理器 - 负责播放各种游戏音效
/// </summary>
public static class AudioManager
{
    private static bool _isInitialized = false;
    private static bool _isEnabled = true;

    // 音效缓存
    private static readonly Dictionary<string, byte[]> SoundCache = new();

    /// <summary>
    /// 是否启用音效
    /// </summary>
    public static bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    /// <summary>
    /// 初始化音效系统
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized) return;

        // 预生成音效数据
        GenerateAllSounds();

        _isInitialized = true;
    }

    /// <summary>
    /// 播放打击音效
    /// </summary>
    public static void PlayHitSound()
    {
        if (!_isEnabled) return;
        PlaySoundFromCache("hit");
    }

    /// <summary>
    /// 播放射击音效
    /// </summary>
    public static void PlayShootSound()
    {
        if (!_isEnabled) return;
        PlaySoundFromCache("shoot");
    }

    /// <summary>
    /// 播放爆炸音效
    /// </summary>
    public static void PlayExplosionSound()
    {
        if (!_isEnabled) return;
        PlaySoundFromCache("explosion");
    }

    /// <summary>
    /// 播放激光音效
    /// </summary>
    public static void PlayLaserSound()
    {
        if (!_isEnabled) return;
        PlaySoundFromCache("laser");
    }

    /// <summary>
    /// 播放电击音效
    /// </summary>
    public static void PlayElectricSound()
    {
        if (!_isEnabled) return;
        PlaySoundFromCache("electric");
    }

    /// <summary>
    /// 播放弹开/反弹音效
    /// </summary>
    public static void PlayBounceSound()
    {
        if (!_isEnabled) return;
        PlaySoundFromCache("bounce");
    }

    /// <summary>
    /// 播放死亡音效
    /// </summary>
    public static void PlayDeathSound()
    {
        if (!_isEnabled) return;
        PlaySoundFromCache("death");
    }

    /// <summary>
    /// 播放怪物受击音效
    /// </summary>
    public static void PlayMonsterHitSound()
    {
        if (!_isEnabled) return;
        PlaySoundFromCache("monster_hit");
    }

    /// <summary>
    /// 播放怪物死亡音效
    /// </summary>
    public static void PlayMonsterDeathSound()
    {
        if (!_isEnabled) return;
        PlaySoundFromCache("monster_death");
    }

    /// <summary>
    /// 播放格斗碰撞音效
    /// </summary>
    public static void PlayClashSound()
    {
        if (!_isEnabled) return;
        PlaySoundFromCache("clash");
    }

    /// <summary>
    /// 根据投射物类型播放对应的音效
    /// </summary>
    public static void PlayProjectileSound(string projectileType)
    {
        if (!_isEnabled) return;

        switch (projectileType)
        {
            case "ROCKET":
                PlaySoundFromCache("rocket_launch");
                break;
            case "LIGHTNING":
                PlaySoundFromCache("electric");
                break;
            case "CANNON":
                PlaySoundFromCache("cannon");
                break;
            case "PLASMA":
                PlaySoundFromCache("plasma");
                break;
            case "SPIT":
            case "INK":
                PlaySoundFromCache("splash");
                break;
            default:
                PlaySoundFromCache("shoot");
                break;
        }
    }

    /// <summary>
    /// 播放投射物命中音效
    /// </summary>
    public static void PlayProjectileHitSound(string projectileType)
    {
        if (!_isEnabled) return;

        switch (projectileType)
        {
            case "ROCKET":
            case "CANNON":
                PlayExplosionSound();
                break;
            case "LIGHTNING":
                PlayElectricSound();
                break;
            case "PLASMA":
                PlaySoundFromCache("plasma_hit");
                break;
            case "SPIT":
            case "INK":
                PlaySoundFromCache("splash_hit");
                break;
            default:
                PlayHitSound();
                break;
        }
    }

    #region Private Methods

    private static void PlaySoundFromCache(string soundName)
    {
        if (!SoundCache.TryGetValue(soundName, out var soundData)) return;

        Task.Run(() =>
        {
            try
            {
                using var stream = new MemoryStream(soundData);
                using var player = new SoundPlayer(stream);
                player.PlaySync();
            }
            catch
            {
                // 忽略音效播放错误
            }
        });
    }

    private static void GenerateAllSounds()
    {
        // 生成所有音效数据（简单的 WAV 格式）
        SoundCache["hit"] = GenerateHitSound();
        SoundCache["shoot"] = GenerateShootSound();
        SoundCache["explosion"] = GenerateExplosionSound();
        SoundCache["laser"] = GenerateLaserSound();
        SoundCache["electric"] = GenerateElectricSound();
        SoundCache["bounce"] = GenerateBounceSound();
        SoundCache["death"] = GenerateDeathSound();
        SoundCache["monster_hit"] = GenerateMonsterHitSound();
        SoundCache["monster_death"] = GenerateMonsterDeathSound();
        SoundCache["clash"] = GenerateClashSound();
        SoundCache["rocket_launch"] = GenerateRocketLaunchSound();
        SoundCache["cannon"] = GenerateCannonSound();
        SoundCache["plasma"] = GeneratePlasmaSound();
        SoundCache["plasma_hit"] = GeneratePlasmaHitSound();
        SoundCache["splash"] = GenerateSplashSound();
        SoundCache["splash_hit"] = GenerateSplashHitSound();
    }

    // 生成简单的 WAV 音效数据
    private static byte[] GenerateWavHeader(int dataLength, int sampleRate = 22050, int channels = 1, int bitsPerSample = 8)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // RIFF header
        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataLength);
        writer.Write("WAVE"u8.ToArray());

        // fmt chunk
        writer.Write("fmt "u8.ToArray());
        writer.Write(16); // Subchunk1Size
        writer.Write((short)1); // AudioFormat (PCM)
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * bitsPerSample / 8); // ByteRate
        writer.Write((short)(channels * bitsPerSample / 8)); // BlockAlign
        writer.Write((short)bitsPerSample);

        // data chunk
        writer.Write("data"u8.ToArray());
        writer.Write(dataLength);

        return ms.ToArray();
    }

    private static byte[] GenerateHitSound()
    {
        // 短促的打击声
        int sampleRate = 22050;
        int duration = 100; // ms
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = 1 - t / (duration / 1000f);
            float freq = 800 + 400 * (1 - decay);
            int sample = (int)(100 * decay * Math.Sin(2 * Math.PI * freq * t));
            data[i] = (byte)(128 + Math.Clamp(sample, -128, 127));
        }

        return GenerateWavHeader(data.Length).Concat(data).ToArray();
    }

    private static byte[] GenerateShootSound()
    {
        // 射击声 - 快速衰减的高频
        int sampleRate = 22050;
        int duration = 80;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = (float)Math.Exp(-t * 30);
            float freq = 1200 - t * 4000;
            int sample = (int)(120 * decay * Math.Sin(2 * Math.PI * freq * t));
            data[i] = (byte)(128 + Math.Clamp(sample, -128, 127));
        }

        return GenerateWavHeader(data.Length).Concat(data).ToArray();
    }

    private static byte[] GenerateExplosionSound()
    {
        // 爆炸声 - 噪声
        int sampleRate = 22050;
        int duration = 300;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];
        var rand = new Random();

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = (float)Math.Exp(-t * 8);
            int sample = (int)(100 * decay * (rand.NextDouble() * 2 - 1));
            data[i] = (byte)(128 + Math.Clamp(sample, -128, 127));
        }

        return GenerateWavHeader(data.Length).Concat(data).ToArray();
    }

    private static byte[] GenerateLaserSound()
    {
        // 激光声 - 上升的音调
        int sampleRate = 22050;
        int duration = 150;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float freq = 200 + t * 3000;
            int sample = (int)(100 * Math.Sin(2 * Math.PI * freq * t));
            data[i] = (byte)(128 + Math.Clamp(sample, -128, 127));
        }

        return GenerateWavHeader(data.Length).Concat(data).ToArray();
    }

    private static byte[] GenerateElectricSound()
    {
        // 电击声 - 高频噪声
        int sampleRate = 22050;
        int duration = 200;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];
        var rand = new Random();

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = (float)Math.Exp(-t * 15);
            int sample = (int)(120 * decay * (rand.NextDouble() * 2 - 1));
            // 添加一些正弦波使其更有电子感
            sample += (int)(60 * decay * Math.Sin(2 * Math.PI * 800 * t));
            data[i] = (byte)(128 + Math.Clamp(sample, -128, 127));
        }

        return GenerateWavHeader(data.Length).Concat(data).ToArray();
    }

    private static byte[] GenerateBounceSound()
    {
        // 弹跳声
        int sampleRate = 22050;
        int duration = 100;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = (float)Math.Exp(-t * 20);
            float freq = 400;
            int sample = (int)(100 * decay * Math.Sin(2 * Math.PI * freq * t));
            data[i] = (byte)(128 + Math.Clamp(sample, -128, 127));
        }

        return GenerateWavHeader(data.Length).Concat(data).ToArray();
    }

    private static byte[] GenerateDeathSound()
    {
        // 死亡音效 - 下降的音调
        int sampleRate = 22050;
        int duration = 400;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = (float)Math.Exp(-t * 3);
            float freq = 600 - t * 800;
            if (freq < 50) freq = 50;
            int sample = (int)(120 * decay * Math.Sin(2 * Math.PI * freq * t));
            data[i] = (byte)(128 + Math.Clamp(sample, -128, 127));
        }

        return GenerateWavHeader(data.Length).Concat(data).ToArray();
    }

    private static byte[] GenerateMonsterHitSound()
    {
        // 怪物受击 - 低沉的打击声
        int sampleRate = 22050;
        int duration = 150;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = 1 - t / (duration / 1000f);
            float freq = 200 + 100 * (1 - decay);
            int sample = (int)(120 * decay * Math.Sin(2 * Math.PI * freq * t));
            data[i] = (byte)(128 + Math.Clamp(sample, -128, 127));
        }

        return GenerateWavHeader(data.Length).Concat(data).ToArray();
    }

    private static byte[] GenerateMonsterDeathSound()
    {
        // 怪物死亡 - 更长的下降音
        int sampleRate = 22050;
        int duration = 600;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];
        var rand = new Random();

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = (float)Math.Exp(-t * 2);
            float freq = 300 - t * 400;
            if (freq < 30) freq = 30;
            int sample = (int)(100 * decay * Math.Sin(2 * Math.PI * freq * t));
            // 添加一些噪声
            sample += (int)(40 * decay * (rand.NextDouble() * 2 - 1));
            data[i] = (byte)(128 + Math.Clamp(sample, -128, 127));
        }

        return GenerateWavHeader(data.Length).Concat(data).ToArray();
    }

    private static byte[] GenerateClashSound()
    {
        // 格斗碰撞声 - 金属撞击
        int sampleRate = 22050;
        int duration = 200;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];
        var rand = new Random();

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = (float)Math.Exp(-t * 10);
            // 金属感的频率
            float freq1 = 800;
            float freq2 = 1200;
            int sample = (int)(100 * decay * (
                Math.Sin(2 * Math.PI * freq1 * t) * 0.5 +
                Math.Sin(2 * Math.PI * freq2 * t) * 0.5 +
                (rand.NextDouble() * 2 - 1) * 0.3
            ));
            data[i] = (byte)(128 + Math.Clamp(sample, -128, 127));
        }

        return GenerateWavHeader(data.Length).Concat(data).ToArray();
    }

    private static byte[] GenerateRocketLaunchSound()
    {
        // 火箭发射声
        int sampleRate = 22050;
        int duration = 250;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];
        var rand = new Random();

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = (float)Math.Exp(-t * 5);
            float freq = 100 + t * 200;
            int sample = (int)(100 * decay * (
                Math.Sin(2 * Math.PI * freq * t) * 0.7 +
                (rand.NextDouble() * 2 - 1) * 0.3
            ));
            data[i] = (byte)(128 + Math.Clamp(sample, -128, 127));
        }

        return GenerateWavHeader(data.Length).Concat(data).ToArray();
    }

    private static byte[] GenerateCannonSound()
    {
        // 重炮声 - 低沉的爆炸
        int sampleRate = 22050;
        int duration = 400;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];
        var rand = new Random();

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = (float)Math.Exp(-t * 6);
            int sample = (int)(120 * decay * (rand.NextDouble() * 2 - 1));
            // 添加低频隆隆声
            sample += (int)(80 * decay * Math.Sin(2 * Math.PI * 80 * t));
            data[i] = (byte)(128 + Math.Clamp(sample, -128, 127));
        }

        return GenerateWavHeader(data.Length).Concat(data).ToArray();
    }

    private static byte[] GeneratePlasmaSound()
    {
        // 等离子发射声
        int sampleRate = 22050;
        int duration = 150;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float freq = 600 + (float)Math.Sin(t * 50) * 200;
            int sample = (int)(100 * Math.Sin(2 * Math.PI * freq * t));
            data[i] = (byte)(128 + Math.Clamp(sample, -128, 127));
        }

        return GenerateWavHeader(data.Length).Concat(data).ToArray();
    }

    private static byte[] GeneratePlasmaHitSound()
    {
        // 等离子命中声
        int sampleRate = 22050;
        int duration = 200;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = (float)Math.Exp(-t * 8);
            float freq = 1000 - t * 2000;
            if (freq < 100) freq = 100;
            int sample = (int)(120 * decay * Math.Sin(2 * Math.PI * freq * t));
            data[i] = (byte)(128 + Math.Clamp(sample, -128, 127));
        }

        return GenerateWavHeader(data.Length).Concat(data).ToArray();
    }

    private static byte[] GenerateSplashSound()
    {
        // 喷射声
        int sampleRate = 22050;
        int duration = 150;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];
        var rand = new Random();

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = 1 - t / (duration / 1000f);
            int sample = (int)(80 * decay * (rand.NextDouble() * 2 - 1));
            data[i] = (byte)(128 + Math.Clamp(sample, -128, 127));
        }

        return GenerateWavHeader(data.Length).Concat(data).ToArray();
    }

    private static byte[] GenerateSplashHitSound()
    {
        // 溅射命中声
        int sampleRate = 22050;
        int duration = 250;
        int samples = sampleRate * duration / 1000;
        byte[] data = new byte[samples];
        var rand = new Random();

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float decay = (float)Math.Exp(-t * 10);
            int sample = (int)(100 * decay * (rand.NextDouble() * 2 - 1));
            data[i] = (byte)(128 + Math.Clamp(sample, -128, 127));
        }

        return GenerateWavHeader(data.Length).Concat(data).ToArray();
    }

    #endregion
}
