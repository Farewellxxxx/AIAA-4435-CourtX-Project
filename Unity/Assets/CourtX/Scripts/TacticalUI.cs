using UnityEngine;

namespace CourtX
{
    public static class TacticalUI
    {
        static Font font;
        static GUIStyle title,body,small,button;
        static void Init()
        {
            if(body!=null)return;
            font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},20);
            body=new GUIStyle(GUI.skin.label){font=font,fontSize=19,wordWrap=true};body.normal.textColor=new Color(.94f,.96f,.91f);
            title=new GUIStyle(body){fontSize=32,fontStyle=FontStyle.Bold};
            small=new GUIStyle(body){fontSize=15};
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=19,wordWrap=true};
        }
        static void Panel(Rect rect,Color color){GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;}
        public static void Draw(TacticalSession s,ref int drill,ref int role,bool help)
        {
            Init();var old=GUI.matrix;float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2),Quaternion.identity,Vector3.one*scale);
            var ink=new Color(.025f,.10f,.13f,.92f);var teal=new Color(.11f,.69f,.60f,1);
            if(s.MenuOpen)
            {
                Panel(new Rect(0,0,1280,720),new Color(.015f,.06f,.08f,.78f));
                Panel(new Rect(260,68,760,586),ink);
                GUI.Label(new Rect(300,96,680,50),"COURTX  /  3v3 配合练习",title);
                GUI.Label(new Rect(300,155,680,40),"固定角色 · 真实传接 · 读防守，找空间",body);
                GUI.Label(new Rect(300,216,680,30),"01   选择战术",small);
                GUI.backgroundColor=drill==0?teal:Color.white;
                if(GUI.Button(new Rect(300,252,326,74),"高位挡拆\n站定掩护 → 绕过 → 顺下接球",button))drill=0;
                GUI.backgroundColor=drill==1?teal:Color.white;
                if(GUI.Button(new Rect(646,252,326,74),"高位接应 · 传切\n提上接球 → 外侧切入 → 回传",button))drill=1;
                GUI.Label(new Rect(300,350,680,30),"02   选择开局角色",small);
                GUI.backgroundColor=role==0?teal:Color.white;
                if(GUI.Button(new Rect(300,388,326,56),"01 持球人开局",button))role=0;
                GUI.backgroundColor=role==1?teal:Color.white;
                if(GUI.Button(new Rect(646,388,326,56),"04 无球队友开局",button))role=1;
                GUI.backgroundColor=teal;
                if(GUI.Button(new Rect(300,484,672,58),"开始新回合  →",button))s.StartDrill((TacticalDrill)drill,role==1);
                GUI.backgroundColor=Color.white;
                if(GUI.Button(new Rect(300,563,326,42),"继续当前回合",button))s.Resume();
                if(GUI.Button(new Rect(646,563,326,42),"退出",button))Application.Quit();
                GUI.Label(new Rect(300,616,680,25),"这是半场配合原型；光圈是建议跑位点。可随时自由移动。",small);
            }
            else
            {
                bool map=s.player.overview.enabled;
                float panelWidth=map?242:570,topHeight=map?216:121;
                Panel(new Rect(24,20,panelWidth,topHeight),ink);Panel(new Rect(24,20,5,topHeight),teal);
                string phase=new[]{"集结 / 04 准备掩护","掩护站定 / 01 绕过","顺下 / 寻找传球窗口","高位接应 / 传给 04","传切 / 01 空切接回传","配合完成 / R 再练一次","回合结束 / R 重试"}[(int)s.Phase];
                GUI.Label(new Rect(43,30,panelWidth-38,map?62:30),"COURTX"+(map?"\n":"   ")+(s.Drill==TacticalDrill.PickAndRoll?"高位挡拆":"高位接应 · 传切"),body);
                GUI.Label(new Rect(43,map?102:66,panelWidth-38,map?53:30),phase,map?small:body);
                string owner=s.ball.Holder<0?(s.ball.Flying?"传递中":s.ball.Deflected?"球被弹开":"无人持球"):s.Number(s.ball.Holder);
                GUI.Label(new Rect(43,map?164:103,panelWidth-38,map?60:25),"你："+s.Number(s.Controlled)+"    球权："+owner+(map?"\n":"    ")+"接球目标："+s.Number(s.SelectedReceiver),small);
                float messageY=map?249:153,messageHeight=map?145:96;
                Panel(new Rect(24,messageY,panelWidth,messageHeight),ink);
                GUI.Label(new Rect(43,messageY+11,panelWidth-38,messageHeight-16),s.Message+(s.RequestSeconds>0?"\n要球剩余 "+s.RequestSeconds.ToString("F1")+" 秒":""),small);
                if(!string.IsNullOrEmpty(s.PassReason)&&s.PassReason!=s.Message)
                {float y=map?407:260;Panel(new Rect(24,y,panelWidth,map?112:53),ink);GUI.Label(new Rect(43,y+9,panelWidth-38,map?96:42),s.PassReason,small);}
                if(help)
                {
                    Panel(new Rect(180,628,920,72),ink);
                    GUI.Label(new Rect(200,638,880,55),"WASD 移动    Shift 跑步    按住 Tab 实时俯视    鼠标 转头\n1 / 2 选队友    按住 Q / 松开 传球    E 要球    R 重开    Esc 菜单    F1 收起提示",small);
                }
                if(s.ball.Holder==s.Controlled&&!map)
                {
                    string reason;bool open=s.CanPass(s.Controlled,s.SelectedReceiver,out reason,s.ball.ReleaseSpeed);
                    GUI.color=open?new Color(.4f,1,.7f):new Color(1,.68f,.3f);
                    GUI.Label(new Rect(620,340,40,40),"+",title);GUI.color=Color.white;
                    if(s.ball.Charging)
                    {Panel(new Rect(550,404,180,7),ink);Panel(new Rect(550,404,180*s.ball.Charge,7),teal);}
                    Panel(new Rect(453,425,390,48),ink);
                    GUI.Label(new Rect(468,434,365,34),open?"传给 "+s.Number(s.SelectedReceiver)+" · 窗口开放":"传球线有风险 · 可换目标或继续移动",small);
                }
            }
            GUI.backgroundColor=Color.white;GUI.color=Color.white;GUI.matrix=old;
        }
    }
}
