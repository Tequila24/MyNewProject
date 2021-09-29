using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CharMotions
{
    abstract public class Motion : MonoBehaviour
    {
        protected InputMaster _inputs;

        [SerializeField]
        protected Vector3 _velocity = Vector3.zero;

        protected Rigidbody _charBody = null;
        protected Collider _charCollider = null;
        protected Vector3 _contactNormal = Vector3.zero;

        protected CrosshairController _csControl = null;

        abstract public void BeginMotion(Vector3 oldVelocity);
        abstract public void EndMotion();

        abstract public void ProcessMotion();

        abstract public Vector3 GetVelocity();

        private void Awake() {
            _inputs = InputMaster.Instance;
        }

        protected Vector3 GetDepenetration(Vector3 deltaTransform = new Vector3(), Quaternion deltaRotation = new Quaternion() )
        {
            Vector3 summDepenetrationVector = Vector3.zero;

            Collider[] hits = new Collider[10];
            int hitsAmount = Physics.OverlapBoxNonAlloc(  _charCollider.transform.position + deltaTransform, _charCollider.bounds.extents, 
                                                            hits, _charCollider.transform.rotation * deltaRotation);

            if ( hitsAmount > 0 )
            {
                for (int i = 0; i < hitsAmount; i++)
                {
                    Collider hit = hits[i];
                    if (hit.gameObject == _charCollider.gameObject)
                        continue;

                    Vector3 depenetrationDirection;
                    float depenetrationDistance;
                    Physics.ComputePenetration( _charCollider, _charCollider.transform.position + deltaTransform, _charCollider.transform.rotation * deltaRotation,
                                                hit, hit.transform.position, hit.transform.rotation,
                                                out depenetrationDirection, out depenetrationDistance);
                    summDepenetrationVector += depenetrationDirection * depenetrationDistance;
                }
            }

            return summDepenetrationVector;
        }

        protected float GetDistanceToCollision(Vector3 _velocity)
        {
            RaycastHit hit;
            Physics.Raycast(_charCollider.transform.position, _velocity, out hit);
            return hit.distance;
        }

        void OnCollisionEnter(Collision hit)
        {
            
            for (int i = 0; i < hit.contactCount; i++)
            {
                _contactNormal += hit.contacts[i].normal;    
            }
            _contactNormal.Normalize();

            /*if (_contactNormal.sqrMagnitude != 0) 
                        if (Vector3.Angle(_velocity, _contactNormal) > 90)
                            _velocity = Vector3.ProjectOnPlane(_velocity, _contactNormal); 
            */
            //Debug.Log("HIT " + hit.gameObject.name);
        }

        void OnCollisionStay(Collision hit)
        {
            
            for (int i = 0; i < hit.contactCount; i++)
            {
                _contactNormal += hit.contacts[i].normal;    
            }
            _contactNormal.Normalize();

            /*if (_contactNormal.sqrMagnitude != 0) 
                if (Vector3.Angle(_velocity, _contactNormal) > 90)
                    _velocity = Vector3.ProjectOnPlane(_velocity, _contactNormal); 
            */
        }

        void OnCollisionExit(Collision hit)
        {
            _contactNormal = Vector3.zero;
            //Debug.Log("UNHIT " + hit.gameObject.name);
        }
    }

}