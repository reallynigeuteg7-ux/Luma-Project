using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace Luma;
[Activity(Label="Luma",MainLauncher=true,Exported=true,ScreenOrientation=ScreenOrientation.Portrait,
    ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.ScreenSize,Theme="@style/LumaTheme")]
public sealed class MainActivity : Activity
{
    private LumaView? gameView;
    protected override void OnCreate(Bundle? bundle)
    {
        base.OnCreate(bundle);
        Window?.SetStatusBarColor(Android.Graphics.Color.Rgb(17,20,38));
        Window?.SetNavigationBarColor(Android.Graphics.Color.Rgb(17,20,38));
        Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
        gameView=new LumaView(this);SetContentView(gameView);
    }
    protected override void OnPause(){gameView?.Suspend();base.OnPause();}
    protected override void OnResume(){base.OnResume();gameView?.ResumeGame();}
    public override void OnBackPressed(){if(gameView?.Back()!=true)Finish();}
    protected override void OnDestroy(){gameView?.Release();base.OnDestroy();}
}
