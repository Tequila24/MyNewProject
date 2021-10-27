using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CharMotions
{
    [RequireComponent(typeof(SurfaceController))]


    public class WalkMotion : CharMotions.Motion
    {
        public const float k_WalkVelocity = 2.0f;
        public const float k_RunVelocity = 5.0f;
        public const float k_FloatHeight = 0.07f;


        private SurfaceController _surfaceControl;

        public bool isGrounded
        {
            get { return _surfaceControl.contactSeparation < k_FloatHeight ? true : false; }
        }

        public static WalkMotion Create(GameObject newParent, Rigidbody newCharBody, Animator newAnimator)
        {
            WalkMotion motion = newParent.GetComponent<WalkMotion>();
            if (motion == null)
                motion = newParent.AddComponent<WalkMotion>();
            
            motion._charBody = newCharBody;

            motion._surfaceControl = newParent.GetComponent<SurfaceController>();
            if (motion._surfaceControl == null)
                motion._surfaceControl = newParent.AddComponent<SurfaceController>();

            motion._animator = newAnimator;

            return motion;
        }

        private void Start() 
        {
            Init();
        }

        private void Init()
        {
        }

        public override void BeginMotion(Vector3 oldVelocity)
        {
            _velocity = _surfaceControl.rotationFromNormal * Vector3.ProjectOnPlane(oldVelocity, _surfaceControl.contactPointNormal);
            _charBody.isKinematic = false;
            _charBody.useGravity = false;
        }

        public override void EndMotion()
        {
            _charBody.isKinematic = false;
        }

        public override void ProcessMotion()
        {
            ProcessVelocity();

            ProcessRotation();
        }

        private void ProcessVelocity()
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
                                            _inputs.forward - _inputs.backward ).normalized * ((_inputs.shift > 0) ? k_RunVelocity : k_WalkVelocity);

                // WALKING ANIMATION
                if (_inputs.shift == 1)
                {
                    _animator.SetBool("isRunning", true);
                    _animator.SetBool("isWalking", true);
                } else if (step.sqrMagnitude > 0 )
                {
                    _animator.SetBool("isWalking", true);
                    _animator.SetBool("isRunning", false);
                } else
                {
                    _animator.SetBool("isWalking", false);
                    _animator.SetBool("isRunning", false);
                }

                // rotate step to follow look direction
                step =  mouseLookDirection * step * 100.0f * Time.deltaTime;
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
            /*Vector3 _heightAdjust = new Vector3(0,
                                                (_surfaceControl.contactPoint.y + floatHeight) - _charBody.transform.position.y, 
                                                0) * 1.0f;*/

            // APPLY VELOCITY
            _charBody.velocity = ( _surfaceControl.rotationToNormal * _velocity );
        }

        private void ProcessRotation()
        {
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