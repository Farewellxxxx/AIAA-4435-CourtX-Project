using UnityEngine;
namespace CourtX
{
    public static class FiveCourtView
    {
        public static readonly Color Floor=new Color(.105f,.235f,.185f),Offense=new Color(.94f,.80f,.62f),Defense=new Color(.94f,.65f,.61f),Accent=new Color(.82f,.91f,.57f);
        static readonly Color[] jerseys={Offense,new Color(.74f,.86f,.78f),new Color(.69f,.83f,.89f),new Color(.88f,.77f,.88f),new Color(.94f,.87f,.57f)};
        public static readonly Color MapOffense=new Color(.81f,.92f,.88f),MapDefense=new Color(.92f,.55f,.45f),MapBall=new Color(1f,.60f,.16f);
        static readonly string[] MapNumbers={"1","2","3","4","5"};
        static readonly Color MapDefenseFill=new Color(.34f,.17f,.15f);
        static Texture2D disc;
        static GUIStyle holderCaption;
        static FiveCourtInk courtInk,trailInk;static Vector2 inkSize;static FiveRecording tailRecord;static float tailTime;static int tailFocus;
        public static Vector2 ScreenPoint(Rect r,Vector2 p)=>new Vector2(r.x+(p.x+7.5f)/15*r.width,r.y+p.y/14*r.height);
        public static Vector2 WorldPoint(Rect r,Vector2 p)=>new Vector2((p.x-r.x)/r.width*15-7.5f,(p.y-r.y)/r.height*14);
        public static float Radius(Rect r)=>Mathf.Max(10,r.width/15*.39f);
        public static int Pick(Rect r,FiveState s,Vector2 p,FiveBoard board=null)
        {
            if(s==null||!r.Contains(p))return -1;float radius=Radius(r)+3;
            Vector2 Position(int id)=>board!=null?board.DisplayPosition(id,Time.realtimeSinceStartup):s.positions[id];
            if(Vector2.Distance(ScreenPoint(r,Position(s.controlled)),p)<=radius)return s.controlled;
            for(int i=9;i>=0;i--)if(Vector2.Distance(ScreenPoint(r,Position(i)),p)<=radius)return i;return -1;
        }
        public static void Box(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        public static void Disc(Vector2 p,float radius,Color c)
        {
            if(disc==null)
            {
                disc=new Texture2D(96,96,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,hideFlags=HideFlags.HideAndDontSave};
                var colors=new Color[96*96];for(int y=0;y<96;y++)for(int x=0;x<96;x++){float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(48,48));colors[y*96+x]=new Color(1,1,1,Mathf.Clamp01(48-d));}disc.SetPixels(colors);disc.Apply();
            }
            var old=GUI.color;GUI.color=c;GUI.DrawTexture(new Rect(p.x-radius,p.y-radius,radius*2,radius*2),disc);GUI.color=old;
        }
        public static void Line(Vector2 a,Vector2 b,Color c,float width=1)
        {
            float length=Vector2.Distance(a,b);if(length<.01f)return;
            var matrix=GUI.matrix;var color=GUI.color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);GUI.color=c;
            GUI.DrawTexture(new Rect(a.x,a.y-width*.5f,length,width),Texture2D.whiteTexture);GUI.matrix=matrix;GUI.color=color;
        }
        static void Arc(Rect r,Vector2 center,float radius,float start,float end,Color color,float width)
        {Vector2 previous=Vector2.zero;for(int i=0;i<=80;i++){float a=Mathf.Lerp(start,end,i/80f)*Mathf.Deg2Rad;var p=ScreenPoint(r,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius);if(i>0)Line(previous,p,color,width);previous=p;}}
        public static Texture2D CourtTexture(Vector2 size)
        {
            var r=new Rect(Vector2.zero,size);
            if(courtInk!=null&&Vector2.Distance(inkSize,r.size)<.1f)return courtInk.Texture;
            courtInk?.Dispose();trailInk?.Dispose();inkSize=r.size;courtInk=new FiveCourtInk(r.width,r.height,2);trailInk=new FiveCourtInk(r.width,r.height,1.5f);tailRecord=null;
            var local=new Rect(0,0,r.width,r.height);var c=new Color(.67f,.79f,.70f,.69f);float w=Mathf.Max(1.8f,r.width/600*2.3f);
            void Stroke(Vector2 a,Vector2 b,Color col,float thick)=>courtInk.Stroke(ScreenPoint(local,a),ScreenPoint(local,b),thick,thick,col,col);
            void L(float x,float y,float a,float b)=>Stroke(new Vector2(x,y),new Vector2(a,b),c,w);
            void A(Vector2 center,float radius,float start,float end,Color col,float thick)
            {Vector2 previous=Vector2.zero;int count=Mathf.Max(32,Mathf.CeilToInt((end-start)*radius*.2f));for(int i=0;i<=count;i++){float angle=Mathf.Lerp(start,end,(float)i/count)*Mathf.Deg2Rad;var p=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;if(i>0)Stroke(previous,p,col,thick);previous=p;}}
            var grid=new Color(.75f,.87f,.72f,.065f);
            for(float x=-6.5f;x<7.5f;x++)Stroke(new Vector2(x,0),new Vector2(x,14),grid,w*.45f);
            for(int y=1;y<14;y++)Stroke(new Vector2(-7.5f,y),new Vector2(7.5f,y),grid,w*.45f);
            L(-7.5f,0,7.5f,0);L(-7.5f,0,-7.5f,14);L(7.5f,0,7.5f,14);L(-7.5f,14,7.5f,14);
            L(-2.45f,0,-2.45f,5.8f);L(2.45f,0,2.45f,5.8f);L(-2.45f,5.8f,2.45f,5.8f);
            L(-6.6f,0,-6.6f,2.99f);L(6.6f,0,6.6f,2.99f);
            A(new Vector2(0,1.575f),6.75f,12.1f,167.9f,c,w);
            A(new Vector2(0,5.8f),1.8f,0,360,c,w);A(new Vector2(0,14),1.8f,180,360,c,w);
            A(new Vector2(0,1.575f),1.25f,0,180,c,w);
            Stroke(new Vector2(-.9f,1.2f),new Vector2(.9f,1.2f),new Color(.87f,.89f,.78f),w*1.5f);
            A(new Vector2(0,1.575f),.225f,0,360,Offense,w*1.2f);courtInk.Upload();return courtInk.Texture;
        }
        static void Court(Rect r){if(Event.current.type!=EventType.Repaint)return;CourtTexture(r.size);DrawInk(courtInk,r);}
        static void DrawInk(FiveCourtInk ink,Rect r){var color=GUI.color;GUI.color=Color.white;GUI.DrawTexture(new Rect(r.x-FiveCourtInk.Pad,r.y-FiveCourtInk.Pad,r.width+6,r.height+6),ink.Texture);GUI.color=color;}
        static void TailSegment(Rect r,FiveState a,FiveState b,int actor,float from,float until,int focus)
        {
            if(b.time<=from||a.time>=until||b.time<=a.time)return;
            // Discontinuous scenario edits are not athletic movement.
            if(Vector2.Distance(a.positions[actor],b.positions[actor])>8*(b.time-a.time)+.15f)return;
            float lo=Mathf.Max(from,a.time),hi=Mathf.Min(until,b.time);
            var p=Vector2.Lerp(a.positions[actor],b.positions[actor],Mathf.InverseLerp(a.time,b.time,lo));
            var q=Vector2.Lerp(a.positions[actor],b.positions[actor],Mathf.InverseLerp(a.time,b.time,hi));
            float ageA=Mathf.Clamp01((lo-from)/2.5f),ageB=Mathf.Clamp01((hi-from)/2.5f),scale=r.width/600;
            Color ca=actor<5?Offense:Defense,cb=ca;float opacity=focus>=0&&focus!=actor?.16f:.72f;
            ca.a=opacity*ageA*ageA;cb.a=opacity*ageB*ageB;
            var local=new Rect(0,0,r.width,r.height);var x=ScreenPoint(local,p);var y=ScreenPoint(local,q);
            Color ha=ca,hb=cb;ha.a*=.12f;hb.a*=.12f;
            trailInk.Stroke(x,y,(2+ageA*7)*scale,(2+ageB*7)*scale,ha,hb);
            trailInk.Stroke(x,y,(.3f+ageA*3.4f)*scale,(.3f+ageB*3.4f)*scale,ca,cb);
        }
        public static void Draw(Rect r,FiveBoard board,GUIStyle label,int hover=-1,bool showTacticalOverlays=true)
            =>Draw(r,board.IsFrameStudy?null:board.Record,board.State,label,hover,board,Vector2.zero,showTacticalOverlays);
        public static Texture2D TrailsTexture(Vector2 size,FiveRecording record,FiveState state,int hover=-1)
        {
            CourtTexture(size);var r=new Rect(Vector2.zero,size);float t=state.time,from=t-2.5f;
            if(tailRecord!=record||tailTime!=t||tailFocus!=hover)
            {
                trailInk.Clear();tailRecord=record;tailTime=t;tailFocus=hover;
                int start=0;while(start+1<record.frames.Count&&record.frames[start+1].time<from)start++;
                for(int i=start+1;i<record.frames.Count&&record.frames[i-1].time<t;i++)
                    for(int a=0;a<10;a++)TailSegment(r,record.frames[i-1],record.frames[i],a,from,t,hover);
                if(record.frames.Count>0&&record.End<t)for(int a=0;a<10;a++)TailSegment(r,record.frames[record.frames.Count-1],state,a,from,t,hover);
                trailInk.Upload();
            }
            return trailInk.Texture;
        }
        public static void Draw(Rect r,FiveRecording record,FiveState state,GUIStyle label,int hover=-1,Vector2 overviewHeading=default,bool showTacticalOverlays=true)
            =>Draw(r,record,state,label,hover,null,overviewHeading,showTacticalOverlays);
        static void Draw(Rect r,FiveRecording record,FiveState state,GUIStyle label,int hover,FiveBoard board,Vector2 overviewHeading,bool showTacticalOverlays)
        {
            Court(r);
            if(record!=null&&Event.current.type==EventType.Repaint)
            {
                TrailsTexture(r.size,record,state,hover);DrawInk(trailInk,r);
            }
            float rad=Radius(r);
            Vector2 Position(int i)=>board!=null?board.DisplayPosition(i,Time.realtimeSinceStartup):state.positions[i];
            void DefensiveLink(int a,int b)
            {
                var start=ScreenPoint(r,Position(a));var end=ScreenPoint(r,Position(b));
                var delta=end-start;float length=delta.magnitude;
                if(length<=2*rad)return;
                var direction=delta/length;var tint=new Color(Defense.r,Defense.g,Defense.b,.36f);
                for(float d=rad;d<length-rad;d+=rad*.7f)
                    Line(start+direction*d,start+direction*Mathf.Min(d+rad*.28f,length-rad),tint,Mathf.Max(1,rad*.07f));
            }
            if(showTacticalOverlays)
            {
                DefensiveLink(5,6);
                int middle=state.playbookVersion==2?9:8;
                DefensiveLink(7,middle);DefensiveLink(middle,middle==9?8:9);
            }
            if(showTacticalOverlays&&state.screenActive)
            {
                var center=ScreenPoint(r,state.screenPoint);
                Vector2 direction=Vector2.down;
                if(state.playbookVersion==2)
                {
                    int screened=FiveCoach.ActiveScreened(state);
                    if(screened>=0&&screened<5&&state.screener>=0&&state.screener<5)
                    {
                        var toward=ScreenPoint(r,Position(screened+5))-ScreenPoint(r,Position(state.screener));
                        if(toward.sqrMagnitude>.01f)direction=toward.normalized;
                    }
                }
                var stop=center+direction*rad*1.65f;
                var cross=new Vector2(-direction.y,direction.x)*rad*.82f;
                Line(center,stop,Accent,Mathf.Max(2,rad*.16f));
                Line(stop-cross,stop+cross,Accent,Mathf.Max(2,rad*.18f));
            }
            if(showTacticalOverlays&&state.flight!=null)
            {
                Vector2 a=ScreenPoint(r,state.flight.origin),b=ScreenPoint(r,state.flight.destination);var dir=(b-a).normalized;float d=Vector2.Distance(a,b);
                Color path=state.flight.shot?new Color(1,.66f,.24f,.82f):new Color(1,.88f,.61f,.35f);
                for(float f=0;f<d;f+=12)Line(a+dir*f,a+dir*Mathf.Min(f+5,d),path,state.flight.shot?2.2f:1.4f);
            }
            if(board!=null&&board.IsFrameStudy)
            {
                float now=Time.realtimeSinceStartup;
                for(int i=5;i<10;i++)
                {
                    float a=board.DefenderShiftOpacity(i,now);if(a<=0)continue;
                    var p=ScreenPoint(r,board.DisplayPosition(i,now));
                    Disc(p,rad+6,new Color(.73f,1f,.95f,.9f*a));
                    Disc(p,rad+3,Floor);
                }
            }
            for(int i=0;i<10;i++)if(i!=state.controlled)Actor(r,state,i,Position(i),label,hover);
            Actor(r,state,state.controlled,Position(state.controlled),label,hover);
            if(overviewHeading!=Vector2.zero)
                DrawHeading(r,Position(state.controlled),overviewHeading,rad);
            bool held=state.flight==null&&state.holder>=0&&state.holder<10;
            var player=held?ScreenPoint(r,Position(state.holder)):Vector2.zero;
            var ball=held?player+new Vector2(rad*1.12f,-rad*.68f):ScreenPoint(r,state.Ball);
            float size=rad*(held?.48f:.60f);
            if(state.flight!=null)size*=1+.23f*Mathf.Sin(Mathf.PI*state.flight.Progress);
            DrawBasketball(ball,size);
            if(held)
            {
                if(holderCaption==null)holderCaption=new GUIStyle(label){alignment=TextAnchor.MiddleCenter,wordWrap=false};
                holderCaption.fontSize=Mathf.Max(10,Mathf.RoundToInt(rad*.73f));
                holderCaption.normal.textColor=new Color(1,.88f,.66f);
                float width=holderCaption.CalcSize(new GUIContent("持球")).x+rad*.6f;
                float height=holderCaption.lineHeight+2;
                var tag=new Rect(player.x-width*.5f,player.y-rad-height-rad*.36f,width,height);
                if(tag.y<r.yMin)tag.y=player.y+rad*1.35f;
                if(state.holder==state.controlled&&Vector2.Dot(overviewHeading,(tag.center-player).normalized)>.35f)
                {
                    float side=overviewHeading.x>=0?-1:1;
                    tag.center=player+Vector2.right*side*(rad*1.85f+width*.5f);
                }
                Box(tag,new Color(.07f,.15f,.11f,.95f));GUI.Label(tag,"持球",holderCaption);
            }
        }
        public static void DrawMiniCourt(Rect court,FiveState state,Vector2 heading,GUIStyle numeral)
        {
            if(Event.current.type!=EventType.Repaint)return;
            Court(court);
            float radius=Mathf.Clamp(court.width*.034f,3.3f,14f);
            bool showNumbers=radius>=4.8f;
            numeral.fontSize=Mathf.RoundToInt(radius*1.45f);
            void Player(int id)
            {
                var point=ScreenPoint(court,state.positions[id]);
                bool mine=id==state.controlled;
                if(mine)
                {
                    Disc(point,radius*1.55f,Accent);
                    Disc(point,radius*1.27f,Floor);
                }
                Disc(point,radius+1,id<5?Floor:MapDefense);
                Disc(point,radius,id<5?(mine?Accent:MapOffense):MapDefenseFill);
                if(!showNumbers)return;
                numeral.normal.textColor=id<5?Floor:Color.white;
                GUI.Label(new Rect(point.x-radius,point.y-radius*1.5f,radius*2,radius*3),MapNumbers[id%5],numeral);
            }
            for(int i=0;i<10;i++)if(i!=state.controlled)Player(i);
            Player(state.controlled);
            if(heading.sqrMagnitude>.001f)DrawHeading(court,state.positions[state.controlled],heading,radius);

            var ball=ScreenPoint(court,state.Ball);
            if(state.flight==null&&state.holder>=0&&state.holder<10)
            {
                var side=new Vector2(ball.x<court.center.x?1:-1,-.55f).normalized;
                if(state.holder==state.controlled&&Vector2.Dot(side,heading.normalized)>.45f)
                    side=new Vector2(-side.y,side.x);
                ball+=side*radius*1.46f;
            }
            DrawBasketball(ball,Mathf.Max(2.5f,radius*.62f));
        }
        static void DrawHeading(Rect court,Vector2 position,Vector2 heading,float radius)
        {
            var center=ScreenPoint(court,position);
            var forward=(ScreenPoint(court,position+heading)-center).normalized;
            var right=new Vector2(-forward.y,forward.x);
            var tip=center+forward*radius*2.6f;
            var leftWing=center+(forward*1.67f-right*.64f)*radius;
            var rightWing=center+(forward*1.67f+right*.64f)*radius;
            Line(leftWing,tip,Floor,radius*.53f);
            Line(rightWing,tip,Floor,radius*.53f);
            Line(leftWing,tip,Accent,radius*.28f);
            Line(rightWing,tip,Accent,radius*.28f);
        }
        static void DrawBasketball(Vector2 p,float radius)
        {
            var seam=new Color(.29f,.14f,.075f);
            Disc(p,radius*1.23f,new Color(1,.93f,.74f));Disc(p,radius*1.08f,seam);
            Disc(p,radius,new Color(1,.54f,.19f));
            float width=Mathf.Max(1,radius*.15f);
            Line(p+new Vector2(-.88f,-.30f)*radius,p+new Vector2(.88f,.30f)*radius,seam,width);
            Line(p+new Vector2(-.32f,.88f)*radius,p+new Vector2(.32f,-.88f)*radius,seam,width);
            Line(p+new Vector2(-.78f,.48f)*radius,p+new Vector2(-.43f,-.12f)*radius,seam,width);
            Line(p+new Vector2(-.43f,-.12f)*radius,p+new Vector2(-.39f,-.82f)*radius,seam,width);
        }
        static void Actor(Rect r,FiveState state,int id,Vector2 position,GUIStyle label,int hover)
        {
            var p=ScreenPoint(r,position);float radius=Radius(r);
            if(id==state.controlled){Disc(p,radius*1.34f,Accent);Disc(p,radius*1.17f,Floor);}
            else if(hover==id){Disc(p,radius+3,Color.white);}
            Disc(p,radius+1,id<5?new Color(.025f,.11f,.09f):Defense);Disc(p,id<5?radius:radius*.95f,id<5?jerseys[id]:new Color(.23f,.29f,.25f));
            var color=label.normal.textColor;var align=label.alignment;label.normal.textColor=id<5?new Color(.08f,.18f,.16f):new Color(1,.84f,.78f);label.alignment=TextAnchor.MiddleCenter;
            GUI.Label(new Rect(p.x-radius,p.y-radius,radius*2,radius*2),FiveRules.Number(id),label);label.normal.textColor=color;label.alignment=align;
            if(state.pinned[id]){Disc(p+new Vector2(radius*.75f,radius*.75f),radius*.27f,Accent);}
        }
    }
}
