using UnityEngine;
using UnityEngine.InputSystem;

public class ManualAlignment : MonoBehaviour
{

    bool bound = false;
    Vector3 prev_pos = Vector3.zero;
    float prev_rot = 0;

    public InputActionReference action;

    // Start is called before the first frame update
    void Start()
    {
        App.I.ControllerActions.Bumper.started += Bind;
        App.I.ControllerActions.Bumper.canceled += Unbind;
        action.action.started += Bind;
        action.action.canceled += Unbind;
    }

    void OnEnable()
    {
        Application.onBeforeRender += UpdateBeforeRender;
    }

    void OnDisable()
    {
        Application.onBeforeRender -= UpdateBeforeRender;
    }

    void Bind(InputAction.CallbackContext _)
    {
        Transform c = Camera.main.transform;
        prev_pos = c.position;
        prev_rot = c.rotation.eulerAngles.y;
        bound = true;
    }
    void Unbind(InputAction.CallbackContext _) => bound = false;

    // Update is called once per frame
    void UpdateBeforeRender()
    {
        if (bound)
        {
            Transform c = Camera.main.transform;

            Vector3 cpos = c.position;
            float crot = c.rotation.eulerAngles.y;

            transform.RotateAround(c.position, Vector3.up, -crot + prev_rot);
            transform.position -= cpos - prev_pos;

            prev_pos = c.position;
            prev_rot = c.rotation.eulerAngles.y;
        }
    }
    }
