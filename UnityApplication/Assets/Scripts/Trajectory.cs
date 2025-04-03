using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;


public static class VecExt
{
    public static Vector2 XZ(this Vector3 v) => new(v.x, v.z);
    public static Vector3 XZY(this Vector3 v) => new(v.x, v.z, v.y);

    public static Vector4 Vec4(this Vector3 v, float w = 0f) => new(v.x, v.y, v.z, w);

    public static Vector3 Multiply(this Vector3 a, Vector3 b) => new(a.x * b.x, a.y * b.y, a.z * b.z);
}


//An animation bezier curve in 3D
class AnimationCurve3D
{
    readonly AnimationCurve x, y, z;
    public float Length => x.keys.Length == 0 ? 0f : x.keys[^1].time;

    public AnimationCurve3D()
    {
        x = new();
        y = new();
        z = new();      
    }

    public void AddKey(float time, Vector3 value, Vector3 tangent1, Vector3 tangent2)
    {
        x.AddKey(new Keyframe(time, value.x, tangent1.x, tangent2.x));
        y.AddKey(new Keyframe(time, value.y, tangent1.y, tangent2.y));
        z.AddKey(new Keyframe(time, value.z, tangent1.z, tangent2.z));
    }

    public Vector3 Evaluate(float t) => new (x.Evaluate(t), y.Evaluate(t), z.Evaluate(t));


    const float delta = 0.01f;
    public Vector3 Tangent(float t) => (Evaluate(t+delta) - Evaluate(t-delta)) / (2 * delta);

    //second derivative
    public Vector3 Acceleration(float t) => (Evaluate(t + delta) + Evaluate(t - delta) - 2 * Evaluate(t)) / (delta * delta);

    public float GetKeyTime(int i) => x.keys[i].time;
}



[System.Serializable]
public class TrajectorySettings
{
    [Range(0f, 20f), Tooltip("The width of the trajectory")]
    public float width = .2f;

    [Range(0.01f, 10f), Tooltip("The base resolution of the trajectory. Is reduced further in areas with high curvature")]
    public float stepSize = 1f;

    [Tooltip("The offset to apply to the y coordinate of the trajectories")]
    public float heightOffset = -1.3f;

    [Tooltip("Select a random color for this trajectory. Otherwise the material one is kept.")]
    public bool randomColor = false;
}



//A class for creating the mesh of one trajectory
public class Trajectory : MonoBehaviour
{
    //Trajectory settings (width, step size & offset)
    [Tooltip("The visual settings for this trajectory")]
    public TrajectorySettings trajectorySettings;

    MeshFilter _meshFilter;
    Material _material;
    float _length;
    public Material Material => _material;
    public float Reveal
    {
        set
        {
            _material.SetFloat("_Reveal", value * _length);
        }
    }

    void Start()
    {
        //Setup the material for in-game use
        _material = GetComponent<MeshRenderer>().material;
        SetupMaterial(_material);
    }

    public void LoadFromPoints(List<Vector3> points)
    {
        //create the bezier curve and the mesh for the curve

        List<Vector3> vertices;
        List<Vector2> uvs;
        List<int> indices;

        //if (points_only) (vertices, indices, uvs) = PointsToLineMesh(points);
        (vertices, indices, uvs) = CurveToLineMesh(CreateCurve(points));

        //Create a new mesh and give it the generated vertices
        if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
        if (_meshFilter.mesh == null)
        {
            _meshFilter.mesh = new()
            {
                name = "TrajectoryMesh",
                vertices = vertices.ToArray(),
                uv = uvs.ToArray(),
                triangles = indices.ToArray()
            };
            _meshFilter.mesh.MarkDynamic();
        }
        else
        {
            Mesh mesh = _meshFilter.mesh;
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
        }


        //if in editor, setup the material immediately (else this will be done in Start())
        if (!Application.isPlaying) SetupMaterial(GetComponent<MeshRenderer>().sharedMaterial);
    }

    public void Clear()
    {
        if (_meshFilter != null && _meshFilter.mesh != null) _meshFilter.mesh.Clear();
    }

    AnimationCurve3D CreateCurve(List<Vector3> points)
    {
        AnimationCurve3D curve = new();


        float dist = 0f;
        //go over all points on the trajectory
        for (int i = 0; i < points.Count; i++)
        {
            //the estimates for the tangents using previous to current and current to next (or zero if points don't exist)
            Vector3 dif1 = points[i] - points[Mathf.Max(0, i-1)];
            Vector3 dif2 = points[Mathf.Min(i+1, points.Count-1)] - points[i];

            //Estimate the tangents, set the tangents of the curve based on the actual distance to the next point
            Vector3 tangent = (dif1 + dif2).normalized;
            Vector3 t1 = tangent;
            Vector3 t2 = tangent;

            //add the current point with the tangents to the bezier curve
            curve.AddKey(dist, points[i] + new Vector3(0, trajectorySettings.heightOffset, 0), t1, t2);
            
            //update distance from the start of the trajectory
            dist += dif2.magnitude;
        }
        return curve;
    }


    //Generate a mesh for the currently set bezier curve
    (List<Vector3>, List<int>, List<Vector2>) CurveToLineMesh(AnimationCurve3D curve)
    {
        //Generate the vertices, indices, and uvs (x = distance from curve start, y = -1 to 1 from the top of curve to bottom)
        List<Vector3> vertices = new();
        List<int> indices = new();
        List<Vector2> uvs = new();

        //the distance travelled so far
        float x = 0f;
        while (x < curve.Length)
        {
            //evaluate the position, tangent, and rate of change of tangent
            Vector3 p = curve.Evaluate(x);
            Vector3 t = curve.Tangent(x);
            float a = curve.Acceleration(x).magnitude;

            //compute a normal to the line, in the XY plane
            Vector3 normal = new Vector3(-t.z, 0,  t.x).normalized;

            int k = vertices.Count;

            //Add two vertices. Both lie in the XY plane passing through the line vertex, both are in the normal directions
            vertices.Add(p - normal * trajectorySettings.width / 2);
            vertices.Add(p + normal * trajectorySettings.width / 2);

            //Add to uvs -> current distance, (-1 & 1) to both sides of the line
            uvs.Add(new(x, -1));
            uvs.Add(new(x, 1));

            //Add two triangles to indices, forming a quad with the last two vertices and the current two
            if (x != 0)
            {
                indices.Add(k - 1);
                indices.Add(k - 2);
                indices.Add(k);

                indices.Add(k - 1);
                indices.Add(k);
                indices.Add(k + 1);
            }

            //add to x: step size, decreased by up to a factor of 10 if the acceleration is too high
            x += trajectorySettings.stepSize * (0.1f + 0.9f * Mathf.Exp(-a));
        }
        _length = x;
        return (vertices, indices, uvs);
    }

    void SetupMaterial(Material mat)
    {
        if (trajectorySettings.randomColor && Application.isPlaying)
        {
            //We are in game - select a random, high-saturation color
            Vector3 base_color_vec = new(Random.value, Random.value, Random.value);
            float mn = base_color_vec.MinComponent();
            base_color_vec = new(base_color_vec.x - mn, base_color_vec.y - mn, base_color_vec.z - mn);

            float mx = base_color_vec.MaxComponent();
            base_color_vec /= mx;

            mat.SetColor("_Color", new(base_color_vec.x, base_color_vec.y, base_color_vec.z));
        }        
        mat.SetFloat("_TrajectoryWidth", trajectorySettings.width);
    }
}
