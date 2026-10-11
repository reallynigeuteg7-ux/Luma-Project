namespace Luma;

public enum Power { None, Row, Column, Bomb, Prism }
public sealed class Gem
{
    public int Id { get; set; }
    public int Color { get; set; }
    public Power Power { get; set; }
    public Gem Copy() => new() { Id=Id, Color=Color, Power=Power };
}
public sealed class Level
{
    public int Number { get; init; }
    public int Moves { get; init; }
    public int Target { get; init; }
    public int Color { get; init; }
    public int Collect { get; init; }
    public int Ice { get; init; }
    public int Colors { get; init; }
    public string Name => Number==0?"Тихий поток":new[]{"Первый свет","Лавандовый сад","Лунный берег","Северное сияние","Звёздная пыль","Сердце кристалла"}[(Number-1)/5];
    public static Level Get(int n) => n==0 ? new(){Number=0,Colors=6,Moves=999,Target=3000} : new()
    {
        Number=n, Moves=27+(n<=5?3:0)+(n>20?3:0), Colors=n<=5?5:6,
        Target=950+n*75, Color=(n+2)% (n<=5?5:6), Collect=n<3?0:12+n/2,
        Ice=n<6?0:Math.Min(24,6+(n-6)/2)
    };
}
public sealed class Session
{
    public int Version { get; set; }=1;
    public int LevelNumber { get; set; }
    public int Moves { get; set; }
    public int Score { get; set; }
    public int Collected { get; set; }
    public int IceBroken { get; set; }
    public int Crystals { get; set; }
    public int BestCascade { get; set; }
    public int Hammers { get; set; }=2;
    public int Shuffles { get; set; }=2;
    public uint RandomState { get; set; }
    public int NextId { get; set; }
    public Gem?[] Gems { get; set; }=new Gem?[64];
    public bool[] Ice { get; set; }=new bool[64];
    public bool Finished { get; set; }
    public bool Won { get; set; }
    public bool Recorded { get; set; }
    public int Stars { get; set; }
}
public enum StepKind { Clear, Drop, Shuffle }
public sealed class GameStep
{
    public StepKind Kind { get; init; }
    public Gem?[] Before { get; init; }=[];
    public Gem?[] After { get; init; }=[];
    public bool[] Ice { get; init; }=[];
    public int[] Cleared { get; init; }=[];
    public int Points { get; init; }
    public int Cascade { get; init; }
    public bool Special { get; init; }
}
public sealed class Turn
{
    public bool Valid { get; set; }
    public List<GameStep> Steps { get; }=[];
}

// Pure C# rules: the Android view only plays the resulting animation steps.
public sealed class Game
{
    public Session State { get; }
    public Level Rules => Level.Get(State.LevelNumber);
    public Game(Session state) { State=state; }
    public static Game New(int level, uint seed=0)
    {
        var g=new Game(new(){LevelNumber=level,Moves=Level.Get(level).Moves,RandomState=seed==0?(uint)Random.Shared.Next(1,int.MaxValue):seed});
        g.FillFresh();
        var places=Enumerable.Range(0,64).OrderBy(_=>g.Next(100000)).Take(g.Rules.Ice);
        foreach(int i in places)g.State.Ice[i]=true;
        return g;
    }
    public static bool IsValid(Session? s)
    {
        if(s is null || s.Version!=1 || s.LevelNumber<0 || s.LevelNumber>30 || s.Gems?.Length!=64 || s.Ice?.Length!=64 || s.Moves<0 || s.Score<0 || s.Collected<0 || s.Hammers<0 || s.Shuffles<0)return false;
        if(s.Gems.Any(g=>g is null || g.Color<0 || g.Color>=Level.Get(s.LevelNumber).Colors || !Enum.IsDefined(g.Power)))return false;
        return s.Gems.Select(g=>g!.Id).Distinct().Count()==64;
    }
    private int Next(int max)
    {
        uint x=State.RandomState; if(x==0)x=1234567; x^=x<<13;x^=x>>17;x^=x<<5;State.RandomState=x;
        return (int)(x%(uint)max);
    }
    private Gem Create() => new(){Id=++State.NextId, Color=Next(Rules.Colors)};
    public static Gem?[] Snapshot(Gem?[] gems)=>gems.Select(g=>g?.Copy()).ToArray();
    public static bool Adjacent(int a,int b)=>a>=0 && a<64 && b>=0 && b<64 && Math.Abs(a/8-b/8)+Math.Abs(a%8-b%8)==1;
    private void Swap(int a,int b)=>(State.Gems[a],State.Gems[b])=(State.Gems[b],State.Gems[a]);
    public List<int[]> Runs()
    {
        var result=new List<int[]>();
        for(int direction=0;direction<2;direction++)for(int line=0;line<8;line++)
        {
            int start=0;
            while(start<8)
            {
                int Idx(int p)=>direction==0?line*8+p:p*8+line;
                var gem=State.Gems[Idx(start)];int end=start+1;
                if(gem is not null && gem.Power!=Power.Prism)
                    while(end<8 && State.Gems[Idx(end)] is {} next && next.Power!=Power.Prism && next.Color==gem.Color)end++;
                if(end-start>=3)result.Add(Enumerable.Range(start,end-start).Select(Idx).ToArray());
                start=end;
            }
        }
        return result;
    }
    public (int A,int B)? Hint()
    {
        for(int a=0;a<64;a++)foreach(int b in new[]{a%8<7?a+1:-1,a<56?a+8:-1})
        {
            if(b<0)continue;
            if(IsSpecialSwap(a,b))return(a,b);
            Swap(a,b);bool match=Runs().Count>0;Swap(a,b);
            if(match)return(a,b);
        }
        return null;
    }
    private bool IsSpecialSwap(int a,int b)
    {
        var x=State.Gems[a];var y=State.Gems[b];return x is not null && y is not null &&
            (x.Power==Power.Prism || y.Power==Power.Prism || x.Power!=Power.None && y.Power!=Power.None);
    }
    public Turn TrySwap(int a,int b)
    {
        var turn=new Turn();if(State.Finished || !Adjacent(a,b))return turn;
        bool special=IsSpecialSwap(a,b);Swap(a,b);
        var runs=Runs();if(!special && runs.Count==0){Swap(a,b);return turn;}
        turn.Valid=true;if(State.LevelNumber!=0)State.Moves--;
        HashSet<int>? forced=null;
        if(special)
        {
            forced=[a,b];var x=State.Gems[a]!;var y=State.Gems[b]!;
            if(x.Power==Power.Prism && y.Power==Power.Prism)forced.UnionWith(Enumerable.Range(0,64));
            else if(x.Power==Power.Prism || y.Power==Power.Prism)
            {
                var other=x.Power==Power.Prism?y:x;
                for(int i=0;i<64;i++)if(State.Gems[i]?.Color==other.Color)
                {forced.Add(i);if(other.Power!=Power.None)State.Gems[i]!.Power=other.Power;}
            }
            else
            {
                int r=b/8,c=b%8;
                if(x.Power==Power.Bomb && y.Power==Power.Bomb)
                    for(int i=0;i<64;i++){if(Math.Abs(i/8-r)<=2 && Math.Abs(i%8-c)<=2)forced.Add(i);}
                else if(x.Power==Power.Bomb || y.Power==Power.Bomb)
                    for(int i=0;i<64;i++){if(Math.Abs(i/8-r)<=1 || Math.Abs(i%8-c)<=1)forced.Add(i);}
                else for(int i=0;i<8;i++){forced.Add(r*8+i);forced.Add(i*8+c);}
            }
        }
        Resolve(turn,forced,b,a);return turn;
    }
    public Turn Hammer(int cell)
    {
        var turn=new Turn();if(State.Finished || State.Hammers<=0 || cell<0 || cell>=64)return turn;
        State.Hammers--;turn.Valid=true;Resolve(turn,[cell],-1,-1);return turn;
    }
    public Turn Shuffle()
    {
        var turn=new Turn();if(State.Finished || State.Shuffles<=0)return turn;
        State.Shuffles--;turn.Valid=true;DoShuffle(turn);return turn;
    }
    private void Resolve(Turn turn,HashSet<int>? forced,int preferred,int alternate)
    {
        int chain=0;
        while(chain<80)
        {
            var runs=Runs();if(runs.Count==0 && forced is null)break;chain++;
            var before=Snapshot(State.Gems);var ice=(bool[])State.Ice.Clone();
            var clear=new HashSet<int>(runs.SelectMany(r=>r));var created=new Dictionary<int,Power>();
            if(forced is null)
            {
                var remaining=runs.ToList();
                while(remaining.Count>0)
                {
                    var group=new List<int[]>{remaining[0]};remaining.RemoveAt(0);var members=new HashSet<int>(group[0]);
                    bool changed;do{changed=false;for(int k=remaining.Count-1;k>=0;k--)if(remaining[k].Any(members.Contains)){members.UnionWith(remaining[k]);group.Add(remaining[k]);remaining.RemoveAt(k);changed=true;}}while(changed);
                    Power power=group.Any(r=>r.Length>=5)?Power.Prism:group.Count>1?Power.Bomb:group[0].Length==4?(group[0][1]-group[0][0]==1?Power.Row:Power.Column):Power.None;
                    if(power==Power.None)continue;
                    int pos=members.Contains(preferred)?preferred:members.Contains(alternate)?alternate:group[0][group[0].Length/2];
                    if(State.Gems[pos]!.Power!=Power.None)pos=members.FirstOrDefault(i=>State.Gems[i]!.Power==Power.None,-1);
                    if(pos>=0)created[pos]=power;
                }
            }
            else clear.UnionWith(forced);
            foreach(int i in created.Keys)clear.Remove(i);
            var queue=new Queue<int>(clear);var fired=new HashSet<int>();bool activated=false;
            while(queue.TryDequeue(out int pos))
            {
                if(!fired.Add(pos) || State.Gems[pos] is not {} gem || gem.Power==Power.None)continue;
                activated=true;var more=new List<int>();
                if(gem.Power==Power.Row)more.AddRange(Enumerable.Range(0,8).Select(i=>pos/8*8+i));
                if(gem.Power==Power.Column)more.AddRange(Enumerable.Range(0,8).Select(i=>i*8+pos%8));
                if(gem.Power==Power.Bomb)more.AddRange(Enumerable.Range(0,64).Where(i=>Math.Abs(i/8-pos/8)<=1 && Math.Abs(i%8-pos%8)<=1));
                if(gem.Power==Power.Prism && (forced is null || forced.Count==1)){int color=Next(Rules.Colors);more.AddRange(Enumerable.Range(0,64).Where(i=>State.Gems[i]?.Color==color));}
                foreach(int i in more)if(clear.Add(i))queue.Enqueue(i);
            }
            foreach(var item in created)if(!clear.Contains(item.Key))State.Gems[item.Key]!.Power=item.Value;
            int count=0;
            foreach(int i in clear)
            {
                if(State.Gems[i] is {} gem){count++;if(gem.Color==Rules.Color)State.Collected++;}
                State.Gems[i]=null;
                if(State.Ice[i]){State.Ice[i]=false;State.IceBroken++;}
            }
            int points=count*30*Math.Min(chain,5)+created.Count*60;
            State.Score+=points;State.Crystals+=count;State.BestCascade=Math.Max(State.BestCascade,chain);
            turn.Steps.Add(new(){Kind=StepKind.Clear,Before=before,After=Snapshot(State.Gems),Ice=ice,Cleared=clear.ToArray(),Points=points,Cascade=chain,Special=activated || created.Count>0});
            var old=Snapshot(State.Gems);
            for(int c=0;c<8;c++)
            {
                int write=7;
                for(int r=7;r>=0;r--)if(State.Gems[r*8+c] is {} g){State.Gems[write*8+c]=g;if(write!=r)State.Gems[r*8+c]=null;write--;}
                while(write>=0){State.Gems[write*8+c]=Create();write--;}
            }
            turn.Steps.Add(new(){Kind=StepKind.Drop,Before=old,After=Snapshot(State.Gems),Ice=(bool[])State.Ice.Clone(),Cascade=chain});
            forced=null;preferred=alternate=-1;
        }
        if(Runs().Count>0 || Hint() is null)DoShuffle(turn);
        if(State.LevelNumber!=0)
        {
            State.Won=State.Score>=Rules.Target && State.Collected>=Rules.Collect && State.IceBroken>=Rules.Ice;
            State.Finished=State.Won || State.Moves==0;
            if(State.Won)State.Stars=State.Moves>=10 || State.Score>=Rules.Target*1.75?3:State.Moves>=4 || State.Score>=Rules.Target*1.3?2:1;
        }
    }
    private void DoShuffle(Turn turn)
    {
        var before=Snapshot(State.Gems);
        bool success=false;
        for(int tries=0;tries<150;tries++)
        {
            for(int i=63;i>0;i--)Swap(i,Next(i+1));
            if(Runs().Count==0 && Hint() is not null){success=true;break;}
        }
        if(!success)FillFresh();
        turn.Steps.Add(new(){Kind=StepKind.Shuffle,Before=before,After=Snapshot(State.Gems),Ice=(bool[])State.Ice.Clone()});
    }
    private void FillFresh()
    {
        for(int attempt=0;attempt<1000;attempt++)
        {
            for(int i=0;i<64;i++)
            {
                Gem g;do{g=Create();}while(i%8>=2 && State.Gems[i-1]?.Color==g.Color && State.Gems[i-2]?.Color==g.Color || i>=16 && State.Gems[i-8]?.Color==g.Color && State.Gems[i-16]?.Color==g.Color);
                State.Gems[i]=g;
            }
            if(Hint() is not null)return;
        }
        throw new InvalidOperationException("Не удалось создать игровое поле.");
    }
}
