using Android.Content;
using Android.Media;
using Android.OS;

namespace Luma;
public sealed class Sound : IDisposable
{
    private readonly SoundPool pool;
    private readonly Dictionary<string,int> samples=[];
    private readonly Vibrator? vibrator;
    private readonly Profile profile;
    public Sound(Context context,Profile profile)
    {
        this.profile=profile;
        using var attributes=new AudioAttributes.Builder().SetUsage(AudioUsageKind.Game)!.SetContentType(AudioContentType.Sonification)!.Build();
        pool=new SoundPool.Builder().SetMaxStreams(5)!.SetAudioAttributes(attributes)!.Build()!;
        foreach(string name in new[]{"tap","match","cascade","special","win","lose","invalid"})
        {
            using var file=context.Assets!.OpenFd(name+".wav");samples[name]=pool.Load(file,1);
        }
        if(OperatingSystem.IsAndroidVersionAtLeast(31))
            vibrator=(context.GetSystemService(Context.VibratorManagerService) as VibratorManager)?.DefaultVibrator;
        else
        {
#pragma warning disable CA1422 // Compatibility path for Android 7–11.
            vibrator=context.GetSystemService(Context.VibratorService) as Vibrator;
#pragma warning restore CA1422
        }
    }
    public void Play(string name,int chain=1)
    {
        if(profile.Sound && samples.TryGetValue(name,out int id))pool.Play(id,.45f,.45f,1,0,Math.Clamp(1+(chain-1)*.055f,.6f,1.6f));
        if(profile.Haptics && vibrator?.HasVibrator==true && name is "match" or "special" or "win")
        {
            long duration=name=="win"?55:name=="special"?32:12;
            if(OperatingSystem.IsAndroidVersionAtLeast(26))vibrator.Vibrate(VibrationEffect.CreateOneShot(duration,65));
            else
            {
#pragma warning disable CA1422 // Android 7 has only the duration API.
                vibrator.Vibrate(duration);
#pragma warning restore CA1422
            }
        }
    }
    public void Pause()=>pool.AutoPause();
    public void Dispose(){pool.Release();pool.Dispose();vibrator?.Dispose();}
}
