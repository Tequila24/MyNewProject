using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CharMotions;


namespace CharMotions
{        
    public class GrappleMotion : Motion
    {
        GrappleHook _grapple;
        public bool isGrappled
        {
            get { return _grapple.isGrappled; }
        }

        // AIRDASH
        [SerializeField]
        static private float dashCooldownDuration = 1.0f;
        static private float dashAccelerationDuration = 0.2f;
        private bool isDashReady = true;
        Vector3 dashMultiplier = Vector3.zero;
        Coroutine dashCoroutine;


        Vector3 strafeVelocity = Vector3.zero;

        public static GrappleMotion Create(GameObject newParent, Rigidbody newCharBody, Collider newCharCollider)
        {
            GrappleMotion motion = newParent.GetComponent<GrappleMotion>();
            if (motion == null)
                motion = newParent.AddComponent<GrappleMotion>();

            motion._charBody = newCharBody;
            motion._charCollider = newCharCollider;

            motion._grapple = new GrappleHook(newParent);

            motion._csControl = GameObject.Find("Canvas").GetComponent<CrosshairController>();

            return motion;
        }

        private void Start() 
        {
            Init();
        }

        private void OnValidate() 
        {
            Init();    
        }

        private void Init()
        {
            _inputs.AddKeyLiftListener(KeyCode.W, this.OnRopeKeysPress);

            _inputs.onInputUpdateEvent.AddListener(this.ProcessCrosshair);

            _inputs.AddKeyPressListener(KeyCode.Space, this.TryGrapple);
            _inputs.AddKeyLiftListener(KeyCode.Space, this._grapple.Reset);

            _inputs.AddKeyDoubleTapListener(KeyCode.A, delegate{this.Dash(Vector3.left);});
            _inputs.AddKeyDoubleTapListener(KeyCode.D, delegate{this.Dash(Vector3.right);});
        }
        
        private void TryGrapple()
        {
            _grapple.TryGrappleFromTo(_charBody.transform.position, _inputs.lookDirection);
        }

        public override void BeginMotion(Vector3 oldVelocity)
        {
            _velocity = oldVelocity;
            _charBody.useGravity = false;
            _charBody.isKinematic = false;
        }

        public override void EndMotion()
        {
            _grapple.Reset();
        }

        private void Dash(Vector3 direction)
        {
            if (this.isActiveAndEnabled)
            {
                if (isDashReady)
                {
                    if (dashCoroutine != null)
                        StopCoroutine(dashCoroutine);
                    dashCoroutine = StartCoroutine(DashCooldown(dashCooldownDuration));
                    StartCoroutine(StartDash(direction));
                }
            }
        }

        private IEnumerator StartDash(Vector3 direction)
        {
            float time = 0;
            while (time < dashAccelerationDuration)
            {
                _velocity += _inputs.lookDirection * direction * 100f * Time.deltaTime;
                time += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator DashCooldown(float time)
        {
            isDashReady = false;
            yield return new WaitForSeconds(time);
            isDashReady = true;
        }

        public override void ProcessMotion()
        {
            if (!_grapple.isGrappled)
                return;

            ProcessGrapple();

            ProcessVelocity();

            ProcessRotation();
        }

        private void ProcessCrosshair()
        {
            RaycastHit aimPointHit;
            bool canReach = _grapple.IsCanReach(_charBody.transform.position, _inputs.lookDirection, out aimPointHit);

            _csControl.SetCrosshairColor(canReach);
            if (canReach)
                _csControl.UpdateCrosshairPosition(aimPointHit.point);
            else
                _csControl.UpdateCrosshairPosition(Vector3.negativeInfinity);
        }

        private void ProcessGrapple()
        {   
            _grapple.UpdateLine(this.transform, _charBody.velocity * Time.deltaTime);
        }

        private void OnRopeKeysPress()
        {
            _grapple.SetNewLength((_grapple.GetLastPoint() - _charBody.transform.position).magnitude);
        }

        private void ProcessVelocity()
        {
            // GRAPPLE ROPE PHYSICS
            Vector3 grappleDirection = _grapple.GetDirectionToParent();
            float distanceToLastPoint = _grapple.GetDistanceToParent();
            float lengthDelta = distanceToLastPoint - _grapple.lengthleft;

            if (lengthDelta > 0) {
                float tensionCoefficient = 50000000 / _grapple.lengthleft;
                float forceAmount = tensionCoefficient * lengthDelta;
                float accelerationAmount = Mathf.Clamp(forceAmount / _charBody.mass, 0, 60);
                Vector3 accelerationVector = grappleDirection * accelerationAmount;
                _velocity += accelerationVector * Time.deltaTime;

                if (Vector3.Dot(grappleDirection, _velocity) < 0) 
                {
                    Vector3 centripetalVelocity = Vector3.Project(_velocity, grappleDirection);
                    _velocity -= centripetalVelocity;
                    Debug.DrawRay(this.transform.position, _velocity, Color.magenta, Time.deltaTime);
                }
            } else
            {
                _grapple.SetNewLength((_grapple.GetLastPoint() - _charBody.transform.position).magnitude);
            }

            // slight strafe on rope
            Vector3 strafeDirection = new Vector3( _inputs.right - _inputs.left, 0, 0);

            // Rope retraction acceleration
            _velocity += grappleDirection * (1.0f) * _inputs.forward;

            // GRAVITYS
            _velocity += Physics.gravity * Time.deltaTime;

            // AIR DRAG
            Vector3 airDragAcceleration = _velocity.normalized * ( 0.002f * ((_velocity.sqrMagnitude)/2)) / _charBody.mass;
            _velocity -= airDragAcceleration * Time.deltaTime;

            // remove part of velocity after hitting something
            if ( (_contactNormal.sqrMagnitude > 0) && (Vector3.Dot(_contactNormal, _velocity) < 0) ) 
            {
                _velocity = Vector3.ProjectOnPlane(_velocity, _contactNormal);
                _velocity -= _velocity * 0.001f;
            }

            // APPLY VELOCITY
            _velocity = Vector3.ClampMagnitude(_velocity, Physics.gravity.sqrMagnitude * 5f);
            _charBody.velocity = _velocity;
        }

        private void ProcessRotation()
        {
            // ROTATION
            Quaternion lookDirection = Quaternion.Euler(0, _inputs.mousePositionX, 0);           // rotation to mouse look
            _charBody.MoveRotation( Quaternion.RotateTowards(   _charBody.transform.rotation,
                                                                lookDirection,
                                                                10.0f ) );
        }

        public override Vector3 GetVelocity()
        {
            return _charBody.velocity;
        }        

    }
}