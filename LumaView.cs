using Android.Content;
using Android.Graphics;
using Android.Views;
using System.Diagnostics;
using AColor=Android.Graphics.Color;
using APath=Android.Graphics.Path;

namespace Luma;
public sealed partial class LumaView : View
{
    private readonly Profile profile;
    private readonly string savePath;
    private readonly Sound sound;
    private readonly Paint paint=new(PaintFlags.AntiAlias);
    private readonly Typeface regular;
    private readonly Typeface bold;
    private readonly Bitmap logo;
    private readonly RectF logoBounds=new();
    private readonly Stopwatch clock=Stopwatch.StartNew();
    private readonly List<Hit> hits=[];
    private readonly Queue<GameStep> steps=[];
    private Game? game;
    private Gem?[] board=[];
    private bool[] ice=new bool[64];
    private GameStep? step;
    private float scale=1,offsetX,h=780,boardY=232;
    private const float Cell=43.5f,BoardX=26;
    private string screen="home",modal="",returnModal="";
    private Action? confirmation;
    private int selected=-1,swapA=-1,swapB=-1,mapPage;
    private float downX,downY;
    private bool swapValid,hammer,suspended;
    private Gem?[]? swapBoard;
    private Turn? pending;
    private double animationStart,lastTime,idleSince,toastUntil;
    private string toast="";
    private (int A,int B)? hint;
    private int displayedScore;
    private bool Busy=>swapBoard is not null || step is not null || steps.Count>0;
    private static readonly AColor Bg=AColor.Rgb(17,20,38),Panel=AColor.Rgb(29,33,57),Line=AColor.Rgb(47,52,81),Ink=AColor.Rgb(245,242,255),Muted=AColor.Rgb(151,157,187),Accent=AColor.Rgb(186,162,255),Mint=AColor.Rgb(104,225,204);
    private static readonly AColor[] Colors=[AColor.Rgb(182,136,255),AColor.Rgb(93,220,192),AColor.Rgb(255,203,105),AColor.Rgb(246,131,184),AColor.Rgb(112,177,255),AColor.Rgb(255,146,116)];
    private readonly record struct Bounds(float Left,float Top,float Right,float Bottom)
    {
        public bool Contains(float x,float y)=>x>=Left && x<Right && y>=Top && y<Bottom;
    }
    private sealed record Hit(Bounds Rect,Action Action);
    public LumaView(Context context):base(context)
    {
        savePath=System.IO.Path.Combine(context.FilesDir!.AbsolutePath,"luma-progress.json");profile=Profile.Load(savePath);
        regular=Typeface.CreateFromAsset(context.Assets,"Montserrat-Regular.ttf")!;bold=Typeface.CreateFromAsset(context.Assets,"Montserrat-Bold.ttf")!;
        using var source=context.Assets!.Open("luma-logo.png");logo=BitmapFactory.DecodeStream(source)!;
        sound=new(context,profile);Focusable=true;ContentDescription="Luma. Игра три в ряд. Меняйте местами соседние кристаллы касанием или свайпом.";
        SetLayerType(LayerType.Hardware,null);
    }
    protected override void OnDraw(Canvas canvas)
    {
        base.OnDraw(canvas);if(Width==0 || Height==0)return;
        double now=clock.Elapsed.TotalSeconds;float dt=(float)Math.Clamp(now-lastTime,0,.05);lastTime=now;
        scale=Math.Min(Width/400f,Height/744f);offsetX=(Width-400*scale)/2;h=Height/scale;
        boardY=224+Math.Max(0,h-760)*.24f;
        if(!suspended)Animate(now);
        canvas.DrawColor(Bg);canvas.Save();canvas.Translate(offsetX,0);canvas.Scale(scale,scale);hits.Clear();
        DrawBackground(canvas,now);
        if(screen=="home")DrawHome(canvas,now);
        else if(screen=="map")DrawMap(canvas);
        else DrawGame(canvas,now,dt);
        if(modal.Length>0){hits.Clear();DrawModal(canvas,now);}
        if(toastUntil>now && modal.Length==0)
        {
            Round(canvas,22,h-53,356,37,12,AColor.Rgb(50,47,80));Text(canvas,toast,200,h-30,11,Ink,false,Paint.Align.Center);
        }
        canvas.Restore();if(!suspended)PostInvalidateDelayed(profile.CalmMotion?40:16);
    }
    private void Animate(double now)
    {
        float speed=profile.CalmMotion?.35f:1;
        if(swapBoard is not null)
        {
            if(now-animationStart<(swapValid?.18:.32)*speed)return;
            swapBoard=null;
            if(pending is not null){foreach(var s in pending.Steps)steps.Enqueue(s);pending=null;}
            NextStep(now);return;
        }
        if(step is not null && now-animationStart>=Duration(step)*speed)
        {board=step.After;ice=step.Ice;step=null;NextStep(now);}
    }
    private static float Duration(GameStep s)=>s.Kind==StepKind.Clear?.25f:s.Kind==StepKind.Drop?.26f:.4f;
    private void NextStep(double now)
    {
        if(steps.TryDequeue(out step))
        {
            board=step.Before;ice=step.Ice;animationStart=now;
            if(step.Kind==StepKind.Clear)sound.Play(step.Special?"special":step.Cascade>1?"cascade":"match",step.Cascade);
            if(step.Kind==StepKind.Shuffle)Say("Новая раскладка — игра продолжается");
        }
        else if(game is not null)
        {
            board=Game.Snapshot(game.State.Gems);ice=(bool[])game.State.Ice.Clone();idleSince=now;
            if(game.State.Finished){modal="result";sound.Play(game.State.Won?"win":"lose");}
        }
    }
    private void Save()
    {
        if(game is not null)profile.Current=game.State;
        if(!profile.Save(savePath))Say("Не удалось сохранить прогресс");
    }
    private void NewGame(int number)
    {
        game=Game.New(number);board=Game.Snapshot(game.State.Gems);ice=(bool[])game.State.Ice.Clone();
        selected=-1;hint=null;hammer=false;steps.Clear();step=null;swapBoard=null;pending=null;
        displayedScore=0;screen="game";modal="intro";idleSince=clock.Elapsed.TotalSeconds;Save();
    }
    private void RequestGame(int number)
    {
        if(profile.Current is {Finished:false} existing)
        {
            if(existing.LevelNumber==number){Continue();return;}
            confirmation=()=>NewGame(number);modal="replace";
        }
        else NewGame(number);
    }
    private void Continue()
    {
        if(profile.Current is null){NewGame(profile.Unlocked);return;}
        game=new(profile.Current);board=Game.Snapshot(game.State.Gems);ice=(bool[])game.State.Ice.Clone();
        displayedScore=game.State.Score;screen="game";modal=game.State.Finished?"result":"";idleSince=clock.Elapsed.TotalSeconds;
    }
    private void GoHome(){screen="home";modal="";hammer=false;selected=-1;Save();}
    private void ApplyStats(int before)
    {
        if(game is null)return;var s=game.State;
        profile.Crystals+=Math.Max(0,s.Crystals-before);profile.BestCascade=Math.Max(profile.BestCascade,s.BestCascade);
        if(s.LevelNumber==0)profile.ZenBest=Math.Max(profile.ZenBest,s.Score);
        else if(s.Finished && !s.Recorded)
        {
            s.Recorded=true;
            profile.BestScores[s.LevelNumber-1]=Math.Max(profile.BestScores[s.LevelNumber-1],s.Score);
            if(s.Won){profile.Wins++;profile.Unlocked=Math.Max(profile.Unlocked,Math.Min(30,s.LevelNumber+1));profile.Stars[s.LevelNumber-1]=Math.Max(profile.Stars[s.LevelNumber-1],s.Stars);}
        }
        Save();
    }
    private void TrySwap(int a,int b)
    {
        if(game is null || Busy || game.State.Finished || !Game.Adjacent(a,b))return;
        int before=game.State.Crystals;swapBoard=Game.Snapshot(game.State.Gems);swapA=a;swapB=b;
        pending=game.TrySwap(a,b);swapValid=pending.Valid;animationStart=clock.Elapsed.TotalSeconds;selected=-1;hint=null;
        sound.Play(swapValid?"tap":"invalid");if(!swapValid)Say("Соберите линию из трёх кристаллов");else ApplyStats(before);
    }
    private void UseHammer(int pos)
    {
        if(game is null || Busy)return;int before=game.State.Crystals;var turn=game.Hammer(pos);hammer=false;
        if(!turn.Valid)return;selected=-1;hint=null;ApplyStats(before);foreach(var s in turn.Steps)steps.Enqueue(s);NextStep(clock.Elapsed.TotalSeconds);
    }
    private void Shuffle()
    {
        if(game is null || Busy)return;var turn=game.Shuffle();if(!turn.Valid)return;
        selected=-1;hint=null;hammer=false;foreach(var s in turn.Steps)steps.Enqueue(s);Save();NextStep(clock.Elapsed.TotalSeconds);
    }
    private void ShowHint()
    {
        if(game is null || Busy)return;hint=game.Hint();idleSince=clock.Elapsed.TotalSeconds;hammer=false;Say("Поменяйте подсвеченные кристаллы");
    }
    public void Suspend()
    {
        suspended=true;sound.Pause();steps.Clear();step=null;swapBoard=null;pending=null;
        if(game is not null){board=Game.Snapshot(game.State.Gems);ice=(bool[])game.State.Ice.Clone();if(screen=="game")modal=game.State.Finished?"result":"pause";}
        Save();
    }
    public void ResumeGame(){suspended=false;lastTime=clock.Elapsed.TotalSeconds;Invalidate();}
    private void PausePlay()
    {
        steps.Clear();step=null;swapBoard=null;pending=null;hammer=false;selected=-1;
        if(game is not null){board=Game.Snapshot(game.State.Gems);ice=(bool[])game.State.Ice.Clone();modal=game.State.Finished?"result":"pause";}
        Save();
    }
    public bool Back()
    {
        if(modal is "help" or "settings" or "achievements"){modal=returnModal;returnModal="";return true;}
        if(modal is "restart" or "replace"){modal=screen=="game"?"pause":"";return true;}
        if(screen=="map"){screen="home";return true;}
        if(screen=="game"){if(modal=="pause")modal="";else if(modal=="result")GoHome();else PausePlay();return true;}
        return false;
    }
    private void Say(string text){toast=text;toastUntil=clock.Elapsed.TotalSeconds+3;}
    public override bool OnTouchEvent(MotionEvent? e)
    {
        if(e is null)return false;float x=(e.GetX()-offsetX)/scale,y=e.GetY()/scale;
        if(e.Action==MotionEventActions.Down){downX=x;downY=y;return true;}
        if(e.Action==MotionEventActions.Cancel)return true;
        if(e.Action!=MotionEventActions.Up)return true;
        PerformClick();idleSince=clock.Elapsed.TotalSeconds;
        if(screen=="game" && modal.Length==0 && !Busy)
        {
            int CellAt(float px,float py)=>px<BoardX || px>=BoardX+Cell*8 || py<boardY || py>=boardY+Cell*8?-1:(int)((py-boardY)/Cell)*8+(int)((px-BoardX)/Cell);
            int start=CellAt(downX,downY),end=CellAt(x,y);
            if(start>=0)
            {
                if(hammer){UseHammer(start);return true;}
                float dx=x-downX,dy=y-downY;
                if(Math.Max(Math.Abs(dx),Math.Abs(dy))>14)
                {
                    int next=start+(Math.Abs(dx)>Math.Abs(dy)?(dx>0?1:-1):(dy>0?8:-8));
                    if(Game.Adjacent(start,next))TrySwap(start,next);return true;
                }
                if(end<0)return true;
                if(selected>=0 && Game.Adjacent(selected,end))TrySwap(selected,end);
                else {selected=selected==end?-1:end;hint=null;sound.Play("tap");}return true;
            }
        }
        if(Math.Abs(x-downX)+Math.Abs(y-downY)<25)
        {
            var hit=hits.LastOrDefault(b=>b.Rect.Contains(x,y) && b.Rect.Contains(downX,downY));
            if(hit is not null){sound.Play("tap");hit.Action();Invalidate();}
        }
        return true;
    }
    public override bool PerformClick(){base.PerformClick();return true;}
    public void Release(){sound.Dispose();paint.Dispose();regular.Dispose();bold.Dispose();logo.Dispose();logoBounds.Dispose();}

    private void DrawBackground(Canvas c,double now)
    {
        using(var gradient=new RadialGradient(320,60,390,new[]{AColor.Argb(115,70,46,130).ToArgb(),AColor.Argb(0,17,20,38).ToArgb()},null,Shader.TileMode.Clamp!))
        {paint.Color=AColor.White;paint.SetStyle(Paint.Style.Fill);paint.SetShader(gradient);c.DrawRect(0,0,400,h,paint);paint.SetShader(null);}
        for(int i=0;i<24;i++)
        {
            float x=(i*97+31)%390,y=(i*157+37)%(int)h;
            paint.Color=AColor.Argb((int)(15+12*(1+Math.Sin(now*.55+i))),183,169,237);c.DrawCircle(x,y,i%4==0?1.3f:.7f,paint);
        }
    }
    private void Round(Canvas c,float x,float y,float w,float height,float radius,AColor color)
    {paint.SetShader(null);paint.SetStyle(Paint.Style.Fill);paint.Color=color;c.DrawRoundRect(x,y,x+w,y+height,radius,radius,paint);}
    private void Outline(Canvas c,float x,float y,float w,float height,float radius,AColor color,float stroke=1)
    {paint.SetShader(null);paint.SetStyle(Paint.Style.Stroke);paint.StrokeWidth=stroke;paint.Color=color;c.DrawRoundRect(x,y,x+w,y+height,radius,radius,paint);paint.SetStyle(Paint.Style.Fill);}
    private void Text(Canvas c,string value,float x,float baseline,float size,AColor color,bool heavy=false,Paint.Align? align=null)
    {paint.SetShader(null);paint.Color=color;paint.SetStyle(Paint.Style.Fill);paint.TextSize=size;paint.SetTypeface(heavy?bold:regular);paint.TextAlign=align??Paint.Align.Left;c.DrawText(value,x,baseline,paint);paint.TextAlign=Paint.Align.Left;}
    private void Paragraph(Canvas c,string text,float x,float y,float width,float size=13,AColor? color=null)
    {
        paint.TextSize=size;paint.SetTypeface(regular);float line=0;
        foreach(string block in text.Split('\n'))
        {
            string current="";
            foreach(string word in block.Split(' '))
            {
                string next=current.Length==0?word:current+" "+word;
                if(paint.MeasureText(next)>width && current.Length>0){Text(c,current,x,y+line,size,color??Muted);line+=size*1.6f;current=word;}else current=next;
            }
            Text(c,current,x,y+line,size,color??Muted);line+=size*1.6f;
        }
    }
    private void Button(Canvas c,string label,float x,float y,float w,float height,Action action,bool primary=false,bool enabled=true,float size=13)
    {
        Round(c,x,y+3,w,height,16,AColor.Argb(35,0,0,0));
        if(primary && enabled)
        {
            using var grad=new LinearGradient(x,y,x+w,y+height,new[]{AColor.Rgb(199,177,255).ToArgb(),AColor.Rgb(159,136,238).ToArgb()},null,Shader.TileMode.Clamp!);
            paint.Color=AColor.White;paint.SetShader(grad);paint.SetStyle(Paint.Style.Fill);c.DrawRoundRect(x,y,x+w,y+height,16,16,paint);paint.SetShader(null);
        }
        else Round(c,x,y,w,height,16,enabled?Panel:AColor.Rgb(24,28,47));
        if(!primary)Outline(c,x,y,w,height,16,enabled?Line:AColor.Rgb(34,38,60));
        Text(c,label,x+w/2,y+height/2+size*.34f,size,!enabled?AColor.Rgb(76,83,113):primary?Bg:Ink,true,Paint.Align.Center);
        if(enabled)hits.Add(new(new Bounds(x,y,x+w,y+height),action));
    }
    private void Logo(Canvas c,float centerX,float centerY,float size)
    {paint.Color=AColor.White;paint.SetShader(null);logoBounds.Set(centerX-size/2,centerY-size/2,centerX+size/2,centerY+size/2);c.DrawBitmap(logo,null,logoBounds,paint);}
    private void Star(Canvas c,float x,float y,float r,AColor color)
    {
        using var p=new APath();for(int i=0;i<10;i++){double angle=-Math.PI/2+i*Math.PI/5;float d=i%2==0?r:r*.45f;float px=x+(float)Math.Cos(angle)*d,py=y+(float)Math.Sin(angle)*d;if(i==0)p.MoveTo(px,py);else p.LineTo(px,py);}p.Close();paint.Color=color;c.DrawPath(p,paint);
    }
    private void GemDraw(Canvas c,Gem gem,float x,float y,float size=32,float alpha=1,float rotation=0)
    {
        if(alpha<=0)return;c.Save();c.Translate(x,y);if(rotation!=0)c.Rotate(rotation);
        var color=gem.Power==Power.Prism?Accent:Colors[gem.Color];
        using var shape=new APath();float r=size*.5f;
        if(gem.Power==Power.Prism)
        {for(int i=0;i<8;i++){float a=-MathF.PI/2+i*MathF.PI/4,d=i%2==0?r:r*.43f;float px=MathF.Cos(a)*d,py=MathF.Sin(a)*d;if(i==0)shape.MoveTo(px,py);else shape.LineTo(px,py);}shape.Close();}
        else if(gem.Color==1)shape.AddRoundRect(-r,-r,r,r,r*.35f,r*.35f,APath.Direction.Cw!);
        else if(gem.Color==2 || gem.Color==4)
        {int n=gem.Color==2?6:8;for(int i=0;i<n;i++){float a=-MathF.PI/2+i*MathF.PI*2/n,px=MathF.Cos(a)*r,py=MathF.Sin(a)*r;if(i==0)shape.MoveTo(px,py);else shape.LineTo(px,py);}shape.Close();}
        else if(gem.Color==5){shape.MoveTo(0,-r);shape.LineTo(r,r*.8f);shape.LineTo(-r,r*.8f);shape.Close();}
        else if(gem.Color==3)shape.AddCircle(0,0,r,APath.Direction.Cw!);
        else {shape.MoveTo(0,-r);shape.LineTo(r,0);shape.LineTo(0,r);shape.LineTo(-r,0);shape.Close();}
        paint.SetStyle(Paint.Style.Fill);paint.Color=AColor.Argb((int)(55*alpha),0,0,0);c.Save();c.Translate(0,3);c.DrawPath(shape,paint);c.Restore();
        AColor bright=AColor.Argb((int)(255*alpha),Math.Min(255,color.R+33),Math.Min(255,color.G+33),Math.Min(255,color.B+33));
        AColor dark=AColor.Argb((int)(255*alpha),(int)(color.R*.65),(int)(color.G*.65),(int)(color.B*.77));
        using(var grad=new LinearGradient(-r,-r,r,r,(gem.Power==Power.Prism?new[]{bright,Mint,Colors[3],Colors[0]}:new[]{bright,color,dark}).Select(x=>x.ToArgb()).ToArray(),null,Shader.TileMode.Clamp!))
        {paint.SetShader(grad);paint.Alpha=(int)(255*alpha);c.DrawPath(shape,paint);paint.SetShader(null);paint.Alpha=255;}
        c.Save();c.ClipPath(shape);using(var face=new APath()){face.MoveTo(-r,-r);face.LineTo(r,-r);face.LineTo(r*.45f,-r*.16f);face.LineTo(-r*.35f,r*.2f);face.Close();paint.Color=AColor.Argb((int)(75*alpha),255,255,255);c.DrawPath(face,paint);}c.Restore();
        paint.Color=AColor.Argb((int)(120*alpha),255,255,255);paint.SetStyle(Paint.Style.Stroke);paint.StrokeWidth=.9f;c.DrawPath(shape,paint);paint.SetStyle(Paint.Style.Fill);
        paint.Color=AColor.Argb((int)(235*alpha),255,255,255);paint.StrokeWidth=2;
        if(gem.Power==Power.Row){c.DrawLine(-r*.72f,-2,r*.72f,-2,paint);c.DrawLine(-r*.72f,3,r*.72f,3,paint);}
        if(gem.Power==Power.Column){c.DrawLine(-2,-r*.72f,-2,r*.72f,paint);c.DrawLine(3,-r*.72f,3,r*.72f,paint);}
        if(gem.Power==Power.Bomb){paint.SetStyle(Paint.Style.Stroke);c.DrawCircle(0,0,r*.4f,paint);paint.SetStyle(Paint.Style.Fill);Star(c,0,0,r*.22f,AColor.White);}
        c.Restore();
    }
}
