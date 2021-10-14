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


[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]


public class CharController : MonoBehaviour
{
    private Rigidbody _charBody;
    private Collider _charCollider;
    private SurfaceController _surfaceControl;

    [SerializeField]
    private CharState _currentState;
    private CharState _previousState;

    private Dictionary<CharState, CharMotions.Motion> _charMotions = new Dictionary<CharState, CharMotions.Motion>();

    [SerializeField]
    float speed;



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

        speed = _charBody.velocity.magnitude;        
    }

    public float GetVelocity()
    {
        if (_charMotions.ContainsKey(_currentState))
            return _charMotions[_currentState].GetVelocity().magnitude;
        else
            return 0;
    }
}
