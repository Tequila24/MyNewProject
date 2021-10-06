using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CharMotions
{
    public class FreefallMotion : CharMotions.Motion
    {
        float dashTimeout = 0;

        public static FreefallMotion Create(GameObject parent, Rigidbody charBody, Collider charCollider)
        {
            FreefallMotion motion = parent.GetComponent<FreefallMotion>();
            if (motion == null)
                motion = parent.AddComponent<FreefallMotion>();

            motion._charBody = charBody;
            motion._charCollider = charCollider;
                
            return motion;
        }

        private void Start() 
        {
            Init();    
        }

        private void Init()
        {
            _inputs.AddKeyDoubleTapListener(KeyCode.W, delegate{this.Dash(Vector3.forward);});
            _inputs.AddKeyDoubleTapListener(KeyCode.A, delegate{this.Dash(Vector3.left);});
            _inputs.AddKeyDoubleTapListener(KeyCode.D, delegate{this.Dash(Vector3.right);});
            _inputs.AddKeyDoubleTapListener(KeyCode.S, delegate{this.Dash(Vector3.back);});
        }

        private void Dash(Vector3 direction)
        {
            if (dashTimeout<=0)
            {
                Debug.Log("DASHING FLYING " + dashTimeout);
                _velocity +=Quaternion.Euler(0, _inputs.mousePositionX, 0) * direction * 30f;
                StartCoroutine(DashTimeout(2));
            }
        }

        private IEnumerator DashTimeout(float timeout)
        {
            while(dashTimeout>0)
            {
                dashTimeout -= Time.deltaTime;
            }
            yield return null;
        }

        public override void BeginMotion(Vector3 oldVelocity)
        {
            _velocity = oldVelocity;
            _charBody.isKinematic = false;
            _charBody.useGravity = false;
        }

        public override void EndMotion()
        {
        }

        public override void ProcessMotion()
        {
            ProcessVelocity();

            ProcessRotation();
        }

        private void ProcessVelocity()
        {
            Quaternion yawLookDirection = Quaternion.AngleAxis(_inputs.mousePositionX, Vector3.up);

            // create step based on inputs
            Vector3 step = new Vector3( _inputs.right - _inputs.left,
                                        0,
                                        _inputs.forward - _inputs.backward ).normalized * ((_inputs.shift > 0) ? 10f : 7f);
            Vector3 freeFallAcceleration = Physics.gravity;
            Vector3 stepAcceleration = yawLookDirection * step;

            _velocity += (freeFallAcceleration + stepAcceleration) * Time.deltaTime;

            // remove part of velocity after hitting something
            if ( (_contactNormal.sqrMagnitude > 0) && (Vector3.Dot(_contactNormal, _velocity) < 0) )
                    _velocity = Vector3.ProjectOnPlane(_velocity, _contactNormal);


            // AIR DRAG
            Vector3 airDragAcceleration = _velocity.normalized * ( 0.002f * ((_velocity.sqrMagnitude)/2)) / _charBody.mass;
            _velocity -= airDragAcceleration * Time.deltaTime;


            // APPLY VELOCITY
            _charBody.velocity = Vector3.ClampMagnitude(_velocity, Physics.gravity.sqrMagnitude * 10);
        }

        private void ProcessRotation()
        {
            _charBody.angularVelocity = Vector3.MoveTowards(_charBody.angularVelocity, Vector3.zero, 0.001f);

            Quaternion lookDirection = Quaternion.Euler(0, _inputs.mousePositionX, 0);
            _charBody.rotation = Quaternion.RotateTowards(  _charBody.transform.rotation,
                                                            lookDirection,
                                                            2.0f );
        }


        public override Vector3 GetVelocity() 
        {
            return _charBody.velocity;
        }

    }
}