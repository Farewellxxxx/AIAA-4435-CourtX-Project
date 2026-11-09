using UnityEngine;
namespace CourtX
{
    public static class TacticalRecapUI
    {
        static int frame;
        static GUIStyle body,small,title,button;
        static Vector2 scroll;
        static readonly Color teal=new Color(.28f,.84f,.70f),orange=new Color(1,.58f,.30f);
        public static void Reset(int index){frame=Mathf.Max(0,index);scroll=Vector2.zero;}
        static void Panel(Rect rect,Color color){GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;}
        static void Line(Vector2 a,Vector2 b,Color color,float width=2)
        {
            var matrix=GUI.matrix;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);
            Panel(new Rect(a.x,a.y-width*.5f,(b-a).magnitude,width),color);GUI.matrix=matrix;
        }
        static Vector2 Point(Vector3 p)=>new Vector2(84+(p.x+7.5f)/15*454,168+(-p.z-.3f)/13.5f*384);
        static void Init()
        {
            if(body!=null)return;
            var font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},20);
            body=new GUIStyle(GUI.skin.label){font=font,fontSize=19,wordWrap=true};body.normal.textColor=new Color(.94f,.97f,.94f);
            small=new GUIStyle(body){fontSize=15};title=new GUIStyle(body){fontSize=30,fontStyle=FontStyle.Bold};
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=18,wordWrap=true};
        }
        public static void Draw(TacticalSession s)
        {
            Init();var old=GUI.matrix;float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2),Quaternion.identity,Vector3.one*scale);
            Panel(new Rect(0,0,1280,720),new Color(.025f,.075f,.09f,.98f));
            GUI.Label(new Rect(64,36,1150,48),"回合复盘  /  看清变化，再练一次",title);
            var episode=s.Journal.Episode;
            GUI.Label(new Rect(64,92,1150,35),"你控制 "+s.Number(episode.controlled)+"  ·  "+s.CoverageName+"  ·  练习时长 "+episode.duration.ToString("F1")+" 秒  ·  "+episode.endReason,small);
            Panel(new Rect(64,144,494,428),new Color(.08f,.21f,.23f));
            Line(new Vector2(84,168),new Vector2(538,168),Color.white);
            Line(new Vector2(84,168),new Vector2(84,552),Color.white);Line(new Vector2(538,168),new Vector2(538,552),Color.white);
            Line(new Vector2(84,552),new Vector2(538,552),Color.white);
            var rim=Point(new Vector3(0,0,-1.575f));GUI.Label(new Rect(rim.x-18,rim.y-15,60,30),"篮筐",small);
            if(episode.frames.Count>0)
            {
                frame=Mathf.Clamp(frame,0,episode.frames.Count-1);var f=episode.frames[frame];
                for(int i=1;i<=frame;i++)Line(Point(episode.frames[i-1].positions[episode.controlled]),Point(episode.frames[i].positions[episode.controlled]),new Color(.3f,.75f,.65f,.32f),2);
                for(int i=0;i<3;i++)Line(Point(f.positions[3+i]),Point(f.positions[f.marks[i]]),new Color(.6f,.65f,.67f,.35f),1);
                foreach(var d in episode.decisions)if(d.frame==frame)
                {Line(Point(d.origin),Point(d.destination),teal,4);GUI.Label(new Rect(88,145,440,25),"出手瞬间 · 亮线为实际目标，灰线为盯防关系",small);}
                for(int i=0;i<6;i++)
                {
                    var p=Point(f.positions[i]);Panel(new Rect(p.x-15,p.y-14,31,28),i<3?teal:orange);
                    GUI.color=new Color(.02f,.08f,.09f);GUI.Label(new Rect(p.x-12,p.y-12,35,28),s.Number(i),small);GUI.color=Color.white;
                    if(i==episode.controlled)GUI.Label(new Rect(p.x-18,p.y+14,60,25),"你",small);
                }
                var ball=Point(f.ball);Panel(new Rect(ball.x-4,ball.y-4,8,8),Color.yellow);
                GUI.Label(new Rect(84,580,470,28),"关键帧 "+(frame+1)+" / "+episode.frames.Count+"   "+f.time.ToString("F1")+" 秒",small);
                frame=Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(84,613,454,20),frame,0,episode.frames.Count-1));
            }
            scroll=GUI.BeginScrollView(new Rect(600,144,615,446),scroll,new Rect(0,0,584,610));
            GUI.Label(new Rect(0,0,574,385),s.Review.text,body);
            GUI.Label(new Rect(0,398,574,180),string.IsNullOrEmpty(s.OptionalNarration)?"当前模式：本地规则。所有判断来自出手时的状态和实际事件。":("可选拓展解说\n"+s.OptionalNarration),small);
            GUI.EndScrollView();
            if(GUI.Button(new Rect(600,597,142,34),"上一出手",button))Jump(episode,-1);
            if(GUI.Button(new Rect(752,597,142,34),"下一出手",button))Jump(episode,1);
            GUI.Label(new Rect(910,597,310,38),s.SavedReview.StartsWith("保存失败")?s.SavedReview:"日志已保存到程序的 Replays 文件夹",small);
            GUI.backgroundColor=teal;
            if(GUI.Button(new Rect(64,652,335,45),"同一场景再练  →",button))s.RetryScenario();
            GUI.backgroundColor=Color.white;
            if(GUI.Button(new Rect(420,652,335,45),"下一场景  →",button))s.NextScenario();
            if(GUI.Button(new Rect(776,652,438,45),"选择战术 / 角色 / 防守",button))s.OpenMenu();
            GUI.matrix=old;
        }
        static void Jump(TacticalEpisode episode,int direction)
        {
            if(direction<0){for(int i=episode.decisions.Count-1;i>=0;i--)if(episode.decisions[i].frame<frame){frame=episode.decisions[i].frame;return;}}
            else foreach(var d in episode.decisions)if(d.frame>frame){frame=d.frame;return;}
        }
    }
}
