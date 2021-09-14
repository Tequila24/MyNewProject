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

    public struct InputState
    {
        public int forward;
        public int backward;
        public int left;
        public int right;
        private int _space;
        public int space
        {
            get { return _space; }
            set { _space = value; }
        }
        public int shift;

        public int mouse1Held;
        public int mouse2Held;

        public float mouseDeltaX;
        public float mouseDeltaY;

        public float mousePositionX;
        public float mousePositionY;
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

        UpdateState();
    }

    void Update()
    {
        Cursor.lockState = CursorLockMode.Locked;
        UpdateInputs();
        UpdateState();
    }

    public void UpdateInputs()
    {
        _inputs.forward = Input.GetKey(KeyCode.W) ? 1 : 0;
        _inputs.backward = Input.GetKey(KeyCode.S) ? 1 : 0;
        _inputs.left = Input.GetKey(KeyCode.A) ? 1 : 0;
        _inputs.right = Input.GetKey(KeyCode.D) ? 1 : 0;
        _inputs.shift = Input.GetKey(KeyCode.LeftShift) ? 1 : 0;

        if (Input.GetKeyDown(KeyCode.Space))
            _inputs.space = 1;
        else if (Input.GetKeyUp(KeyCode.Space))
            _inputs.space = -1;
        else
            _inputs.space = 0;

        _inputs.mouse1Held = Input.GetKey(KeyCode.Mouse0) ? 1 : 0;
        _inputs.mouse2Held = Input.GetKey(KeyCode.Mouse1) ? 1 : 0;

        _inputs.mouseDeltaX = Input.GetAxis("Mouse X");
        _inputs.mouseDeltaY = -Input.GetAxis("Mouse Y");

        _inputs.mousePositionX += _inputs.mouseDeltaX;
        _inputs.mousePositionY = Mathf.Clamp(_inputs.mousePositionY + _inputs.mouseDeltaY, -90, 90);
    }

    private void UpdateState()
    {
        _previousState = _currentState;

        if ( (_inputs.space > 0) && (!(_charMotions[CharState.Grappling] as GrappleMotion).isGrappled()) ) 
        {
            Quaternion lookDirection =  Quaternion.AngleAxis(inputs.mousePositionX, Vector3.up)
                                        * Quaternion.AngleAxis(inputs.mousePositionY, Vector3.right);
            (_charMotions[CharState.Grappling] as GrappleMotion).TryGrapple(lookDirection);
            if ( (_charMotions[CharState.Grappling] as GrappleMotion).isGrappled())
                _currentState = CharState.Grappling; 
        } else if ( (_inputs.space < 0))
        {
            float min_stand_distance = _charCollider.bounds.extents.y * 1.2f;
            if (_surfaceControl.contactSeparation < min_stand_distance)
                _currentState = CharState.Walking;
            else
                _currentState = CharState.Freefalling;
        }


        if (_previousState != _currentState)
        {
            Debug.Log(_previousState + " => " + _currentState);
            if (_charMotions.ContainsKey(_currentState))
                if (_charMotions.ContainsKey(_previousState))   
                {
                    _charMotions[_previousState].EndMotion();
                    _charMotions[_currentState].BeginMotion(_charMotions[_previousState].GetVelocity());
                }
                else
                    _charMotions[_currentState].BeginMotion(Vector3.zero);
        }
    }

    void FixedUpdate()
    {
        _surfaceControl.UpdateSurface();

        if (_charMotions.ContainsKey(_currentState))
        {
            _charMotions[_currentState].UpdateInputs(_inputs);
            _charMotions[_currentState].ProcessMotion();
        }

        /*lookRotation = Quaternion.Euler(_inputs.mousePositionY, _inputs.mousePositionX, 0);
        RaycastHit lookHit;
        if (Physics.Raycast(this.transform.position, lookRotation * Vector3.forward, out lookHit))
            lookPoint = lookHit.point;
        else
            lookPoint = this.transform.position + lookRotation * Vector3.forward * 10;*/
    }
}
