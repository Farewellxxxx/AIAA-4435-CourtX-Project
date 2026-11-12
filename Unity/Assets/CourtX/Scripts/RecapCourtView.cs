using System.Collections.Generic;
using UnityEngine;
namespace CourtX
{
    public static class RecapCourtView
    {
        static Texture2D disc,basketball,stroke,courtStrokes;
        static readonly Dictionary<Vector2Int,Texture2D> rings=new Dictionary<Vector2Int,Texture2D>();
        static Color[] raster;
        static int rasterWidth,rasterHeight;
        static readonly Color teal=new Color(.28f,.84f,.70f),orange=new Color(1,.56f,.35f),gold=new Color(1,.78f,.22f);
        static readonly Color courtLine=new Color(.76f,.85f,.80f,.88f);
        static readonly Color pathInk=new Color(.025f,.07f,.085f),passTarget=new Color(.96f,.97f,.88f);
        struct TrailSpan { public Vector2 from,to; public float freshness; }
        static readonly List<TrailSpan> trail=new List<TrailSpan>(32);
        public static void Panel(Rect rect,Color color){GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;}
        static Vector2 Point(Vector3 p)=>new Vector2(112+(p.x+7.5f)*28,146-p.z*28);
        static Vector2 Point(float x,float z)=>Point(new Vector3(x,0,z));
        static void Line(Vector2 a,Vector2 b,Color color,float width=2)
        {
            if(raster!=null){RasterLine(a,b,color,width);return;}
            var matrix=GUI.matrix;var aa=(Vector2)matrix.MultiplyPoint3x4(a);var bb=(Vector2)matrix.MultiplyPoint3x4(b);
            float pixels=Mathf.Max(1.6f,width*matrix.MultiplyVector(Vector3.up).magnitude);
            if(Mathf.Abs(aa.x-bb.x)<.01f)aa.x=bb.x=Mathf.Round(aa.x)+.5f;
            if(Mathf.Abs(aa.y-bb.y)<.01f)aa.y=bb.y=Mathf.Round(aa.y)+.5f;
            var delta=bb-aa;if(delta.sqrMagnitude<.0001f)return;
            // One-pixel alpha fringe keeps diagonals continuous without blurring their solid core.
            GUI.matrix=Matrix4x4.TRS(aa,Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg),Vector3.one);
            GUI.color=color;GUI.DrawTexture(new Rect(-.5f,-(pixels+2)*.5f,delta.magnitude+1,pixels+2),stroke);GUI.color=Color.white;GUI.matrix=matrix;
        }
        static void WorldLine(float x,float z,float xx,float zz)=>Line(Point(x,z),Point(xx,zz),courtLine,2);
        static void Arc(float x,float z,float r,float start,float end,bool dashed=false)
        {
            for(int i=0;i<128;i++)
            {
                if(dashed && i%16>=8)continue;
                float a=Mathf.Lerp(start,end,i/128f),b=Mathf.Lerp(start,end,(i+1)/128f);
                WorldLine(x+r*Mathf.Cos(a),z+r*Mathf.Sin(a),x+r*Mathf.Cos(b),z+r*Mathf.Sin(b));
            }
        }
        static void Circle(Vector2 center,float radius,Color color,float width=2)
        {
            if(raster==null)
            {
                var matrix=GUI.matrix;float scale=matrix.MultiplyVector(Vector3.up).magnitude;
                float r=radius*scale,w=Mathf.Max(1.6f,width*scale);
                var key=new Vector2Int(Mathf.RoundToInt(r*100),Mathf.RoundToInt(w*100));
                if(!rings.TryGetValue(key,out var texture))
                {
                    if(rings.Count>=32){foreach(var cached in rings.Values)Object.Destroy(cached);rings.Clear();}
                    int size=Mathf.CeilToInt(2*r+w+2);texture=new Texture2D(size,size,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
                    var pixels=new Color[size*size];
                    for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                    {float distance=new Vector2(x+.5f-size*.5f,y+.5f-size*.5f).magnitude;pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(w*.5f+.5f-Mathf.Abs(distance-r)));}
                    texture.SetPixels(pixels);texture.Apply();rings[key]=texture;
                }
                var p=matrix.MultiplyPoint3x4(center);GUI.matrix=Matrix4x4.identity;GUI.color=color;
                GUI.DrawTexture(new Rect(Mathf.Round(p.x-texture.width*.5f),Mathf.Round(p.y-texture.height*.5f),texture.width,texture.height),texture);
                GUI.color=Color.white;GUI.matrix=matrix;return;
            }
            for(int i=0;i<40;i++)
            {
                float a=i*Mathf.PI/20,b=(i+1)*Mathf.PI/20;
                Line(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,color,width);
            }
        }
        static void InitTextures()
        {
            if(disc!=null)return;
            stroke=new Texture2D(1,4,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            stroke.SetPixels(new[]{new Color(1,1,1,0),Color.white,Color.white,new Color(1,1,1,0)});stroke.Apply();
            disc=new Texture2D(64,64,TextureFormat.RGBA32,true);basketball=new Texture2D(64,64,TextureFormat.RGBA32,true);
            disc.filterMode=basketball.filterMode=FilterMode.Trilinear;disc.wrapMode=basketball.wrapMode=TextureWrapMode.Clamp;
            disc.hideFlags=basketball.hideFlags=HideFlags.HideAndDontSave;
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {
                float dx=(x-31.5f)/30,dy=(y-31.5f)/30,r=Mathf.Sqrt(dx*dx+dy*dy);
                float alpha=Mathf.Clamp01((1-r)*30);disc.SetPixel(x,y,new Color(1,1,1,alpha));
                bool seam=r>.88f||Mathf.Abs(dx)<.045f||Mathf.Abs(dy)<.045f||Mathf.Abs(Mathf.Abs(dx)-(.62f-.27f*dy*dy))<.045f;
                var c=seam?new Color(.21f,.11f,.025f):new Color(1,.70f,.16f);c.a=alpha;basketball.SetPixel(x,y,c);
            }
            disc.Apply();basketball.Apply();
        }
        static void Disc(Vector2 p,float radius,Color color)
        {GUI.color=color;GUI.DrawTexture(new Rect(p.x-radius,p.y-radius,radius*2,radius*2),disc);GUI.color=Color.white;}
        static void Court()
        {
            Panel(new Rect(48,140,548,404),new Color(.065f,.19f,.21f));
            var paint=Point(-2.45f,0);Panel(new Rect(paint.x,paint.y,4.9f*28,5.8f*28),new Color(.10f,.29f,.30f));
            var matrix=GUI.matrix;int width=Mathf.Max(1,Mathf.RoundToInt(548*matrix.MultiplyVector(Vector3.right).magnitude)),height=Mathf.Max(1,Mathf.RoundToInt(404*matrix.MultiplyVector(Vector3.up).magnitude));
            if(courtStrokes==null||courtStrokes.width!=width||courtStrokes.height!=height)
            {
                if(courtStrokes!=null)Object.Destroy(courtStrokes);
                rasterWidth=width;rasterHeight=height;raster=new Color[width*height];CourtLines();
                courtStrokes=new Texture2D(width,height,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
                courtStrokes.SetPixels(raster);courtStrokes.Apply();raster=null;
            }
            var origin=matrix.MultiplyPoint3x4(new Vector2(48,140));GUI.matrix=Matrix4x4.identity;GUI.color=Color.white;
            GUI.DrawTexture(new Rect(Mathf.Round(origin.x),Mathf.Round(origin.y),width,height),courtStrokes);GUI.matrix=matrix;
        }
        // Cache the stationary court at screen resolution. Joining arc segments by maximum
        // coverage avoids dark gaps and bright beads caused by alpha-overlapping tiny quads.
        static void RasterLine(Vector2 a,Vector2 b,Color color,float width)
        {
            var scale=new Vector2(rasterWidth/548f,rasterHeight/404f);
            a=Vector2.Scale(a-new Vector2(48,140),scale);b=Vector2.Scale(b-new Vector2(48,140),scale);
            float radius=Mathf.Max(1.6f,width*scale.y)*.5f;var delta=b-a;float length=delta.sqrMagnitude;
            int minX=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(a.x,b.x)-radius-1)),maxX=Mathf.Min(rasterWidth-1,Mathf.CeilToInt(Mathf.Max(a.x,b.x)+radius+1));
            int minY=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(a.y,b.y)-radius-1)),maxY=Mathf.Min(rasterHeight-1,Mathf.CeilToInt(Mathf.Max(a.y,b.y)+radius+1));
            for(int y=minY;y<=maxY;y++)for(int x=minX;x<=maxX;x++)
            {
                var p=new Vector2(x+.5f,y+.5f);float t=length>.0001f?Mathf.Clamp01(Vector2.Dot(p-a,delta)/length):0;
                float alpha=Mathf.Clamp01(radius+.5f-Vector2.Distance(p,a+delta*t))*color.a;int index=(rasterHeight-1-y)*rasterWidth+x;
                if(alpha>raster[index].a)raster[index]=new Color(color.r,color.g,color.b,alpha);
            }
        }
        static void CourtLines()
        {
            WorldLine(-7.5f,0,7.5f,0);WorldLine(-7.5f,0,-7.5f,-14);WorldLine(7.5f,0,7.5f,-14);WorldLine(-7.5f,-14,7.5f,-14);
            WorldLine(-2.45f,0,-2.45f,-5.8f);WorldLine(2.45f,0,2.45f,-5.8f);WorldLine(-2.45f,-5.8f,2.45f,-5.8f);
            Arc(0,-5.8f,1.8f,Mathf.PI,Mathf.PI*2);Arc(0,-5.8f,1.8f,0,Mathf.PI,true);
            float a=-Mathf.Acos(6.6f/6.75f),cornerZ=-1.575f+6.75f*Mathf.Sin(a);
            WorldLine(-6.6f,0,-6.6f,cornerZ);WorldLine(6.6f,0,6.6f,cornerZ);Arc(0,-1.575f,6.75f,-Mathf.PI-a,a);
            Arc(0,-1.575f,1.25f,Mathf.PI,Mathf.PI*2);WorldLine(-1.25f,-1.575f,-1.25f,-1.2f);WorldLine(1.25f,-1.575f,1.25f,-1.2f);
            Arc(0,-14,1.8f,0,Mathf.PI);
            Line(Point(-.9f,-1.2f),Point(.9f,-1.2f),Color.white,3);Circle(Point(0,-1.575f),6.5f,gold,2.5f);
        }
        static float ViewScale=>Mathf.Max(.001f,GUI.matrix.MultiplyVector(Vector3.up).magnitude);
        static Vector3 RecordedPoint(TacticalFrame frame,int actor)=>actor==6?frame.ball:frame.positions[actor];
        static void AddTrailSpan(TacticalFrame a,TacticalFrame b,int actor,float start,float now)
        {
            if(b.time<=start||b.time<=a.time||b.time>now+.001f)return;
            // Carrying/dribbling already follows the player's path. Gold isolates recorded flight.
            if(actor==6&&a.holder>=0&&b.holder>=0)return;
            float cut=Mathf.Clamp01((start-a.time)/(b.time-a.time));
            var from=Point(Vector3.Lerp(RecordedPoint(a,actor),RecordedPoint(b,actor),cut));
            var to=Point(RecordedPoint(b,actor));
            if((to-from).sqrMagnitude<.01f)return;
            trail.Add(new TrailSpan{from=from,to=to,freshness=Mathf.InverseLerp(start,now,b.time)});
        }
        static void Trail(TacticalEpisode episode,RecapPlayback replay,TacticalFrame sample,int actor)
        {
            trail.Clear();float start=replay.Time-2;
            int first=replay.FrameIndex;
            while(first>0&&episode.frames[first-1].time>start)first--;
            for(int i=Mathf.Max(1,first);i<=replay.FrameIndex;i++)
                AddTrailSpan(episode.frames[i-1],episode.frames[i],actor,start,replay.Time);
            AddTrailSpan(episode.frames[replay.FrameIndex],sample,actor,start,replay.Time);
            if(trail.Count==0)return;
            float scale=ViewScale,core=(actor==6?3.4f:3.0f)/scale;
            Color color=actor==6?gold:actor<3?new Color(.38f,.95f,.85f):new Color(1,.61f,.42f);
            // All dark edges precede all cores, so adjacent samples cannot erase each other.
            foreach(var span in trail)Line(span.from,span.to,pathInk,core+2.6f/scale);
            foreach(var span in trail)
            {
                // Fade hue toward the dark edge, keeping an opaque crisp core at joins.
                var ink=Color.Lerp(pathInk,color,Mathf.Lerp(.62f,1,span.freshness));
                Line(span.from,span.to,ink,core);
            }
            if(actor==6)return;
            // Place a direction chevron behind the jersey, only on a sufficiently long path.
            float remaining=27/scale;
            for(int i=trail.Count-1;i>=0;i--)
            {
                var span=trail[i];var delta=span.to-span.from;float length=delta.magnitude;
                if(length<remaining){remaining-=length;continue;}
                var dir=delta/length;var tip=span.to-dir*remaining;var side=new Vector2(-dir.y,dir.x);
                var left=tip-dir*(7/scale)+side*(4/scale);var right=tip-dir*(7/scale)-side*(4/scale);
                Line(tip,left,pathInk,5/scale);Line(tip,right,pathInk,5/scale);
                Line(tip,left,color,2.5f/scale);Line(tip,right,color,2.5f/scale);break;
            }
        }
        static void TargetArrow(PassDecision decision)
        {
            var a=Point(decision.origin);var b=Point(decision.destination);var delta=b-a;float length=delta.magnitude;if(length<1)return;
            var dir=delta/length;
            float scale=ViewScale;
            for(float d=0;d<length;d+=15)
            {
                var from=a+dir*d;var to=a+dir*Mathf.Min(d+8,length);
                Line(from,to,pathInk,4/scale);Line(from,to,passTarget,1.8f/scale);
            }
            var side=new Vector2(-dir.y,dir.x);Line(b,b-dir*11+side*5,passTarget,2/scale);Line(b,b-dir*11-side*5,passTarget,2/scale);
        }
        public static int PickActor(Rect rect,TacticalFrame frame,Vector2 pointer)
        {
            if(frame==null||!rect.Contains(pointer))return -1;
            float radius=Mathf.Max(12,17*Mathf.Min(rect.width/548,rect.height/404));
            float nearest=radius*radius;int selected=-1;
            // Visible badge interiors follow reverse paint order, not nearest centre.
            // Extra hit padding can choose the nearest actor only outside every painted badge.
            for(int i=frame.positions.Length-1;i>=0;i--)
            {
                var p=Point(frame.positions[i]);
                var screen=new Vector2(rect.x+(p.x-48)*rect.width/548,rect.y+(p.y-140)*rect.height/404);
                var delta=screen-pointer;
                var local=new Vector2(delta.x/(17*rect.width/548),delta.y/(17*rect.height/404));
                if(local.sqrMagnitude<=1)return i;
                float distance=delta.sqrMagnitude;
                if(distance<nearest){nearest=distance;selected=i;}
            }
            return selected;
        }
        public static void DrawAt(Rect rect,TacticalSession s,TacticalEpisode episode,RecapPlayback replay,GUIStyle label,bool showTarget=true,int focusActor=-1)
        {
            var matrix=GUI.matrix;
            GUI.matrix=matrix*Matrix4x4.TRS(new Vector3(rect.x-48*rect.width/548,rect.y-140*rect.height/404,0),Quaternion.identity,new Vector3(rect.width/548,rect.height/404,1));
            Draw(s,episode,replay,replay.Sample(),label,showTarget,focusActor);GUI.matrix=matrix;
        }
        public static void Draw(TacticalSession s,TacticalEpisode episode,RecapPlayback replay,TacticalFrame frame,GUIStyle label,bool showTarget=true,int focusActor=-1)
        {
            InitTextures();Court();if(frame==null)return;
            for(int i=0;i<6;i++)if(focusActor<0||focusActor==i)Trail(episode,replay,frame,i);
            if(showTarget&&replay.DecisionIndex>=0)TargetArrow(episode.decisions[replay.DecisionIndex]);
            Trail(episode,replay,frame,6);
            var canvas=GUI.matrix;float pixelScale=canvas.MultiplyVector(Vector3.up).magnitude;
            var numberStyle=new GUIStyle(label){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,fontSize=Mathf.Max(13,Mathf.RoundToInt(16*pixelScale)),padding=new RectOffset()};numberStyle.normal.textColor=new Color(.02f,.08f,.09f);
            for(int i=0;i<6;i++)
            {
                var p=Point(frame.positions[i]);if(i==episode.controlled)Circle(p,18,Color.white,2);
                if(i==frame.holder)Circle(p,i==episode.controlled?22:19,gold,3);
                Disc(p,17,pathInk);Disc(p,15,i<3?teal:orange);var screen=canvas.MultiplyPoint3x4(p);GUI.matrix=Matrix4x4.identity;
                GUI.Label(new Rect(Mathf.Round(screen.x-17*pixelScale),Mathf.Round(screen.y-17*pixelScale),Mathf.Round(34*pixelScale),Mathf.Round(34*pixelScale)),s.Number(i),numberStyle);GUI.matrix=canvas;
            }
            var ball=Point(frame.ball);var display=ball;
            // Keep the basketball legible beside an overlapping jersey; a connector locates its recorded position.
            for(int i=0;i<6;i++)if(Vector2.Distance(display,Point(frame.positions[i]))<25)
            {
                var p=Point(frame.positions[i]);var offset=display-p;if(offset.sqrMagnitude<1)offset=Vector2.right;
                display=p+offset.normalized*28;break;
            }
            if(Vector2.Distance(ball,display)>1){Line(ball,display,gold,1);Disc(ball,2,gold);}
            Disc(display,12,new Color(.02f,.08f,.09f));GUI.DrawTexture(new Rect(display.x-11,display.y-11,22,22),basketball);
        }
    }
}
