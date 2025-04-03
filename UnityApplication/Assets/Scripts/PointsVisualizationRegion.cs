using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointsVisualizationRegion : MonoBehaviour
{
    public Bounds bounds;

    MeshRenderer _meshRenderer;
    Transform _cameraTransform;

    readonly List<(Vector3 p, Color32 c)> _regionPoints = new();

    // Start is called before the first frame update
    void Start()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        App.AddHeartbeatObject(gameObject);
        _cameraTransform = Camera.main.transform;
        GetComponent<MeshFilter>().mesh = MeshUtils.CreatePointCloudMesh(_regionPoints);
        _regionPoints.Clear();
    }

    public void AddPoint(Vector3 p, Color32 c) => _regionPoints.Add((p, c));

    // Update is called once per frame
    void Update()
    {
        float dist = Mathf.Sqrt(bounds.SqrDistance(transform.worldToLocalMatrix * _cameraTransform.position.Vec4(1f)));
        float reveal = Mathf.Clamp01((30 - dist) / 10);
        _meshRenderer.enabled = reveal != 0;
        if (reveal != 0)
        {
            _meshRenderer.material.SetFloat("_Reveal", reveal);
        }
    }
}
