using UnityEngine;
using System.IO;
using Microsoft.MixedReality.Toolkit.SampleGazeData;

public class HeatmapExporter : MonoBehaviour
{
    public DrawOn3DTexture heatmapSource;
    public MeshFilter meshFilter;

    public void ExportHeatmap()
    {
        // 1. Export texture
        Texture2D heatmap = heatmapSource.MyDrawTexture;
        byte[] pngData = heatmap.EncodeToPNG();
        File.WriteAllBytes(Application.persistentDataPath + "/heatmap.png", pngData);

        // 2. Export 3D model with UV mapping
        //ObjExporter.ExportMesh(meshFilter.sharedMesh, "heatmap.obj");
    }
}

//public static class ObjExporter
//{
//    public static void ExportMesh(Mesh mesh, string filename)
//    {
//        using (StreamWriter sw = new StreamWriter(filename))
//        {
//            sw.Write(MeshToString(mesh));
//        }
//    }

//    private static string MeshToString(Mesh mesh)
//    {
//        // Standard OBJ export implementation
//        // (Include vertices, UVs, and faces here)
//    }
//}