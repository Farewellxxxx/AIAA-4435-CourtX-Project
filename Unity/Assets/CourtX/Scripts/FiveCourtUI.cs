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
            public Rect At(float x,float y,float w,float h)=>new Rect(Offset.x+x*Scale,Offset.y+y*Scale,w*Scale,h*Scale);
            public Rect Canvas=>At(224,68,962,676);
            public Rect Court=>At(375,103,660,660*14f/15);
            public Rect History=>At(18,126,200,650);
            public Rect Intent=>At(1198,68,224,372);
            public Rect Timeline=>At(224,747,962,96);
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
            if(!serif)serif=Font.CreateDynamicFontFromOSFont(new[]{"SimSun","Songti SC","Noto Serif CJK SC","Georgia"},28);
            if(!font)font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei UI","Microsoft YaHei","Arial"},20);
            body=new GUIStyle(GUI.skin.label){font=font,fontSize=Mathf.Max(12,Mathf.RoundToInt(18*scale)),wordWrap=true,richText=false,padding=new RectOffset(0,0,0,0)};body.normal.textColor=Ink;
            small=new GUIStyle(body){fontSize=Mathf.Max(10,Mathf.RoundToInt(15*scale))};small.normal.textColor=Muted;
            heading=new GUIStyle(body){font=serif?serif:font,fontSize=Mathf.Max(13,Mathf.RoundToInt(20*scale)),fontStyle=FontStyle.Bold};
            title=new GUIStyle(heading){fontSize=Mathf.RoundToInt(28*scale)};
            possession=new GUIStyle(body){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,wordWrap=false};
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
            var layout=new BoardLayout(Screen.width,Screen.height);scale=layout.Scale;offset=layout.Offset;
            Init();GUI.color=Color.white;
            if(g.Practicing){Practice(g);return;}
            FiveCourtView.Box(new Rect(0,0,Screen.width,Screen.height),Paper);
            Text(18,16,150,36,"COURTX.",title);
            if(Button(174,11,130,39,"战术板",g.Page==0)&&g.Page!=0){g.ShowPage(0);ChangeTab();}
            if(Button(313,11,130,39,"即时回放",g.Page==1)&&g.Page!=1){g.ShowPage(1);ChangeTab();}
            Text(467,22,380,24,"五人协作  /  破解 2–3 联防",small);
            if(g.HasPausedPractice&&Button(1200,11,222,39,"继续暂停的练习  →"))g.ResumePractice();
            Box(18,58,1404,1,Border);
            HistoryPixels=layout.History;
            if(g.Page==0)Settings(g);else History(g);
            var b=g.Board;
            if(g.Page==1&&g.History.Count==0)
            {
                FiveCourtView.Box(layout.Canvas,White);CourtPixels=Rect.zero;Text(370,365,650,50,"第一轮练习结束后，回放会出现在这里。",heading);
                Text(370,430,640,70,"选择战术并开始 3D 练习，按 V 结束。每轮真实经过都能在这里暂停、拖动和推演。",body);
                if(Button(515,535,355,54,"返回战术板  →",true)){g.LoadLesson();ChangeTab();}Footer(g);return;
            }
            if(prior!=b.Record){ResetSelection();prior=b.Record;}
            FiveCourtView.Box(layout.Canvas,FiveCourtView.Floor);
            Box(224,68,962,32,new Color(.085f,.19f,.15f));
            Color old=heading.normal.textColor;heading.normal.textColor=White;
            Text(242,75,750,28,b.Record.PlayTitle+BranchSuffix(b.State),heading);
            heading.normal.textColor=old;
            var oldSmall=small.normal.textColor;small.normal.textColor=FiveCourtView.Accent;
            Text(1035,79,140,24,b.IsFrameStudy?"暂停布防":b.IsBranch?"假设推演":g.Page==0?"教学演练":"回放 · 第 "+(g.SelectedReplayIndex+1)+" 轮",small);small.normal.textColor=oldSmall;
            CourtPixels=layout.Court;
            HandleCourt(g);
            FiveCourtView.Draw(CourtPixels,b,badge,hover);
            small.normal.textColor=FiveCourtView.Offense;Text(242,694,122,24,"●  进攻 01–05",small);
            small.normal.textColor=FiveCourtView.Defense;Text(242,716,122,24,"●  防守 X1–X5",small);
            small.normal.textColor=FiveCourtView.Accent;Text(1055,694,120,24,"◯  你的角色",small);
            small.normal.textColor=new Color(.73f,.81f,.70f);Text(1055,716,120,24,b.IsFrameStudy?"绿点  已固定":"短线  近 2.5 秒",small);
            small.normal.textColor=oldSmall;
            Controls(g);Intent(g,layout.Intent);Footer(g);Tooltip(g);
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
                if(historyFocus){pressedHistory=g.History[hit].id;GUIUtility.hotControl=control;e.Use();}
            }
            if(e.rawType==EventType.MouseUp&&e.button==0&&pressedHistory!=null)
            {
                string id=pressedHistory;pressedHistory=null;if(GUIUtility.hotControl==control)GUIUtility.hotControl=0;
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
                TextFit(Local(9,5,170,25),r.PlayTitle,heading);
                GUI.Label(Local(9,31,170,23),"我控 "+FiveRules.Number(start.controlled)+" · "+r.CooperationOutcome,body);
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
                result.Add((t,string.IsNullOrEmpty(frame.lastEvent)?frame.phaseName:frame.lastEvent));
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
        static void Practice(FiveCourtGame g)
        {
            var s=g.Live;if(s==null)return;
            Box(24,20,630,140,new Color(.97f,.97f,.93f,.94f));
            Text(42,34,590,35,g.LiveRecord.PlayTitle+BranchSuffix(s),heading);
            Text(42,77,590,29,"你是 "+FiveRules.Number(s.controlled)+" "+FiveRules.Roles[s.controlled]+"    ·    "+(s.playbookVersion==2?"2–3 联防":FiveRules.Defenses[(int)s.defense]),body);
            Text(42,117,590,28,s.phaseName,small);
            string ownership=s.madeBasket?"投篮命中":s.turnover?"回合结束":
                s.flight!=null?(s.flight.shot?"投篮中":"传球中"):
                s.holder==s.controlled?"你持球":s.holder>=0?"无球 · "+FiveRules.Number(s.holder)+" 持球":"无球 · 球权待定";
            if(g.Overview)
            {
                Box(325,163,790,569,FiveCourtView.Floor);
                FiveCourtView.Draw(R(457.5f,214,525,490),g.LiveRecord,s,badge,overviewHeading:g.ViewHeading);
            }
            else
            {
                var center=FiveActors.Crosshair(g.player?g.player.eyes:null);
                var p=new Vector2(center.x,Screen.height-center.y);Color c=g.Target>=0?FiveCourtView.Accent:Color.white;
                FiveCourtView.Line(p-new Vector2(7,0),p+new Vector2(7,0),c,2);FiveCourtView.Line(p-new Vector2(0,7),p+new Vector2(0,7),c,2);
            }
            var aim=FiveActors.Crosshair(g.player?g.player.eyes:null);
            float tagWidth=Mathf.Max(95*scale,possession.CalcSize(new GUIContent(ownership)).x+24*scale);
            float tagHeight=Mathf.Max(26*scale,possession.lineHeight+7*scale);
            var tag=g.Overview?R(780,166,210,25):new Rect(aim.x-tagWidth*.5f,Screen.height-aim.y+27*scale,tagWidth,tagHeight);
            FiveCourtView.Box(tag,new Color(.06f,.15f,.12f,.97f));
            possession.normal.textColor=s.holder==s.controlled&&s.flight==null?new Color(1,.83f,.53f):White;
            GUI.Label(tag,ownership,possession);
            Box(24,734,1392,142,new Color(.97f,.97f,.93f,.94f));
            Text(44,750,1350,40,FiveCoach.Cue(s,s.controlled),heading);
            string hint="";
            if(g.Overview)hint="箭头表示你的朝向。移动鼠标转向，按 W 沿箭头前进，松开 Tab 回到第一人称。";
            else if(s.success||s.turnover)hint="V 查看本轮回放    ·    C 再练一次";
            else if(s.holder==s.controlled&&s.flight==null&&g.Target==FiveActors.RimTarget)hint="已瞄准篮筐    ·    Q 投篮";
            else if(s.holder==s.controlled&&s.flight==null&&g.Target>=0)hint="已瞄准 "+FiveRules.Number(g.Target)+"    ·    Q 传球";
            else if((s.playbookVersion==2||s.phase<3)&&s.flight?.shot!=true&&!FiveCoach.InPlace(s,s.controlled))
            {
                Vector2 delta=s.targets[s.controlled]-s.positions[s.controlled];
                Vector3 local=g.player.transform.InverseTransformDirection(FiveRules.World(delta));
                string direction=(local.x<-.5f?"左":local.x>.5f?"右":"")+(local.z<-.5f?"后方":local.z>.5f?"前方":"侧");
                hint="高亮圈在"+direction+" "+delta.magnitude.ToString("0.0")+" 米    ·    WASD 移动 或按 F 跑向高亮圈";
            }
            else if(s.holder==s.controlled)hint="准星对队友按 Q 传球，对篮筐按 Q 投篮";
            else hint="按住 Tab 可查看全队站位";
            Text(44,802,1340,32,hint,body);
            Text(44,844,1340,24,"WASD 移动    鼠标转动视角    按住 Tab 查看全队站位    C 重新组织    V 即时回放    Esc 暂停",small);
        }
    }
}
