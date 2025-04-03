using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class App : MonoBehaviour
{
    public static App I;

    public Navigation Navigation;
    public GameObject WorldSpaceMap;
    public WorldSettings worldSettings;
    public UserCarryMap userCarryMap;



    MagicLeapInputs.ControllerActions _actions;
    public MagicLeapInputs.ControllerActions ControllerActions => _actions;

    // Start is called before the first frame update
    void Awake()
    {
        if (I == null) I = this;
        else Destroy(this);
        MagicLeapInputs inputs = new();
        inputs.Enable();
        _actions = new(inputs);
    }

    private void OnDestroy()
    {
        if (I == this) I = null;
    }



    public static GameObject CreateTrajectory(GameObject trajectory_prefab, List<Vector3> points, Transform parent)
    {
        GameObject t = Instantiate(trajectory_prefab, parent);
        t.GetComponent<Trajectory>().LoadFromPoints(points);
        return t;
    }

    public static void AddHeartbeatObject(GameObject o) => I.worldSettings.AddHeartbeatObject(o);

    public static void AddCameraMarker(Vector3 world_position, float time) => I.userCarryMap.AddCameraMarker(world_position, time);
}
