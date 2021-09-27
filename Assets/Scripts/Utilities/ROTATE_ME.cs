using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ROTATE_ME : MonoBehaviour
{
    [SerializeField]
    float _rotation_velocity = 0.0f;

    void FixedUpdate()
    {
        Quaternion additionalRotation = Quaternion.AngleAxis(_rotation_velocity * Time.deltaTime, Vector3.up);
        this.transform.rotation = additionalRotation * this.transform.rotation;
    }
}
