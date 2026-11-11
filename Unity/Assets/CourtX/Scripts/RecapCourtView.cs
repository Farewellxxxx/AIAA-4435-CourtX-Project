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
        static void Trails(TacticalEpisode episode,RecapPlayback replay,TacticalFrame sample)
        {
            float start=replay.Time-2;
            for(int i=1;i<=replay.FrameIndex;i++)
            {
                var a=episode.frames[i-1];var b=episode.frames[i];if(b.time<start)continue;
                for(int actor=0;actor<6;actor++)Line(Point(a.positions[actor]),Point(b.positions[actor]),actor<3?new Color(.28f,.84f,.70f,.28f):new Color(1,.56f,.35f,.26f),2);
                Line(Point(a.ball),Point(b.ball),new Color(1,.78f,.22f,.75f),3);
            }
            var last=episode.frames[replay.FrameIndex];
            for(int actor=0;actor<6;actor++)Line(Point(last.positions[actor]),Point(sample.positions[actor]),actor<3?teal:orange,2);
            Line(Point(last.ball),Point(sample.ball),gold,3);
        }
        static void TargetArrow(PassDecision decision)
        {
            var a=Point(decision.origin);var b=Point(decision.destination);var delta=b-a;float length=delta.magnitude;if(length<1)return;
            var dir=delta/length;
            for(float d=0;d<length;d+=13)Line(a+dir*d,a+dir*Mathf.Min(d+7,length),new Color(1,.78f,.22f,.65f),2);
            var side=new Vector2(-dir.y,dir.x);Line(b,b-dir*11+side*5,gold);Line(b,b-dir*11-side*5,gold);
        }
        public static void DrawAt(Rect rect,TacticalSession s,TacticalEpisode episode,RecapPlayback replay,GUIStyle label,bool showTarget=true)
        {
            var matrix=GUI.matrix;
            GUI.matrix=matrix*Matrix4x4.TRS(new Vector3(rect.x-48*rect.width/548,rect.y-140*rect.height/404,0),Quaternion.identity,new Vector3(rect.width/548,rect.height/404,1));
            Draw(s,episode,replay,replay.Sample(),label,showTarget);GUI.matrix=matrix;
        }
        public static void Draw(TacticalSession s,TacticalEpisode episode,RecapPlayback replay,TacticalFrame frame,GUIStyle label,bool showTarget=true)
        {
            InitTextures();Court();if(frame==null)return;Trails(episode,replay,frame);
            if(showTarget&&replay.DecisionIndex>=0)TargetArrow(episode.decisions[replay.DecisionIndex]);
            var canvas=GUI.matrix;float pixelScale=canvas.MultiplyVector(Vector3.up).magnitude;
            var numberStyle=new GUIStyle(label){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,fontSize=Mathf.Max(13,Mathf.RoundToInt(16*pixelScale)),padding=new RectOffset()};numberStyle.normal.textColor=new Color(.02f,.08f,.09f);
            for(int i=0;i<6;i++)
            {
                var p=Point(frame.positions[i]);if(i==episode.controlled)Circle(p,18,Color.white,2);
                if(i==frame.holder)Circle(p,i==episode.controlled?22:19,gold,3);
                Disc(p,15,i<3?teal:orange);var screen=canvas.MultiplyPoint3x4(p);GUI.matrix=Matrix4x4.identity;
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
