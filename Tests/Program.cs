using Luma;
using System.Text.Json;

int assertions=0,played=0;
void Check(bool condition,string message){assertions++;if(!condition)throw new Exception(message);}
Game Fixture()
{
    var s=new Session{LevelNumber=6,Moves=30,RandomState=42,NextId=64};
    for(int i=0;i<64;i++)s.Gems[i]=new(){Id=i+1,Color=(i/8*2+i%8)%6};
    return new(s);
}
for(int level=0;level<=30;level++)for(uint seed=1;seed<=20;seed++)
{
    var g=Game.New(level,seed);Check(Game.IsValid(g.State),"Invalid new state");
    Check(g.Runs().Count==0,"Starting match");Check(g.Hint() is not null,"No starting move");
    Check(g.State.Ice.Count(x=>x)==g.Rules.Ice,"Ice count");
    for(int i=0;i<20 && !g.State.Finished;i++)
    {
        var hint=g.Hint()!.Value;int moves=g.State.Moves,score=g.State.Score;
        var turn=g.TrySwap(hint.A,hint.B);played++;
        Check(turn.Valid,"Hint is not legal");Check(g.State.Moves==moves-(level==0?0:1),"Move accounting");
        Check(g.State.Score>score,"Valid move scores zero");Check(g.Runs().Count==0,"Unresolved cascade");
        Check(Game.IsValid(g.State),"Invalid resolved board");Check(g.Hint() is not null,"Dead board");
        Check(turn.Steps.Count>0,"Missing animation");
        Check(JsonSerializer.Serialize(turn.Steps[^1].After)==JsonSerializer.Serialize(g.State.Gems),"Animation final state differs");
    }
}
{
    var g=Fixture();string before=JsonSerializer.Serialize(g.State);Check(!g.TrySwap(0,1).Valid,"Invalid move accepted");
    Check(JsonSerializer.Serialize(g.State)==before,"Invalid move changed state");
    Check(!g.TrySwap(-1,0).Valid && !g.TrySwap(0,63).Valid,"Out-of-bounds move accepted");
}
foreach(int length in new[]{4,5})
{
    var g=Fixture();for(int c=0;c<length;c++)g.State.Gems[24+c]!.Color=1;
    g.State.Gems[26]!.Color=2;g.State.Gems[34]!.Color=1;
    var result=g.TrySwap(34,26);Check(result.Valid,"Special formation move");
    Check(result.Steps[0].After.Any(x=>x?.Power==(length==4?Power.Row:Power.Prism)),"Special not created");
}
{
    var g=Fixture();foreach(int i in new[]{18,19,20,12,28})g.State.Gems[i]!.Color=1;
    g.State.Gems[20]!.Color=2;g.State.Gems[21]!.Color=1;
    var t=g.TrySwap(21,20);Check(t.Valid,"T formation invalid");Check(t.Steps[0].After.Any(x=>x?.Power==Power.Bomb),"Bomb not formed");
}
{
    var g=Fixture();g.State.Gems[0]!.Power=Power.Prism;g.State.Gems[1]!.Power=Power.Prism;
    g.State.Ice=Enumerable.Repeat(true,64).ToArray();var t=g.TrySwap(0,1);
    Check(t.Steps[0].Cleared.Length==64,"Double prism must clear board");Check(g.State.IceBroken==64,"Ice must clear once per cell");
}
{
    var g=Fixture();g.State.Gems[0]!.Power=Power.Row;g.State.Gems[1]!.Power=Power.Column;
    var t=g.TrySwap(0,1);Check(t.Valid,"Power combo invalid");Check(t.Steps[0].Cleared.Length>=15,"Power combo too small");
}
{
    var g=Game.New(7,56);int moves=g.State.Moves;g.Hammer(12);Check(g.State.Hammers==1 && g.State.Moves==moves,"Hammer cost");
    g.Hammer(14);Check(!g.Hammer(16).Valid,"Unlimited hammers");
    g.Shuffle();Check(g.State.Shuffles==1 && g.State.Moves==moves && g.Runs().Count==0,"Shuffle cost/stability");
}
{
    var g=Game.New(30,28);g.State.Score=99999;g.State.Collected=999;g.State.IceBroken=999;g.State.Moves=1;
    var hint=g.Hint()!.Value;g.TrySwap(hint.A,hint.B);Check(g.State.Finished && g.State.Won,"Last-move win lost");
    Check(!g.TrySwap(0,1).Valid,"Finished game accepts input");
    var loss=Game.New(30,72);loss.State.Moves=1;var next=loss.Hint()!.Value;loss.TrySwap(next.A,next.B);Check(loss.State.Finished && !loss.State.Won,"Loss missing");
}
{
    string path=Path.Combine(Path.GetTempPath(),"luma-test-"+Guid.NewGuid().ToString("N")+".json");
    try
    {
        var p=new Profile{Current=Game.New(9,127).State,Unlocked=9,Sound=false};Check(p.Save(path),"Save failed");
        var loaded=Profile.Load(path);Check(JsonSerializer.Serialize(p)==JsonSerializer.Serialize(loaded),"Save round trip");
        var a=new Game(p.Current!);var b=new Game(loaded.Current!);var move=a.Hint()!.Value;a.TrySwap(move.A,move.B);b.TrySwap(move.A,move.B);
        Check(JsonSerializer.Serialize(a.State)==JsonSerializer.Serialize(b.State),"RNG state not restored");
        File.WriteAllText(path,"{broken");Check(Profile.Load(path).Unlocked==1,"Corrupt save fallback");
    }
    finally{if(File.Exists(path))File.Delete(path);}
}
Console.WriteLine($"PASS: {assertions:N0} assertions, {played:N0} simulated moves across 30 levels and Zen.");
