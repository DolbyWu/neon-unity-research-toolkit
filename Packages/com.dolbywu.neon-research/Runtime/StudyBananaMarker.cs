using System.Collections.Generic;
using UnityEngine;
namespace NeonResearch
{
    /// <summary>Code-native visual cue only; no collider or research identity. Aim is the fixed root, not the outline.</summary>
    public static class StudyBananaMarker
    {
        public static Mesh CreateMesh()
        {
            const int rings=19,sides=10;var vertices=new List<Vector3>();var colors=new List<Color>();var body=new List<int>();var stem=new List<int>();var tip=new List<int>();
            for(int i=0;i<rings;i++)
            {
                float t=i/(float)(rings-1),a=Mathf.Lerp(-1.25f,1.25f,t);var centre=new Vector3(.5f*(Mathf.Cos(a)-.72f),.56f*Mathf.Sin(a),0);
                float radius=t>.83f?.024f:.012f+.10f*Mathf.Pow(Mathf.Clamp01(Mathf.Sin(t*Mathf.PI)),.65f);
                for(int j=0;j<sides;j++){float p=j*Mathf.PI*2/sides;vertices.Add(centre+new Vector3(Mathf.Cos(a)*Mathf.Cos(p),Mathf.Sin(a)*Mathf.Cos(p),.7f*Mathf.Sin(p))*radius);colors.Add(i<2||i>rings-3?new Color(.2f,.12f,.025f):new Color(1,.85f,.04f));}
            }
            for(int i=0;i<rings-1;i++)for(int j=0;j<sides;j++){int a=i*sides+j,b=i*sides+(j+1)%sides,c=a+sides,d=b+sides;var triangles=i<2?tip:i>=rings-4?stem:body;triangles.AddRange(new[]{a,c,b,b,c,d});}
            for(int j=1;j<sides-1;j++){tip.AddRange(new[]{0,j+1,j});int a=(rings-1)*sides;stem.AddRange(new[]{a,a+j,a+j+1});}
            var mesh=new Mesh{name="GazeFlow curved banana with stem and dark tip"};mesh.SetVertices(vertices);mesh.subMeshCount=3;mesh.SetTriangles(body,0);mesh.SetTriangles(stem,1);mesh.SetTriangles(tip,2);mesh.SetColors(colors);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
