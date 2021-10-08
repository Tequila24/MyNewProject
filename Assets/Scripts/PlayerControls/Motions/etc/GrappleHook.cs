using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CharMotions
{        
    public class GrappleHook
    {
        private struct LinePoint
        {
            Transform transform;
            Vector3 localPoint;

            public LinePoint(RaycastHit hit)
            {
                transform = hit.transform;
                localPoint = transform.InverseTransformPoint(hit.point);
            }

            public LinePoint(Vector3 worldPoint, Transform newTransform)
            {
                transform = newTransform;
                localPoint = transform.InverseTransformPoint(worldPoint);
            }

            public Vector3 GetWorldPoint()
            {
                if (Exists())
                    return transform.TransformPoint(localPoint);
                else
                    return Vector3.zero;
            }

            public bool Exists()
            {
                return (transform == null) ? false : true;
            }
        }

        private float _maxLength = 200.0f;
        private float _minLength = 1.0f;

        private float _lengthCurrent;
        private float _lengthWrapped;

        private float _lengthLeft;
        public float lengthleft
        {
            get { return _lengthLeft; }
        }


        private bool _isGrappled;
        public bool isGrappled
        {
            get { return _isGrappled ;}
        }

        private int _winchDirection;
        public int winchDirection { get { return _winchDirection; } }

        private List<LinePoint> _linePoints = new List<LinePoint>();
        private LineRenderer _lineRenderer;


        public GrappleHook(GameObject parentObject)
        {
            if (parentObject.GetComponent<LineRenderer>() == null) 
            {
                _lineRenderer = parentObject.AddComponent<LineRenderer>();
            } else
            {
                _lineRenderer = parentObject.GetComponent<LineRenderer>();
		        _lineRenderer.startWidth = _lineRenderer.endWidth = 0.15f;
		        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
		        _lineRenderer.startColor = _lineRenderer.endColor = Color.black;
            }
        }

        public void SetNewLength(float newLength)
        {
            _lengthCurrent = newLength + _lengthWrapped;
        }

        public void Reset()
        {
            _lengthCurrent = _minLength;
            _lengthWrapped = 0.0f;
            _linePoints.Clear();
            _lineRenderer.positionCount = 0;

            _isGrappled = false;
        }

        public Vector3 GetLastPoint()
        {
            if (_isGrappled)
                return (_linePoints[_linePoints.Count-1]).GetWorldPoint();
            else
                return Vector3.zero;
        }

        private void RecalculateLength()
        {
            _lengthWrapped = 0;
            for (int point_n = 0; point_n < (_linePoints.Count-1); point_n++)
                _lengthWrapped += (_linePoints[point_n+1].GetWorldPoint() - _linePoints[point_n].GetWorldPoint()).magnitude;
            _lengthLeft = _lengthCurrent - _lengthWrapped;
        }

        public bool IsCanReach(Vector3 fromPosition, Quaternion lookDirection, out RaycastHit hit)
        {
            return Physics.Raycast(fromPosition, lookDirection * Vector3.forward, out hit, _maxLength, 1, 0);
        }

        public bool IsCanReach(Vector3 fromPosition, Quaternion lookDirection)
        {
            return Physics.Raycast(fromPosition, lookDirection * Vector3.forward, _maxLength, 1, 0);
        }

        public void TryGrappleFromTo(Vector3 fromPosition, Quaternion lookDirection)
        {
            RaycastHit rayHit; 
            if (IsCanReach(fromPosition, lookDirection, out rayHit)) 
            {
                _lengthCurrent = rayHit.distance;
                _linePoints.Add(new LinePoint(rayHit));

                _isGrappled = true;
            }
        }


        public void UpdateLine(Transform charTransform, Vector3 charVelocity)
        {
            //check if wrapped objects exist
            for (int point_n = 0; point_n < _linePoints.Count; point_n++)
            {
                if (!_linePoints[point_n].Exists())
                    _linePoints.RemoveAt(point_n);
            }
            if (_linePoints.Count == 0)
            {
                this.Reset();
                return;
            }

            // cast ray between last point and character
            RaycastHit hit;
            if (Physics.Linecast(charTransform.position, _linePoints[_linePoints.Count-1].GetWorldPoint(), out hit)) 
                if (hit.transform != charTransform)
                    if (Vector3.Distance(hit.point, _linePoints[_linePoints.Count-1].GetWorldPoint()) > 0.1f) 
                        _linePoints.Add(new LinePoint(hit.point, hit.transform));


            // to unstuck the rope
            // check if rope point can move to new position
            if (_linePoints.Count > 1)
            {
                /*Vector3 directionToPrevious = (_linePoints[_linePoints.Count-2].GetWorldPoint() - charTransform.position).normalized;
                float distanceToLastPoint = Vector3.Distance(charTransform.position, _linePoints[_linePoints.Count-1].GetWorldPoint());
                Vector3 newRopePosition = charTransform.position + directionToPrevious * distanceToLastPoint;*/
                Quaternion fromLastToPrevious = Quaternion.FromToRotation( (_linePoints[_linePoints.Count-1].GetWorldPoint() - charTransform.position),
                                                                               (_linePoints[_linePoints.Count-2].GetWorldPoint() - charTransform.position) );
                Vector3 newRopePosition = charTransform.position + fromLastToPrevious * (_linePoints[_linePoints.Count-1].GetWorldPoint() - charTransform.position);
                Vector3 checkGap = (_linePoints[_linePoints.Count-1].GetWorldPoint() - newRopePosition).normalized * 0.1f;

                bool isObstructed = Physics.Linecast( _linePoints[_linePoints.Count-1].GetWorldPoint() + checkGap,
                                                      newRopePosition);
                if ( isObstructed )
                {
                    _linePoints.RemoveAt(_linePoints.Count -1);
                    //Debug.Break();
                }
                Debug.Log(isObstructed);
                Debug.DrawLine( _linePoints[_linePoints.Count-1].GetWorldPoint() + checkGap, 
                                newRopePosition,
                                Color.white,
                                Time.deltaTime );

                //Debug.DrawLine( _linePoints[_linePoints.Count-1].GetWorldPoint(), charTransform.position, Color.blue, Time.deltaTime);
            }


            /*
            if (_linePoints.Count > 1)
            {
                RaycastHit unstuckHit;
                if ( !(Physics.Linecast(charTransform.position, _linePoints[_linePoints.Count-2].GetWorldPoint(), out unstuckHit, 1, 0)) )
                {
                    Quaternion fromLastToPrevious = Quaternion.FromToRotation( (_linePoints[_linePoints.Count-1].GetWorldPoint() - charTransform.position),
                                                                               (_linePoints[_linePoints.Count-2].GetWorldPoint() - charTransform.position) );
                    Vector3 unstuckCheckPoint = charTransform.position + fromLastToPrevious * (_linePoints[_linePoints.Count-1].GetWorldPoint() - charTransform.position);
                    Vector3 unstuckGap = (_linePoints[_linePoints.Count-1].GetWorldPoint() - unstuckCheckPoint).normalized * 0.05f;
                    if ( !Physics.Linecast(_linePoints[_linePoints.Count-1].GetWorldPoint() + unstuckGap, unstuckCheckPoint) )
                    {
                        _linePoints.RemoveAt(_linePoints.Count-1);
                    }
                    Debug.DrawLine( _linePoints[_linePoints.Count-1].GetWorldPoint() + unstuckGap, 
                                        unstuckCheckPoint,//(charTransform.position + charVelocity + _linePoints[_linePoints.Count-2].GetWorldPoint())/2,
                                        Color.white,
                                        Time.deltaTime );
                    //Debug.DrawRay( _linePoints[_linePoints.Count-1].GetWorldPoint(), unstuckGap, Color.red, Time.deltaTime);
                    Debug.DrawLine( _linePoints[_linePoints.Count-1].GetWorldPoint(), charTransform.position, Color.blue, Time.deltaTime);
                    //Debug.Break();
                }
            }
            */

            _lineRenderer.positionCount = 1;
            for (int point_n = 0; point_n < _linePoints.Count; point_n++) 
            {
                _lineRenderer.SetPosition(point_n, _linePoints[point_n].GetWorldPoint());
                _lineRenderer.positionCount++;
            }
            _lineRenderer.SetPosition(_lineRenderer.positionCount-1, charTransform.position + charVelocity);



            // DEBUG
            /*
            for (int point_n = 0; point_n < _linePoints.Count; point_n++)
            {
                Debug.DrawLine(_linePoints[point_n], _linePoints[point_n+1], Color.yellow, Time.deltaTime);    
                Debug.DrawLine(new Vector3(0, 0, 0), _linePoints[point_n], Color.magenta, Time.deltaTime);
            }
            Debug.DrawLine(_linePoints[_linePoints.Count-1], characterPosition, Color.yellow, Time.deltaTime);
            */

            RecalculateLength();
        }

        private static bool VectorIntersection(out Vector3 intersectionPoint, Vector3 line1Pos, Vector3 line1Dir, Vector3 line2Pos, Vector2 line2Dir)
        {
            Vector3 line2to1Dir = line2Pos - line1Pos;
            Vector3 crossVec1and2 = Vector3.Cross(line1Dir, line2Dir);
            Vector3 crossVec3and2 = Vector3.Cross(line2to1Dir, line2Dir);

            float planarFactor = Vector3.Dot(line2to1Dir, crossVec1and2);

            if( Mathf.Abs(planarFactor) < 0.0001f && crossVec1and2.sqrMagnitude > 0.0001f)
            {
                float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
                intersectionPoint = line1Pos + (line1Dir * s);
                return true;
            }
            else
            {
                intersectionPoint = Vector3.zero;
                return false;
            }
        }

    }
}
