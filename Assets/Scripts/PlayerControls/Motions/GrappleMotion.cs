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
            _inputs.GetLiftedEvent(KeyCode.W).AddListener(OnRopeKeysPress);
            _inputs.GetLiftedEvent(KeyCode.S).AddListener(OnRopeKeysPress);

            _inputs.onInputUpdateEvent.AddListener(ProcessCrosshair);

            _inputs.GetPressedEvent(KeyCode.Mouse0).AddListener(TryGrapple);
            _inputs.GetLiftedEvent(KeyCode.Mouse0).AddListener(_grapple.Reset);

            _inputs.GetPressedEvent(KeyCode.W).AddListener(_grapple.RetractWinch);
            _inputs.GetPressedEvent(KeyCode.S).AddListener(_grapple.ExtendWinch);
            _inputs.GetLiftedEvent(KeyCode.W).AddListener(_grapple.StopWinch);
            _inputs.GetLiftedEvent(KeyCode.S).AddListener(_grapple.StopWinch);
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
            // initial values
            Vector3 grappleDirection = _grapple.GetFromTo(_charBody.transform.position).normalized;
            float distanceToLastPoint = _grapple.GetFromTo(_charBody.transform.position).magnitude;
            float ropeLengthDelta = distanceToLastPoint - _grapple.lengthleft;


            // APPLY PHYSICS
            _velocity += Physics.gravity * Time.deltaTime;
            
            // if 
            if ( (_grapple.winchDirection == 0) && (ropeLengthDelta > 0) )
            {
                _velocity = Vector3.ProjectOnPlane(_velocity, grappleDirection);
                _adjustPosToRope = grappleDirection * ropeLengthDelta;
            } else {
                _adjustPosToRope = Vector3.zero;
            }

            if (_grapple.winchDirection > 0)
            {
                // do nothing
            }

            if (_grapple.winchDirection < 0) 
            {
                _ropeRetractionSpeed = Mathf.MoveTowards(_ropeRetractionSpeed, 4000.0f * Time.deltaTime, 100.0f * Time.deltaTime);
                _velocity = Vector3.ProjectOnPlane(_velocity, grappleDirection);
            }
            else
                _ropeRetractionSpeed = 0;
                //_ropeRetractionSpeed = Mathf.MoveTowards(_ropeRetractionSpeed, 0, 200.0f * Time.deltaTime);
        

            
            // slide against contact point
            if ( (_contactNormal.sqrMagnitude > 0) && (Vector3.Dot(_contactNormal, _velocity) < 0) ) 
            {
                _velocity = Vector3.ProjectOnPlane(_velocity, _contactNormal);
            }

            // AIR DRAG
            //Vector3 airDragAcceleration = _velocity.normalized * ( 0.2f * ((_velocity.sqrMagnitude)/2)) / _charBody.mass;
            //_velocity -= airDragAcceleration * Time.deltaTime;

            _summVelocity = _velocity + _adjustPosToRope + (grappleDirection * _ropeRetractionSpeed);

            // APPLY VELOCITY
            _charBody.velocity = _summVelocity;
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