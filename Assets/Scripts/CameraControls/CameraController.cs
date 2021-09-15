using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CameraController : MonoBehaviour
{
    private CharController _player;
    Camera thisCamera;

    private Vector3 offset = new Vector3( 1.5f, 1.5f, -5.0f );
    public Vector3 worldPointLookAt = Vector3.zero;


    void OnValidate()
    {
        Start();
    }
    void Start()
    {
        thisCamera = this.gameObject.GetComponent<Camera>();
        _player = GameObject.Find("Player").GetComponent<CharController>();
    }

    void FixedUpdate()
    {
        Quaternion charLookRotation =  Quaternion.AngleAxis(_player.inputs.mousePositionX, Vector3.up)
                                    * Quaternion.AngleAxis(_player.inputs.mousePositionY, Vector3.right);
        RaycastHit hit;
        if (Physics.Raycast(this.transform.position, charLookRotation * Vector3.forward, out hit))
            worldPointLookAt = Vector3.Lerp(hit.point, _player.transform.forward * 10, 0.1f);
        else
            worldPointLookAt = Vector3.Lerp(worldPointLookAt, _player.transform.position + _player.transform.forward * 10, 0.1f);

        Debug.DrawLine(this.transform.position, worldPointLookAt, Color.red, Time.deltaTime);
        
        Quaternion cameraLookRotation = Quaternion.LookRotation(worldPointLookAt - this.transform.position, Vector3.up);

        this.transform.position = Vector3.Lerp(this.transform.position, _player.transform.position + Quaternion.AngleAxis(_player.inputs.mousePositionX, Vector3.up) * offset, 0.5f);   
        this.transform.rotation = Quaternion.Slerp( this.transform.rotation, charLookRotation, 0.5f);
    }

    void OnGUI()
    {
        //Vector3 viewportPoint = thisCamera.WorldToViewportPoint( _player.lookPoint );
    }
}
