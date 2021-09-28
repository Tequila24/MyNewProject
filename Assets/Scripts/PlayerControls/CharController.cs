using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CharMotions;

public enum CharState
{
    None = 0,
    Freefalling,
    Sliding,
    Walking,
    Jumping,
    Grappling,
    Flying
}

public struct KeyState
{
    private bool _isPressed;
    public bool isPressed
    {
        get { return _isPressed; }
    }
    private bool _isLifted;
    public bool isLifted
    {
        get { return _isLifted; }
    }
    private bool _isHeld;
    public bool isHeld
    {
        get { return _isHeld; }
    }

    private int _state;
    public int state
    {
        get { return _state; }
    }

    public void Update(KeyCode code)
    {
        _isPressed = Input.GetKeyDown(code);
        _isLifted = Input.GetKeyUp(code);
        _isHeld = Input.GetKey(code);

        _state = isHeld ? 1 : 0;
    }
}

public struct InputState
{
    public KeyState forward;
    public KeyState backward;
    public KeyState left;
    public KeyState right;
    
    public KeyState spaceHeld;
    public KeyState spaceSign;

    public KeyState shift;

    public KeyState mouse1;
    public KeyState mouse2;

    public float mouseDeltaX;
    public float mouseDeltaY;

    public float mousePositionX;
    public float mousePositionY;

    public Quaternion lookDirection;
};

    


    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]


public class CharController : MonoBehaviour
{
    private Rigidbody _charBody;
    private Collider _charCollider;
    private SurfaceController _surfaceControl;

    [SerializeField]
    private InputState _inputs;
    public ref InputState inputs
    {
        get { return ref _inputs;}
    }

    [SerializeField]
    private CharState _currentState;
    private CharState _previousState;

    private Dictionary<CharState, CharMotions.Motion> _charMotions = new Dictionary<CharState, CharMotions.Motion>();




    void OnValidate()
    {
        Init();
    }

    void Start()
    {
        Init();
    }

    void Init()
    {
        if (_charBody == null)
            _charBody = gameObject.GetComponent<Rigidbody>();

        if (_charCollider == null) 
            _charCollider = gameObject.GetComponent<Collider>();

        if (this.gameObject.GetComponent<SurfaceController>() == null)
            _surfaceControl = this.gameObject.AddComponent<SurfaceController>();
        else
            _surfaceControl = this.gameObject.GetComponent<SurfaceController>();


        if (!_charMotions.ContainsKey(CharState.Freefalling))
            _charMotions.Add(CharState.Freefalling, FreefallMotion.Create(this.gameObject, _charBody, _charCollider));

        if (!_charMotions.ContainsKey(CharState.Walking))
            _charMotions.Add(CharState.Walking, WalkMotion.Create(this.gameObject, _charBody, _charCollider, _surfaceControl));
        
        if (!_charMotions.ContainsKey(CharState.Grappling))
            _charMotions.Add(CharState.Grappling, GrappleMotion.Create(this.gameObject, _charBody, _charCollider));

        _charBody.velocity = Vector3.zero;
    }

    void Update()
    {
        Cursor.lockState = CursorLockMode.Locked;
        UpdateState();

        foreach (CharMotions.Motion motion in _charMotions.Values)
        {
            motion.UpdateInputs(_inputs);
        }
    }

    void OnGUI() 
    {
        ReadInputs();
    }

    public void ReadInputs()
    {
        _inputs.forward.Update(KeyCode.W);
        _inputs.backward.Update(KeyCode.S);
        _inputs.left.Update(KeyCode.A);
        _inputs.right.Update(KeyCode.D);
        _inputs.shift.Update(KeyCode.LeftShift);

        // MOUSE
        _inputs.mouse1.Update(KeyCode.Mouse0);
        _inputs.mouse2.Update(KeyCode.Mouse1);

        _inputs.mouseDeltaX = Input.GetAxis("Mouse X");
        _inputs.mouseDeltaY = -Input.GetAxis("Mouse Y");

        _inputs.mousePositionX += _inputs.mouseDeltaX;
        _inputs.mousePositionY = Mathf.Clamp(_inputs.mousePositionY + _inputs.mouseDeltaY, -90, 90);

        // DEBUG
        if (Input.GetKeyDown(KeyCode.R)) {
            this.transform.position = new Vector3(0, 100, 0);
            _charBody.velocity = Vector3.zero;
        }
        //======

         _inputs.lookDirection =    Quaternion.AngleAxis(_inputs.mousePositionX, Vector3.up)
                                    * Quaternion.AngleAxis(_inputs.mousePositionY, Vector3.right);


    }

    private void UpdateState()
    {
        _previousState = _currentState;

        bool isGrounded = false, isGrappled = false;

        // check ground
        float min_stand_distance = _charCollider.bounds.extents.y * 1.2f;
        if (_surfaceControl.contactSeparation < min_stand_distance)
            isGrounded = true;

        // check is grappled
        isGrappled = (_charMotions[CharState.Grappling] as GrappleMotion).isGrappled;


        // select current state based on state variables
        if (!isGrounded && !isGrappled)
            _currentState = CharState.Freefalling;
        else if (isGrounded && !isGrappled)
            _currentState = CharState.Walking;
        else if (isGrappled)
            _currentState = CharState.Grappling;

        if (_previousState != _currentState)
        {
            //Debug.Log(_previousState + " => " + _currentState);
            if (_charMotions.ContainsKey(_currentState))
                if (_charMotions.ContainsKey(_previousState))   
                {
                    _charMotions[_previousState].EndMotion();
                    _charMotions[_previousState].enabled = false;
                    _charMotions[_currentState].BeginMotion(_charMotions[_previousState].GetVelocity());
                }
                else
                    _charMotions[_currentState].BeginMotion(Vector3.zero);

                _charMotions[_currentState].enabled = true;
        }
    }

    void FixedUpdate()
    {
        _surfaceControl.UpdateSurface();

        if (_charMotions.ContainsKey(_currentState))
        {
            _charMotions[_currentState].ProcessMotion();
        }

        /*lookRotation = Quaternion.Euler(_inputs.mousePositionY, _inputs.mousePositionX, 0);
        RaycastHit lookHit;
        if (Physics.Raycast(this.transform.position, lookRotation * Vector3.forward, out lookHit))
            lookPoint = lookHit.point;
        else
            lookPoint = this.transform.position + lookRotation * Vector3.forward * 10;*/
    }

    public float GetVelocity()
    {
        if (_charMotions.ContainsKey(_currentState))
            return _charMotions[_currentState].GetVelocity().magnitude;
        else
            return 0;
    }
}
