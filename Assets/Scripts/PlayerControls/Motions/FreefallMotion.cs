using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CharMotions
{
    public class FreefallMotion : CharMotions.Motion
    {
        public static FreefallMotion Create(GameObject parent, Rigidbody charBody, Collider charCollider)
        {
            FreefallMotion motion = parent.GetComponent<FreefallMotion>();
            if (motion == null)
                motion = parent.AddComponent<FreefallMotion>();

            motion._charBody = charBody;
            motion._charCollider = charCollider;
                
            return motion;
        }

        public override void BeginMotion(Vector3 oldVelocity)
        {
            _velocity = oldVelocity;
            _charBody.isKinematic = false;
            _charBody.useGravity = false;
        }

        public override void ProcessMotion()
        {
            Quaternion mouseLookDirection = Quaternion.AngleAxis(_inputs.mousePositionX, Vector3.up);

            // create step based on inputs
            Vector3 step = new Vector3( _inputs.right - _inputs.left,
                                        0,
                                        _inputs.forward - _inputs.backward ).normalized * ((_inputs.shift > 0) ? 10f : 7f);

            // rotate step to follow look direction
            step =  mouseLookDirection * step;

            _velocity.x = Mathf.MoveTowards(_velocity.x, step.x, 0.035f );
            _velocity.y = Mathf.MoveTowards(_velocity.y, Physics.gravity.y * 3, 0.2f );
            _velocity.z = Mathf.MoveTowards(_velocity.z, step.z, 0.035f );

            _charBody.velocity = _velocity;
            
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