using UnityEngine;
namespace CourtX
{
    public static class FiveCourtView
    {
        public static readonly Color Floor=new Color(.075f,.20f,.17f),Offense=new Color(.94f,.77f,.53f),Defense=new Color(.52f,.75f,.79f),Accent=new Color(.82f,.91f,.57f);
        static Texture2D disc;
        static FiveCourtInk courtInk,trailInk;static Vector2 inkSize;static FiveRecording tailRecord;static float tailTime;static int tailFocus;
        public static Vector2 ScreenPoint(Rect r,Vector2 p)=>new Vector2(r.x+(p.x+7.5f)/15*r.width,r.y+p.y/14*r.height);
        public static Vector2 WorldPoint(Rect r,Vector2 p)=>new Vector2((p.x-r.x)/r.width*15-7.5f,(p.y-r.y)/r.height*14);
        public static float Radius(Rect r)=>Mathf.Max(10,r.width/15*.39f);
        public static int Pick(Rect r,FiveState s,Vector2 p)
        {
            if(s==null||!r.Contains(p))return -1;float radius=Radius(r)+3;
            if(Vector2.Distance(ScreenPoint(r,s.positions[s.controlled]),p)<=radius)return s.controlled;
            for(int i=9;i>=0;i--)if(Vector2.Distance(ScreenPoint(r,s.positions[i]),p)<=radius)return i;return -1;
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
        public static void Draw(Rect r,FiveBoard board,GUIStyle label,int hover=-1)=>Draw(r,board.Record,board.State,label,hover);
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
        public static void Draw(Rect r,FiveRecording record,FiveState state,GUIStyle label,int hover=-1)
        {
            Box(new Rect(r.x-16,r.y-16,r.width+32,r.height+32),Floor);Court(r);
            float t=state.time,from=t-2.5f;
            if(record!=null&&Event.current.type==EventType.Repaint)
            {
                TrailsTexture(r.size,record,state,hover);DrawInk(trailInk,r);
            }
            float rad=Radius(r);
            if(state.screenActive)
            {
                var p=ScreenPoint(r,state.screenPoint);p.y+=rad*1.35f;
                Line(p-new Vector2(rad*.8f,0),p+new Vector2(rad*.8f,0),Accent,3);
                Line(p+new Vector2(rad*.8f,0),p+new Vector2(rad*.8f,-rad*.55f),Accent,3);
            }
            if(state.flight!=null)
            {
                Vector2 a=ScreenPoint(r,state.flight.origin),b=ScreenPoint(r,state.flight.destination);var dir=(b-a).normalized;float d=Vector2.Distance(a,b);
                for(float f=0;f<d;f+=12)Line(a+dir*f,a+dir*Mathf.Min(f+5,d),new Color(1,.88f,.61f,.35f),1.4f);
            }
            for(int i=0;i<10;i++)if(i!=state.controlled)Actor(r,state,i,label,hover);
            Actor(r,state,state.controlled,label,hover);
            var ball=ScreenPoint(r,state.Ball);if(state.holder>=0)ball+=new Vector2(rad*.8f,-rad*.8f);
            Disc(ball,rad*.35f,new Color(.09f,.18f,.14f));Disc(ball,rad*.26f,new Color(1,.62f,.25f));
        }
        static void Actor(Rect r,FiveState state,int id,GUIStyle label,int hover)
        {
            var p=ScreenPoint(r,state.positions[id]);float radius=Radius(r);
            if(id==state.controlled){Disc(p,radius+4,Accent);Disc(p,radius+2,Floor);}
            else if(hover==id){Disc(p,radius+3,Color.white);}
            Disc(p,radius+1,new Color(.025f,.11f,.09f));Disc(p,radius,id<5?Offense:Defense);
            var color=label.normal.textColor;var align=label.alignment;label.normal.textColor=new Color(.08f,.18f,.16f);label.alignment=TextAnchor.MiddleCenter;
            GUI.Label(new Rect(p.x-radius,p.y-radius,radius*2,radius*2),FiveRules.Number(id),label);label.normal.textColor=color;label.alignment=align;
            if(state.pinned[id]){Disc(p+new Vector2(radius*.75f,radius*.75f),radius*.27f,Accent);}
        }
    }
}
