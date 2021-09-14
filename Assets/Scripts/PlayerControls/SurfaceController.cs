using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CharMotions
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    
    public class SurfaceController : MonoBehaviour
    {

        public GameObject surfaceObject;
	    public Vector3 contactPoint;
	    public Vector3 contactPointNormal;
        public float contactSeparation;
	    public Vector3 contactPointVelocity;
        public Vector3 contactPointRelativeVelocity;
	    public Vector3 angularVelocity;
        public Vector3 fullRotation;
        public Vector3 downhillVector;
        public Quaternion rotationToNormal;
        public Quaternion rotationFromNormal;


        private Collider _charCollider;

        void Start() 
        {
            if (_charCollider == null)
                _charCollider = gameObject.GetComponent<Collider>();
        }

        public void Set(RaycastHit rayHit, Vector3 bodyPosition)
        {
            surfaceObject = rayHit.transform.gameObject;
            contactPoint = rayHit.point;
            contactPointNormal = rayHit.normal;

            Rigidbody surfaceBody = surfaceObject.GetComponent<Rigidbody>();
            if ( surfaceBody != null) {
                contactPointVelocity = surfaceBody.GetPointVelocity(contactPoint);
                contactPointRelativeVelocity = surfaceBody.GetRelativePointVelocity(contactPoint);
                angularVelocity = surfaceBody.angularVelocity;
                fullRotation += angularVelocity;
            }
            downhillVector = Vector3.Cross(contactPointNormal, Vector3.Cross(contactPointNormal, Vector3.up)).normalized;

            rotationToNormal = Quaternion.FromToRotation(Vector3.up, contactPointNormal);
            rotationFromNormal = Quaternion.Inverse(rotationToNormal);

            contactSeparation = rayHit.distance;
        }
        
        public void Reset()
        {
            surfaceObject = null;
            contactPoint = Vector3.zero;
            contactPointNormal = Vector3.zero;
            contactSeparation = Mathf.Infinity;
            contactPointVelocity = Vector3.zero;
            contactPointRelativeVelocity = Vector3.zero;
            angularVelocity = Vector3.zero;
            fullRotation = Vector3.zero;
            downhillVector = Vector3.zero;

            rotationToNormal = Quaternion.identity;
            rotationFromNormal = Quaternion.identity;
        }

        public void UpdateSurface()
        {
            Vector3 rayPosition = this.gameObject.transform.position;
            Vector3 rayDirection = Physics.gravity.normalized;
            float distance = _charCollider.bounds.size.y * 2;
            Transform parent = this.transform;

            Debug.DrawRay(  rayPosition, rayDirection.normalized * distance, Color.red, Time.deltaTime);
            
            RaycastHit surfaceRay;
            if (Physics.Raycast(    rayPosition, rayDirection, out surfaceRay, distance) ) {
                if ( !(surfaceRay.transform.IsChildOf(parent)) )
            	    Set(surfaceRay, this.gameObject.transform.position);
            } else {
            	Reset();
            }
        }
    }
}