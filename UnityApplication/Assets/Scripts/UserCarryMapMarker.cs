using Unity.XR.CoreUtils;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;


public class UserCarryMapMarker : MonoBehaviour
{
    public bool matchUserPosition = false;

    GameObject _userCamera;
    Material _material;

    public Transform childMarker;
    Vector3 _defaultChildMarkerOffset;

    bool _hidden = false;
    bool _removed = false;
    public bool Hidden
    {
        get => _hidden;
        set => _hidden = value;
    }

    public float revealSpeed = 2f;

    float _remainingTime = float.PositiveInfinity;
    public float RemainingTime
    {
        get => _remainingTime;
        set => _remainingTime = value;
    }


    float _reveal = -1f;
    float Reveal
    {
        get => _reveal;
        set
        {
            if (_reveal ==  value) return;
            _material.SetFloat("_Reveal", value);
            childMarker.gameObject.SetActive(value != 0f);
            _reveal = value;
            childMarker.transform.localPosition = AnimatedLocalPosition;
        }
    }

    Vector3 AnimatedLocalPosition => _defaultChildMarkerOffset + new Vector3(0, Mathf.Pow(4 * (1 - _reveal), 3f), 0);


    // Start is called before the first frame update
    void Start()
    {
        _userCamera = Camera.main.gameObject;
        _material = GetComponent<MeshRenderer>().material;
        _defaultChildMarkerOffset = childMarker.transform.localPosition;

        Reveal = 0f;
        transform.localScale = transform.localScale.Multiply(transform.parent.localScale.Inverse());
    }

    // Update is called once per frame
    void Update()
    {
        if (_remainingTime != float.PositiveInfinity)
        {
            _remainingTime -= Time.deltaTime;
            if (_remainingTime < 0) Remove();
        }

        Reveal = Mathf.Clamp01(Reveal + ((Hidden || _removed) ? -1 : 1) * Time.deltaTime * revealSpeed);

        if (matchUserPosition)
        {
            Vector3 p = _userCamera.transform.position;
            Vector4 map_p = App.I.WorldSpaceMap.transform.worldToLocalMatrix * p.Vec4(1f);

            transform.localPosition = new Vector3(map_p.x, 0.001f, map_p.z);
        }

        if (Reveal == 0f && _removed) Destroy(gameObject);
    }

    public void Remove() => _removed = true;
}
