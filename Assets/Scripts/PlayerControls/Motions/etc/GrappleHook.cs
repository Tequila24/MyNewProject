﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CharMotions
{        
    public class GrappleHook
    {
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

        public GameObject _grappledObject;
        public Rigidbody _grappledRigidbody;
        public List<Vector3> _linePoints = new List<Vector3>();
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
                _lineRenderer.positionCount = 1;
            }
        }

        public void Retract()
        {
            // TO DO
            if (_lengthLeft > _minLength)
                _lengthCurrent = Mathf.MoveTowards(_lengthCurrent, _minLength, 50.0f * Time.deltaTime);
        }

        public void Extend()
        {
            // TO DO
            _lengthCurrent = Mathf.MoveTowards(_lengthCurrent, _maxLength, 50.0f * Time.deltaTime);            
        }

        public void Set(RaycastHit rayHit)
        {
            _grappledObject = rayHit.collider.gameObject;
            _lengthCurrent = rayHit.distance;
            _linePoints.Add(rayHit.point);

            _lineRenderer.positionCount += 1;
            _lineRenderer.SetPosition(_lineRenderer.positionCount-2, rayHit.point);

            _isGrappled = true;
        }

        public void Reset()
        {
            _grappledObject = null;
            _grappledRigidbody = null;
            _lengthCurrent = _minLength;
            _lengthWrapped = 0.0f;
            _linePoints.Clear();
            _lineRenderer.positionCount = 1;

            _isGrappled = false;
        }

        public Vector3 GetFromTo(Vector3 pointFrom)
        {
            if (_isGrappled)
                return (_linePoints[_linePoints.Count-1] - pointFrom);
            else
                return Vector3.zero;
        }

        private void RecalculateLength()
        {
            _lengthWrapped = 0;
            for (int point_n = 0; point_n < (_linePoints.Count-1); point_n++)
                _lengthWrapped += (_linePoints[point_n+1] - _linePoints[point_n]).magnitude;
            _lengthLeft = _lengthCurrent - _lengthWrapped;
        }

        public bool IsCanReach(Vector3 fromPosition, Quaternion lookDirection, out RaycastHit hit)
        {
            return Physics.Raycast(fromPosition, lookDirection * Vector3.forward, out hit, _maxLength);
        }

        public bool IsCanReach(Vector3 fromPosition, Quaternion lookDirection)
        {
            return Physics.Raycast(fromPosition, lookDirection * Vector3.forward, _maxLength);
        }

        public void TryGrappleFromTo(Vector3 fromPosition, Quaternion lookDirection)
        {
            RaycastHit rayHit;
            if (IsCanReach(fromPosition, lookDirection, out rayHit))
                Set(rayHit);
            else 
                Reset();
        }


        public void UpdateLine(Vector3 characterPosition)
        {

            // cast ray between last point and character
            RaycastHit hit;
            if (Physics.Linecast(characterPosition, _linePoints[_linePoints.Count-1], out hit)) 
            {
                if (Vector3.Distance(hit.point, _linePoints[_linePoints.Count-1]) > 0.1f) 
                {
                    Vector3 toOriginalPoint = _linePoints[_linePoints.Count-1] - hit.point;
                    Vector3 toChar = characterPosition - hit.point;
                    Vector3 gapVector = /*( (hit.normal * 0.1f) + */((toOriginalPoint + toChar).normalized * 0.02f);
                    Vector3 wrapPoint = hit.point + gapVector;

                    _linePoints.Add(wrapPoint);
                    _lineRenderer.positionCount += 1;
                    _lineRenderer.SetPosition(_lineRenderer.positionCount-2, wrapPoint);

                    Debug.DrawRay(hit.point, toOriginalPoint, Color.yellow, 10);
                    Debug.DrawRay(hit.point, toChar, Color.yellow, 10);
                    Debug.DrawRay(hit.point, gapVector, Color.red, 10);
                }
            }


            // cast ray between point before last and character
            // to unstuck the rope
            if (_linePoints.Count > 1)
            {
                RaycastHit unstuckHit;
                if ( !(Physics.Linecast(characterPosition, _linePoints[_linePoints.Count-2], out unstuckHit)) )
                {
                    _linePoints.RemoveAt(_linePoints.Count-1);
                    _lineRenderer.positionCount -= 1;
                } else 
                {
                    if (Vector3.Distance(unstuckHit.point, _linePoints[_linePoints.Count-2]) < 0.05f)
                    {
                        _linePoints.RemoveAt(_linePoints.Count-1);
                        _lineRenderer.positionCount -= 1;
                    }
                }
            }
            _lineRenderer.SetPosition(_lineRenderer.positionCount-1, characterPosition);

            // DEBUG
            for (int point_n = 0; point_n < _linePoints.Count; point_n++)
            {
                //Debug.DrawLine(_linePoints[point_n], _linePoints[point_n+1], Color.yellow, Time.deltaTime);    
                //Debug.DrawLine(new Vector3(0, 0, 0), _linePoints[point_n], Color.magenta, Time.deltaTime);
            }
            //Debug.DrawLine(_linePoints[_linePoints.Count-1], characterPosition, Color.yellow, Time.deltaTime);

            RecalculateLength();
        }
    }
}
