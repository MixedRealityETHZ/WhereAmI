using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;




public static class ReadExt
{
    public static Vector3 ReadVector3(this BinaryReader r) => new (r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
}




public static class MeshUtils
{
    public static Mesh CreatePointCloudMesh(IEnumerable<(Vector3 position, Color32 color)> points)
    {
        //List of all vertices, indices, uvs and colors
        List<Vector3> vertices = new();
        List<int> indices = new();
        List<Vector2> uvs = new();
        List<Color32> colors = new();


        int i = 0;
        foreach ((Vector3 p, Color32 c) in points)
        {
            // Create a vertex quad - all points in the same place, same colors, but different uvs. Shader is responsible for moving vertices to create a quad
            vertices.Add(p);
            vertices.Add(p);
            vertices.Add(p);
            vertices.Add(p);

            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(1, 1));

            indices.Add(i);
            indices.Add(i + 1);
            indices.Add(i + 2);

            indices.Add(i + 2);
            indices.Add(i + 1);
            indices.Add(i + 3);

            colors.Add(c);
            colors.Add(c);
            colors.Add(c);
            colors.Add(c);

            i += 4;
        }

        //Set the mesh 
        return new()
        {
            name = "PointsMesh",
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32, // use an Uint32 to allow a huge number of points
            vertices = vertices.ToArray(),
            uv = uvs.ToArray(),
            triangles = indices.ToArray(),
            colors32 = colors.ToArray()
        };

    }
}









//Visualize a set of points in game
public class PointsVisualization : MonoBehaviour
{
    [Tooltip("The txt file to load points from")]    
    public TextAsset pointsFile;

    [Tooltip("How much to move points alongside their normals to avoid them to be hidden inside the building mesh")]
    public float depthCompensation = 0.6f;

    //[Tooltip("Whether to load and show points in editor as well as in game")]
    //public bool showPointsInEditor = false;


    public GameObject pointsRegionPrefab;

    const float bounds_size = 10;
    Dictionary<(int, int), PointsVisualizationRegion> _regions;

   //If in editor and showing points is enabled, load them
//#if UNITY_EDITOR
//    private void OnValidate()
//    {
//        UnityEditor.EditorApplication.delayCall += () =>
//        {
//            if (Application.isPlaying) return;
//            if (this.IsDestroyed()) return;
//            if (!showPointsInEditor)
//            {
//                GetComponent<MeshFilter>().mesh = null;
//                return;
//            }
//            RegenerateMesh();
//        };
//    }
//#endif

    //When the game starts, generate the mesh and reset the animation timer
    private void Start()
    {
        RegenerateMesh();
    }

    IEnumerable<(Vector3 p, Color32 color)> ReadPointsFromFile(BinaryReader f)
    {
        int vert_count = f.ReadInt32();

        for (int i = 0; i < vert_count; i++)
        {
            Vector3 p = f.ReadVector3();
            byte r = f.ReadByte(), g = f.ReadByte(), b = f.ReadByte();
            Vector3 n = f.ReadVector3();

            yield return (p + depthCompensation * n, new Color32(r, g, b, 255));
        }
    }

    //Generate the point mesh
    private void RegenerateMesh()
    {
        if (_regions != null)
            foreach (var region in _regions)
                Destroy(region.Value.gameObject);

        _regions = new();

        BinaryReader f = new(new MemoryStream(pointsFile.bytes));
        foreach ((Vector3 p, Color32 c) in ReadPointsFromFile(f))
        {
            int x = Mathf.FloorToInt(p.x / bounds_size), z = Mathf.FloorToInt(p.z / bounds_size);
            if (_regions.TryGetValue((x, z), out PointsVisualizationRegion reg)){
                reg.AddPoint(p, c);
            }
            else
            {
                var r = Instantiate(pointsRegionPrefab, transform).GetComponent<PointsVisualizationRegion>();
                r.AddPoint(p, c);
                Vector3 extent = new(bounds_size, 1000, bounds_size);
                r.bounds = new Bounds(new Vector3(bounds_size * (x+0.5f), 0, bounds_size * (z+0.5f)), extent);
                _regions[(x, z)] = r;
            }
        }
    }
}
