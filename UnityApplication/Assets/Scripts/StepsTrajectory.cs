using UnityEngine;

public class StepsTrajectory : MonoBehaviour
{
    void Start() => GetComponent<MeshRenderer>().material.SetFloat("_TravellerOffset", Random.Range(0f, 1f));
}
