using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeScript : MonoBehaviour
{

	[SerializeField]
	private Vector3 firstPoint = Vector3.positiveInfinity;
	//private GameObject firstObject = null;
	[SerializeField]
	private Rigidbody firstBody = null;
	[SerializeField]
	private Vector3 secondPoint = Vector3.positiveInfinity;
	//private GameObject secondObject = null;
	[SerializeField]
	private Rigidbody secondBody = null;

	private int numberOfRigids = 2;
	[SerializeField]
	[Range(1.0f, 10.0f)]
	private float maxDistanceBetween = 10.0f;
	[SerializeField]
	private float Young_Modulus = 35000000000.0f;
	[SerializeField]
	private float Rope_area = 0.001f;

	private LineRenderer lineRender = null;

	void Start()
	{
		lineRender = this.gameObject.AddComponent<LineRenderer>();
		lineRender.positionCount = 2;
		lineRender.startWidth = lineRender.endWidth = 0.15f;
		lineRender.material = new Material(Shader.Find("Sprites/Default"));
		lineRender.startColor = lineRender.endColor = Color.black;
	}

	void SetPoint(Vector3 newPoint, GameObject newObject)
	{
		if (firstPoint.magnitude == Mathf.Infinity) {
			firstPoint = newPoint;
			firstBody = newObject.GetComponent<Rigidbody>();
			if (firstBody != null)
				numberOfRigids++;
			
			return;
		}
		if (secondPoint.magnitude == Mathf.Infinity) {
			secondPoint = newPoint;
			secondBody = newObject.GetComponent<Rigidbody>();
			if (secondBody != null)
				numberOfRigids++;

			maxDistanceBetween = (secondBody.transform.position - firstBody.transform.position).magnitude;
			return;
		}
	}

	void FixedUpdate() 
	{
		// yay physics
		if (!ArePointsSet())
			return;

		Vector3 firstContactPointWorldPosition = firstBody.transform.position + firstBody.transform.rotation * firstPoint;
		Vector3 secondContactPointWorldPosition = secondBody.transform.position + secondBody.transform.rotation * secondPoint;

		float distanceBetween = (firstContactPointWorldPosition - secondContactPointWorldPosition).magnitude;

		if (distanceBetween > maxDistanceBetween) { 
			/*if (distanceBetween > (maxDistanceBetween*1.3f)) {
				Debug.Log("SNAP!");
				Destroy(this.gameObject);
				return;
			}*/

			if (numberOfRigids == 2)	{

				// process physics for both rigids
		
				// Hook's Law
				// 
				float k_coeff = (Young_Modulus * Rope_area) / maxDistanceBetween;
				float delta_length = distanceBetween - maxDistanceBetween;
				float PullForceAmount = k_coeff * delta_length;

				Vector3 fromFirstToSecond = (secondContactPointWorldPosition - firstContactPointWorldPosition).normalized;

				firstBody.AddForceAtPosition(fromFirstToSecond * (PullForceAmount * 0.5f) * Time.deltaTime, firstContactPointWorldPosition, ForceMode.Force);
				secondBody.AddForceAtPosition(-fromFirstToSecond * (PullForceAmount * 0.5f) * Time.deltaTime, secondContactPointWorldPosition, ForceMode.Force);			


				// damping force
				Vector3 dampingVelocity;
				float Angle;

				dampingVelocity = Vector3.Project(firstBody.GetPointVelocity(firstContactPointWorldPosition), fromFirstToSecond);
				Angle = Vector3.Angle(dampingVelocity, fromFirstToSecond);

				if (Angle > 90) {
					Debug.DrawLine(firstContactPointWorldPosition, firstContactPointWorldPosition + dampingVelocity, Color.red);
					firstBody.AddForceAtPosition(-dampingVelocity * 0.9f * Time.deltaTime, firstContactPointWorldPosition, ForceMode.VelocityChange);
				}

				dampingVelocity = Vector3.Project(firstBody.GetPointVelocity(secondContactPointWorldPosition), -fromFirstToSecond);
				Angle = Vector3.Angle(dampingVelocity, -fromFirstToSecond);
				if (Angle > 90) {
					Debug.DrawLine(secondContactPointWorldPosition, secondContactPointWorldPosition + dampingVelocity, Color.red);
					secondBody.AddForceAtPosition(-dampingVelocity * 0.9f * Time.deltaTime, secondContactPointWorldPosition, ForceMode.VelocityChange);
				}

			} else

			if (numberOfRigids == 1)	{
				
				// process physics for single rigid
				
			} else

			if (numberOfRigids == 0) {
				return;
			}
		}
	}

	void Update()
	{
		lineRender.SetPosition(0, firstBody.transform.position + firstBody.transform.rotation * firstPoint);
		lineRender.SetPosition(1, secondBody.transform.position + secondBody.transform.rotation * secondPoint);
	}

	private bool ArePointsSet() {
		if (	(firstPoint.magnitude == Mathf.Infinity) ||
				(secondPoint.magnitude == Mathf.Infinity)	) 
		{
			return false;
		} else {
			return true;
		}
	}


}
