using System;
using System.Linq;
using UnityEngine;
namespace CourtX
{
    // All interactions use the same controller as actual practice; this layer only renders it.
    public static class LearningHubUI
    {
        static GUIStyle body,small,tiny,title,heading,button,slider,thumb;
        static Font font;
        static float scale=1,styleScale=-1;
        static Vector2 offset;
        static bool inScroll;
        static Texture2D trackTexture,thumbTexture;
        static Vector2 rightScroll,leftScroll;
        static float rightHeight=600;
        static string prior="";
        static bool focusFeedback;
        static readonly Color ink=new Color(.90f,.95f,.94f),muted=new Color(.58f,.71f,.72f),teal=new Color(.28f,.84f,.70f),panel=new Color(.055f,.13f,.15f);
        static void Init()
        {
            if(body!=null&&Mathf.Abs(styleScale-scale)<.001f)return;styleScale=scale;
            if(font==null)font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei UI","Microsoft YaHei","Arial"},20);
            body=new GUIStyle(GUI.skin.label){font=font,fontSize=Mathf.Max(17,Mathf.RoundToInt(20*scale)),wordWrap=true,richText=false};body.normal.textColor=ink;
            small=new GUIStyle(body){fontSize=Mathf.Max(15,Mathf.RoundToInt(17*scale))};tiny=new GUIStyle(body){fontSize=Mathf.Max(13,Mathf.RoundToInt(14*scale))};tiny.normal.textColor=new Color(.68f,.79f,.80f);
            title=new GUIStyle(body){fontSize=Mathf.RoundToInt(28*scale),fontStyle=FontStyle.Bold};heading=new GUIStyle(body){fontSize=Mathf.Max(18,Mathf.RoundToInt(22*scale)),fontStyle=FontStyle.Bold};
            button=new GUIStyle(body){alignment=TextAnchor.MiddleCenter,padding=new RectOffset(6,6,3,3),fontSize=Mathf.Max(15,Mathf.RoundToInt(17*scale))};
            if(trackTexture==null){trackTexture=Solid(new Color(.24f,.40f,.42f));thumbTexture=Solid(teal);}
            slider=new GUIStyle(GUI.skin.horizontalSlider){fixedHeight=Mathf.Round(6*scale),padding=new RectOffset(0,0,0,0),margin=new RectOffset()};slider.normal.background=trackTexture;
            thumb=new GUIStyle(GUI.skin.horizontalSliderThumb){fixedWidth=Mathf.Round(14*scale),fixedHeight=Mathf.Round(20*scale),padding=new RectOffset(),margin=new RectOffset()};
            thumb.normal.background=thumb.hover.background=thumb.active.background=thumbTexture;
        }
        static Texture2D Solid(Color c){var t=new Texture2D(1,1){hideFlags=HideFlags.HideAndDontSave};t.SetPixel(0,0,c);t.Apply();return t;}
        // Map layout coordinates, not the glyph texture. Text rasterizes once at its native pixel size.
        static Rect Pixel(Rect r)=>new Rect(Mathf.Round(r.x*scale+(inScroll?0:offset.x)),Mathf.Round(r.y*scale+(inScroll?0:offset.y)),Mathf.Round(r.width*scale),Mathf.Round(r.height*scale));
        static void Label(Rect r,string text,GUIStyle style)=>GUI.Label(Pixel(r),text,style);
        static void Box(Rect r,Color c)=>RecapCourtView.Panel(Pixel(r),c);
        static Vector2 BeginScroll(Rect r,Vector2 value,Rect content)
        {var result=GUI.BeginScrollView(Pixel(r),value,new Rect(0,0,Mathf.Round(content.width*scale),Mathf.Ceil(content.height*scale)));inScroll=true;return result;}
        static void EndScroll(){GUI.EndScrollView();inScroll=false;}
        static bool Button(Rect r,string text,bool active=false)
        {
            var c=active?new Color(.12f,.38f,.35f):new Color(.10f,.22f,.25f);
            if(GUI.enabled&&Pixel(r).Contains(Event.current.mousePosition))c=Color.Lerp(c,teal,.18f);
            if(!GUI.enabled)c=Color.Lerp(c,panel,.55f);Box(r,c);
            if(active)Box(new Rect(r.x,r.y+r.height-2,r.width,2),teal);
            return GUI.Button(Pixel(r),text,button);
        }
        static float Text(float y,string text,GUIStyle style=null,float width=352)
        {style=style??body;float height=Mathf.Ceil(style.CalcHeight(new GUIContent(text??""),width*scale))/scale+2;Label(new Rect(0,y,width,height),text??"",style);return y+height+18;}
        static string Config(LearningScenario c)=>LearningCatalog.Title(c.drill)+"  ·  你是 "+LearningCatalog.Number(c.controlled)+"  ·  "+(c.drill==TacticalDrill.HighPostCut?"协防回位":TacticalSession.CoverageLabel(c.coverage));
        public static void Draw(TacticalSession s)
        {
            scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);offset=new Vector2((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2);Init();inScroll=false;var h=s.Learning;var matrix=GUI.matrix;var color=GUI.color;var bg=GUI.backgroundColor;bool enabled=GUI.enabled;
            GUI.matrix=Matrix4x4.identity;
            GUI.color=GUI.backgroundColor=Color.white;RecapCourtView.Panel(new Rect(0,0,Screen.width,Screen.height),new Color(.025f,.07f,.085f));
            Label(new Rect(36,22,580,40),"CourtX  /  战术学习与复盘",title);
            Label(new Rect(36,66,1000,25),"3v3 半场   ·   看懂配合，进入球场，再用真实记录验证选择",small);
            string[] tabs={"01  战术演练","02  What-if 对比","03  读防守挑战","04  实战复盘"};
            for(int i=0;i<4;i++)if(Button(new Rect(36+i*304,105,292,38),tabs[i],(int)h.Page==i))h.Show((LearningPage)i);
            Box(new Rect(36,157,218,475),panel);Box(new Rect(834,157,410,475),panel);
            string key=h.Page+"/"+h.Current?.id+"/"+h.QuestionIndex;
            if(key!=prior){rightScroll=leftScroll=Vector2.zero;prior=key;}
            if(h.Page==LearningPage.Lesson||h.Page==LearningPage.WhatIf)Settings(h);
            else if(h.Page==LearningPage.Challenge)Questions(h);
            else History(h);
            Controls(h);
            var frame=h.Cursor.Sample();
            string source=h.IsExample?"教学示例 · 固定录制开局 4435":h.Current==null?"完成练习后，这里显示真实记录":"实际练习 · "+(h.Current==h.Baseline?"A 轮":h.Current==h.Candidate?"B 轮":"你控制 "+LearningCatalog.Number(h.Current.controlled));
            Label(new Rect(270,157,548,27),source,small);
            RecapCourtView.DrawAt(Pixel(new Rect(299,188,490,361)),s,h.Current,h.Cursor,tiny,h.Page!=LearningPage.Challenge||h.Answer>=0);
            if(frame==null)Label(new Rect(310,340,470,90),"从一次实际练习开始。\n按 V 结束片段，就能回到这里看完整球路。",heading);
            Label(new Rect(278,619,540,28),"青：进攻  橙：防守  白圈：你  金圈：持球  金色球：篮球",tiny);
            Right(s,h);
            Box(new Rect(36,649,1208,1),new Color(.17f,.29f,.30f));
            Label(new Rect(36,661,715,25),Config(h.Scenario),small);
            Label(new Rect(36,687,715,28),"WASD 移动 · Shift 跑 · Tab 俯视 · Q 传球 · E 要球 · V 复盘",tiny);
            GUI.enabled=s.Journal!=null&&!s.Journal.Closed&&s.ActiveTime>.05f;
            if(Button(new Rect(766,663,170,42),"继续暂停的练习"))s.Resume();
            GUI.enabled=h.CanPractice;
            if(Button(new Rect(950,663,294,42),h.Page==LearningPage.Challenge?"进入题目场景练习  →":h.Page==LearningPage.Review?"同一开局再练  →":"从开局进入练习  →",true))h.StartPractice();
            GUI.enabled=enabled;GUI.matrix=matrix;GUI.color=color;GUI.backgroundColor=bg;
        }
        static void Settings(LearningHub h)
        {
            Label(new Rect(50,170,192,30),h.Page==LearningPage.WhatIf?"配置下一轮":"选择你的练习",heading);
            if(h.Page==LearningPage.WhatIf)
            {
                Label(new Rect(50,211,190,82),h.Baseline==null?"先练一轮，在实战复盘中设为 A。再从相同开局练 B。":"A 已保存。先恢复 A 的开局，再选择 B 的防守。",small);
                GUI.enabled=h.Baseline!=null;if(Button(new Rect(50,302,190,42),"恢复 A 的开局"))h.UseBaselineStart();GUI.enabled=true;
                Label(new Rect(50,354,190,50),LearningCatalog.Title(h.Scenario.drill)+"\n操控 "+LearningCatalog.Number(h.Scenario.controlled),small);
            }
            else
            {
                for(int i=0;i<2;i++)if(Button(new Rect(50,212+i*58,190,50),LearningCatalog.Title((TacticalDrill)i),h.Scenario.drill==(TacticalDrill)i))
                {var c=h.Scenario.Copy();c.drill=(TacticalDrill)i;h.SelectScenario(c);}
                for(int i=0;i<2;i++)if(Button(new Rect(50+i*98,340,92,44),i==0?"01 持球":"04 无球",h.Scenario.controlled==i))
                {var c=h.Scenario.Copy();c.controlled=i;h.SelectScenario(c);}
            }
            Label(new Rect(50,410,190,26),"防守反应",small);
            if(h.Scenario.drill==TacticalDrill.HighPostCut)Label(new Rect(50,444,190,68),"盯人 · 协防回位\n观察回传线路与 05 接应。",small);
            else for(int i=0;i<3;i++)if(Button(new Rect(50,444+i*41,190,35),TacticalSession.CoverageLabel((DefenseCoverage)i),h.Scenario.coverage==(DefenseCoverage)i))
            {var c=h.Scenario.Copy();c.coverage=(DefenseCoverage)i;h.SelectScenario(c);}
            Label(new Rect(50,574,190,50),"开局种子 "+h.Scenario.seed+"\n从头练习，自由跑传。",tiny);
        }
        static void Questions(LearningHub h)
        {
            Label(new Rect(50,170,190,30),"六个防守阅读点",heading);
            string[] names={"让掩护生效","换防后认对位","给持球人出口","无球先找角度","传球后继续跑","回传被封再转移"};
            for(int i=0;i<h.Questions.Length;i++)if(Button(new Rect(50,214+i*58,190,50),(i+1)+"  "+names[i],h.QuestionIndex==i))h.SelectQuestion(i);
            Label(new Rect(50,574,190,50),"先看防守，再选动作。\n局面变，选择也会变。",tiny);
        }
        static void History(LearningHub h)
        {
            Label(new Rect(50,170,190,30),"最近实际练习",heading);
            Label(new Rect(50,202,190,43),"保留最近 30 条 · 本地读取\n已跳过无效文件 "+h.History.SkippedFiles,tiny);
            var labels=h.History.Entries.Select((e,i)=>(i+1)+" · "+(e.drill=="PickAndRoll"?"挡拆":"高位传切")+" · "+LearningCatalog.Number(e.controlled)+"\n"+e.coverage+"\n"+e.duration.ToString("F1")+" 秒 / "+e.decisions.Count+" 次传球").ToArray();
            var heights=labels.Select(text=>Mathf.Max(70,button.CalcHeight(new GUIContent(text),174*scale)/scale+10)).ToArray();
            leftScroll=BeginScroll(new Rect(48,254,198,359),leftScroll,new Rect(0,0,176,Mathf.Max(345,heights.Sum()+heights.Length*8)));float y=0;
            for(int i=0;i<h.History.Entries.Count;i++)
            {if(Button(new Rect(0,y,174,heights[i]),labels[i],h.Current?.id==h.History.Entries[i].id))h.SelectHistory(i);y+=heights[i]+8;}
            EndScroll();
        }
        static void Controls(LearningHub h)
        {
            if(h.Page==LearningPage.Challenge)
            {Box(new Rect(280,555,528,58),panel);Label(new Rect(294,558,502,55),"题目定格在 "+h.Cursor.Time.ToString("F1")+" 秒\n作答后，可在对应演练中回看全过程。",small);return;}
            GUI.enabled=h.Current!=null;
            float time=GUI.HorizontalSlider(Pixel(new Rect(288,557,510,20)),h.Cursor.Time,h.Cursor.StartTime,h.Cursor.EndTime,slider,thumb);
            if(Mathf.Abs(time-h.Cursor.Time)>.001f)h.Seek(time);
            if(Button(new Rect(280,581,78,32),h.Cursor.Playing?"暂停":"播放"))h.Cursor.Toggle();
            if(Button(new Rect(366,581,78,32),h.Cursor.Speed<1?"0.5×":"1×"))h.Cursor.Speed=h.Cursor.Speed<1?1:.5f;
            if(Button(new Rect(452,581,92,32),"前一节点"))Jump(h,-1);
            if(Button(new Rect(552,581,92,32),"后一节点"))Jump(h,1);
            Label(new Rect(658,586,150,25),h.Cursor.Time.ToString("F1")+" / "+h.Cursor.EndTime.ToString("F1")+" 秒",tiny);
            GUI.enabled=true;
        }
        static void Jump(LearningHub h,int direction)
        {
            if(h.Page==LearningPage.Lesson)
            {var steps=h.Steps;float t=h.Cursor.Time;if(direction<0)h.Seek(steps.LastOrDefault(x=>x.time<t-.01f)?.time??0);else h.Seek(steps.FirstOrDefault(x=>x.time>t+.01f)?.time??h.Cursor.EndTime);}
            else h.Cursor.Jump(direction);
        }
        static void Right(TacticalSession s,LearningHub h)
        {
            string headingText=h.Page==LearningPage.Lesson?"这一刻，六个人在做什么":h.Page==LearningPage.Challenge?"先判断，再看解释":h.Page==LearningPage.WhatIf?"比较真实发生的两轮":"沿着球路复盘你的选择";
            Label(new Rect(850,171,378,30),headingText,heading);
            string warning=h.RecordWarning;bool warned=!string.IsNullOrEmpty(warning);
            float warningHeight=warned?Mathf.Max(58,small.CalcHeight(new GUIContent(warning),366*scale)/scale+18):0;
            if(warned){Box(new Rect(846,207,384,warningHeight),new Color(.34f,.17f,.09f));Label(new Rect(854,215,366,warningHeight-12),warning,small);}
            float top=warned?warningHeight+219:210;
            rightScroll=BeginScroll(new Rect(850,top,378,618-top),rightScroll,new Rect(0,0,352,rightHeight));float y=0;
            if(h.SaveFailed){if(Button(new Rect(0,y,352,40),"重试保存这轮记录",true))h.RetrySaveCurrent();y+=54;}
            if(h.Page==LearningPage.Lesson)
            {
                var step=h.Steps.LastOrDefault(x=>x.time<=h.Cursor.Time+.001f);
                if(step!=null)
                {
                    y=Text(y,step.title,heading);y=Text(y,step.brief);
                    foreach(int i in Enumerable.Range(0,6).OrderBy(i=>i==h.Scenario.controlled?0:1))
                    {
                        float height=Mathf.Max(42,small.CalcHeight(new GUIContent(step.roles[i]),294*scale)/scale+16);
                        Box(new Rect(0,y,352,height),i==h.Scenario.controlled?new Color(.08f,.25f,.25f):new Color(.07f,.17f,.19f));
                        Label(new Rect(8,y+8,40,28),LearningCatalog.Number(i),heading);
                        Label(new Rect(52,y+8,294,height-10),step.roles[i],small);y+=height+12;
                    }
                }
                y=Text(y,"训练重点\n"+LearningCatalog.Goal(h.Scenario.drill),small);
                y=Text(y,"示例是已经发生的一次配合，不代表每次都能成功。进入练习后，队友回应取决于距离、传球线和防守位置。",tiny);
            }
            else if(h.Page==LearningPage.Challenge)
            {
                y=Text(y,h.Question.prompt,heading);y=Text(y,h.Question.condition,small);
                for(int i=0;i<3;i++)
                {float height=Mathf.Max(54,button.CalcHeight(new GUIContent("ABC"[i]+". "+h.Question.options[i]),352*scale)/scale+16);
                    if(Button(new Rect(0,y,352,height),"ABC"[i]+". "+h.Question.options[i],h.Answer==i)&&h.Answer<0){h.ChooseAnswer(i);focusFeedback=true;}y+=height+12;}
                if(focusFeedback){rightScroll.y=Mathf.Max(0,(y-55)*scale);focusFeedback=false;}
                y=Text(y,h.Answer<0?"请选择一个动作。":h.Answer==h.Question.correct?"判断与当前条件相符":"再看看这个选择的风险",heading);
                y=Text(y,h.Feedback);
                if(h.Answer>=0){if(Button(new Rect(0,y,170,38),"重新作答")){h.ResetAnswer();rightScroll=Vector2.zero;}if(Button(new Rect(182,y,170,38),"看对应演练"))h.ShowQuestionLesson();y+=52;}
                y=Text(y,"题目固定在这一瞬间。想看前后经过，请选择“看对应演练”；进入练习会从该场景开局开始。",tiny);
            }
            else if(h.Page==LearningPage.WhatIf)
            {
                GUI.enabled=h.Baseline!=null;if(Button(new Rect(0,y,170,40),"查看 A 轮",h.Current==h.Baseline))h.SetCompareSide(false);
                GUI.enabled=h.Candidate!=null;if(Button(new Rect(182,y,170,40),"查看 B 轮",h.Current==h.Candidate))h.SetCompareSide(true);GUI.enabled=true;y+=54;
                y=Text(y,h.Notice,small);
                y=Text(y,"切换 A / B 保留当前回放秒数。较短记录停在末帧；这是两轮完整练习对照，不是从中途恢复状态。",tiny);
                y=Metrics(y,"A",h.Baseline);y=Metrics(y,"B",h.Candidate);
                y=Text(y,"开放 / 风险按出手时的线路判断；接到球不等于选择最佳。双方操作不同，不能把结果差异全部归因于防守。",tiny);
            }
            else if(h.Current!=null)
            {
                y=Text(y,h.Current.endReason+" · "+h.Current.duration.ToString("F1")+" 秒",heading);
                if(!string.IsNullOrEmpty(h.Current.learningObjective))y=Text(y,"本轮目标\n"+h.Current.learningObjective,small);
                y=Text(y,TacticalJournal.Build(h.Current,h.Cursor.DecisionIndex).text);
                y=Text(y,"记录到的变化\n"+string.Join("\n",h.Current.events.Where(e=>e.time<=h.Cursor.Time&&e.kind=="defense").TakeLast(3).Select(e=>e.time.ToString("F1")+"s  "+e.text)),small);
                if(Button(new Rect(0,y,352,40),"把这一轮设为 What-if 的 A"))h.PinBaseline();y+=54;y=Text(y,h.Notice,small);
                if(h.Current.id==s.Journal?.Episode.id&&!string.IsNullOrEmpty(s.OptionalNarration))y=Text(y,"整轮可选解说\n"+s.OptionalNarration,small);
                y=Text(y,"本地事实复盘。没有记录视线或注意力，不推断你当时在想什么。文件保存在程序旁 Replays 文件夹。",tiny);
            }
            else y=Text(y,"还没有实际练习记录。选择战术进入球场，按 V 结束片段并保存。教学示例不会计入你的练习历史。");
            y=Text(y,"球场实线：近 2 秒运动轨迹。金色虚线：已发生出手的目标，区别于篮球实际走过的轨迹。",tiny);
            rightHeight=Mathf.Max(406,y+12);EndScroll();
        }
        static float Metrics(float y,string name,TacticalEpisode e)
        {
            if(e==null)return Text(y,name+" 轮：尚未记录",small);
            var m=LearningHistory.Summary(e);y=Text(y,name+" 轮 · "+e.coverage,heading);
            return Text(y,"开放线路 "+m.open+" / 风险线路 "+m.risky+"\n接到 "+m.caught+" / 未接到 "+m.missed+" / 尚未落定 "+m.pending+"\n记录 "+e.duration.ToString("F1")+" 秒 · "+e.endReason,small);
        }
    }
}
