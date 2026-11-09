using UnityEngine;
namespace CourtX
{
    public static class TacticalRecapUI
    {
        static GUIStyle body,small,title,button,section,tiny;
        static Vector2 scroll;
        static int priorDecision=-2;
        static readonly Color teal=new Color(.28f,.84f,.70f);
        public static void Reset(){scroll=Vector2.zero;priorDecision=-2;}
        static void Init()
        {
            if(body!=null)return;
            var font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},20);
            body=new GUIStyle(GUI.skin.label){font=font,fontSize=19,wordWrap=true};body.normal.textColor=new Color(.94f,.97f,.94f);
            small=new GUIStyle(body){fontSize=16};tiny=new GUIStyle(body){fontSize=14};
            title=new GUIStyle(body){fontSize=29,fontStyle=FontStyle.Bold};section=new GUIStyle(body){fontSize=22,fontStyle=FontStyle.Bold};
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=18,wordWrap=true};
        }
        public static void Draw(TacticalSession s)
        {
            Init();var old=GUI.matrix;var oldColor=GUI.color;var oldBackground=GUI.backgroundColor;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2),Quaternion.identity,Vector3.one*scale);
            GUI.color=Color.white;GUI.backgroundColor=Color.white;
            RecapCourtView.Panel(new Rect(0,0,1280,720),new Color(.025f,.075f,.09f,.99f));
            GUI.Label(new Rect(48,24,1184,44),"回合复盘  /  跟着球，看懂每次配合",title);
            var episode=s.Journal.Episode;var replay=s.Replay;
            GUI.Label(new Rect(48,76,1184,30),"你控制 "+s.Number(episode.controlled)+"  ·  "+episode.coverage+"  ·  练习 "+episode.duration.ToString("F1")+" 秒  ·  "+episode.endReason,small);
            // Controls run before rendering, keeping a click's visible content synchronized.
            var enabled=GUI.enabled;GUI.enabled=episode.frames.Count>1;
            float value=GUI.HorizontalSlider(new Rect(64,575,516,20),replay.Time,replay.StartTime,replay.EndTime);
            if(Mathf.Abs(value-replay.Time)>.0001f)replay.Seek(value);
            if(GUI.Button(new Rect(64,602,94,34),replay.Playing?"暂停":"播放",button))replay.Toggle();
            if(GUI.Button(new Rect(166,602,94,34),"回到开头",button))replay.Seek(replay.StartTime);
            if(GUI.Button(new Rect(268,602,94,34),replay.Speed<1?"0.5× 慢放":"1× 正常",button))replay.Speed=replay.Speed<1?1:.5f;
            GUI.enabled=episode.decisions.Exists(d=>d.time<replay.Time&&d.time>=replay.StartTime);
            if(GUI.Button(new Rect(624,602,136,34),"上一出手",button))replay.Jump(-1);
            GUI.enabled=episode.decisions.Exists(d=>d.time>replay.Time&&d.time<=replay.EndTime);
            if(GUI.Button(new Rect(770,602,136,34),"下一出手",button))replay.Jump(1);
            GUI.enabled=enabled;s.RefreshReviewSelection();
            if(priorDecision!=replay.DecisionIndex){scroll=Vector2.zero;priorDecision=replay.DecisionIndex;}
            GUI.Label(new Rect(378,605,218,30),replay.Time.ToString("F1")+" / "+replay.EndTime.ToString("F1")+" 秒"+(replay.Playing?" · 回放中":" · 已暂停"),small);
            GUI.Label(new Rect(924,604,292,34),s.SavedReview.StartsWith("保存失败")?s.SavedReview:"记录已保存 · Replays 文件夹",tiny);
            var f=replay.Sample();
            GUI.Label(new Rect(64,112,530,28),f==null?"没有可回放的记录":f.holder>=0?"当前持球："+s.Number(f.holder)+"  ·  金色外圈标记持球人":"当前持球：无人控球  ·  观察金色篮球的去向",small);
            RecapCourtView.Draw(s,episode,replay,f,tiny);
            GUI.Label(new Rect(64,545,540,24),"青：进攻  橙：防守  白圈：你  细线：近 2 秒跑位",tiny);
            RecapCourtView.Panel(new Rect(620,132,612,412),new Color(.055f,.14f,.16f));
            string heading=replay.DecisionIndex<0?"观察跑位 · 尚未出手":"第 "+(replay.DecisionIndex+1)+" / "+episode.decisions.Count+" 次出手  ·  "+s.Number(episode.decisions[replay.DecisionIndex].from)+" → "+s.Number(episode.decisions[replay.DecisionIndex].to);
            GUI.Label(new Rect(640,140,570,34),heading,section);
            string extra=string.IsNullOrEmpty(s.OptionalNarration)?"本地规则模式\n位置按记录插值回放；采样间隔约 0.2 秒。金色实线是球的记录轨迹，虚线箭头仅表示出手目标。":"整段练习的可选解说（不随出手切换）\n"+s.OptionalNarration;
            const float width=550;
            float localHeight=body.CalcHeight(new GUIContent(s.Review.text),width);
            float extraHeight=small.CalcHeight(new GUIContent(extra),width);
            float extraTop=localHeight+26;
            scroll=GUI.BeginScrollView(new Rect(640,184,576,346),scroll,new Rect(0,0,width,extraTop+extraHeight+16));
            GUI.Label(new Rect(0,0,width,localHeight),s.Review.text,body);
            RecapCourtView.Panel(new Rect(0,localHeight+12,width,1),new Color(.23f,.37f,.39f));
            GUI.Label(new Rect(0,extraTop,width,extraHeight),extra,small);GUI.EndScrollView();
            GUI.Label(new Rect(624,550,594,45),"金色实线：篮球近 2 秒轨迹   虚线箭头：该次出手目标\n播放或拖动时间轴，右侧自动切换到对应传球。",tiny);
            GUI.backgroundColor=teal;
            if(GUI.Button(new Rect(48,658,360,42),"同一场景再练  →",button))s.RetryScenario();
            GUI.backgroundColor=Color.white;
            if(GUI.Button(new Rect(424,658,360,42),"下一场景  →",button))s.NextScenario();
            if(GUI.Button(new Rect(800,658,432,42),"选择战术 / 角色 / 防守",button))s.OpenMenu();
            GUI.matrix=old;GUI.color=oldColor;GUI.backgroundColor=oldBackground;
        }
    }
}
