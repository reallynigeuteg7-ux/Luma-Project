using Android.Graphics;
using AColor=Android.Graphics.Color;

namespace Luma;
public sealed partial class LumaView
{
    private void DrawHome(Canvas c,double now)
    {
        Text(c,"LUMA STUDIO",28,42,10,Muted,true);
        Button(c,"?",288,20,36,36,()=>{returnModal="";modal="help";},size:16);
        Button(c,"☷",334,20,38,36,()=>{returnModal="";modal="settings";},size:19);
        float hero=Math.Min(h*.28f,245);
        float bob=profile.CalmMotion?0:(float)Math.Sin(now*1.15)*5;
        Logo(c,200,hero-6+bob,175);
        GemDraw(c,new(){Color=1},57,hero+57,24,.7f,-16);
        GemDraw(c,new(){Color=2},343,hero-40,21,.7f,17);
        GemDraw(c,new(){Color=3},327,hero+104,15,.5f,12);
        Text(c,"LUMA",200,hero+137,58,Ink,true,Paint.Align.Center);
        Text(c,"Т Р И  В  Р Я Д",200,hero+164,11,Accent,true,Paint.Align.Center);
        Text(c,"Найди свой маленький момент света.",200,hero+204,12,Muted,false,Paint.Align.Center);
        float y=Math.Max(hero+234,h-269);
        bool saved=profile.Current is {Finished:false};
        string label=saved?profile.Current!.LevelNumber==0?"Продолжить поток":"Продолжить · уровень "+profile.Current.LevelNumber:"Начать путешествие";
        Button(c,label,36,y,328,58,()=>{if(saved)Continue();else NewGame(profile.Unlocked);},true,size:15);
        Button(c,"Уровни",36,y+72,158,50,()=>{screen="map";mapPage=(profile.Unlocked-1)/15;});
        Button(c,"Тихий поток",206,y+72,158,50,()=>RequestGame(0));
        Text(c,"30 уровней",105,y+148,11,Muted,false,Paint.Align.Center);
        Text(c,"Без лимита ходов",285,y+148,11,Muted,false,Paint.Align.Center);
        Text(c,$"{profile.Stars.Sum()} / 90",147,h-45,12,Accent,true,Paint.Align.Center);Star(c,108,h-49,7,Accent);
        Button(c,"Достижения",205,h-68,158,38,()=>{returnModal="";modal="achievements";},size:11);
        Text(c,"ОФЛАЙН  ·  БЕЗ РЕКЛАМЫ",200,h-17,8,Muted,false,Paint.Align.Center);
    }
    private void DrawMap(Canvas c)
    {
        Button(c,"‹",24,20,42,40,()=>screen="home",size:27);
        Text(c,"Путешествие",82,48,21,Ink,true);
        Text(c,$"{profile.Stars.Sum()} / 90",372,47,12,Accent,true,Paint.Align.Right);
        Text(c,mapPage==0?"От первого света к лунному берегу":"От сияния к сердцу кристалла",28,94,12,Muted);
        float top=125,spacing=Math.Min(99,(h-233)/5);
        for(int i=0;i<15;i++)
        {
            int n=mapPage*15+i+1;float x=26+i%3*119,y=top+i/3*spacing;bool unlocked=n<=profile.Unlocked;
            Round(c,x,y,110,spacing-12,18,unlocked?Panel:AColor.Rgb(22,26,44));
            Outline(c,x,y,110,spacing-12,18,n==profile.Unlocked?Accent:Line,n==profile.Unlocked?1.5f:1);
            Text(c,n.ToString("00"),x+55,y+35,24,unlocked?Ink:AColor.Rgb(70,76,104),true,Paint.Align.Center);
            if(unlocked){for(int s=0;s<3;s++)Star(c,x+37+s*18,y+56,6,s<profile.Stars[n-1]?Accent:Line);hits.Add(new(new Bounds(x,y,x+110,y+spacing-12),()=>RequestGame(n)));}
            else Text(c,"ЗАКРЫТ",x+55,y+59,8,AColor.Rgb(74,80,107),true,Paint.Align.Center);
        }
        Button(c,"‹",26,h-80,64,43,()=>mapPage=0,enabled:mapPage>0,size:26);
        Text(c,$"{mapPage+1} / 2",200,h-52,13,Muted,true,Paint.Align.Center);
        Button(c,"›",310,h-80,64,43,()=>mapPage=1,enabled:mapPage<1,size:26);
        Text(c,"Пройди уровень, чтобы открыть следующий",200,h-18,10,Muted,false,Paint.Align.Center);
    }
    private void DrawGame(Canvas c,double now,float dt)
    {
        if(game is null)return;var s=game.State;var rules=game.Rules;
        Button(c,"Ⅱ",24,20,42,40,PausePlay,size:18);
        Text(c,rules.Number==0?"ТИХИЙ ПОТОК":"УРОВЕНЬ "+rules.Number.ToString("00"),82,34,10,Accent,true);
        Text(c,rules.Name,82,56,15,Ink,true);
        Round(c,298,16,78,56,16,Panel);Text(c,rules.Number==0?"∞":s.Moves.ToString(),337,44,27,s.Moves<=5 && rules.Number!=0?Colors[3]:Ink,true,Paint.Align.Center);
        Text(c,rules.Number==0?"СВОБОДА":"ХОДОВ",337,60,8,Muted,true,Paint.Align.Center);
        displayedScore=Math.Min(s.Score,displayedScore+Math.Max(1,(int)((s.Score-displayedScore)*dt*10)));
        Text(c,displayedScore.ToString("N0"),26,112,27,Ink,true);
        Text(c,rules.Number==0?"РЕКОРД "+profile.ZenBest.ToString("N0"):"ЦЕЛЬ "+rules.Target.ToString("N0"),373,110,10,Muted,true,Paint.Align.Right);
        Round(c,26,128,348,5,2,Line);
        float progress=rules.Number==0?(s.Score%3000)/3000f:Math.Min(1,displayedScore/(float)rules.Target);
        if(progress>0)Round(c,26,128,Math.Max(5,348*progress),5,2,Accent);
        if(rules.Collect==0 && rules.Ice==0)
        {
            Round(c,26,152,348,49,13,Panel);Star(c,49,176,10,Accent);
            Text(c,rules.Number==0?"Выдохни. Каждый ход — в своём ритме.":"Собери очки и зажги свою первую звезду",70,181,11,Muted);
        }
        else
        {
            float w=rules.Ice>0?168:348;
            Round(c,26,152,w,49,13,Panel);GemDraw(c,new(){Color=rules.Color},50,176,23);
            Text(c,$"{Math.Min(s.Collected,rules.Collect)} / {rules.Collect}",72,182,17,s.Collected>=rules.Collect?Mint:Ink,true);
            if(rules.Ice==0)Text(c,"СОБЕРИ КРИСТАЛЛЫ",360,181,8,Muted,true,Paint.Align.Right);
            if(rules.Ice>0)
            {
                Round(c,206,152,168,49,13,Panel);Outline(c,221,164,25,25,6,Colors[4],1.5f);
                Text(c,$"{s.IceBroken} / {rules.Ice}",258,182,17,s.IceBroken>=rules.Ice?Mint:Ink,true);
                Text(c,"ЛЁД",362,181,8,Muted,true,Paint.Align.Right);
            }
        }
        DrawBoard(c,now);
        float tools=boardY+Cell*8+26;
        Button(c,"Подсказка",26,tools,110,49,ShowHint,enabled:!Busy,size:11);
        Button(c,$"Молоток · {s.Hammers}",145,tools,110,49,()=>{hammer=!hammer;selected=-1;hint=null;},primary:hammer,enabled:!Busy && s.Hammers>0,size:11);
        Button(c,$"Смешать · {s.Shuffles}",264,tools,110,49,Shuffle,enabled:!Busy && s.Shuffles>0,size:11);
        Text(c,hammer?"Нажми на кристалл, который хочешь разбить":"Свайпни кристалл или выбери два соседних",200,tools+76,10,hammer?Accent:Muted,false,Paint.Align.Center);
        if(now-idleSince>7 && !Busy && modal.Length==0 && hint is null)hint=game.Hint();
    }
    private void DrawBoard(Canvas c,double now)
    {
        Round(c,18,boardY-8,364,364,22,AColor.Rgb(23,27,47));Outline(c,18,boardY-8,364,364,22,Line);
        for(int i=0;i<64;i++)
        {
            float x=BoardX+i%8*Cell,y=boardY+i/8*Cell;
            Round(c,x+2,y+2,Cell-4,Cell-4,9,(i/8+i%8)%2==0?AColor.Rgb(30,35,59):AColor.Rgb(27,32,55));
            if(ice[i])
            {
                Round(c,x+2,y+2,Cell-4,Cell-4,9,AColor.Argb(35,138,211,255));
                Outline(c,x+2,y+2,Cell-4,Cell-4,9,AColor.Argb(150,148,213,255));
            }
            if(i==selected || hint is {} hintPair && (i==hintPair.A || i==hintPair.B) && !Busy)
            {
                float pulse=.6f+.4f*(float)Math.Sin(now*4);Outline(c,x+1,y+1,Cell-2,Cell-2,10,i==selected?Ink:AColor.Argb((int)(150+100*pulse),186,162,255),2);
            }
        }
        c.Save();c.ClipRect(BoardX,boardY,BoardX+Cell*8,boardY+Cell*8);
        if(swapBoard is not null)
        {
            float t=(float)((now-animationStart)/((swapValid?.18:.32)*(profile.CalmMotion?.35:1)));t=Math.Clamp(t,0,1);t=swapValid?t*t*(3-2*t):MathF.Sin(t*MathF.PI);
            for(int i=0;i<64;i++)if(swapBoard[i] is {} g)
            {
                float col=i%8,row=i/8;if(i==swapA){col+=(swapB%8-swapA%8)*t;row+=(swapB/8-swapA/8)*t;}if(i==swapB){col+=(swapA%8-swapB%8)*t;row+=(swapA/8-swapB/8)*t;}
                GemDraw(c,g,BoardX+(col+.5f)*Cell,boardY+(row+.5f)*Cell);
            }
        }
        else if(step is not null)
        {
            float t=Math.Clamp((float)((now-animationStart)/(Duration(step)*(profile.CalmMotion?.35:1))),0,1);
            for(int i=0;i<64;i++)
            {
                float col=i%8,row=i/8,alpha=1,size=32;
                var gem=step.Kind==StepKind.Clear?step.Before[i]:step.After[i];if(gem is null)continue;
                if(step.Kind==StepKind.Drop)
                {
                    int old=Array.FindIndex(step.Before,g=>g?.Id==gem.Id);float from=old<0?row-8:old/8;
                    float eased=1-MathF.Pow(1-t,3);row=from+(row-from)*eased;
                }
                if(step.Kind==StepKind.Clear && step.Cleared.Contains(i)){alpha=1-t;size=32*(1+t*.35f);}
                if(step.Kind==StepKind.Shuffle){alpha=Math.Abs(t-.5f)*2;size=32*(.6f+.4f*alpha);}
                GemDraw(c,gem,BoardX+(col+.5f)*Cell,boardY+(row+.5f)*Cell,size,alpha);
            }
            if(step.Kind==StepKind.Clear && !profile.CalmMotion)
            {
                foreach(int i in step.Cleared)for(int p=0;p<5;p++)
                {
                    float a=p*MathF.PI*2/5+i,d=8+t*29,x=BoardX+(i%8+.5f)*Cell+MathF.Cos(a)*d,y=boardY+(i/8+.5f)*Cell+MathF.Sin(a)*d+t*t*15;
                    var col=Colors[step.Before[i]?.Color??0];paint.Color=AColor.Argb((int)((1-t)*220),col.R,col.G,col.B);c.DrawCircle(x,y,2*(1-t)+.5f,paint);
                }
            }
        }
        else for(int i=0;i<board.Length;i++)if(board[i] is {} gem)GemDraw(c,gem,BoardX+(i%8+.5f)*Cell,boardY+(i/8+.5f)*Cell);
        c.Restore();
        if(step is {Kind:StepKind.Clear,Cascade:>1})
        {
            Round(c,133,boardY+138,134,54,18,AColor.Argb(235,59,43,104));
            Text(c,$"КАСКАД ×{step.Cascade}",200,boardY+161,12,Ink,true,Paint.Align.Center);
            Text(c,$"+{step.Points}",200,boardY+181,12,Accent,true,Paint.Align.Center);
        }
        if(hammer)Outline(c,18,boardY-8,364,364,22,Accent,2);
    }
    private void ModalBase(Canvas c,string eyebrow,string title,float height,out float top)
    {
        paint.SetShader(null);paint.Color=AColor.Argb(210,8,10,23);c.DrawRect(0,0,400,h,paint);
        top=(h-height)/2;Round(c,22,top,356,height,25,AColor.Rgb(28,31,54));Outline(c,22,top,356,height,25,AColor.Rgb(64,60,93));
        Text(c,eyebrow,200,top+35,10,Accent,true,Paint.Align.Center);Text(c,title,200,top+73,25,Ink,true,Paint.Align.Center);
    }
    private void DrawModal(Canvas c,double now)
    {
        if(modal=="intro" && game is not null)
        {
            var r=game.Rules;ModalBase(c,r.Number==0?"БЕЗ СПЕШКИ":"УРОВЕНЬ "+r.Number,r.Name,420,out float y);
            Logo(c,200,y+140,88);
            if(r.Number==0)Paragraph(c,"Играй в своём ритме. Ходы бесконечны, а каждый каскад приближает новый рекорд.",53,y+215,294,13);
            else
            {
                Text(c,$"Набери {r.Target:N0} очков за {r.Moves} ходов",200,y+215,13,Ink,true,Paint.Align.Center);
                if(r.Collect>0){GemDraw(c,new(){Color=r.Color},75,y+246,23);Text(c,$"Собери {r.Collect} кристаллов этого цвета",99,y+251,11,Muted);}
                if(r.Ice>0)Text(c,$"Разбей лёд на {r.Ice} клетках совпадениями",200,y+284,11,Colors[4],false,Paint.Align.Center);
                if(r.Collect==0)Text(c,"Три одинаковых в ряд — и свет зажжётся",200,y+259,11,Muted,false,Paint.Align.Center);
            }
            Button(c,"Начать",48,y+320,304,53,()=>{modal=profile.TutorialSeen?"":"help";returnModal="";profile.TutorialSeen=true;Save();},true,size:15);
            Text(c,"Подсказки бесплатны. Молоток и смешивание — по 2.",200,y+399,8,Muted,false,Paint.Align.Center);
        }
        else if(modal=="pause")
        {
            ModalBase(c,"МОМЕНТ ТИШИНЫ","Пауза",411,out float y);
            Button(c,"Продолжить",48,y+111,304,53,()=>modal="",true,size:15);
            Button(c,"Начать заново",48,y+178,304,48,()=>{confirmation=()=>NewGame(game!.State.LevelNumber);modal="restart";});
            Button(c,"Правила",48,y+240,146,46,()=>{returnModal="pause";modal="help";});
            Button(c,"Настройки",206,y+240,146,46,()=>{returnModal="pause";modal="settings";});
            Button(c,"В главное меню",48,y+302,304,46,GoHome);
            Text(c,"Текущая игра сохранена",200,y+384,10,Muted,false,Paint.Align.Center);
        }
        else if(modal=="restart" || modal=="replace")
        {
            ModalBase(c,"НОВАЯ ПОПЫТКА",modal=="restart"?"Начать заново?":"Начать новую игру?",295,out float y);
            Paragraph(c,"Текущая попытка будет заменена. Пройденные уровни и рекорды сохранятся.",49,y+119,302,13);
            Button(c,"Отмена",48,y+216,146,48,()=>modal=screen=="game"?"pause":"");
            Button(c,"Начать",206,y+216,146,48,()=>{var action=confirmation;confirmation=null;action?.Invoke();},true);
        }
        else if(modal=="result" && game is not null)
        {
            var s=game.State;ModalBase(c,s.Won?"СВЕТ СТАЛ ЯРЧЕ":"ЕЩЁ ОДНА ВОЗМОЖНОСТЬ",s.Won?s.LevelNumber==30?"Путешествие пройдено!":"Уровень пройден!":"Ходы закончились",450,out float y);
            if(s.Won)
            {
                for(int i=0;i<3;i++)Star(c,144+i*56,y+137,i==1?28:23,i<s.Stars?Accent:Line);
            }
            else Logo(c,200,y+141,86);
            Text(c,s.Score.ToString("N0"),200,y+217,38,Ink,true,Paint.Align.Center);
            Text(c,"ОЧКОВ",200,y+239,9,Muted,true,Paint.Align.Center);
            Text(c,s.Won?$"Осталось ходов: {s.Moves}  ·  Лучший каскад: ×{s.BestCascade}":"Попробуй спецкристаллы и бесплатные подсказки",200,y+272,10,Muted,false,Paint.Align.Center);
            Button(c,s.Won?s.LevelNumber<30?"Следующий уровень":"К карте уровней":"Попробовать снова",48,y+302,304,53,()=>{if(s.Won && s.LevelNumber==30){screen="map";mapPage=1;modal="";}else NewGame(s.Won?s.LevelNumber+1:s.LevelNumber);},true,size:14);
            Button(c,"В главное меню",48,y+370,304,46,GoHome);
        }
        else if(modal=="help")
        {
            ModalBase(c,"ПРАВИЛА ИГРЫ","Поймай совпадение",620,out float y);
            Paragraph(c,"Свайпни кристалл или нажми на два соседних. Три одинаковых в линию исчезнут. Неудачный обмен не тратит ход.",48,y+116,304,12);
            int[] colors=[0,1,2,3];Power[] powers=[Power.Row,Power.Bomb,Power.Prism,Power.Column];
            string[] captions=["4 в ряд → линейный кристалл","Т или Г из 5 → бомба 3 × 3","5 в ряд → цветная призма","Две спецфишки → мощное комбо"];
            string[] descriptions=["Собери с ним ряд — очистится вся линия.","Собери с ней ряд — взорвётся область.","Обменяй с цветом, чтобы собрать весь цвет.","Меняй их местами, даже без совпадения."];
            for(int i=0;i<4;i++){GemDraw(c,new(){Color=colors[i],Power=powers[i]},65,y+203+i*69,31);Text(c,captions[i],93,y+198+i*69,11,Ink,true);Paragraph(c,descriptions[i],93,y+218+i*69,250,9);}
            Paragraph(c,"Лёд исчезает под собранными кристаллами. Цели уровня показаны над полем. Молоток и смешивание не тратят ходы.",48,y+478,304,11);
            Button(c,"Всё понятно",48,y+546,304,49,()=>{modal=returnModal;returnModal="";idleSince=clock.Elapsed.TotalSeconds;},true);
        }
        else if(modal=="settings")
        {
            ModalBase(c,"ТВОЙ РИТМ","Настройки",406,out float y);
            Toggle(c,"Звуки кристаллов",y+106,profile.Sound,()=>{profile.Sound=!profile.Sound;Save();});
            Toggle(c,"Вибрация",y+171,profile.Haptics,()=>{profile.Haptics=!profile.Haptics;Save();});
            Toggle(c,"Меньше анимации",y+236,profile.CalmMotion,()=>{profile.CalmMotion=!profile.CalmMotion;Save();});
            Button(c,"Готово",48,y+327,304,49,()=>{modal=returnModal;returnModal="";},true);
        }
        else if(modal=="achievements")
        {
            ModalBase(c,"КОЛЛЕКЦИЯ СВЕТА","Достижения",573,out float y);
            var all=new[]{("Первый луч","Пройди первый уровень",profile.Stars[0]>0),("Созвездие","Собери 15 звёзд",profile.Stars.Sum()>=15),("Собиратель","Собери 1 000 кристаллов",profile.Crystals>=1000),("Цепная реакция","Получи каскад ×4",profile.BestCascade>=4),("Хранитель света","Пройди все 30 уровней",profile.Stars[29]>0)};
            for(int i=0;i<all.Length;i++){float row=y+113+i*69;Star(c,65,row+10,15,all[i].Item3?Accent:Line);Text(c,all[i].Item1,96,row+7,13,all[i].Item3?Ink:Muted,true);Text(c,all[i].Item2,96,row+28,10,Muted);}
            Text(c,$"Всего кристаллов: {profile.Crystals:N0}",200,y+476,11,Muted,false,Paint.Align.Center);
            Button(c,"Продолжить",48,y+503,304,46,()=>modal="",true);
        }
    }
    private void Toggle(Canvas c,string title,float y,bool value,Action toggle)
    {
        Round(c,46,y,308,52,14,Panel);Text(c,title,61,y+31,12,Ink);
        Round(c,292,y+15,44,23,12,value?Accent:Line);paint.Color=value?Bg:Muted;c.DrawCircle(value?325:303,y+26.5f,8,paint);
        hits.Add(new(new Bounds(46,y,354,y+52),toggle));
    }
}
