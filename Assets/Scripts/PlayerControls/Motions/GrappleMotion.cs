﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CharMotions
{
    public struct GrappleInfo
    {
        public GameObject grappledObject;
        public Rigidbody grappledRigidbody;
        public Vector3 grapplePoint;
        public Vector3 localGrapplePoint;
        public Vector3 toGrapplePoint;
        public float length;
        public bool isGrappled;

        public void Set(RaycastHit rayHit)
        {
            grapplePoint = rayHit.point;
            grappledObject = rayHit.collider.gameObject;
            localGrapplePoint = Quaternion.Inverse(grappledObject.transform.rotation) * (rayHit.point - grappledObject.transform.position);
            grappledRigidbody = rayHit.collider.attachedRigidbody;
            length = rayHit.distance;
            isGrappled = true;
        }

        public Vector3 GetFromTo(Vector3 pointFrom)
        {
            if (isGrappled)
                return (grapplePoint - pointFrom);
            else
                return Vector3.zero;
        }

        public void Reset()
        {
            grappledObject = null;
            grapplePoint = Vector3.zero;
            localGrapplePoint = Vector3.zero;
            grappledRigidbody = null;
            length = 0;
            isGrappled = false;
        }
    }

    public class GrappleMotion : CharMotions.Motion
    {
        float _maxGrappleDistance = 200.0f;

        private LineRenderer _lineRender = null;

        GrappleInfo _grappleInfo = new GrappleInfo();

        [SerializeField]
        bool IS_LONGER = false;
        [SerializeField]
        float LENGTH = 0;

        [SerializeField]
        float accelLength;

        public static GrappleMotion Create(GameObject newParent, Rigidbody newCharBody, Collider newCharCollider)
        {
            GrappleMotion motion = newParent.GetComponent<GrappleMotion>();
            if (motion == null)
                motion = newParent.AddComponent<GrappleMotion>();

            motion._charBody = newCharBody;
            motion._charCollider = newCharCollider;



            if (motion._charBody.GetComponent<LineRenderer>() == null) {
                motion._lineRender = motion._charBody.gameObject.AddComponent<LineRenderer>();
            } else
            {
                motion._lineRender = motion._charBody.GetComponent<LineRenderer>();
                motion._lineRender.SetPosition(0, motion._charBody.transform.position);
                motion._lineRender.SetPosition(1, motion._charBody.transform.position);
		        motion._lineRender.positionCount = 2;
		        motion._lineRender.startWidth = motion._lineRender.endWidth = 0.15f;
		        motion._lineRender.material = new Material(Shader.Find("Sprites/Default"));
		        motion._lineRender.startColor = motion._lineRender.endColor = Color.black;
            }

            return motion;
        }

        public override void UpdateInputs(InputState newInputs)
        {
            _inputs = newInputs;


            if (_inputs.spaceSign > 0) {
                Quaternion lookDirection =  Quaternion.AngleAxis(_inputs.mousePositionX, Vector3.up)
                                            * Quaternion.AngleAxis(_inputs.mousePositionY, Vector3.right);
                TryGrapple(lookDirection);
            }
            if (_inputs.spaceSign < 0) {
                _grappleInfo.Reset();
            }
            
            /*if (_grappleInfo.isGrappled)
            {
                if (_inputs.spaceHeld > 0)
                {
                    _grappleInfo.length = Mathf.MoveTowards(_grappleInfo.length, 3, 0.1f);
                }
            }*/
        }

        public override void BeginMotion(Vector3 oldVelocity)
        {
            _lineRender.positionCount = 2;
            _lineRender.enabled = true;
            _velocity = oldVelocity;
            _charBody.useGravity = false;
            _charBody.isKinematic = false;
        }

        public override void EndMotion()
        {
            _lineRender.enabled = false;
        }

        public override void ProcessMotion()
        {
            

            if (!_grappleInfo.isGrappled)
                return;

            // RENDER GRAPPLE LINE
            _lineRender.SetPosition(1, _grappleInfo.grapplePoint );
            _lineRender.SetPosition(0, _charBody.transform.position);


            // Retract or extend the rope
            if (_inputs.forward > 0) 
            {
                _grappleInfo.length = Mathf.MoveTowards(_grappleInfo.length, 1, 10f * Time.deltaTime);
            } else if (_inputs.backward > 0) 
            {
                _grappleInfo.length = Mathf.MoveTowards(_grappleInfo.length, 200, 10f * Time.deltaTime);
            }
            
            ProcessVelocity();

            ProcessRotation();    

        }

        private void ProcessVelocity()
        {
            //interpolate for 5 steps
            int steps = 5;
            float stepTime = Time.deltaTime/steps;
            Vector3 newPosition = _charBody.transform.position + (_velocity * stepTime);
            for (int cnt = 0; cnt < steps; cnt++)
            {
                Vector3 grappleDirection = _grappleInfo.GetFromTo(newPosition).normalized;
                float grappleRopeLength = _grappleInfo.GetFromTo(newPosition).magnitude;
                float lengthDelta = (grappleRopeLength - _grappleInfo.length);
                LENGTH = lengthDelta;

                if (lengthDelta > 0) {
                    float tensionCoefficient = 10000 / lengthDelta;
                    float forceAmount = tensionCoefficient * lengthDelta;
                    float accelerationAmount = forceAmount / _charBody.mass;
                    Vector3 accelerationVector = grappleDirection * accelerationAmount;
                    _velocity += accelerationVector * stepTime;

                } else
                {
                    
                }

                _velocity += Physics.gravity * stepTime;

                // AIR DRAG
                Vector3 airDragAcceleration = _velocity.normalized * ( 0.5f * ((_velocity.sqrMagnitude)/2)) / _charBody.mass;
                _velocity -= airDragAcceleration * stepTime;

                newPosition = newPosition + (_velocity * stepTime);
            }

            // remove part of velocity after hitting something
            if ( (_contactNormal.sqrMagnitude > 0) && (Vector3.Dot(_contactNormal, _velocity) < 0) )
                    _velocity = Vector3.ProjectOnPlane(_velocity, _contactNormal);

            //Debug.DrawRay(this.transform.position, _velocity, Color.yellow, Time.deltaTime);

            // APPLY VELOCITY
            _charBody.velocity = _velocity;
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
            _lineRender.positionCount = 0;
            return _charBody.velocity;
        }


        public void TryGrapple(Quaternion direction)
        {
            RaycastHit rayHit;

            if (Physics.Raycast(_charBody.transform.position, 
                                direction * Vector3.forward,
                                out rayHit, 
                                _maxGrappleDistance) )
            {
                _grappleInfo.Set(rayHit);
            } else 
            {
                _grappleInfo.Reset();
            }
        }

        public void ResetGrapple()
        {
            _grappleInfo.Reset();
        }

        public bool isGrappled()
        {
            return _grappleInfo.isGrappled;
        }

    }
}