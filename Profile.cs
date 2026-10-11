using System.Text.Json;
using System.Text.Json.Serialization;

namespace Luma;
public sealed class Profile
{
    public int Version { get; set; }=1;
    public int Unlocked { get; set; }=1;
    public int[] Stars { get; set; }=new int[30];
    public int[] BestScores { get; set; }=new int[30];
    public int ZenBest { get; set; }
    public long Crystals { get; set; }
    public int Wins { get; set; }
    public int BestCascade { get; set; }
    public bool Sound { get; set; }=true;
    public bool Haptics { get; set; }=true;
    public bool CalmMotion { get; set; }
    public bool TutorialSeen { get; set; }
    public Session? Current { get; set; }
    public static Profile Load(string path)
    {
        try
        {
            var p=JsonSerializer.Deserialize(File.ReadAllText(path),SaveJsonContext.Default.Profile);
            if(p is null || p.Version!=1 || p.Stars?.Length!=30 || p.BestScores?.Length!=30)return new();
            p.Unlocked=Math.Clamp(p.Unlocked,1,30);
            if(!Game.IsValid(p.Current))p.Current=null;
            return p;
        }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException or JsonException){return new();}
    }
    public bool Save(string path)
    {
        try{var temp=path+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(this,SaveJsonContext.Default.Profile));File.Move(temp,path,true);return true;}
        catch(Exception e)when(e is IOException or UnauthorizedAccessException){return false;}
    }
}

[JsonSerializable(typeof(Profile))]
internal partial class SaveJsonContext : JsonSerializerContext { }
