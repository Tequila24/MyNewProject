using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CharMotions
{
    public struct GrappleInfo
    {
        public GameObject grappledObject;
        public Rigidbody grappledRigidbody;
        public Vector3 localGrapplePoint;
        public Vector3 toGrapplePoint;
        public float length;
        public bool isGrappled;

        public void Set(RaycastHit rayHit)
        {
            grappledObject = rayHit.collider.gameObject;
            localGrapplePoint = Quaternion.Inverse(grappledObject.transform.rotation) * (rayHit.point - grappledObject.transform.position);
            grappledRigidbody = rayHit.collider.attachedRigidbody;
            length = rayHit.distance;
            isGrappled = true;
        }

        public Vector3 GetWorldPoint()
        {
            if (isGrappled)
                return grappledObject.transform.position + grappledObject.transform.rotation * localGrapplePoint;
            else
                return Vector3.zero;
        }
        public Vector3 GetFromTo(Vector3 pointFrom)
        {
            if (isGrappled)
                return (GetWorldPoint() - pointFrom);
            else
                return Vector3.zero;
        }

        public void Reset()
        {
            grappledObject = null;
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
            if (_grappleInfo.isGrappled)
            {
                if (_inputs.spaceHeld > 0)
                {
                    _grappleInfo.length = Mathf.MoveTowards(_grappleInfo.length, 3, 0.1f);
                }
            }
        }

        public override void BeginMotion(Vector3 oldVelocity)
        {
            _lineRender.positionCount = 2;
            _lineRender.enabled = true;
            _velocity = oldVelocity;
            _charBody.useGravity = true;
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
            _lineRender.SetPosition(1, _grappleInfo.GetWorldPoint() );
            _lineRender.SetPosition(0, _charBody.transform.position);


            Quaternion lookRotation = Quaternion.Euler(_inputs.mousePositionY, _inputs.mousePositionX, 0);
            Vector3 lookVector = Vector3.ProjectOnPlane(lookRotation * Vector3.forward, _grappleInfo.GetFromTo(_charBody.transform.position)).normalized;

            Vector3 grappleDirection = _grappleInfo.GetFromTo(_charBody.transform.position).normalized;
            float grappleRopeLength = _grappleInfo.GetFromTo(_charBody.transform.position).magnitude;


            // VELOCITY
            if (grappleRopeLength > _grappleInfo.length) 
            {
                //Vector3 dampedVelocity = Vector3.Project(_charBody.velocity, grappleDirection);
                //_charBody.AddForce( -dampedVelocity * 0.9f * Time.deltaTime, ForceMode.VelocityChange);

                float tensionCoefficient = Mathf.Pow(10, 3) / _grappleInfo.length;
                Vector3 ropeTension = (grappleDirection * (grappleRopeLength - _grappleInfo.length)) * tensionCoefficient;
                _charBody.AddForce( ropeTension, ForceMode.Force);

                _charBody.velocity = Vector3.ClampMagnitude(_charBody.velocity, Physics.gravity.y * 3);

                /*_velocity = Vector3.ProjectOnPlane(_velocity + (Physics.gravity * 0.1f * Time.deltaTime), grappleDirection);
                Quaternion angularVelocityRotation = Quaternion.AngleAxis(  grappleRopeLength * _velocity.magnitude / Mathf.Pow(grappleRopeLength, 2),
                                                                        Vector3.Cross(grappleDirection, _velocity)  );
                _velocity = angularVelocityRotation * _velocity;
                _velocity = Vector3.ClampMagnitude(_velocity, Physics.gravity.magnitude * 3);
                _charBody.MovePosition( _charBody.transform.position + _velocity);*/
            } else 
            {
                Vector3 dampedVelocity = _charBody.velocity * 0.1f;
                _charBody.AddForce( -dampedVelocity, ForceMode.VelocityChange);
            }

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