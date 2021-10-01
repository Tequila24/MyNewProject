using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CharMotions
{
    public class WalkMotion : CharMotions.Motion
    {
        public static float stairHeight = 0.25f;
        private static SurfaceController _surfaceControl;
        private Vector3 _heightAdjust;
        private Vector3 _dashVelocity;

        public static WalkMotion Create(GameObject newParent, Rigidbody newCharBody, Collider newCharCollider, SurfaceController newSurfaceControl)
        {

            WalkMotion motion = newParent.GetComponent<WalkMotion>();
            if (motion == null)
                motion = newParent.AddComponent<WalkMotion>();
            
            motion._charBody = newCharBody;
            motion._charCollider = newCharCollider;

            _surfaceControl = newSurfaceControl;

            return motion;
        }

        public override void BeginMotion(Vector3 oldVelocity)
        {
            _velocity = _surfaceControl.rotationFromNormal * Vector3.ProjectOnPlane(oldVelocity, _surfaceControl.contactPointNormal);
            _charBody.isKinematic = false;
            _charBody.useGravity = false;
        }

        private void Start() 
        {
            Init();
        }

        private void Init()
        {
            _inputs.AddKeyDoubleTapListener(KeyCode.W, this.Dash);
        }

        private void Dash()
        {
            _velocity += Quaternion.Euler(0, _inputs.mousePositionX, 0) * (Vector3.forward + Vector3.up * 0.3f) * 30f;
        }

        public override void EndMotion()
        {
            _charBody.isKinematic = false;
        }

        public override void ProcessMotion()
        {
            if (_velocity.sqrMagnitude < 0.01f)
                _velocity = Vector3.zero;
            _charBody.angularVelocity = Vector3.zero;
            

            // VELOCITY
            // Consider _velocity vector as flat-plane vector, which stays that way all the time /
            // and rotates to surface only when applied to Rigidbody
            //
            float currentSurfaceIncline = Vector3.Angle(Vector3.up, _surfaceControl.contactPointNormal);

            // if surface is too steep
            if (currentSurfaceIncline < 30) 
            {
                Quaternion mouseLookDirection = Quaternion.AngleAxis(_inputs.mousePositionX, Vector3.up);

                // create step based on inputs
                Vector3 step = new Vector3( _inputs.right - _inputs.left,
                                            0,
                                            _inputs.forward - _inputs.backward ).normalized * ((_inputs.shift > 0) ? 10f : 7f);
                // rotate step to follow look direction
                step =  mouseLookDirection * step;
                //
                _velocity = Vector3.Lerp(_velocity, step, 0.2f);

                // if sliding along the wall
                if ( (_contactNormal.sqrMagnitude > 0) && (Vector3.Dot(_contactNormal, _velocity) < 0) )
                    _velocity = Vector3.ProjectOnPlane(_velocity, _contactNormal);

            } else 
            {
                _velocity = Vector3.Lerp(_velocity, _surfaceControl.rotationFromNormal * _surfaceControl.downhillVector * 10f, 0.1f);
            }

            // Adjust char position a little bit above surface
            Vector3 _heightAdjust = new Vector3(0,
                                                (_surfaceControl.contactPoint.y + _charCollider.bounds.extents.y * 1.2f) - _charBody.transform.position.y, 
                                                0) * 1.0f;

            // APPLY VELOCITY
            _charBody.velocity = ( _heightAdjust
                                   + _surfaceControl.rotationToNormal * _velocity);


            // ROTATION
            if (_velocity.sqrMagnitude != 0)
                _charBody.transform.rotation = Quaternion.RotateTowards(_charBody.transform.rotation, 
                                                                        Quaternion.LookRotation(_velocity, Vector3.up), 
                                                                        10f);
            else
                _charBody.transform.rotation = Quaternion.RotateTowards(_charBody.transform.rotation,
                                                                        Quaternion.LookRotation(Vector3.ProjectOnPlane( this.transform.forward,
                                                                                                                        Vector3.up),
                                                                                                Vector3.up), 
                                                                        10f);
        }

        public override Vector3 GetVelocity()
        {
            return (_surfaceControl.rotationToNormal * _velocity);
        }

    }
}