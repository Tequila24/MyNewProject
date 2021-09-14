using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CharMotions
{
    public class SlideMotion : CharMotions.Motion
    {
        public static float stairHeight = 0.25f;
        private static SurfaceController _surfaceControl;
        private Vector3 _heightAdjust;

        public static SlideMotion Create(GameObject newParent, Rigidbody newCharBody, Collider newCharCollider, SurfaceController newSurfaceControl)
        {
            SlideMotion motion = newParent.GetComponent<SlideMotion>();
            if (motion == null)
                motion = newParent.AddComponent<SlideMotion>();
            
            motion._charBody = newCharBody;
            motion._charCollider = newCharCollider;

            _surfaceControl = newSurfaceControl;

            return motion;
        }

        public override void BeginMotion(Vector3 oldVelocity)
        {
            _velocity = _surfaceControl.rotationFromNormal * Vector3.ProjectOnPlane(oldVelocity, _surfaceControl.contactPointNormal);
            _charBody.isKinematic = true;
        }

        public override void EndMotion()
        {
            _charBody.isKinematic = false;
        }

        public override void ProcessMotion()
        {
            // Consider _velocity vector as flat-plane vector, which stays that way all the time /
            // and rotates to surface only when applied to Rigidbody
            //
            _velocity = Vector3.Lerp(_velocity, _surfaceControl.downhillVector * 10f, 0.1f);            

            Vector3 _heightAdjust = new Vector3(0, (_surfaceControl.contactPoint.y + _charCollider.bounds.extents.y * 1.1f) - _charBody.transform.position.y, 0) * 0.2f;

            _charBody.MovePosition( _charBody.transform.position
                                    + _heightAdjust
                                    + _surfaceControl.rotationToNormal * _velocity * Time.deltaTime);
        }

        public override Vector3 GetVelocity()
        {
            return (_surfaceControl.rotationToNormal * _velocity);
        }
    }
}