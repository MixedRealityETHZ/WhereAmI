using UnityEngine;


//Rotate the minimap circle. Allow for shifting to set north correctly
public class MinimapCircle : MonoBehaviour
{
    GameObject _mainCamera;
    RectTransform _rectTransform;

    //the offset for the north direction, in degrees
    public float northOffset = 0f;


    // Start is called before the first frame update
    void Start()
    {
        _mainCamera = Camera.main.gameObject;
        _rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        //copy the rotation of the main camera, + the north offset, to rotate the minimap circle
        _rectTransform.localRotation = Quaternion.Euler(0f, 0f, _mainCamera.transform.eulerAngles.y + northOffset);
    }
}
