using System.Collections.Generic;
using UnityEngine;

public class WorldSettings : MonoBehaviour
{
    public float heartbeatSpeed = 15f;
    public float heartbeatPeriod = 10f;
    float _heartbeatTime = 0f;

    readonly List<Material> _materials = new();

    public void AddHeartbeatObject(GameObject o)
    {
        _materials.Add(o.GetComponent<MeshRenderer>().material);
        _materials[^1].SetFloat("_HeartbeatSpeed", heartbeatSpeed);
        _materials[^1].SetFloat("_HeartbeatPeriod", heartbeatPeriod);
    }

    // Update is called once per frame
    void Update()
    {
        foreach (Material mat in _materials)
        {
            mat.SetFloat("_HeartbeatTime", _heartbeatTime);
        }
        _heartbeatTime += Time.deltaTime;
    }

    public void PlayRevealAnimation() => _heartbeatTime = 0f;
}
