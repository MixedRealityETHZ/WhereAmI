using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.VisualScripting;




//One record in lamar trajectory json -> the name of the sensor, position (3 floats), rotation (4 floats, quaternion)
[Serializable]
public class RecordJSON
{
    public string sensor;
    public List<float> position;
    public List<float> rotation;

    public Vector3 Position => new(position[0], position[1], position[2]);
}


//Set of lamar records, taken at the same time (e.g. 6 navvis cameras) + the timestamp
[Serializable]
public class TrajectoryRecordJSON
{
    public string timestamp;
    public List<RecordJSON> sensor_records;
}


//A trajectory JSON - a set of trajectory records + a name of the session, e.g. navvis_2024_18_10_...
[Serializable]
public class TrajectoryJSON
{
    public string name;
    public List<TrajectoryRecordJSON> trajectory;

    public List<Vector3> ToPoints()
    {
        List<Vector3> p = new();
        foreach (TrajectoryRecordJSON t in trajectory) p.Add(t.sensor_records[0].Position.XZY());
        return p;
    }
}


//A JSON of all trajectories exported from a dataset, gathered over multiple sessions
[Serializable]
class TrajectoriesListJSON
{
    public List<TrajectoryJSON> trajectories;


    public List<List<Vector3>> ToPoints()
    {
        List<List<Vector3>> ps = new();
        foreach (TrajectoryJSON j in trajectories) ps.Add(j.ToPoints());
        return ps;
    }
}





//Class responsible for managing multiple trajectories in a dataset
public class TrajectoriesVisualization : MonoBehaviour
{
    [Tooltip("The input JSON text file")]
    public TextAsset trajectoriesJSON;

    [Tooltip("The prefab for one trajectory. Will be cloned for each trajectory in the JSON")]
    public GameObject trajectoryPrefab;

    [Tooltip("If true, trajectories will be displayed in the editor as well as in game")]
    public bool viewInEditor = false;



    // Reload the trajectories when a parameter of the object has changed and viewInEditor is enabled 
#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (Application.isPlaying) return;
            if (this.IsDestroyed()) return;
            while (transform.childCount != 0) DestroyImmediate(transform.GetChild(transform.childCount - 1).gameObject);
            if (viewInEditor) GenerateTrajectories();
        };
    }
#endif


    private void Start()
    {
        //Destroy all trajectories that might have pertained from edit mode & generate new ones
        for (int i = 0; i < transform.childCount; i++) Destroy(transform.GetChild(i).gameObject);
        GenerateTrajectories();
    }

    void GenerateTrajectories()
    {
        //Load trajectories from the JSON
        TrajectoriesListJSON trajectories = JsonUtility.FromJson<TrajectoriesListJSON>(trajectoriesJSON.text);

        //Loop over all trajectories in the JSON, create each one
        foreach (TrajectoryJSON trajectory in trajectories.trajectories)
        {
            GameObject child = Instantiate(trajectoryPrefab, transform);
            child.GetComponent<Trajectory>().LoadFromPoints(trajectory.ToPoints());
            App.AddHeartbeatObject(child);
        }
    }
}
