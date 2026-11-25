using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Time=UnityEngine.Time;
namespace CourtX
{
    public static class FiveCourtUI
    {
        static readonly Color Paper=new Color(.967f,.961f,.935f),White=new Color(.998f,.994f,.978f),Ink=new Color(.10f,.21f,.18f),Muted=new Color(.40f,.46f,.41f),Border=new Color(.85f,.86f,.80f),Green=new Color(.20f,.35f,.25f);
        static Font font,serif;static GUIStyle body,small,title,heading,button,badge,possession;
        static GUIStyle hudTitle,hudInfo,hudCue,hudCenter,miniNumber;
#if UNITY_ANDROID && !UNITY_EDITOR
        static GUIStyle touchButton;
        static float historyTouchStartY,historyTouchScroll;
        static bool historySwiped;
#endif
        static readonly Color HudPanel=new Color(.055f,.13f,.11f,.94f),HudText=new Color(.97f,.97f,.93f),HudMuted=new Color(.78f,.84f,.79f);
        static float scale,oldScale,historyScroll;static Vector2 offset;
        static int hover=-1,selected=-1,drag=-1;static float hoverSince;static Vector2 dragStart;
        static bool moved;static FiveRecording prior;
        static int courtControl;static FiveBoard dragBoard;
        public static bool Dragging=>drag>=0;
        static string pressedHistory;static bool historyFocus,practiceSettings,more;
        public static Rect CourtPixels{get;private set;}
        public static Rect HistoryPixels{get;private set;}
        public readonly struct BoardLayout
        {
            public readonly float Scale;
            public readonly Vector2 Offset;
            public BoardLayout(int width,int height)
            {
                Scale=Mathf.Min(width/1440f,height/900f);
                Offset=new Vector2((width-1440*Scale)*.5f,(height-900*Scale)*.5f);
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            public BoardLayout(Rect safe):this((int)safe.width,(int)safe.height){Offset+=safe.position;}
#endif
            public Rect At(float x,float y,float w,float h)=>new Rect(Offset.x+x*Scale,Offset.y+y*Scale,w*Scale,h*Scale);
            public Rect Canvas=>At(224,68,962,676);
            public Rect Court=>At(375,103,660,660*14f/15);
            public Rect History=>At(18,126,200,650);
            public Rect Intent=>At(1198,68,224,372);
            public Rect Timeline=>At(224,747,962,96);
        }
        // All rectangles use the same top-left pixel coordinates as OnGUI.
        public readonly struct PracticeLayout
        {
            public readonly Rect View,MapPanel,MapCourt,Header,Footer;
            public readonly float Scale,Gap;
            public readonly Vector2 EdgeSize;
            public PracticeLayout(Rect view,bool teaching)
            {
                View=view;
                Scale=Mathf.Clamp(Mathf.Min(view.width/1280f,view.height/800f),.55f,2.25f);
                Gap=Mathf.Max(5,12*Scale);
                float pad=9*Scale;
                float courtWidth=Mathf.Min(206*Scale,Mathf.Min(view.width*.235f,view.height*.26f));
                MapCourt=new Rect(view.x+Gap+pad,view.y+Gap+pad,courtWidth,courtWidth*14f/15);
                MapPanel=new Rect(view.x+Gap,view.y+Gap,courtWidth+2*pad,MapCourt.height+2*pad+Mathf.Max(14,20*Scale));
                float headerWidth=Mathf.Min(315*Scale,view.width*.48f);
                Header=new Rect(view.xMax-Gap-headerWidth,view.y+Gap,headerWidth,Mathf.Max(39,55*Scale));
                float footerWidth=Mathf.Min(view.width-2*Gap,(teaching?790:510)*Scale);
                float footerHeight=teaching?Mathf.Max(57,72*Scale):Mathf.Max(22,30*Scale);
                Footer=new Rect(view.center.x-footerWidth*.5f,view.yMax-Gap-footerHeight,footerWidth,footerHeight);
                EdgeSize=new Vector2(Mathf.Max(91,128*Scale),Mathf.Max(23,30*Scale));
            }
        }
        public readonly struct HistoryRow
        {
            public const float Height=78,Width=188,HeaderHeight=27;
            public readonly float Y;
            public readonly string Group,Details;
            public HistoryRow(float y,string group,string details){Y=y;Group=group;Details=details;}
        }
        public static HistoryRow[] HistoryLayout(IReadOnlyList<FiveRecording> records,DateTime now,out float height)
        {
            var rows=new HistoryRow[records.Count];height=0;
            DateTime? previous=null;
            for(int i=0;i<records.Count;i++)
            {
                var record=records[i];var date=record.CreatedAt(now);DateTime? day=date?.Date;
                string group=null;
                if(i==0||day!=previous)
                {
                    group=day==now.Date?"今天":day==now.Date.AddDays(-1)?"昨天":day?.ToString("yyyy年M月d日",CultureInfo.InvariantCulture)??"日期未知";
                    height+=HistoryRow.HeaderHeight;previous=day;
                }
                string clock=record.CreatedClock(now);
                rows[i]=new HistoryRow(height,group,clock+" · "+(record.End-record.Start).ToString("0.0")+" 秒");
                height+=HistoryRow.Height+8;
            }
            return rows;
        }
        public static int HistoryHit(Rect view,Vector2 mouse,float scroll,float size,IReadOnlyList<HistoryRow> rows)
        {
            if(!view.Contains(mouse)||mouse.x>=view.x+HistoryRow.Width*size)return -1;
            float y=(mouse.y-view.y+scroll)/size;
            int low=0,high=rows.Count-1;
            while(low<=high)
            {
                int mid=(low+high)/2;float top=rows[mid].Y;
                if(y<top)high=mid-1;else if(y>=top+HistoryRow.Height)low=mid+1;else return mid;
            }
            return -1;
        }
        static Rect R(float x,float y,float w,float h)=>new Rect(offset.x+x*scale,offset.y+y*scale,w*scale,h*scale);
        static void Text(float x,float y,float w,float h,string value,GUIStyle style=null)=>GUI.Label(R(x,y,w,h),value,style??body);
        static string Fit(string value,float width,float height,GUIStyle style)
        {
            if(string.IsNullOrEmpty(value)||style.CalcHeight(new GUIContent(value),width)<=height)return value;
            int low=0,high=value.Length;
            while(low<high)
            {
                int mid=(low+high+1)/2;
                if(style.CalcHeight(new GUIContent(value.Substring(0,mid)+"…"),width)<=height)low=mid;else high=mid-1;
            }
            return value.Substring(0,low)+"…";
        }
        static void TextFit(Rect r,string value,GUIStyle style)=>GUI.Label(r,Fit(value,r.width,r.height,style),style);
        static void Box(float x,float y,float w,float h,Color c)=>FiveCourtView.Box(R(x,y,w,h),c);
        static void Init()
        {
            if(body!=null&&Mathf.Abs(scale-oldScale)<.001f)return;oldScale=scale;
#if UNITY_ANDROID && !UNITY_EDITOR
            if(!font)font=Resources.Load<Font>("FiveMobileUI");
            serif=font;
#else
            if(!serif)serif=Font.CreateDynamicFontFromOSFont(new[]{"SimSun","Songti SC","Noto Serif CJK SC","Georgia"},28);
            if(!font)font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei UI","Microsoft YaHei","PingFang SC","Heiti SC","Arial"},20);
#endif
            body=new GUIStyle(GUI.skin.label){font=font,fontSize=Mathf.Max(12,Mathf.RoundToInt(18*scale)),wordWrap=true,richText=false,padding=new RectOffset(0,0,0,0)};body.normal.textColor=Ink;
            small=new GUIStyle(body){fontSize=Mathf.Max(10,Mathf.RoundToInt(15*scale))};small.normal.textColor=Muted;
            heading=new GUIStyle(body){font=serif?serif:font,fontSize=Mathf.Max(13,Mathf.RoundToInt(20*scale)),fontStyle=FontStyle.Bold};
            title=new GUIStyle(heading){fontSize=Mathf.RoundToInt(28*scale)};
            possession=new GUIStyle(body){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,wordWrap=false};
            hudTitle=new GUIStyle(possession){alignment=TextAnchor.MiddleLeft,fontSize=Mathf.Max(12,Mathf.RoundToInt(17*scale))};hudTitle.normal.textColor=HudText;
            hudInfo=new GUIStyle(small){alignment=TextAnchor.MiddleLeft,wordWrap=false};hudInfo.normal.textColor=HudMuted;
            hudCue=new GUIStyle(body){alignment=TextAnchor.MiddleLeft,wordWrap=true,fontSize=Mathf.Max(12,Mathf.RoundToInt(16*scale))};hudCue.normal.textColor=HudText;
            hudCenter=new GUIStyle(hudInfo){alignment=TextAnchor.MiddleCenter};
#if UNITY_ANDROID && !UNITY_EDITOR
            touchButton=new GUIStyle(hudCenter){fontSize=Mathf.Max(14,Mathf.RoundToInt(24*scale))};
#endif
            miniNumber=new GUIStyle(possession);
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
            if(g.Practicing)
            {
                var eye=g.player?g.player.eyes:null;
                var view=eye?eye.pixelRect:new Rect(0,0,Screen.width,Screen.height);
                view.y=Screen.height-view.yMax;
                var hud=new PracticeLayout(view,!g.IsDrill);
                scale=hud.Scale;Init();GUI.color=Color.white;
                Practice(g,eye,hud);return;
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            var layout=new BoardLayout(FiveTouch.SafeArea);
#else
            var layout=new BoardLayout(Screen.width,Screen.height);
#endif
            scale=layout.Scale;offset=layout.Offset;
            Init();GUI.color=Color.white;
            FiveCourtView.Box(new Rect(0,0,Screen.width,Screen.height),Paper);
            Text(18,16,150,36,"COURTX.",title);
            if(Button(174,11,130,39,"战术板",g.Page==0)&&g.Page!=0){g.ShowPage(0);ChangeTab();}
            if(Button(313,11,130,39,"即时演练",g.Page==2)&&g.Page!=2){g.OpenDrill();ChangeTab();}
            if(Button(452,11,130,39,"即时回放",g.Page==1)&&g.Page!=1){g.ShowPage(1);ChangeTab();}
            Text(611,22,340,24,"五人协作  /  破解 2–3 联防",small);
            if(g.HasPausedPractice&&!(g.Page==2&&g.IsDrill)&&Button(1200,11,222,39,g.IsDrill?"继续暂停的演练  →":"继续暂停的练习  →"))g.ResumePractice();
            Box(18,58,1404,1,Border);
            if(g.Page==2){DrillLobby(g);return;}
            HistoryPixels=layout.History;
            if(g.Page==0)Settings(g);else History(g);
            var b=g.Board;
            if(g.Page==1&&g.History.Count==0)
            {
                FiveCourtView.Box(layout.Canvas,White);CourtPixels=Rect.zero;Text(370,365,650,50,"第一轮练习结束后，回放会出现在这里。",heading);
#if UNITY_ANDROID && !UNITY_EDITOR
                Text(370,430,640,70,"选择战术并开始练习，点「看回放」保存本轮。每轮都能在这里暂停、拖动和推演。",body);
#else
                Text(370,430,640,70,"选择战术并开始 3D 练习，按 V 结束。每轮真实经过都能在这里暂停、拖动和推演。",body);
#endif
                if(Button(515,535,355,54,"返回战术板  →",true)){g.LoadLesson();ChangeTab();}Footer(g);return;
            }
            if(prior!=b.Record){ResetSelection();prior=b.Record;}
            bool drillReplay=g.Page==1&&b.TimelineRecord.IsDrill;
            FiveCourtView.Box(layout.Canvas,FiveCourtView.Floor);
            Box(224,68,962,32,new Color(.085f,.19f,.15f));
            Color old=heading.normal.textColor;heading.normal.textColor=White;
            Text(242,75,750,28,drillReplay?"即时演练":b.Record.PlayTitle+BranchSuffix(b.State),heading);
            heading.normal.textColor=old;
            var oldSmall=small.normal.textColor;small.normal.textColor=FiveCourtView.Accent;
            Text(1035,79,140,24,b.IsFrameStudy?"暂停布防":b.IsBranch?"假设推演":g.Page==0?"教学演练":"回放 · 第 "+(g.SelectedReplayIndex+1)+" 轮",small);small.normal.textColor=oldSmall;
            CourtPixels=layout.Court;
            HandleCourt(g);
            FiveCourtView.Draw(CourtPixels,b,badge,hover,showTacticalOverlays:!drillReplay);
            small.normal.textColor=FiveCourtView.Offense;Text(242,694,122,24,"●  进攻 01–05",small);
            small.normal.textColor=FiveCourtView.Defense;Text(242,716,122,24,"●  防守 X1–X5",small);
            small.normal.textColor=FiveCourtView.Accent;Text(1055,694,120,24,"◯  你的角色",small);
            small.normal.textColor=new Color(.73f,.81f,.70f);Text(1055,716,120,24,b.IsFrameStudy?"绿点  已固定":"短线  近 2.5 秒",small);
            small.normal.textColor=oldSmall;
            Controls(g);
            if(drillReplay)DrillRecap(g,layout.Intent);else {Intent(g,layout.Intent);Tooltip(g);}
            Footer(g);
        }
        static void DrillLobby(FiveCourtGame g)
        {
            CourtPixels=HistoryPixels=Rect.zero;
            bool paused=g.HasPausedPractice&&g.IsDrill;
            bool finished=FiveDrill.IsFinished(g.Live);
            if(finished)
            {
                Box(365,236,710,340,White);
                Text(415,275,600,35,"即时演练 · 回合结果",small);
                TextFit(R(415,336,610,90),FiveDrill.Summary(g.Live),title);
                if(Button(415,460,250,54,"下一轮即时演练  →",true))g.StartDrill();
#if UNITY_ANDROID && !UNITY_EDITOR
                if(Button(683,460,250,54,"查看本轮回放")){g.SelectReplay(g.SelectedReplayId);ChangeTab();}
#endif
            }
            else DrawDrillStart(g,paused);
            if(g.SavePending)
            {
                Text(18,880,1070,20,"有回合尚未保存。",small);
                if(Button(1198,855,224,34,"重试保存待存回合"))g.RetrySave();
            }
        }
        static void DrawDrillStart(FiveCourtGame g,bool paused)
        {
            Box(74,145,1292,570,White);
            Box(715,145,651,570,FiveCourtView.Floor);
            if(Event.current.type==EventType.Repaint)
            {
                var court=R(776,178,529,494);
                GUI.DrawTexture(court,FiveCourtView.CourtTexture(court.size));
            }
            Text(122,222,530,65,paused?"演练已暂停":"即时演练",title);
            string description=paused
                ?"你控制 "+FiveRules.Number(g.Live.controlled)+" "+FiveRules.Roles[g.Live.controlled]+"。继续后将回到暂停时的场上位置。"
                :"每轮随机抽取一个回合并分配角色。观察队友和防守人的位置，自己决定如何处理球。";
            Text(122,310,502,105,description,body);
            if(paused)
            {
                if(Button(122,471,220,58,"继续本轮  →",true))g.ResumePractice();
                if(Button(357,471,210,58,"开始新一轮"))g.StartDrill();
            }
            else if(Button(122,471,250,58,"开始演练  →",true))g.StartDrill();
#if UNITY_ANDROID && !UNITY_EDITOR
            Text(122,583,510,84,"左侧摇杆移动，滑动空白区域转向。用准星瞄准队友或篮筐，点右侧出球按钮。",small);
#else
            Text(122,583,510,84,"WASD 移动，鼠标转向。持球时用准星瞄准队友或篮筐，按 Q 传球或投篮。",small);
#endif
        }
        static void CancelDrag()
        {dragBoard?.EndEdit();dragBoard=null;drag=-1;if(courtControl!=0&&GUIUtility.hotControl==courtControl)GUIUtility.hotControl=0;courtControl=0;}
        static void ResetSelection(){CancelDrag();selected=hover=-1;prior=null;}
        static void ChangeTab(){ResetSelection();pressedHistory=null;historyFocus=false;more=false;}
        static void Settings(FiveCourtGame g)
        {
            Text(18,77,194,28,"进攻战术",heading);
            for(int i=0;i<3;i++)if(Button(18,112+i*48,194,41,FiveZoneBook.PlayName((FivePlay)i),(int)g.Play==i)){g.SelectPlay((FivePlay)i);practiceSettings=false;}
            Box(18,269,194,1,Border);
            Text(18,286,194,30,"战术分支",heading);
            for(int i=0;i<3;i++)
                if(Button(18,324+i*44,194,38,FiveZoneBook.BranchName(g.Play,i),g.Branch==i)&&g.Branch!=i)
                {g.SelectBranch(i);ResetSelection();}
            Box(18,467,194,1,Border);
            Text(18,484,194,30,"我的角色",heading);
            Text(18,523,194,38,FiveRules.Number(g.Role)+" "+FiveRules.Roles[g.Role]+(FiveCoach.Recommended(g.Play)==g.Role?" · 推荐":""),body);
            if(Button(18,565,105,35,practiceSettings?"收起":"换角色"))practiceSettings=!practiceSettings;
            if(practiceSettings)
                for(int i=0;i<5;i++)
                    if(Button(18,611+i*40,194,35,FiveRules.Number(i)+" "+FiveRules.Roles[i],g.Role==i))
                    {g.Role=i;g.LoadLesson();practiceSettings=false;}
        }
        static string BranchSuffix(FiveState s)=>s.playbookVersion==2?"  /  "+FiveZoneBook.BranchName(s.play,s.branch):"";
        static void History(FiveCourtGame g)
        {
            Text(18,77,200,27,"练习记录 · "+g.History.Count,heading);Text(18,106,200,21,"点击回合查看原片段",small);
            var viewport=HistoryPixels;var rows=HistoryLayout(g.History,DateTime.Now,out float height);
            float visible=viewport.height/scale,max=Mathf.Max(0,height-visible);
            historyScroll=Mathf.Clamp(historyScroll,0,max);var e=Event.current;
            if(e.type==EventType.ScrollWheel&&viewport.Contains(e.mousePosition)){historyScroll=Mathf.Clamp(historyScroll+e.delta.y*25,0,max);e.Use();}
            int hit=HistoryHit(viewport,e.mousePosition,historyScroll*scale,scale,rows);
            int control=GUIUtility.GetControlID(0x51F2,FocusType.Passive);
            if(e.type==EventType.MouseDown&&e.button==0)
            {
                historyFocus=hit>=0;
#if UNITY_ANDROID && !UNITY_EDITOR
                historyTouchStartY=e.mousePosition.y;historyTouchScroll=historyScroll;historySwiped=false;
#endif
                if(historyFocus){pressedHistory=g.History[hit].id;GUIUtility.hotControl=control;e.Use();}
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            if(e.type==EventType.MouseDrag&&pressedHistory!=null)
            {
                float delta=(historyTouchStartY-e.mousePosition.y)/scale;
                if(Mathf.Abs(delta)>8)historySwiped=true;
                if(historySwiped)historyScroll=Mathf.Clamp(historyTouchScroll+delta,0,max);
                e.Use();
            }
#endif
            if(e.rawType==EventType.MouseUp&&e.button==0&&pressedHistory!=null)
            {
                string id=pressedHistory;pressedHistory=null;if(GUIUtility.hotControl==control)GUIUtility.hotControl=0;
#if UNITY_ANDROID && !UNITY_EDITOR
                if(historySwiped)hit=-1;
#endif
                if(hit>=0&&g.History[hit].id==id){g.SelectReplay(id);ResetSelection();}
                e.Use();
            }
            if(historyFocus&&e.type==EventType.KeyDown&&(e.keyCode==KeyCode.UpArrow||e.keyCode==KeyCode.DownArrow)&&g.History.Count>0)
            {
                int index=Mathf.Clamp(g.SelectedReplayIndex+(e.keyCode==KeyCode.DownArrow?1:-1),0,g.History.Count-1);
                g.SelectReplay(g.History[index].id);historyScroll=Mathf.Clamp(rows[index].Y-visible*.4f,0,max);
                ResetSelection();e.Use();
            }
            GUI.BeginGroup(viewport);
            var previousSmall=small.normal.textColor;var previousBody=body.normal.textColor;var previousHeading=heading.normal.textColor;
            for(int i=0;i<rows.Length;i++)
            {
                var row=rows[i];float y=row.Y-historyScroll;
                if(y+HistoryRow.Height<0||y-HistoryRow.HeaderHeight>visible)continue;
                Rect Local(float x,float dy,float w,float h)=>new Rect(x*scale,(y+dy)*scale,w*scale,h*scale);
                var r=g.History[i];var box=Local(0,0,HistoryRow.Width,HistoryRow.Height);bool active=r.id==g.SelectedReplayId;
                small.normal.textColor=previousSmall;
                if(row.Group!=null)GUI.Label(Local(2,-HistoryRow.HeaderHeight+4,HistoryRow.Width-4,22),row.Group,small);
                FiveCourtView.Box(box,active?Green:hit==i?new Color(.90f,.93f,.86f):White);
                var start=r.frames[0];heading.normal.textColor=body.normal.textColor=active?White:Ink;
                small.normal.textColor=active?new Color(.83f,.88f,.78f):previousSmall;
                TextFit(Local(9,5,170,25),r.IsDrill?"即时演练":r.PlayTitle,heading);
                string role="我控 "+FiveRules.Number(start.controlled);
                GUI.Label(Local(9,31,170,23),r.IsDrill?role+" "+FiveRules.Roles[start.controlled]:role+" · "+r.CooperationOutcome,body);
                GUI.Label(Local(9,55,170,22),row.Details,small);
            }
            small.normal.textColor=previousSmall;body.normal.textColor=previousBody;heading.normal.textColor=previousHeading;
            GUI.EndGroup();
            if(max>0)historyScroll=GUI.VerticalScrollbar(new Rect(viewport.x+190*scale,viewport.y,10*scale,viewport.height),historyScroll,visible,0,height);
            if(g.SkippedFiles>0)Text(18,787,194,48,"已跳过 "+g.SkippedFiles+" 份无法读取的文件。",small);
        }
        static FiveRecording markerRecord;
        static int markerFrameCount,markerEvent;
        static float markerEnd;
        static (float time,string label)[] markers=Array.Empty<(float,string)>();
        static (float time,string label)[] TimelineMarkers(FiveRecording record)
        {
            int count=record.frames.Count,serial=record.frames[count-1].eventSerial;
            if(markerRecord==record&&markerFrameCount==count&&markerEvent==serial&&markerEnd==record.End)return markers;
            markerRecord=record;markerFrameCount=count;markerEvent=serial;markerEnd=record.End;
            var candidates=new List<float>();float gap=(record.End-record.Start)*.038f,last=record.Start-gap;
            foreach(float time in record.Nodes())
            {
                if(time<=record.Start+.04f||time>=record.End-.04f||time-last<gap)continue;
                candidates.Add(time);last=time;
            }
            var result=new List<(float,string)>();
            int length=Math.Min(6,candidates.Count);
            for(int i=0;i<length;i++)
            {
                float t=candidates[length<=1?0:Mathf.RoundToInt(i*(candidates.Count-1f)/(length-1))];
                var frame=record.Sample(t);
                result.Add((t,record.IsDrill?Possession(frame):string.IsNullOrEmpty(frame.lastEvent)?frame.phaseName:frame.lastEvent));
            }
            return markers=result.ToArray();
        }
        static void Controls(FiveCourtGame g)
        {
            var b=g.Board;float end=Mathf.Max(b.Start+.01f,b.End);
            const float trackStart=244,trackWidth=710;
            string markerLabel="节点标记 · 点击跳转";
            foreach(var marker in TimelineMarkers(b.TimelineRecord))
            {
                float x=trackStart+Mathf.InverseLerp(b.Start,end,marker.time)*trackWidth;
                if(R(x-11,750,22,25).Contains(Event.current.mousePosition))
                    markerLabel=marker.time.ToString("F1")+" 秒 · "+marker.label;
                if(Button(x-11,750,22,25,"◆",Mathf.Abs(marker.time-b.Time)<.08f))
                {CancelDrag();b.Seek(marker.time);}
            }
            TextFit(R(973,750,205,25),markerLabel,small);
            float time=GUI.HorizontalSlider(R(trackStart-6,780,trackWidth+12,19),b.Time,b.Start,end);
            if(Mathf.Abs(time-b.Time)>.015f){CancelDrag();b.Seek(time);}
            Text(978,778,200,24,b.Time.ToString("F1")+" / "+b.End.ToString("F1")+" 秒",small);
            if(Button(224,805,108,37,"上一节点")){CancelDrag();b.Jump(-1);}
            if(Button(340,805,157,37,b.Playing?"暂停  Ⅱ":b.IsBranch?"恢复并播放":"播放  ▶",true))
            {CancelDrag();if(b.IsBranch&&!b.Playing)b.Restore();b.Playing=!b.Playing;}
            if(Button(505,805,108,37,"下一节点")){CancelDrag();b.Jump(1);}
            if(Button(621,805,80,37,b.Speed.ToString("0.##")+"×"))b.Speed=b.Speed<.75f?1:b.Speed<1.25f?1.5f:b.Speed<1.75f?2:.5f;
            if(b.IsBranch)
            {
                GUI.enabled=b.CanUndo;if(Button(722,805,84,37,"撤销",b.CanUndo)){CancelDrag();b.Undo();}GUI.enabled=true;
                if(Button(814,805,148,37,"恢复原站位",true)){CancelDrag();b.Restore();ResetSelection();}
            }
            if(Button(1079,805,107,37,more?"更多  ▾":"更多  ▸",more))more=!more;
            if(!more)return;
            if(Button(224,847,108,29,"回到起点")){CancelDrag();b.Seek(b.Start);}
            if(Button(340,847,157,29,b.IsFrameStudy?"原片段单步":"单步")){CancelDrag();b.StepOnce();}
            if(selected<0)return;
            GUI.enabled=!b.Playing&&!b.State.pinned[selected];
            if(Button(505,847,150,29,"固定 "+FiveRules.Number(selected)))b.SetPinned(selected,true);
            if(selected<5)
            {
                GUI.enabled=!b.Playing;
                if(Button(663,847,150,29,"设为持球者"))b.SetHolder(selected);
                var s=b.State;
                if(s.playbookVersion!=2)
                {
                    GUI.enabled=!b.IsFrameStudy&&!b.Playing&&s.holder>=0&&s.holder<5&&s.holder!=selected&&s.flight==null&&!s.turnover;
                    if(Button(821,847,141,29,"传给 "+FiveRules.Number(selected))&&b.Pass(selected))b.Playing=true;
                }
            }
            GUI.enabled=true;
        }
        static void Intent(FiveCourtGame g,Rect panel)
        {
            var b=g.Board;var s=b.State;FiveCourtView.Box(panel,White);
            string lessonEnding=b.LessonEnding;
            if(string.IsNullOrEmpty(lessonEnding))
            {
                Text(1208,82,204,24,s.playbookVersion==2?"阶段  "+(s.phase+1)+" / "+FiveZoneBook.Count(s.play,s.branch):"当前阶段",small);
                float titleHeight=Mathf.Max(29,heading.CalcHeight(new GUIContent(s.phaseName),204*scale)/scale);
                Text(1208,109,204,titleHeight,s.phaseName,heading);
                float ruleY=116+titleHeight;Box(1208,ruleY,204,1,Border);
                Text(1208,ruleY+10,204,24,"进攻目标",small);
                var intent=s.playbookVersion==2?FiveZoneBook.Frame(s.play,s.branch,s.phase).cue:s.attackIntent;
                TextFit(R(1208,ruleY+36,204,81),intent?.TrimEnd('。'),body);
                if(!string.IsNullOrEmpty(s.defenseIntent))
                {
                    float defenseY=ruleY+122;
                    Text(1208,defenseY,204,24,"防守反应",small);
                    TextFit(R(1208,defenseY+26,204,Mathf.Max(45,432-defenseY-26)),s.defenseIntent?.TrimEnd('。'),body);
                }
            }
            else
            {
                TextFit(R(1208,85,204,64),lessonEnding,heading);
                string detail=b.LessonEndingDetail;
                if(!string.IsNullOrEmpty(detail))TextFit(R(1208,153,204,100),detail,body);
            }
            Box(1198,461,224,1,Border);
            Text(1201,475,216,57,"暂停后拖动球员观察补防。悬停可查看职责。",small);
            if(selected>=0)
            {
                Text(1201,541,220,29,"已选 "+FiveRules.Number(selected)+(s.pinned[selected]?" · 已固定":""),heading);
                if(s.pinned[selected])
                {
                    GUI.enabled=!b.Playing;
                    if(Button(1198,577,224,38,selected>=5?"允许自动补防":"解除固定"))b.SetPinned(selected,false);
                    GUI.enabled=true;
                }
            }
            bool legacyReplay=g.Page==1&&s.playbookVersion==0;
            string startLabel=legacyReplay?"选择新版战术练习":"以 "+FiveRules.Number(g.Role)+" "+FiveRules.Roles[g.Role]+" 开始练习";
            if(Button(1198,788,224,54,startLabel,true))
            {
                if(legacyReplay)g.SelectPlay(s.play);else g.StartPractice();
                ResetSelection();
            }
        }
        static void DrillRecap(FiveCourtGame g,Rect panel)
        {
            var b=g.Board;
            FiveCourtView.Box(panel,White);
            Text(1208,84,204,32,"本轮回顾",heading);
            Text(1208,135,204,54,"我控 "+FiveRules.Number(b.State.controlled)+" "+FiveRules.Roles[b.State.controlled],body);
            Box(1208,198,204,1,Border);
            Text(1208,218,204,27,"回合结果",small);
            TextFit(R(1208,251,204,174),b.TimelineRecord.DrillSummary,body);
            Text(1201,475,216,80,"拖动时间轴回看球权变化。暂停后可拖动球员观察补防。",small);
            if(selected>=0&&b.State.pinned[selected]&&Button(1198,577,224,38,"解除固定 "+FiveRules.Number(selected)))
                b.SetPinned(selected,false);
            if(Button(1198,788,224,54,"下一轮即时演练  →",true)){g.StartDrill();ChangeTab();}
        }
        static void HandleCourt(FiveCourtGame g)
        {
            var b=g.Board;var e=Event.current;int hit=FiveCourtView.Pick(CourtPixels,b.State,e.mousePosition,b);
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
            string text=FiveRules.Number(hover)+"  "+g.Board.State.tasks[hover];
            var bounds=R(240,112,930,565);var mouse=Event.current.mousePosition;
            float w=258*scale,inset=10*scale,textWidth=w-2*inset;
            text=Fit(text,textWidth,63*scale,small);
            float h=small.CalcHeight(new GUIContent(text),textWidth)+2*inset;
            float x=mouse.x>bounds.center.x?mouse.x-w-24*scale:mouse.x+24*scale;
            x=Mathf.Clamp(x,bounds.xMin,bounds.xMax-w);
            float y=Mathf.Clamp(mouse.y-8*scale,bounds.yMin,bounds.yMax-h);
            FiveCourtView.Box(new Rect(x-1,y-1,w+2,h+2),Border);FiveCourtView.Box(new Rect(x,y,w,h),White);
            GUI.Label(new Rect(x+inset,y+inset,textWidth,h-2*inset),text,small);
        }
        static void Footer(FiveCourtGame g)
        {
            TextFit(R(18,880,g.SavePending?1160:1404,20),string.IsNullOrEmpty(g.Message)?g.Board.Notice:g.Message,small);
            if(g.SavePending&&Button(1198,855,224,34,"重试保存待存回合"))g.RetrySave();
        }
        static string Possession(FiveState s)
        {
            if(FiveDrill.IsFinished(s))return FiveDrill.Summary(s);
            if(s.madeBasket)return "投篮命中";
            if(s.turnover)return "回合结束";
            if(s.flight!=null)return s.flight.shot?"投篮中":"传球中";
            if(s.holder==s.controlled)return "你持球";
            return s.holder>=0?"无球 · "+FiveRules.Number(s.holder)+" 持球":"无球 · 球权待定";
        }
        public static bool TryWaypointEdge(Camera eye,PracticeLayout hud,Vector3 marker,out Rect tag,out Vector2 direction)
        {
            tag=Rect.zero;direction=Vector2.zero;
            if(!eye||hud.View.width<=0||hud.View.height<=0)return false;
            var projected=eye.WorldToViewportPoint(marker);
            var pixel=new Vector2(hud.View.x+projected.x*hud.View.width,hud.View.y+(1-projected.y)*hud.View.height);
            bool inFront=projected.z>=eye.nearClipPlane;
            if(inFront&&projected.z<=eye.farClipPlane&&hud.View.Contains(pixel)&&
                !hud.MapPanel.Contains(pixel)&&!hud.Header.Contains(pixel)&&!hud.Footer.Contains(pixel))return false;

            if(inFront)direction=pixel-hud.View.center;
            else
            {
                // Perspective projection reverses left/right behind the camera. Keep
                // camera-space left/right and use the lower edge for a turn back.
                Vector3 local=eye.transform.InverseTransformPoint(marker);
                direction=new Vector2(local.x,Mathf.Abs(local.z));
            }
            direction=direction.sqrMagnitude>.00001f?direction.normalized:Vector2.up;
            var half=hud.EdgeSize*.5f;
            float left=hud.View.x+hud.Gap+half.x,right=hud.View.xMax-hud.Gap-half.x;
            float top=hud.View.y+hud.Gap+half.y,bottom=hud.Footer.yMin-hud.Gap-half.y;
            var origin=hud.View.center;
            float tx=Mathf.Abs(direction.x)<.0001f?float.PositiveInfinity:((direction.x>0?right:left)-origin.x)/direction.x;
            float ty=Mathf.Abs(direction.y)<.0001f?float.PositiveInfinity:((direction.y>0?bottom:top)-origin.y)/direction.y;
            var point=origin+direction*Mathf.Min(tx,ty);
            if(ty<=tx&&direction.y<0)
            {
                float clearLeft=hud.MapPanel.xMax+hud.Gap+half.x,clearRight=hud.Header.xMin-hud.Gap-half.x;
                if(clearLeft<=clearRight)point.x=Mathf.Clamp(point.x,clearLeft,clearRight);
                else
                {
                    // A small window may have no room between the two top panels.
                    if(point.x<clearLeft)point.y=hud.MapPanel.yMax+hud.Gap+half.y;
                    if(point.x>clearRight)point.y=Mathf.Max(point.y,hud.Header.yMax+hud.Gap+half.y);
                }
            }
            else if(tx<ty)
            {
                var obstacle=direction.x<0?hud.MapPanel:hud.Header;
                point.y=Mathf.Max(point.y,obstacle.yMax+hud.Gap+half.y);
            }
            tag=new Rect(point-half,hud.EdgeSize);
            return true;
        }

        static void DrawMap(FiveState s,Vector2 heading,PracticeLayout hud)
        {
            FiveCourtView.Box(hud.MapPanel,new Color(.045f,.12f,.09f,.73f));
            FiveCourtView.Box(hud.MapCourt,new Color(.12f,.26f,.19f,.14f));
            FiveCourtView.DrawMiniCourt(hud.MapCourt,s,heading,miniNumber);
            float column=hud.MapCourt.width/3;
            float y=hud.MapCourt.yMax+3*hud.Scale;
            float height=hud.MapPanel.yMax-y;
            void Key(int index,string label,Color tint)
            {
                var row=new Rect(hud.MapCourt.x+index*column,y,column,height);
                float radius=Mathf.Max(2.4f,3.8f*hud.Scale);
                float textWidth=hudInfo.CalcSize(new GUIContent(label)).x;
                float gap=4*hud.Scale;
                float left=row.center.x-(2*radius+gap+textWidth)*.5f;
                FiveCourtView.Disc(new Vector2(left+radius,row.center.y),radius,tint);
                GUI.Label(new Rect(left+2*radius+gap,y,textWidth+1,height),label,hudInfo);
            }
            bool compact=column<28;
            Key(0,compact?"攻":"进攻",FiveCourtView.MapOffense);Key(1,compact?"守":"防守",FiveCourtView.MapDefense);Key(2,"球",FiveCourtView.MapBall);
        }

        static void DrawHeader(FiveCourtGame g,PracticeLayout hud)
        {
            var s=g.Live;
            var r=hud.Header;
            float inset=11*hud.Scale;
            string title=(g.IsDrill?"即时演练":g.LiveRecord.PlayTitle)+"  ·  "+FiveRules.Number(s.controlled)+" "+FiveRules.Roles[s.controlled];
            FiveCourtView.Box(r,HudPanel);
            GUI.Label(new Rect(r.x+inset,r.y+2*hud.Scale,r.width-2*inset,r.height*.5f),title,hudTitle);
            string phase=s.phaseName;
            if(g.IsDrill)
            {
                switch(s.drill.stage)
                {
                    case FiveDrillStage.Prefix: phase="战术展开";break;
                    case FiveDrillStage.Decision: phase="持球决策";break;
                    case FiveDrillStage.Resolve: phase="观察结果";break;
                    default: phase="回合结束";break;
                }
            }
            var status=new Rect(r.x+inset,r.center.y,r.width-2*inset,r.height*.5f-2*hud.Scale);
            GUI.Label(status,phase,hudInfo);
            float opacity=g.Visuals?.ArrivalOpacity??0;
            if(opacity<=0||(g.IsDrill&&s.drill.stage!=FiveDrillStage.Prefix))return;
            float width=hudCenter.CalcSize(new GUIContent("已到位")).x+14*hud.Scale;
            var arrival=new Rect(r.xMax-inset-width,r.y+4*hud.Scale,width,r.height*.42f);
            FiveCourtView.Box(arrival,new Color(FiveCourtView.Accent.r,FiveCourtView.Accent.g,FiveCourtView.Accent.b,opacity));
            var old=hudCenter.normal.textColor;hudCenter.normal.textColor=new Color(Ink.r,Ink.g,Ink.b,opacity);
            GUI.Label(arrival,"已到位",hudCenter);hudCenter.normal.textColor=old;
        }

        static void DrawReticle(FiveCourtGame g,Camera eye,PracticeLayout hud)
        {
            var aim=FiveActors.Crosshair(eye);
            var p=new Vector2(aim.x,Screen.height-aim.y);
            Color tint=g.Target>=0?FiveCourtView.Accent:Color.white;
            float radius=Mathf.Max(4,6*hud.Scale),stroke=Mathf.Max(1.5f,2*hud.Scale);
            FiveCourtView.Line(p-Vector2.right*radius,p+Vector2.right*radius,tint,stroke);
            FiveCourtView.Line(p-Vector2.up*radius,p+Vector2.up*radius,tint,stroke);
            string ownership=Possession(g.Live);
            if(g.IsDrill&&g.Live.drill.stage==FiveDrillStage.Decision&&g.Live.time-g.Live.drill.decisionOpened<2.5f)
                ownership="现在由你决定  ·  "+ownership;
#if UNITY_ANDROID && !UNITY_EDITOR
            if(g.CanActWithBall&&g.Target>=0)ownership+=g.Target==FiveActors.RimTarget?"  ·  投篮":"  ·  传给 "+FiveRules.Number(g.Target);
#else
            if(g.CanActWithBall&&g.Target>=0)
                ownership+=g.Target==FiveActors.RimTarget?"  ·  Q 投篮":"  ·  Q 传给 "+FiveRules.Number(g.Target);
#endif
            float width=possession.CalcSize(new GUIContent(ownership)).x+20*hud.Scale;
            float height=Mathf.Max(23*hud.Scale,possession.lineHeight+5*hud.Scale);
            var tag=new Rect(p.x-width*.5f,p.y+19*hud.Scale,width,height);
            FiveCourtView.Box(tag,HudPanel);
            possession.normal.textColor=g.Live.holder==g.Live.controlled&&g.Live.flight==null?new Color(1,.85f,.61f):HudText;
            GUI.Label(tag,ownership,possession);
        }

        static void DrawWaypointEdge(FiveCourtGame g,Camera eye,PracticeLayout hud)
        {
            if(!eye||!g.ShowRunTarget)return;
            var s=g.Live;
            var target=FiveCoach.RunTarget(s);
            float distance=Vector2.Distance(s.positions[s.controlled],target);
            var anchor=FiveActors.WaypointAnchor(eye,target,distance,s.time);
            if(!TryWaypointEdge(eye,hud,anchor,out var tag,out var direction))return;
            FiveCourtView.Box(tag,HudPanel);
            float size=Mathf.Max(4,6*hud.Scale),stroke=Mathf.Max(1.7f,2.5f*hud.Scale);
            var pivot=new Vector2(tag.x+size*2.4f,tag.center.y);
            var tip=pivot+direction*size;
            var side=new Vector2(-direction.y,direction.x)*size*.86f;
            FiveCourtView.Line(pivot-direction*size,tip,FiveCourtView.Accent,stroke);
            FiveCourtView.Line(tip-direction*size+side,tip,FiveCourtView.Accent,stroke);
            FiveCourtView.Line(tip-direction*size-side,tip,FiveCourtView.Accent,stroke);
            var local=eye.transform.InverseTransformPoint(anchor);
            string bearing="跑位";
            if(local.z<0)
                bearing=Mathf.Abs(local.x)<=Mathf.Abs(local.z)*.22f?"后方":local.x<0?"左后方":"右后方";
            float x=pivot.x+size*1.9f;
            GUI.Label(new Rect(x,tag.y,tag.xMax-x,tag.height),bearing+" "+distance.ToString("0.0")+"米",hudInfo);
        }

        static void Practice(FiveCourtGame g,Camera eye,PracticeLayout hud)
        {
            var s=g.Live;if(s==null||Event.current.type!=EventType.Repaint)return;
            Vector2 heading=g.player?g.ViewHeading:s.facing[s.controlled];
            DrawMap(s,heading,hud);
            DrawHeader(g,hud);
            DrawReticle(g,eye,hud);
            DrawWaypointEdge(g,eye,hud);
            var footer=hud.Footer;
            FiveCourtView.Box(footer,HudPanel);
            float inset=12*hud.Scale;
            var keys=footer;
            if(!g.IsDrill)
            {
                keys.height=Mathf.Max(18,24*hud.Scale);
                keys.y=footer.yMax-keys.height-2*hud.Scale;
                var instruction=new Rect(footer.x+inset,footer.y+2*hud.Scale,footer.width-2*inset,keys.y-footer.y-2*hud.Scale);
                var cue=FiveCoach.Cue(s,s.controlled);
#if UNITY_ANDROID && !UNITY_EDITOR
                cue=cue.Replace("按 Q","点出球按钮").Replace("按 C 重新组织","点「重组」重新组织").Replace("C 可重新组织","点「重组」可重新组织");
#endif
                TextFit(instruction,cue,hudCue);
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            GUI.Label(keys,"左侧摇杆移动    滑动空白处瞄准",hudCenter);
            FiveTouch.Draw(g,touchButton);
#else
            string controls="WASD 移动    鼠标转向    ";
            if(g.ShowRunTarget)controls+="F 跑位    ";
            if(g.CanActWithBall)controls+="Q 传球 / 投篮    ";
            if(!g.IsDrill)controls+="C 重组    ";
            controls+="V 回放    Esc 暂停";
            GUI.Label(keys,controls,hudCenter);
#endif
        }
    }
}
