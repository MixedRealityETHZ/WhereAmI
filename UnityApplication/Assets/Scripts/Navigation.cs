using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;





class NavigationVertex
{
    public readonly Vector3 position;
    public readonly List<int> out_edges;
    public float distance;
    public int previous_vertex;

    public NavigationVertex(BinaryReader stream)
    {
        out_edges = new();
        position = stream.ReadVector3();
        int edge_count = stream.ReadInt32();
        for (int i = 0; i < edge_count; i++) out_edges.Add(stream.ReadInt32());
        distance = 0f;
    }
}


class Graph
{
    readonly List<NavigationVertex> vertices;

    public Graph(BinaryReader stream)
    {
        int vertex_count = stream.ReadInt32();
        vertices = new();
        for (int i = 0; i < vertex_count; i++)
        {
            vertices.Add(new NavigationVertex(stream));
        }
    }

    public List<Vector3> FindPath(Vector3 from_point, Vector3 to_point)
    {
        //Uses A* to find the shortest path. We use a simple but inefficient implementation with worst case quadratic performance
        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i].distance = float.MaxValue;
            vertices[i].previous_vertex = -1;
        }


        int from = FindClosestVertex(from_point);
        int to = FindClosestVertex(to_point);

        SortedSet<(float distance, int vertex)> vq = new()
        {
            (0, from)
        };
        vertices[from].distance = 0;

        while (vq.Count > 0)
        {
            (float best_dist, int best_vert) = vq.Min;
            
            if (best_vert == to)
            {
                List<Vector3> path = new() { };
                int p = to;
                while (p != -1)
                {
                    path.Add(vertices[p].position);
                    p = vertices[p].previous_vertex;
                }
                path.Reverse();

                return path;
            }

            vq.Remove((best_dist, best_vert));

            foreach (int nxt in vertices[best_vert].out_edges)
            {
                float dist = best_dist + Vector3.Distance(vertices[best_vert].position, vertices[nxt].position);// + Vector3.Distance(vertices[nxt].position, vertices[to].position);
                //first time visiting this vertex
                if (vertices[nxt].distance == float.MaxValue)
                {
                    vertices[nxt].distance = dist;
                    vertices[nxt].previous_vertex = best_vert;
                    vq.Add((dist, nxt));
                }
                else if (dist < vertices[nxt].distance)
                {
                    vq.Remove((vertices[nxt].distance, nxt));
                    vq.Add((dist, nxt));
                    vertices[nxt].distance = dist;
                    vertices[nxt].previous_vertex = best_vert;
                }
            }
        }
        return new List<Vector3>();
    }

    int FindClosestVertex(Vector3 x)
    {
        float best_dist = float.MaxValue;
        int best_i = -1;
        for (int i = 0; i < vertices.Count; i++)
        {
            float d = Vector2.Distance(x.XZ(), vertices[i].position.XZ());
            if (d < best_dist)
            {
                best_dist = d;
                best_i = i;
            }
        }
        return best_i;
    }

    public IEnumerable<(Vector3 pos, Color32 c)> DemoPointsIterator(Color32 color)
    {
        foreach (var v in vertices)
        {
            yield return (v.position, color);
        }
    }
}





public class Navigation : MonoBehaviour
{
    [Tooltip("The binary file to load the navigation graph from")]
    public TextAsset navigationGraphFile;


    Graph _graph;

    public GameObject pointPreviewPrefab;

    public GameObject plannedTrajectoryPrefab;

    GameObject _plannedPath;


    // Start is called before the first frame update
    void Start()
    {
        MemoryStream ms = new(navigationGraphFile.bytes);
        BinaryReader br = new(ms);
        
        _graph = new Graph(br);
    }

    public void CreateNavigationTrail(Vector3 from, Vector3 to)
    {
        List<Vector3> ps = PlanPath(from, to);
        ps.Insert(0, from);
        //We do not add to as we are not sure of the target height (makes stuff look weird)

        if (_plannedPath != null) Destroy(_plannedPath);
        _plannedPath = App.CreateTrajectory(plannedTrajectoryPrefab, ps, transform);
    }

    public List<Vector3> PlanPath(Vector3 from, Vector3 to) => _graph.FindPath(from, to);

    //void CreateDemoMesh()
    //{
    //    pointPreviewPrefab = Instantiate(pointPreviewPrefab, transform);
    //    pointPreviewPrefab.GetComponent<MeshFilter>().mesh = MeshUtils.CreatePointCloudMesh(_graph.DemoPointsIterator(new Color32(0, 210, 255, 255)));
    //}
}
