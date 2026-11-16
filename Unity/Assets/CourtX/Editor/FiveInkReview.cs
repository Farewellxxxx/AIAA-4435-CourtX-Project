using System.IO;
using CourtX;
using UnityEngine;
public static class FiveInkReview
{
    public static void Export()
    {
        Directory.CreateDirectory(ReactiveReview.Folder);
        foreach(int width in new[]{433,542,650})
        {
            int height=Mathf.RoundToInt(width*14f/15);var size=new Vector2(width,height);
            var record=FiveRecording.Lesson(FivePlay.Screen,FiveDefense.Balanced,0,0);var state=record.Sample(2.4f);
            var court=FiveCourtView.CourtTexture(size);var trails=FiveCourtView.TrailsTexture(size,record,state);
            var output=new Texture2D(width+6,height+6,TextureFormat.RGB24,false);var pixels=new Color[output.width*output.height];
            for(int y=0;y<output.height;y++)for(int x=0;x<output.width;x++)
            {
                float u=(x+.5f)/output.width,v=(y+.5f)/output.height;
                var c=court.GetPixelBilinear(u,v);var t=trails.GetPixelBilinear(u,v);
                pixels[y*output.width+x]=Color.Lerp(Color.Lerp(FiveCourtView.Floor,c,c.a),t,t.a);
            }
            output.SetPixels(pixels);output.Apply();File.WriteAllBytes(Path.Combine(ReactiveReview.Folder,"ink-"+width+".png"),output.EncodeToPNG());Object.DestroyImmediate(output);
        }
        Debug.Log("INK EXPORT COMPLETE: same runtime rasterizer, offscreen; not native UI acceptance");
    }
}
