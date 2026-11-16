using System;
using UnityEngine;
namespace CourtX
{
    // Distance-covered strokes: joined segments share coverage instead of producing
    // bright overlapping ends or subpixel gaps. Buffers are reused every frame.
    internal sealed class FiveCourtInk
    {
        public readonly Texture2D Texture;
        readonly Color32[] pixels;readonly int width,height;readonly float density;
        public const float Pad=3;
        public FiveCourtInk(float w,float h,float density)
        {
            this.density=density;width=Mathf.CeilToInt((w+Pad*2)*density);height=Mathf.CeilToInt((h+Pad*2)*density);
            Texture=new Texture2D(width,height,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
            pixels=new Color32[width*height];
        }
        public void Clear()=>Array.Clear(pixels,0,pixels.Length);
        public void Upload(){Texture.SetPixels32(pixels);Texture.Apply(false);}
        public void Dispose(){if(Application.isPlaying)UnityEngine.Object.Destroy(Texture);else UnityEngine.Object.DestroyImmediate(Texture);}
        public void Stroke(Vector2 a,Vector2 b,float wa,float wb,Color ca,Color cb)
        {
            float radius=Mathf.Max(wa,wb)*.5f+1;
            int minX=Mathf.Max(0,Mathf.FloorToInt((Mathf.Min(a.x,b.x)-radius+Pad)*density));
            int maxX=Mathf.Min(width-1,Mathf.CeilToInt((Mathf.Max(a.x,b.x)+radius+Pad)*density));
            int minY=Mathf.Max(0,Mathf.FloorToInt((Mathf.Min(a.y,b.y)-radius+Pad)*density));
            int maxY=Mathf.Min(height-1,Mathf.CeilToInt((Mathf.Max(a.y,b.y)+radius+Pad)*density));
            Vector2 delta=b-a;float sq=delta.sqrMagnitude;
            for(int y=minY;y<=maxY;y++)for(int x=minX;x<=maxX;x++)
            {
                var p=new Vector2((x+.5f)/density-Pad,(y+.5f)/density-Pad);
                float t=sq>.00001f?Mathf.Clamp01(Vector2.Dot(p-a,delta)/sq):0;
                float coverage=Mathf.Clamp01((Mathf.Lerp(wa,wb,t)*.5f+.65f-Vector2.Distance(p,a+delta*t))/1.3f);
                Color color=Color.Lerp(ca,cb,t);byte alpha=(byte)Mathf.RoundToInt(coverage*color.a*255);int index=(height-1-y)*width+x;
                if(alpha>pixels[index].a)pixels[index]=new Color32((byte)(color.r*255),(byte)(color.g*255),(byte)(color.b*255),alpha);
            }
        }
    }
}
