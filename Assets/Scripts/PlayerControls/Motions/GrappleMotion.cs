using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CharMotions
{
    public class GrappleInfo
    {
        private float _maxLength = 200.0f;
        public float maxLength
        {
            get { return _maxLength;}
        }
        private float _minLength = 1.0f;
        public float minlength
        {
            get { return _minLength;}
        }
        private float _lengthLeft;
        public float lengthLeft
        {
            get { return _lengthLeft;}
        }
        public int PointsCount
        {
            get { return linePoints.Count; }
        }

        public GameObject grappledObject;
        public Rigidbody grappledRigidbody;
        public List<Vector3> linePoints = new List<Vector3>();

        public Vector3 Point
        {
            get { 
                if (linePoints.Count > 0)
                    return linePoints[0];
                else
                    return Vector3.zero; }
        }
        
        public Vector3 localGrapplePoint;
        public Vector3 toGrapplePoint;
        public float length;
        public bool isGrappled;

        public GrappleInfo()
        {
            Reset();
        }

        public void Set(RaycastHit rayHit)
        {
            AppendPoint(rayHit.point);
            grappledObject = rayHit.collider.gameObject;
            length = rayHit.distance;
            isGrappled = true;

            linePoints.Add(Point);
        }

        public Vector3 GetFromTo(Vector3 pointFrom)
        {
            if (isGrappled)
                return (GetLastPoint() - pointFrom);
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
            _lengthLeft = _maxLength;

            linePoints.Clear();
        }

        public Vector3[] GetPoints()
        {
            List<Vector3> reversed = new List<Vector3>(linePoints);
            reversed.Reverse();
            return linePoints.ToArray();
        }

        public Vector3 GetLastPoint()
        {
            if (linePoints.Count > 0)
                return linePoints[linePoints.Count-1];
            else
                return Vector3.zero;
        }

        public void AppendPoint(Vector3 newPoint)
        {
            if (lengthLeft > minlength)
            {
                linePoints.Add(newPoint);
                RecalculateLength();
            }
        }
        
        private void RecalculateLength()
        {
            float pointsDistance = 0;
            for (int point_n = 0; point_n < (linePoints.Count-1); point_n++)
            {
                pointsDistance += (linePoints[point_n+1] - linePoints[point_n]).magnitude;
            }
            _lengthLeft = _maxLength - pointsDistance;
        }
    }

    public class GrappleMotion : CharMotions.Motion
    {
        GrappleInfo _grapple;
        LineRenderer lineRend;

        public static GrappleMotion Create(GameObject newParent, Rigidbody newCharBody, Collider newCharCollider)
        {
            GrappleMotion motion = newParent.GetComponent<GrappleMotion>();
            if (motion == null)
                motion = newParent.AddComponent<GrappleMotion>();

            motion._charBody = newCharBody;
            motion._charCollider = newCharCollider;
            
            if (motion._charBody.GetComponent<LineRenderer>() == null) 
            {
                motion.lineRend = newParent.AddComponent<LineRenderer>();
            } else
            {
                motion.lineRend = newParent.GetComponent<LineRenderer>();
		        motion.lineRend.startWidth = motion.lineRend.endWidth = 0.15f;
		        motion.lineRend.material = new Material(Shader.Find("Sprites/Default"));
		        motion.lineRend.startColor = motion.lineRend.endColor = Color.black;
            }

            motion._grapple = new GrappleInfo();

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
                _grapple.Reset();
            }
        }

        public override void BeginMotion(Vector3 oldVelocity)
        {
            _velocity = oldVelocity;
            _charBody.useGravity = false;
            _charBody.isKinematic = false;
        }

        public override void EndMotion()
        {
        }

        public override void ProcessMotion()
        {
            ProcessGrapple();

            if (!_grapple.isGrappled)
                return;

            ProcessVelocity();

            ProcessRotation();    
        }

        private void ProcessGrapple()
        {
            // check if grappled object exists
            if (_grapple.grappledObject == null) 
            {
                _grapple.Reset();
                return;
            }

            // Retract or extend the rope
            if (_inputs.forward > 0) 
                _grapple.length = Mathf.MoveTowards(_grapple.length, 1, 30f * Time.deltaTime);
            else if (_inputs.backward > 0) 
                _grapple.length = Mathf.MoveTowards(_grapple.length, 200, -Physics.gravity.y * Time.deltaTime);
                

            // cast ray between Point and character
            RaycastHit hit;
            if (Physics.Linecast(_charBody.position, _grapple.GetLastPoint(), out hit)) 
            {
                if (Vector3.Distance(hit.point, _grapple.GetLastPoint()) > 0.1f)
                    _grapple.AppendPoint(hit.point);

                    lineRend.positionCount = _grapple.PointsCount;
                    lineRend.SetPositions(_grapple.GetPoints());
                    lineRend.positionCount += 1;
            }
            lineRend.SetPosition(lineRend.positionCount-1, this.transform.position);

            for (int point_n = 0; point_n < _grapple.PointsCount-1; point_n++)
            {
                Debug.DrawLine(_grapple.linePoints[point_n], _grapple.linePoints[point_n+1], Color.yellow, Time.deltaTime);    
            }
            Debug.DrawLine(_grapple.GetLastPoint(), this.transform.position, Color.yellow, Time.deltaTime);
        }

        private void ProcessVelocity()
        {
            Quaternion charLookRotation =   Quaternion.AngleAxis(_inputs.mousePositionX, Vector3.up)
                                            * Quaternion.AngleAxis(_inputs.mousePositionY, Vector3.right);
            Vector3 lookDirection = charLookRotation * Vector3.forward * _velocity.sqrMagnitude * 15f;

            // GRAPPLE ROPE PHYSICS Interpolate for 5 steps
            int steps = 1;
            float stepTime = Time.deltaTime/steps;
            Vector3 newPosition = _charBody.transform.position + (_velocity * stepTime);
            for (int cnt = 0; cnt < steps; cnt++)
            {
                Vector3 grappleDirection = _grapple.GetFromTo(newPosition).normalized;
                float grappleRopeLength = _grapple.GetFromTo(newPosition).magnitude;
                float lengthDelta = grappleRopeLength - _grapple.length;

                if (lengthDelta > 0) {
                    float tensionCoefficient = 30000 / lengthDelta;
                    float forceAmount = tensionCoefficient * lengthDelta;
                    float accelerationAmount = Mathf.Clamp(forceAmount / _charBody.mass, 0, 30);
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
            if ( (_contactNormal.sqrMagnitude > 0) && (Vector3.Dot(_contactNormal, _velocity) < 0) ) {
                    _velocity = Vector3.ProjectOnPlane(_velocity, _contactNormal);
                    _velocity -= _velocity * 0.01f;
            }

            //Debug.DrawRay(this.transform.position, _velocity, Color.yellow, Time.deltaTime);

            // APPLY VELOCITY
            _charBody.velocity = _velocity + lookDirection * Time.deltaTime;

            // CLAMP?
            _charBody.velocity = Vector3.ClampMagnitude(_velocity, Physics.gravity.sqrMagnitude * 50);
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


        public void TryGrapple(Quaternion direction)
        {
            RaycastHit rayHit;

            if (Physics.Raycast(_charBody.transform.position, 
                                direction * Vector3.forward,
                                out rayHit, 
                                _grapple.maxLength) )
            {
                _grapple.Set(rayHit);
            } else 
            {
                _grapple.Reset();
            }
        }

        public void ResetGrapple()
        {
            _grapple.Reset();
        }

        public bool isGrappled()
        {
            return _grapple.isGrappled;
        }

    }
}