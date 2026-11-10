using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CourtX
{
    public sealed class LearningStep
    {public float time;public string title,brief;public string[] roles;}
    public sealed class LearningQuestion
    {
        public string id,title,prompt,condition;
        public LearningScenario scenario;
        public float time;
        public string[] options,feedback;
        public int correct;
    }
    public static class LearningCatalog
    {
        public static string Number(int id)=>new[]{"01","04","05","11","12","13"}[Mathf.Clamp(id,0,5)];
        public static string Title(TacticalDrill drill)=>drill==TacticalDrill.PickAndRoll?"高位挡拆":"高位接应 · 传切";
        public static string Goal(TacticalDrill drill)=>drill==TacticalDrill.PickAndRoll?
            "先让掩护成立，再读沉退、换防或夹击。比较顺下与弱侧接应的传球窗口。":
            "04 提上形成接球角度，01 传后空切。回传被封时，让 05 参与转移并继续找机会。";
        public static TacticalEpisode Load(LearningScenario scenario)
        {
            var c=scenario.Normalize();string key=c.drill==TacticalDrill.HighPostCut?"HighPostCut-Help":"PickAndRoll-"+c.ResolveCoverage();
            var asset=Resources.Load<TextAsset>("CourtXLearning/"+key+"-role"+c.controlled);
            if(asset==null)return null;
            var e=JsonUtility.FromJson<TacticalEpisode>(asset.text);return LearningHistory.Valid(e)?e:null;
        }
        public static LearningStep[] Steps(TacticalEpisode e)
        {
            if(!LearningHistory.Valid(e))return new LearningStep[0];
            var list=new List<LearningStep>();string previous="";
            foreach(var frame in e.frames)
            {
                string state=frame.phase+"/"+frame.defense;
                if(state==previous)continue;previous=state;
                string title=PhaseTitle(frame.phase);
                if(frame.defense=="Engaged")title="识别防守 · "+e.coverage;
                if(frame.defense=="Recover"&&frame.phase!="Complete")title="出球后 · 防守回位";
                list.Add(new LearningStep{time=frame.time,title=title,brief=Explain(e,frame),roles=Roles(e,frame)});
            }
            foreach(var d in e.decisions)
            {
                var f=e.frames.LastOrDefault(x=>x.time<=d.time)??e.frames[0];
                list.Add(new LearningStep{time=d.time,title=Number(d.from)+" → "+Number(d.to)+" 出手",
                    brief="这是实际记录的出手时刻。先比较两名队友的线路，再看球是否真正接稳。出手风险与最终结果要分开判断。",roles=Roles(e,f)});
            }
            return list.OrderBy(x=>x.time).GroupBy(x=>x.time).Select(x=>x.Last()).ToArray();
        }
        public static string PhaseTitle(string phase)
        {
            switch(phase)
            {
                case "Assemble":return "拉开 · 准备掩护";
                case "ScreenReady":return "掩护站定 · 等待使用";
                case "Roll":return "观察回应 · 寻找窗口";
                case "HighPost":return "提上接应 · 建立角度";
                case "Cut":return "传后空切 · 继续传导";
                case "Complete":return "接稳后的空间";
                case "Turnover":return "回合结束 · 回看原因";
                default:return "观察当前站位";
            }
        }
        static string Explain(TacticalEpisode e,TacticalFrame f)
        {
            if(f.phase=="Complete")return "本例已达到练习中的配合空间目标。它不是投篮得分；接稳后仍需要观察下一层防守。";
            if(f.phase=="Turnover")return "回看出手前的线路、接球人的移动以及球的实际轨迹。一次失误不能只归因于战术选择。";
            if(f.phase=="ScreenReady")return "04 已在掩护点站定。01 靠近并绕过后，防守才对这次挡拆作回应；提前出球或离开掩护会改变局面。";
            if(f.defense=="Engaged")
            {
                if(e.coverage.StartsWith("Switch"))return "两名防守交换了对 01 / 04 的责任。重新识别谁守谁，再比较接球空间；换防不等于自动出现空位。";
                if(e.coverage.StartsWith("Blitz"))return "夹击启动。04 优先提供较短的接应出口，05 观察弱侧协防调整位置；有清楚的线路再出球。";
                return "内线防守回收保护篮下。01 保持观察，比较 04 顺下和 05 弱侧的接球窗口，不预设某一传必定成功。";
            }
            if(f.phase=="HighPost")return "04 向高位移动，01 先看传球角度。线路被封时可以横移、等待或转移；站到光圈不代表一定能接到球。";
            if(f.phase=="Cut")return "01 传球后从外侧空切。04 读回传线；05 是可用的中转与空间点，回传未开放时可以继续传导。";
            if(f.defense=="Recover")return "球离开压力点后，防守重新回位。接球人先接稳，再根据当前站位继续处理。";
            return "01 持球观察，04 提上准备掩护，05 保持弱侧空间。先确认队友与防守位置，再决定何时使用配合。";
        }
        static string[] Roles(TacticalEpisode e,TacticalFrame f)
        {
            bool post=e.drill==TacticalDrill.HighPostCut.ToString();
            var r=new string[6];
            r[0]=post?(f.phase=="HighPost"?"持球找 04 的高位接球角度。":"传后从外侧空切；回传被封就继续找角度。"):
                f.phase=="ScreenReady"?"靠近并绕过已站定的掩护，观察两名防守。":"保护球，比较顺下与弱侧窗口。";
            r[1]=post?"向高位接应，接稳后观察 01 空切与 05 中转。":
                f.phase=="Assemble"||f.phase=="ScreenReady"?"到掩护点站定，等待 01 真正使用掩护。":
                e.coverage.StartsWith("Blitz")&&f.defense=="Engaged"?"夹击时提供短顺下出口，不直接冲进篮下。":"使用掩护后顺下，调整到可见的传球线上。";
            r[2]=f.help?"观察 13 协防后的空间，可拉开、上提或空切。":"保持弱侧空间，准备接应与中转。";
            for(int i=3;i<6;i++)r[i]="当前负责 "+Number(f.marks[i-3])+"，根据球与掩护调整防守位置。";
            if(f.help)r[5]="参与弱侧协防，并准备回到接应人的线路。";
            if(f.holder>=0&&f.holder<3)r[f.holder]+=" 当前持球，出球前再次确认线路。";
            return r;
        }
        static LearningScenario Config(TacticalDrill drill,int role,DefenseCoverage defense=DefenseCoverage.Drop)
            =>new LearningScenario{drill=drill,controlled=role,coverage=defense,seed=4435};
        public static LearningQuestion[] Questions()
        {
            var pnr=Config(TacticalDrill.PickAndRoll,0);var swap=Config(TacticalDrill.PickAndRoll,0,DefenseCoverage.Switch);
            var trap=Config(TacticalDrill.PickAndRoll,1,DefenseCoverage.Blitz);var high=Config(TacticalDrill.HighPostCut,1);var cut=Config(TacticalDrill.HighPostCut,0);
            float At(LearningScenario c,Func<TacticalFrame,bool> condition)=>Load(c).frames.First(condition).time;
            var lane=Load(high).decisions.First(d=>d.from==1&&!d.options.First(o=>o.receiver==0).open&&d.options.First(o=>o.receiver==2).open);
            return new[]{
                new LearningQuestion{id="screen",title="让掩护真正被使用",scenario=pnr,time=At(pnr,f=>f.phase=="ScreenReady"),correct=0,
                    prompt="你是 01，04 刚在掩护点站定。下一步怎样继续这次挡拆？",condition="本练习需要 01 持球使用已站定的掩护，才触发挡拆防守。",
                    options=new[]{"靠近并绕过掩护，再观察防守回应","站在原地，等待防守自动换位","无论线路怎样，立刻把球扔到篮下"},
                    feedback=new[]{"先让掩护参与进攻，再读防守；你的移动是配合的触发条件。","防守不会仅因为等待就进入挡拆分支。先使用掩护。","提前出球会改变配合；目标和时机仍需要依据实际线路。"}},
                new LearningQuestion{id="switch",title="重新认清谁守谁",scenario=swap,time=At(swap,f=>f.defense=="Engaged"),correct=1,
                    prompt="掩护后，11 转去跟 04，12 接管 01。你看到的是什么变化？",condition="依据图中这个时刻的盯防责任；不要只看一名防守是否离开原位置。",
                    options=new[]{"两人都在夹击 01","两名防守交换了盯防责任","防守已经停止，04 必然空位"},
                    feedback=new[]{"夹击需要两名防守同时施压；这里两人分别接管了 01 与 04。","这是换防。先识别新的对位，再比较线路与接球空间。","换防仍在防守，不能把换人当作没有人防。"}},
                new LearningQuestion{id="blitz",title="给持球人一个短出口",scenario=trap,time=At(trap,f=>f.defense=="Engaged"),correct=2,
                    prompt="你是 04，01 使用掩护后受到夹击。你优先怎样接应？",condition="夹击分支已经启动；接应位置和传球线仍需清楚。",
                    options=new[]{"跟着 01 挤进两名防守之间","不再移动，只等球自动来到手里","短顺下，形成持球人能看见的近距离出口"},
                    feedback=new[]{"继续挤向夹击区域会缩小出球空间。","真实传球需要线路和接球位置，不会自动归属队友。","短顺下提供较近出口；若线路被封，继续调整，也可以由 05 接应。"}},
                new LearningQuestion{id="highpost",title="无球时先建立角度",scenario=high,time=0,correct=1,
                    prompt="你是无球的 04，01 正在外侧持球。怎样为高位传切创造第一传？",condition="本场景的高位接应点在罚球线附近；要球不是强制传球。",
                    options=new[]{"一直停在篮下，反复要球","向高位移动，形成角度后再示意要球","紧贴 01，和他站在同一个位置"},
                    feedback=new[]{"停在篮下没有建立本次配合需要的高位接应角度。","先让自己成为可见接应点，再观察防守和传球线；必要时继续换角度。","与持球人重叠会压缩空间，难以形成有用的传球出口。"}},
                new LearningQuestion{id="cut",title="传完球继续参与",scenario=cut,time=At(cut,f=>f.phase=="Cut"),correct=0,
                    prompt="01 已把球交到高位 04 手中。作为 01，你接下来如何参与配合？",condition="回传窗口会随防守移动变化；空切不保证立刻接到球。",
                    options=new[]{"从外侧空切，边移动边找回传角度","原地等 04 把球立刻还回来","冲到 04 身边，把两人的空间挤在一起"},
                    feedback=new[]{"传后移动能改变防守和回传角度；没有窗口时，04 也可以先经 05 转移。","本次配合需要传后空切，原地等待不会主动改变角度。","两名队友挤在一起会缩小接球空间。"}},
                new LearningQuestion{id="lane",title="回传被封时找下一传",scenario=high,time=lane.time,correct=2,
                    prompt="你是持球的 04。01 正受近侧防守限制，05 在弱侧提供接应。此刻应优先比较哪种选择？",condition="以该次出手记录的线路预测为准：预测开放与最终接稳是两件事。",
                    options=new[]{"只要是预定配合，就必须强传 01","看到 01 被防就判定整套配合失败","观察 05 的线路，开放时先转移，再继续找 01"},
                    feedback=new[]{"这次记录中 01 的预测线路有风险，不能因预定路线而忽略防守。","05 仍提供中转可能，第一条线路被封不等于回合已经结束。","这次出手记录中 05 的线路开放、01 的线路被封。转移后可继续读防守；仍需完成真实接球。"}}
            };
        }
    }
}
