using UnityEngine;
namespace CourtX
{
    public static class FiveCourtUI
    {
        static readonly Color Paper=new Color(.967f,.961f,.935f),White=new Color(.998f,.994f,.978f),Ink=new Color(.10f,.21f,.18f),Muted=new Color(.40f,.46f,.41f),Border=new Color(.85f,.86f,.80f),Green=new Color(.20f,.35f,.25f);
        static Font font;static GUIStyle body,small,title,heading,button,badge;
        static float scale,oldScale;static Vector2 offset,historyScroll;
        static int hover=-1,selected=-1,drag=-1;static float hoverSince;static Vector2 dragStart;
        static bool moved;static FiveRecording prior;
        static int courtControl;static FiveBoard dragBoard;
        public static bool Dragging=>drag>=0;
        static int pressedHistory=-1;static bool historyFocus;
        public static Rect CourtPixels{get;private set;}
        public static Rect HistoryPixels{get;private set;}
        public static int HistoryHit(Rect view,Vector2 mouse,float scroll,float size,int count)
        {
            if(!view.Contains(mouse)||mouse.x>view.x+198*size)return -1;
            float row=(mouse.y-view.y+scroll)/size;int index=Mathf.FloorToInt(row/91);
            return index>=0&&index<count&&row-index*91<81?index:-1;
        }
        static Rect R(float x,float y,float w,float h)=>new Rect(offset.x+x*scale,offset.y+y*scale,w*scale,h*scale);
        static void Text(float x,float y,float w,float h,string value,GUIStyle style=null)=>GUI.Label(R(x,y,w,h),value,style??body);
        static void Box(float x,float y,float w,float h,Color c)=>FiveCourtView.Box(R(x,y,w,h),c);
        static void Init()
        {
            scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);offset=new Vector2((Screen.width-1440*scale)*.5f,(Screen.height-900*scale)*.5f);
            if(body!=null&&Mathf.Abs(scale-oldScale)<.001f)return;oldScale=scale;
            if(!font)font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei UI","Microsoft YaHei","Arial"},20);
            body=new GUIStyle(GUI.skin.label){font=font,fontSize=Mathf.Max(14,Mathf.RoundToInt(19*scale)),wordWrap=true,richText=false,padding=new RectOffset(0,0,0,0)};body.normal.textColor=Ink;
            small=new GUIStyle(body){fontSize=Mathf.Max(12,Mathf.RoundToInt(15*scale))};small.normal.textColor=Muted;
            heading=new GUIStyle(body){fontSize=Mathf.Max(15,Mathf.RoundToInt(21*scale)),fontStyle=FontStyle.Bold};
            title=new GUIStyle(heading){fontSize=Mathf.RoundToInt(30*scale)};
            button=new GUIStyle(body){alignment=TextAnchor.MiddleCenter,wordWrap=true};button.normal.background=button.hover.background=button.active.background=null;
            badge=new GUIStyle(body){fontSize=Mathf.Max(12,Mathf.RoundToInt(18*scale)),fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
        }
        static bool Button(float x,float y,float w,float h,string value,bool active=false)
        {
            var r=R(x,y,w,h);Color color=active?Green:White;
            if(r.Contains(Event.current.mousePosition)&&GUI.enabled)color=Color.Lerp(color,Green,.12f);
            if(!GUI.enabled)color=Color.Lerp(color,Paper,.65f);
            FiveCourtView.Box(r,Border);FiveCourtView.Box(new Rect(r.x+1,r.y+1,r.width-2,r.height-2),color);
            button.normal.textColor=button.hover.textColor=button.active.textColor=active?White:Ink;
            return GUI.Button(r,value,button);
        }
        public static void Draw(FiveCourtGame g)
        {
            Init();GUI.color=Color.white;
            if(g.Practicing){Practice(g);return;}
            FiveCourtView.Box(new Rect(0,0,Screen.width,Screen.height),Paper);
            Text(28,23,230,46,"COURTX.",title);Text(220,35,550,26,"五人协作  /  战术学习与即时回放",small);
            Text(1128,35,290,26,"5 × 5    ·    破解 2–3 联防",small);Box(24,83,1392,1,Border);
            if(Button(28,99,190,42,"01  战术板",g.Page==0)){g.ShowPage(0);ResetSelection();}
            if(Button(230,99,190,42,"02  即时回放",g.Page==1)){g.ShowPage(1);ResetSelection();}
            if(g.HasPausedPractice&&Button(1168,99,248,42,"继续暂停的练习  →"))g.ResumePractice();
            Text(451,109,700,26,"暂停在这一帧，改变站位，直接查看新的布防。",small);
            if(g.Page==0)Settings(g);else History(g);
            var b=g.Board;
            if(g.Page==1&&g.History.Count==0)
            {
                Box(264,160,880,620,White);Text(370,365,650,50,"第一轮练习结束后，回放会出现在这里。",heading);
                Text(370,430,640,70,"选择战术并开始 3D 练习，按 V 结束。每轮真实经过都能在这里暂停、拖动和推演。",body);
                if(Button(515,535,355,54,"返回战术板  →",true))g.LoadLesson();Footer(g);return;
            }
            if(prior!=b.Record){ResetSelection();prior=b.Record;}
            Box(264,160,880,620,FiveCourtView.Floor);
            Color old=heading.normal.textColor;heading.normal.textColor=White;
            Text(288,180,540,34,FiveRules.Plays[(int)b.State.play],heading);
            heading.normal.textColor=old;
            var oldSmall=small.normal.textColor;small.normal.textColor=FiveCourtView.Accent;
            Text(888,185,230,28,b.IsFrameStudy?"FRAME  /  暂停布防":b.IsBranch?"WHAT-IF  /  假设推演":g.Page==0?"PLAYBOOK  /  教学演练":"REPLAY  /  第 "+(g.SelectedReplayIndex+1)+" 轮",small);small.normal.textColor=oldSmall;
            CourtPixels=R(433,235,542,506);
            HandleCourt(g);
            FiveCourtView.Draw(CourtPixels,b,badge,hover);
            small.normal.textColor=new Color(.73f,.81f,.70f);Text(290,750,830,25,"●  进攻 01–05     ●  防守 X1–X5     ◯  代入角色     ━┫  掩护     拖尾仅保留 2.5 秒",small);small.normal.textColor=oldSmall;
            Controls(g);Intent(g);Footer(g);Tooltip(g);
        }
        static void CancelDrag()
        {dragBoard?.EndEdit();dragBoard=null;drag=-1;if(courtControl!=0&&GUIUtility.hotControl==courtControl)GUIUtility.hotControl=0;courtControl=0;}
        static void ResetSelection(){CancelDrag();selected=hover=-1;prior=null;}
        static void Settings(FiveCourtGame g)
        {
            Text(28,169,218,28,"进攻战术",heading);
            for(int i=0;i<3;i++)if(Button(28,210+i*65,212,54,(i+1).ToString("00")+"  "+FiveRules.Plays[i],(int)g.Play==i)){g.Play=(FivePlay)i;g.LoadLesson();}
            Text(28,419,215,28,"代入你的角色",heading);
            for(int i=0;i<5;i++)if(Button(28+i*43,460,38,36,"0"+(i+1),g.Role==i)){g.Role=i;g.LoadLesson();}
            Text(28,505,212,28,FiveRules.Roles[g.Role],small);
            Text(28,548,212,26,"开局持球人",small);
            for(int i=0;i<5;i++)if(Button(28+i*43,579,38,34,"0"+(i+1),g.InitialHolder==i)){g.InitialHolder=i;g.LoadLesson();}
            Text(28,643,212,28,"整体防守策略",heading);
            for(int i=0;i<3;i++)if(Button(28,680+i*41,212,34,FiveRules.Defenses[i],(int)g.Defense==i)){g.Defense=(FiveDefense)i;g.LoadLesson();}
        }
        static void History(FiveCourtGame g)
        {
            Text(28,169,212,30,"最近的练习 · "+g.History.Count,heading);Text(28,205,212,49,"点击任一回合切换。绿色标出当前选择，原记录保留。",small);
            var viewport=R(26,271,216,470);HistoryPixels=viewport;float height=g.History.Count*91*scale,max=Mathf.Max(0,height-viewport.height);
            historyScroll.y=Mathf.Clamp(historyScroll.y,0,max);var e=Event.current;
            if(e.type==EventType.ScrollWheel&&viewport.Contains(e.mousePosition)){historyScroll.y=Mathf.Clamp(historyScroll.y+e.delta.y*25*scale,0,max);e.Use();}
            int hit=HistoryHit(viewport,e.mousePosition,historyScroll.y,scale,g.History.Count);
            int control=GUIUtility.GetControlID(0x51F2,FocusType.Passive);
            if(e.type==EventType.MouseDown&&e.button==0&&hit>=0){pressedHistory=hit;historyFocus=true;GUIUtility.hotControl=control;e.Use();}
            if(e.rawType==EventType.MouseUp&&pressedHistory>=0)
            {int clicked=pressedHistory;pressedHistory=-1;GUIUtility.hotControl=0;if(clicked==hit){g.SelectReplay(clicked);ResetSelection();}e.Use();}
            if(historyFocus&&e.type==EventType.KeyDown&&(e.keyCode==KeyCode.UpArrow||e.keyCode==KeyCode.DownArrow)&&g.History.Count>0)
            {int index=Mathf.Clamp(g.SelectedReplayIndex+(e.keyCode==KeyCode.DownArrow?1:-1),0,g.History.Count-1);g.SelectReplay(index);historyScroll.y=Mathf.Clamp(index*91*scale-viewport.height*.4f,0,max);ResetSelection();e.Use();}
            GUI.BeginGroup(viewport);
            for(int i=0;i<g.History.Count;i++)
            {
                var r=g.History[i];var box=new Rect(0,i*91*scale-historyScroll.y,198*scale,81*scale);bool active=r.id==g.SelectedReplayId;
                FiveCourtView.Box(box,active?Green:hit==i?new Color(.90f,.93f,.86f):White);
                button.normal.textColor=active?White:Ink;string value=(active?"● ":"")+(i+1).ToString("00")+"  "+r.name+"\n"+r.created+"\n"+r.End.ToString("F1")+" 秒";
                GUI.Label(box,value,button);
            }
            GUI.EndGroup();if(max>0)historyScroll.y=GUI.VerticalScrollbar(R(229,271,12,470),historyScroll.y,viewport.height,0,height);
            if(g.SkippedFiles>0)Text(28,756,212,45,"已跳过 "+g.SkippedFiles+" 份无法读取的文件。",small);
        }
        static void Controls(FiveCourtGame g)
        {
            var b=g.Board;float end=Mathf.Max(b.Start+.01f,b.End);
            float time=GUI.HorizontalSlider(R(286,791,731,18),b.Time,b.Start,end);
            if(Mathf.Abs(time-b.Time)>.015f){CancelDrag();b.Seek(time);}
            Text(1032,783,107,28,b.Time.ToString("F1")+" / "+b.End.ToString("F1"),small);
            if(Button(280,821,80,40,"起点"))b.Seek(b.Start);
            if(Button(369,821,85,40,"上一拍"))b.Jump(-1);
            if(Button(463,821,112,40,b.IsFrameStudy?"播放原片段":b.Playing?"暂停  Ⅱ":"播放  ▶",true))b.Playing=!b.Playing;
            if(Button(584,821,85,40,"下一拍"))b.Jump(1);
            if(Button(678,821,100,40,b.IsFrameStudy?"原片单步":"单步"))b.StepOnce();
            if(Button(789,821,78,40,b.Speed.ToString("0.##")+"×"))b.Speed=b.Speed<.75f?1:b.Speed<1.25f?1.5f:b.Speed<1.75f?2:.5f;
            GUI.enabled=b.CanUndo;if(Button(880,821,84,40,"撤销"))b.Undo();GUI.enabled=b.IsBranch;
            if(Button(975,821,153,40,"返回原片段")){b.Restore();ResetSelection();}GUI.enabled=true;
        }
        static void Intent(FiveCourtGame g)
        {
            var b=g.Board;var s=b.State;Box(1166,160,250,330,White);
            Text(1184,178,214,28,"这一拍的目标",heading);Text(1184,217,214,25,s.phaseName,small);Box(1184,253,214,1,Border);
            Text(1184,269,214,25,"进攻意图",small);Text(1184,301,214,79,s.attackIntent,small);
            Text(1184,390,214,25,"防守意图",small);Text(1184,420,214,65,s.defenseIntent,small);
            Text(1178,512,226,65,b.Playing?"暂停后拖动球员，直接查看这一帧的新布防。":"拖动即重排布防，时间保持不变。悬停查看当前职责。",small);
            if(selected>=0)
            {
                Text(1178,591,226,28,"已选 "+FiveRules.Number(selected),heading);
                GUI.enabled=!b.Playing;
                if(Button(1178,632,226,38,s.pinned[selected]?"释放站位":"固定此人",s.pinned[selected]))b.SetPinned(selected,!s.pinned[selected]);
                if(selected<5)
                {
                    if(Button(1178,679,226,38,"设为持球者"))b.SetHolder(selected);
                    GUI.enabled=!b.IsFrameStudy&&!b.Playing&&s.holder>=0&&s.holder<5&&s.holder!=selected&&s.flight==null&&!s.turnover;
                    if(Button(1178,726,226,38,"传给 "+FiveRules.Number(selected))){b.Pass(selected);b.Playing=true;}
                }
                GUI.enabled=true;
            }
            if(Button(1174,811,234,56,"开始 3D 练习  →",true)){g.StartPractice();ResetSelection();}
        }
        static void HandleCourt(FiveCourtGame g)
        {
            var b=g.Board;var e=Event.current;int hit=FiveCourtView.Pick(CourtPixels,b.State,e.mousePosition);
            if(hit!=hover){hover=hit;hoverSince=Time.realtimeSinceStartup;}
            int control=GUIUtility.GetControlID(0x51F1,FocusType.Passive);
            if(e.type==EventType.MouseDown&&e.button==0&&hit>=0)
            {
                selected=hit;if(!b.Playing){drag=hit;dragStart=e.mousePosition;moved=false;b.BeginEdit();dragBoard=b;courtControl=control;GUIUtility.hotControl=control;}e.Use();
            }
            if(e.type==EventType.MouseDrag&&drag>=0)
            {
                if(Vector2.Distance(e.mousePosition,dragStart)>3*scale)moved=true;
                if(moved){b.PlaceFrame(drag,FiveCourtView.WorldPoint(CourtPixels,e.mousePosition));prior=b.Record;g.Message="";}e.Use();
            }
            if(e.rawType==EventType.MouseUp&&drag>=0){CancelDrag();e.Use();}
        }
        static void Tooltip(FiveCourtGame g)
        {
            if(hover<0||drag>=0||Time.realtimeSinceStartup-hoverSince<.35f)return;
            string text=FiveRules.Number(hover)+"  "+g.Board.State.tasks[hover];float w=280*scale,h=body.CalcHeight(new GUIContent(text),w-24)+20;
            var mouse=Event.current.mousePosition;float x=Mathf.Clamp(mouse.x+16,4,Screen.width-w-4),y=Mathf.Clamp(mouse.y+20,4,Screen.height-h-4);
            FiveCourtView.Box(new Rect(x-1,y-1,w+2,h+2),Border);FiveCourtView.Box(new Rect(x,y,w,h),White);
            GUI.Label(new Rect(x+12,y+10,w-24,h-20),text,body);
        }
        static void Footer(FiveCourtGame g)
        {
            Text(28,869,g.SavePending?1130:1384,26,string.IsNullOrEmpty(g.Message)?g.Board.Notice:g.Message,small);
            if(g.SavePending&&Button(1180,870,220,27,"重试保存待存回合"))g.RetrySave();
        }
        static void Practice(FiveCourtGame g)
        {
            var s=g.Live;if(s==null)return;
            Box(24,20,630,140,new Color(.97f,.97f,.93f,.94f));
            Text(42,34,590,35,"COURTX  /  "+FiveRules.Plays[(int)s.play],heading);
            Text(42,77,590,29,"你是 "+FiveRules.Number(s.controlled)+" "+FiveRules.Roles[s.controlled]+"    ·    "+FiveRules.Defenses[(int)s.defense],body);
            Text(42,117,590,28,s.phaseName+"  ·  "+(s.flight!=null?"球在传递中":s.holder>=0?FiveRules.Number(s.holder)+" 持球":"回合暂停"),small);
            if(g.Overview)
            {
                Box(325,175,790,610,FiveCourtView.Floor);FiveCourtView.Draw(R(438,203,563,525),g.LiveRecord,s,badge);
                var color=small.normal.textColor;small.normal.textColor=White;Text(365,751,720,26,"实时俯视 · 松开 Tab 回到第一人称 · 场上继续运行",small);small.normal.textColor=color;
            }
            else
            {
                var p=new Vector2(Screen.width*.5f,Screen.height*.5f);Color c=g.Target>=0?FiveCourtView.Accent:Color.white;
                FiveCourtView.Line(p-new Vector2(7,0),p+new Vector2(7,0),c,2);FiveCourtView.Line(p-new Vector2(0,7),p+new Vector2(0,7),c,2);
                Box(485,510,470,47,new Color(.97f,.97f,.93f,.9f));
                string target=s.holder==s.controlled?g.Target>=0?"Q  直接传给 "+FiveRules.Number(g.Target):"指向队友，或按 1–5 选择目标":"E  向持球队友要球";
                Text(502,520,436,30,target,heading);
            }
            Box(24,734,1392,142,new Color(.97f,.97f,.93f,.94f));
            Text(44,750,1350,45,s.success?"决策完成：已经利用防守移动找到接应。按 C 可重新 call，V 查看这一轮。":s.lastEvent,heading);
            Text(44,802,1340,32,"WASD 移动  ·  Shift 加速  ·  鼠标指向队友 + Q 传球  ·  E 要球  ·  Tab 俯视",body);
            Text(44,844,1340,24,"C 重新 call 战术    R 从预设重开    V 结束并回放    Esc 暂停返回战术板",small);
        }
    }
}
