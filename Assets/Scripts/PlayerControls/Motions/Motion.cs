using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CharMotions
{
    abstract public class Motion : MonoBehaviour
    {
        [SerializeField]
        protected InputMaster _inputs;
        protected Animator _animator;

        //[SerializeField]
        protected Vector3 _velocity = Vector3.zero;

        protected Rigidbody _charBody = null;
        protected GameObject _contactObject = null;
        protected Vector3 _contactNormal;

        abstract public void BeginMotion(Vector3 oldVelocity);
        abstract public void EndMotion();

        abstract public void ProcessMotion();

        abstract public Vector3 GetVelocity();

        private void Awake() 
        {
            _inputs = InputMaster.Instance;
        }

        private void LateUpdate()
        {
            if (_contactObject == null)
                _contactNormal = Vector3.zero;
        }

        void OnCollisionEnter(Collision hit)
        {
            _contactObject = hit.gameObject;
            for (int i = 0; i < hit.contactCount; i++)
            {
                _contactNormal += hit.contacts[i].normal;
            }
            _contactNormal.Normalize();
        }

        void OnCollisionStay(Collision hit)
        {
            _contactObject = hit.gameObject;
            for (int i = 0; i < hit.contactCount; i++)
            {
                _contactNormal += hit.contacts[i].normal;
            }
            _contactNormal.Normalize();
        }

        void OnCollisionExit(Collision hit)
        {
            _contactObject = null;
            _contactNormal = Vector3.zero;
        }
    }

}