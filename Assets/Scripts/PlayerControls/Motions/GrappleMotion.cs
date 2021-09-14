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
            return grappledObject.transform.position + grappledObject.transform.rotation * localGrapplePoint;
        }
        public Vector3 GetFromTo(Vector3 pointFrom)
        {
            return (GetWorldPoint() - pointFrom);
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
        float MaxGrappleDistance = 200.0f;

        private LineRenderer lineRender = null;

        GrappleInfo grappleInfo = new GrappleInfo();

        public static GrappleMotion Create(GameObject newParent, Rigidbody newCharBody, Collider newCharCollider)
        {
            GrappleMotion motion = newParent.GetComponent<GrappleMotion>();
            if (motion == null)
                motion = newParent.AddComponent<GrappleMotion>();

            motion._charBody = newCharBody;
            motion._charCollider = newCharCollider;



            if (motion._charBody.GetComponent<LineRenderer>() == null) {
                motion.lineRender = motion._charBody.gameObject.AddComponent<LineRenderer>();
            } else
            {
                motion.lineRender = motion._charBody.GetComponent<LineRenderer>();
                motion.lineRender.SetPosition(0, motion._charBody.transform.position);
                motion.lineRender.SetPosition(1, motion._charBody.transform.position);
		        motion.lineRender.positionCount = 2;
		        motion.lineRender.startWidth = motion.lineRender.endWidth = 0.15f;
		        motion.lineRender.material = new Material(Shader.Find("Sprites/Default"));
		        motion.lineRender.startColor = motion.lineRender.endColor = Color.black;
            }

            return motion;
        }

        public override void BeginMotion(Vector3 oldVelocity)
        {
            lineRender.positionCount = 2;
            lineRender.enabled = true;
            _velocity = oldVelocity;
            _charBody.useGravity = false;
            _charBody.isKinematic = false;
        }

        public override void EndMotion()
        {
            lineRender.enabled = false;
        }

        public override void ProcessMotion()
        {
            // RENDER GRAPPLE LINE
            lineRender.SetPosition(1, grappleInfo.GetWorldPoint() );
            lineRender.SetPosition(0, _charBody.transform.position);


            Quaternion lookRotation = Quaternion.Euler(_inputs.mousePositionY, _inputs.mousePositionX, 0);
            Vector3 lookVector = Vector3.ProjectOnPlane(lookRotation * Vector3.forward, grappleInfo.GetFromTo(_charBody.transform.position)).normalized;

            Vector3 grappleDirection = grappleInfo.GetFromTo(_charBody.transform.position).normalized;
            float grappleRopeLength = grappleInfo.GetFromTo(_charBody.transform.position).magnitude;


            // VELOCITY
            if (grappleRopeLength > grappleInfo.length) 
            {
                Vector3 dampedVelocity = Vector3.Project(_charBody.velocity, grappleDirection);
                _charBody.AddForce( -dampedVelocity * 0.9f * Time.deltaTime, ForceMode.VelocityChange);

                float tensionCoefficient = 0.01f * Mathf.Pow(10, 9) / grappleInfo.length;
                Vector3 ropeTension = (grappleDirection * (grappleRopeLength - grappleInfo.length)) * tensionCoefficient;
                _charBody.AddForce( ropeTension, ForceMode.Force);

                /*_velocity = Vector3.ProjectOnPlane(_velocity + (Physics.gravity * 0.1f * Time.deltaTime), grappleDirection);
                Quaternion angularVelocityRotation = Quaternion.AngleAxis(  grappleRopeLength * _velocity.magnitude / Mathf.Pow(grappleRopeLength, 2),
                                                                        Vector3.Cross(grappleDirection, _velocity)  );
                _velocity = angularVelocityRotation * _velocity;
                _velocity = Vector3.ClampMagnitude(_velocity, Physics.gravity.magnitude * 3);
                _charBody.MovePosition( _charBody.transform.position + _velocity);*/
            } else 
            {
                /*_velocity.x = Mathf.MoveTowards(_velocity.x, 0, 0.00035f );
                _velocity.y = Mathf.MoveTowards(_velocity.y, Physics.gravity.y * 3, 0.0030f);
                _velocity.z = Mathf.MoveTowards(_velocity.z, 0, 0.00035f );

                _charBody.MovePosition( _charBody.transform.position + _velocity);*/
            }

            // ROTATION
            Quaternion lookDirection = Quaternion.Euler(0, _inputs.mousePositionX, 0);           // rotation to mouse look
            _charBody.MoveRotation( Quaternion.RotateTowards(   _charBody.transform.rotation,
                                                                lookDirection,
                                                                10.0f ) );

        }

        public override Vector3 GetVelocity()
        {
            lineRender.positionCount = 0;
            return _velocity;
        }


        public void TryGrapple(Quaternion direction)
        {
            RaycastHit rayHit;

            if (Physics.Raycast(_charBody.transform.position, 
                                direction * Vector3.forward,
                                out rayHit, 
                                MaxGrappleDistance) )
            {
                grappleInfo.Set(rayHit);
            } else 
            {
                grappleInfo.Reset();
            }
        }

        public void ResetGrapple()
        {
            grappleInfo.Reset();
        }

        public bool isGrappled()
        {
            return grappleInfo.isGrappled;
        }

    }
}