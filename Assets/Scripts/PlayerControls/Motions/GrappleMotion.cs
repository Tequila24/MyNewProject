using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CharMotions;


namespace CharMotions
{        
    public class GrappleMotion : Motion
    {
        GrappleHook _grapple;

        [SerializeField]
        private Vector3 _adjustPosToRope = Vector3.zero;
        [SerializeField]
        private float _ropeRetractionSpeed = 0;
        [SerializeField]
        private Vector3 _summVelocity = Vector3.zero;

        [SerializeField]
        string kek;

        public bool isGrappled
        {
            get { return _grapple.isGrappled; }
        }

        public static GrappleMotion Create(GameObject newParent, Rigidbody newCharBody, Collider newCharCollider)
        {
            GrappleMotion motion = newParent.GetComponent<GrappleMotion>();
            if (motion == null)
                motion = newParent.AddComponent<GrappleMotion>();

            motion._charBody = newCharBody;
            motion._charCollider = newCharCollider;

            motion._grapple = new GrappleHook(newParent);

            motion._csControl = GameObject.Find("Canvas").GetComponent<CrosshairController>();

            return motion;
        }

        private void Start() 
        {
            Init();
        }

        private void Init()
        {
            _inputs.AddKeyLiftListener(KeyCode.W, this.OnRopeKeysPress);
            _inputs.AddKeyLiftListener(KeyCode.S, this.OnRopeKeysPress);

            _inputs.onInputUpdateEvent.AddListener(this.ProcessCrosshair);

            _inputs.AddKeyPressListener(KeyCode.Mouse0, this.TryGrapple);
            _inputs.AddKeyLiftListener(KeyCode.Mouse0, this._grapple.Reset);

            _inputs.AddKeyPressListener(KeyCode.W, this._grapple.RetractWinch);
            _inputs.AddKeyPressListener(KeyCode.S, this._grapple.ExtendWinch);
            _inputs.AddKeyLiftListener(KeyCode.W, this._grapple.StopWinch);
            _inputs.AddKeyLiftListener(KeyCode.S, this._grapple.StopWinch);

            _inputs.AddKeyDoubleTapListener(KeyCode.W, this.test);
        }
        
        private void test()
        {
            Debug.Log("dash");
        }

        private void TryGrapple()
        {
            _grapple.TryGrappleFromTo(_charBody.transform.position, _inputs.lookDirection);
        }

        public override void BeginMotion(Vector3 oldVelocity)
        {
            _velocity = oldVelocity;
            _charBody.useGravity = false;
            _charBody.isKinematic = false;
        }

        public override void EndMotion()
        {
            _adjustPosToRope = Vector3.zero;
            _ropeRetractionSpeed = 0;

            _grapple.Reset();
        }

        public override void ProcessMotion()
        {
            if (!_grapple.isGrappled)
                return;

            ProcessGrapple();

            ProcessVelocity();

            ProcessRotation();
        }

        private void ProcessCrosshair()
        {
            RaycastHit aimPointHit;
            bool canReach = _grapple.IsCanReach(_charBody.transform.position, _inputs.lookDirection, out aimPointHit);

            _csControl.SetCrosshairColor(canReach);
            if (canReach)
                _csControl.UpdateCrosshairPosition(aimPointHit.point);
            else
                _csControl.UpdateCrosshairPosition(Vector3.negativeInfinity);
        }

        private void ProcessGrapple()
        {   
            _grapple.UpdateLine(this.transform.position + _charBody.velocity * Time.deltaTime);
        }

        private void OnRopeKeysPress()
        {
            _grapple.SetNewLength(_grapple.GetFromTo(_charBody.transform.position).magnitude);
        }

        private void ProcessVelocity()
        {
            // GRAPPLE ROPE PHYSICS Interpolate for 5 steps
            int steps = 10;
            float stepTime = Time.deltaTime/steps;
            Vector3 newPosition = _charBody.transform.position + (_velocity * stepTime);
            for (int cnt = 0; cnt < steps; cnt++)
            {
                Vector3 grappleDirection = _grapple.GetFromTo(newPosition).normalized;
                float distanceToLastPoint = _grapple.GetFromTo(newPosition).magnitude;
                float lengthDelta = distanceToLastPoint - _grapple.lengthleft;

                if (lengthDelta > 0) {
                    float tensionCoefficient = 30000 / lengthDelta;
                    float forceAmount = tensionCoefficient * lengthDelta;
                    float accelerationAmount = Mathf.Clamp(forceAmount / _charBody.mass, 0, 60);
                    Vector3 accelerationVector = grappleDirection * accelerationAmount;
                    _velocity += accelerationVector * stepTime;
                    
                    if (Vector3.Dot(grappleDirection, _velocity) < 0) {
                        Vector3 centripetalVelocity = Vector3.Project(_velocity, grappleDirection);
                        _velocity -= centripetalVelocity;
                    }
                }

                // GRAVITY
                _velocity += Physics.gravity * stepTime;

                // AIR DRAG
                Vector3 airDragAcceleration = _velocity.normalized * ( 0.002f * ((_velocity.sqrMagnitude)/2)) / _charBody.mass;
                _velocity -= airDragAcceleration * stepTime;

                // SAVE NEW POSITION
                newPosition = newPosition + (_velocity * stepTime);
            }

            // remove part of velocity after hitting something
            if ( (_contactNormal.sqrMagnitude > 0) && (Vector3.Dot(_contactNormal, _velocity) < 0) ) 
            {
                //Debug.Log("CONTACT" + _contactNormal);
                _velocity = Vector3.ProjectOnPlane(_velocity, _contactNormal);
                _velocity -= _velocity * 0.01f;
            }

            // NEEDS TWEAKING
            //Vector3 lookDirection = _inputs.lookDirection * Vector3.forward * _velocity.sqrMagnitude * 0.5f;

            // APPLY VELOCITY
            _charBody.velocity = Vector3.ClampMagnitude(_velocity /*+ lookDirection * Time.deltaTime*/, Physics.gravity.sqrMagnitude * 50);
        }

        private void ProcessRotation()
        {
            // ROTATION
            Quaternion lookDirection = Quaternion.Euler(0, _inputs.mousePositionX, 0);           // rotation to mouse look
            _charBody.MoveRotation( Quaternion.RotateTowards(   _charBody.transform.rotation,
                                                                lookDirection,
                                                                10.0f ) );
        }

        public override Vector3 GetVelocity()
        {
            return _charBody.velocity;
        }        

    }
}