using UnityEngine;


[System.Serializable]
public struct BVector3
{
    public bool x, y, z;
}


//Copy parts of a world transform from one element to another
public class CopyTransform : MonoBehaviour
{
    [Tooltip("The object to copy the transform of")]
    public GameObject copy;

    [Tooltip("Which components of the position should the new object copy")]
    public BVector3 copyPosition;
    
    
    [Tooltip("Which components of the rotation, represented as Euler angles, should the new object copy")]
    public BVector3 copyRotation;

    // Update is called once per frame
    void Update()
    {
        Vector3 position = Where(copyPosition, copy.transform.position, transform.position);
        Vector3 rotation = Where(copyRotation, copy.transform.rotation.eulerAngles, transform.rotation.eulerAngles);
        transform.SetPositionAndRotation(position, Quaternion.Euler(rotation));
    }

    //Definition equivalent to numpy np.where for 3D vectors. Where the flag is true, return yes, elsewhere return no
    static Vector3 Where(BVector3 condition, Vector3 yes, Vector3 no) => new(condition.x ? yes.x : no.x, condition.y ? yes.y : no.y, condition.z ? yes.z : no.z);
}
