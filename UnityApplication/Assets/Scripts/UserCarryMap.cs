using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class UserCarryMap : MonoBehaviour
{
    [Tooltip("The position and forward direction of this object will be used to determine path planning starting points")]
    public GameObject planPathPointerObject;

    [Tooltip("The larger dimension of the map, in world units")]
    public float size;

    public InputActionReference _revealAction;
    //public InputActionReference _planPathAction;

    public Trajectory currentMapTrajectory;
    public Trajectory temporaryMapTrajectory;


    public GameObject worldTrajectoryPrefab;

    public UserCarryMapMarker playerMarker;
    UserCarryMapMarker _targetMarker;

    List<UserCarryMapMarker> _mapMarkers;



    public GameObject targetMarkerPrefab;
    public GameObject cameraMarkerPrefab;



    GameObject _userCamera;
    Material _material;

    Trajectory _worldTrajectory;

    List<Vector3> _currentPlannedPathWorld;
    List<Vector3> _currentPlannedPathLocal;

    float _revealAmount = 0f;
    float _plannedPathRevealAmount = 0f;
    bool _revealed = false;


    const float mapElementRevealDelay = 0.8f;

    bool ElementsRevealed => _revealAmount >= mapElementRevealDelay;

    float Revealed
    {
        get => _revealAmount;
        set
        {
            if (value == _revealAmount) return;
            foreach (UserCarryMapMarker m in _mapMarkers) m.Hidden = !ElementsRevealed;

            _material.SetFloat("_Reveal", value);

            float el_reveal = ElementRevealed;
            currentMapTrajectory.Reveal = el_reveal;
            temporaryMapTrajectory.Reveal = el_reveal;
            _revealAmount = value;
        }
    }

    float ElementRevealed => Mathf.Clamp01((Revealed - mapElementRevealDelay) / (1 - mapElementRevealDelay));


    private void Awake()
    {
        //perform things related to scaling as child objects depend on them in Start()
        _material = GetComponent<MeshRenderer>().material;
        Texture2D tex = _material.GetTexture("_MainTex") as Texture2D;
        int mx = Mathf.Max(tex.width, tex.height);
        Vector2 size_ws = new(size * tex.width / mx, size * tex.height / mx);
        transform.localScale = new Vector3(size_ws.x, 1, size_ws.y);
        _material.SetVector("_Size", size_ws);
    }

    void Start()
    {
        _revealAction.action.performed += OnSelectAction;
        App.I.ControllerActions.Trigger.performed += OnSelectAction;

        _userCamera = Camera.main.gameObject;

        if (planPathPointerObject == null) planPathPointerObject = _userCamera;
        _worldTrajectory = Instantiate(worldTrajectoryPrefab).GetComponent<Trajectory>();




        _mapMarkers = new() { playerMarker };
    }

    void OnSelectAction(InputAction.CallbackContext x)
    {
        if (!_revealed)
        {
            _revealed = !_revealed;
            _revealAmount = 0f;
            Transform t = _userCamera.transform;
            transform.SetPositionAndRotation(t.position + t.forward - new Vector3(0, .5f, 0), Quaternion.Euler(0, 180+t.rotation.eulerAngles.y, 0));
        }
        else
        {
            if (_currentPlannedPathWorld != null && _currentPlannedPathLocal != null) 
            {
                _plannedPathRevealAmount = 0f;
                _worldTrajectory.LoadFromPoints(_currentPlannedPathWorld);
                currentMapTrajectory.LoadFromPoints(_currentPlannedPathLocal);

                if (_targetMarker != null) _targetMarker.Remove();
                _targetMarker = Instantiate(targetMarkerPrefab, transform).GetComponent<UserCarryMapMarker>();
                _targetMarker.transform.localPosition = _currentPlannedPathLocal[^1];
                _targetMarker.Hidden = false;
                _mapMarkers.Add(_targetMarker);
            }
            else
            {
                _revealed = !_revealed;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        Revealed = Mathf.Clamp01(Revealed + 0.4f * Time.deltaTime * (_revealed ? 1 : -1));
        _plannedPathRevealAmount = Mathf.Min(
            ElementRevealed,
            Mathf.Clamp01(_plannedPathRevealAmount + 0.4f / (1-mapElementRevealDelay) * Time.deltaTime * (_revealed ? 1 : -1))
        );
        if (currentMapTrajectory != null) currentMapTrajectory.Reveal = _plannedPathRevealAmount;

        _mapMarkers.RemoveAll(x => x.IsDestroyed());

        if (Revealed == 1f && Physics.Raycast(new(planPathPointerObject.transform.position, planPathPointerObject.transform.forward), out RaycastHit hit) && hit.collider.CompareTag("UserCarryMap"))
        {
            
            Vector3 from_pos = _userCamera.transform.position;

            Vector3 to_pos = App.I.WorldSpaceMap.transform.localToWorldMatrix * transform.worldToLocalMatrix * hit.point.Vec4(1f);

            //planned path in world coords
            _currentPlannedPathWorld = App.I.Navigation.PlanPath(from_pos, to_pos);

            Matrix4x4 world_to_map_coord = App.I.WorldSpaceMap.transform.worldToLocalMatrix;

            _currentPlannedPathLocal = new();
            foreach (Vector3 p_ in _currentPlannedPathWorld)
            {
                Vector3 p = world_to_map_coord * p_.Vec4(1f);
                _currentPlannedPathLocal.Add(new(p.x, 0.01f, p.z));
            }
            temporaryMapTrajectory.LoadFromPoints(_currentPlannedPathLocal);
            temporaryMapTrajectory.Reveal = 1f;
        }
        else
        {
            temporaryMapTrajectory.Clear();
            _currentPlannedPathLocal = null;
            _currentPlannedPathWorld = null;
        }

        //if (Random.Range(0f, 1f) < 0.1f * Time.deltaTime) AddCameraMarker(new Vector3(Random.Range(-40f, 40f), 0f, Random.Range(-40f, 40f)));
    }

    public void AddCameraMarker(Vector3 world_position, float show_time = 10f)
    {
        var mr = Instantiate(cameraMarkerPrefab, transform).GetComponent<UserCarryMapMarker>();
        mr.RemainingTime = show_time;
        Vector3 p = App.I.WorldSpaceMap.transform.worldToLocalMatrix * world_position.Vec4(1f);
        mr.transform.localPosition = new(p.x, 0, p.z);
        mr.Hidden = !ElementsRevealed;
        _mapMarkers.Add(mr);
    }
}
