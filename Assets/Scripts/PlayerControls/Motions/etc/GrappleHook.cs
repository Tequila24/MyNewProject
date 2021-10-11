﻿using System.Collections;
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
        [SerializeField]
        private float ropeThickness = 0.1f;

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
                     {
                        RaycastHit reverseHit;
                        if (Physics.Linecast(_linePoints[_linePoints.Count-1].GetWorldPoint(), charTransform.position, out reverseHit))
                        {
                            _linePoints.Add(new LinePoint(hit.point + (hit.normal + reverseHit.normal)/2 * ropeThickness, hit.transform));
                        }
                        else
                        {
                            _linePoints.Add(new LinePoint(hit.point + hit.normal * ropeThickness, hit.transform));
                        }
                     }


            // to unstuck the rope
            // check if rope point can move to new position
            if (_linePoints.Count > 1)
            {
                Quaternion fromLastToPrevious = Quaternion.FromToRotation( (_linePoints[_linePoints.Count-1].GetWorldPoint() - charTransform.position),
                                                                               (_linePoints[_linePoints.Count-2].GetWorldPoint() - charTransform.position) );
                Vector3 newRopePosition = charTransform.position + fromLastToPrevious * (_linePoints[_linePoints.Count-1].GetWorldPoint() - charTransform.position);

                RaycastHit unstuckHit;
                bool isPreviousVisible = !(Physics.Linecast(charTransform.position, _linePoints[_linePoints.Count-2].GetWorldPoint()));
                bool isWayObstructed = Physics.Linecast(newRopePosition, _linePoints[_linePoints.Count-1].GetWorldPoint(), out unstuckHit);

                if ( (!isWayObstructed && isPreviousVisible) || ( isPreviousVisible && (unstuckHit.point - _linePoints[_linePoints.Count-1].GetWorldPoint()).sqrMagnitude < 0.1f) )
                {
                    _linePoints.RemoveAt(_linePoints.Count -1);
                }
            }

            // DRAW LINE
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
    }
}
