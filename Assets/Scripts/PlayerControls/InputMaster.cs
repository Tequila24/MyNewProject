using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Key
{
    private UnityEvent _onPressEvent;
    public UnityEvent pressEvent { get { return _onPressEvent; } }
    UnityEvent _onLiftEvent;
    public UnityEvent liftEvent { get { return _onLiftEvent; } }


    private int _state;
    public int state { get { return _state; } }

    public Key()
    {
        _onPressEvent = new UnityEvent();
        _onLiftEvent = new UnityEvent();
    }


    public void Update(KeyCode code)
    {
        if (Input.GetKeyDown(code))
            _onPressEvent.Invoke();

        if (Input.GetKeyUp(code))
            _onLiftEvent.Invoke();

        _state = Input.GetKey(code) ? 1 : 0;
    }
}




public class InputMaster : MonoBehaviour
{
    public static InputMaster _instance;
    public static InputMaster Instance { get { return _instance; } }

    private Dictionary<KeyCode, Key> _keys = new Dictionary<KeyCode, Key>();

    public float mouseDeltaX;
    public float mouseDeltaY;

    public float mousePositionX;
    public float mousePositionY;

    public Quaternion lookDirection;

    private UnityEvent _onInputUpdateEvent;
    public UnityEvent onInputUpdateEvent { get { return _onInputUpdateEvent; } }

    // keys shortcuts
    // ==========
    public int forward { get { return _keys[KeyCode.W].state; } }
    public int backward { get { return _keys[KeyCode.S].state; } }
    public int left { get { return _keys[KeyCode.A].state; } }
    public int right { get { return _keys[KeyCode.D].state; } }

    public int shift { get { return _keys[KeyCode.LeftShift].state; } }
    // ==========


    private void OnValidate() 
    {
        Init();
    }

    private void Awake()
    {
        if(_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        } else {
            _instance = this;
        }

        Init();
    }

    private void Init()
    {
        if (_keys.Count != 0)
            return;

        _keys.Add(KeyCode.W, new Key());
        _keys.Add(KeyCode.S, new Key());
        _keys.Add(KeyCode.A, new Key());
        _keys.Add(KeyCode.D, new Key());

        _keys.Add(KeyCode.Space, new Key());
        _keys.Add(KeyCode.LeftShift, new Key());

        _keys.Add(KeyCode.Mouse0, new Key());
        _keys.Add(KeyCode.Mouse1, new Key());

        _onInputUpdateEvent = new UnityEvent();
    }

    private void Update()
    {
        foreach(KeyCode code in _keys.Keys)
        {
            _keys[code].Update(code);
        }

        mouseDeltaX = Input.GetAxis("Mouse X");
        mouseDeltaY = -Input.GetAxis("Mouse Y");

        mousePositionX += mouseDeltaX;
        mousePositionY = Mathf.Clamp(mousePositionY + mouseDeltaY, -90, 90);
        
        lookDirection =    Quaternion.AngleAxis(mousePositionX, Vector3.up)
                           * Quaternion.AngleAxis(mousePositionY, Vector3.right);

        onInputUpdateEvent.Invoke();
    }

    public UnityEvent GetLiftedEvent(KeyCode code)
    {
        return _keys[code].liftEvent;
    }

    public UnityEvent GetPressedEvent(KeyCode code)
    {
        return _keys[code].pressEvent;
    }

}
