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
        float delta_length = 0;

        [SerializeField]
        private Vector3 ropeRetractionVelocity = Vector3.zero;
        [SerializeField]
        private Vector3 summVelocity = Vector3.zero;

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

        public override void UpdateInputs(InputState newInputs)
        {
            _inputs = newInputs;

            if (_inputs.mouse1.isPressed) {
                _grapple.TryGrappleFromTo(_charBody.transform.position, _inputs.lookDirection);
            }
            if (_inputs.mouse1.isLifted) {
                _grapple.Reset();
            }

            ProcessCrosshair();
        }

        public override void BeginMotion(Vector3 oldVelocity)
        {
            _velocity = oldVelocity;
            _charBody.useGravity = false;
            _charBody.isKinematic = false;
        }

        public override void EndMotion()
        {
            ropeRetractionVelocity = Vector3.zero;
            _grapple.Reset();
        }

        public override void ProcessMotion()
        {
            if (!_grapple.isGrappled)
                return;

            ProcessVelocity();

            ProcessRotation();

            ProcessGrapple();
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
            _grapple.UpdateLine(this.transform.position);
        }

        private void ProcessVelocity()
        {   
            // adjust to rope length
            Vector3 grappleDirection = _grapple.GetFromTo(_charBody.transform.position).normalized;
            float distanceToLastPoint = _grapple.GetFromTo(_charBody.transform.position).magnitude;
            float lengthDelta = distanceToLastPoint - _grapple.lengthleft;
            delta_length = lengthDelta;

            _velocity += Physics.gravity * Time.deltaTime;


            // if rope is not retracting nor extending, character is just swinging on it
            // 
            if ( (!_inputs.forward.isHeld) && (!_inputs.backward.isHeld) && (lengthDelta > 0) ) 
            {
                _velocity = Vector3.ProjectOnPlane(_velocity, grappleDirection);
            }

            kek = "FWRD " + _inputs.forward.isPressed + " " + _inputs.forward.isLifted + " " + _inputs.forward.isHeld;

            if (_inputs.backward.isLifted)
                Debug.Log("KEK");
            
            if (_inputs.forward.isHeld)
            {
                ropeRetractionVelocity = grappleDirection * 1000.0f * Time.deltaTime;
            }

            
            // set new length if stopped retracting or extending rope
            if ( (_inputs.forward.isLifted) || (_inputs.backward.isLifted) )
            {
                _grapple.SetNewLength(_grapple.GetFromTo(_charBody.transform.position).magnitude);
                ropeRetractionVelocity = Vector3.zero;
            }

            // remove part of velocity after hitting something
            if ( (_contactNormal.sqrMagnitude > 0) && (Vector3.Dot(_contactNormal, _velocity) < 0) ) 
            {
                //Debug.Log("CONTACT" + _contactNormal);
                _velocity = Vector3.ProjectOnPlane(_velocity, _contactNormal);
                //_velocity -= _velocity * 0.01f;
            }


            // AIR DRAG
            Vector3 airDragAcceleration = _velocity.normalized * ( 0.2f * ((_velocity.sqrMagnitude)/2)) / _charBody.mass;
            _velocity -= airDragAcceleration * Time.deltaTime;

            //summVelocity = Vector3.ClampMagnitude(_velocity + ropeRetractionVelocity, Physics.gravity.sqrMagnitude * 50);
            summVelocity = _velocity + ropeRetractionVelocity;

            // APPLY VELOCITY
            _charBody.velocity = summVelocity;
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