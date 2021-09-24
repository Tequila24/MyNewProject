using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairController : MonoBehaviour
{
    Color _active_color = Color.red;
    Color _inactive_color = Color.blue;

    [SerializeField]
    Canvas _canvas = null;
    [SerializeField]
    Camera _camera = null;

    [SerializeField]
    Image _image_ver = null;
    [SerializeField]
    Image _image_hor = null;


    void Start()
    {
        //_canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        //_camera = GameObject.Find("Camera").GetComponent<Camera>();

        //_image_ver = _canvas.transform.FindChild .Find("cs_vertical").GetComponent<Image>();
        //_image_ver = GameObject.Find("cs_horizontal").GetComponent<Image>();
    }

    public void UpdateCrosshairPosition(Vector3 worldPosition)
    {
        if (worldPosition.x == Mathf.NegativeInfinity) {
            _image_ver.transform.position = new Vector3(Screen.width * 0.5f,
                                                        Screen.height*0.5f,
                                                        0);
            _image_hor.transform.position = _image_ver.transform.position;
        } else {
            _image_ver.transform.position = _camera.WorldToScreenPoint(worldPosition);
            _image_hor.transform.position = _image_ver.transform.position;
        }
    }

    public void SetCrosshairColor(bool isActive)
    {
        if (isActive)
        {
            _image_ver.color = _active_color;
            _image_hor.color = _active_color;
        } else
        {
            _image_ver.color = _inactive_color;
            _image_hor.color = _inactive_color;
        }
    }
}
