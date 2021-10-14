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
    UnityEvent _onDoubleTapEvent;
    public UnityEvent doubleTapEvent { get { return _onDoubleTapEvent; } }

    private float _tapTimeStamp;

    private int _state;
    public int state { get { return _state; } }

    public Key()
    {
        _onPressEvent = new UnityEvent();
        _onLiftEvent = new UnityEvent();
        _onDoubleTapEvent = new UnityEvent();

        _tapTimeStamp = 0;
    }

    public void Update(KeyCode code)
    {
        if (Input.GetKeyDown(code))
        {
            _onPressEvent.Invoke();
            if ( (Time.time - _tapTimeStamp) < 0.3f)
                _onDoubleTapEvent.Invoke();
            _tapTimeStamp = Time.time;
        }

        if (Input.GetKeyUp(code))
            _onLiftEvent.Invoke();

        _state = Input.GetKey(code) ? 1 : 0;
    }
}



public class InputMaster : MonoBehaviour
{
    public static InputMaster _instance;
    public static InputMaster Instance { get { return _instance; } }

    private Dictionary<KeyCode, Key> _keys = new Dictionary<KeyCode, Key>()
    {
        { KeyCode.W, new Key() },
        { KeyCode.S, new Key() },
        { KeyCode.A, new Key() },
        { KeyCode.D, new Key() },
        { KeyCode.Space, new Key() },
        { KeyCode.LeftShift, new Key() },
        { KeyCode.Mouse0, new Key() },
        { KeyCode.Mouse1, new Key() }
    };

    public float mouseDeltaX;
    public float mouseDeltaY;

    public float mousePositionX;
    public float mousePositionY;

    public Quaternion lookDirection;

    private UnityEvent _onInputUpdateEvent = new UnityEvent();
    public UnityEvent onInputUpdateEvent { get { return _onInputUpdateEvent; } }

    // keys shortcuts
    // ==============================
    public int forward 
    { 
        get { if (_keys.ContainsKey(KeyCode.W))
                return _keys[KeyCode.W].state;
            else
                return 0; } 
    }
    public int backward 
    { 
        get { if (_keys.ContainsKey(KeyCode.S))
                return _keys[KeyCode.S].state;
            else
                return 0; } 
    }
    public int left 
    { 
        get { if (_keys.ContainsKey(KeyCode.A))
                return _keys[KeyCode.A].state;
            else
                return 0; } 
    }
    public int right 
    { 
        get { if (_keys.ContainsKey(KeyCode.D))
                return _keys[KeyCode.D].state;
            else
                return 0; } 
    }
    public int shift 
    { 
        get { if (_keys.ContainsKey(KeyCode.LeftShift))
                return _keys[KeyCode.LeftShift].state;
            else
                return 0; } 
    }
    public int space 
    { 
        get { if (_keys.ContainsKey(KeyCode.Space))
                return _keys[KeyCode.Space].state;
            else
                return 0; } 
    }
    // ==============================


    private void OnValidate() 
    {
        Init();
    }

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        if(_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        } else {
            _instance = this;
        }
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

    public bool AddKeyLiftListener(KeyCode code, UnityAction action)
    {
        if (_keys.ContainsKey(code)) 
        {
            _keys[code].liftEvent.RemoveListener(action);
            _keys[code].liftEvent.AddListener(action);
            return true;
        }
        else
            return false;
    }

    public bool AddKeyPressListener(KeyCode code, UnityAction action)
    {
        if (_keys.ContainsKey(code)) 
        {
            _keys[code].liftEvent.RemoveListener(action);
            _keys[code].pressEvent.AddListener(action);
            return true;
        }
        else
            return false;
    }

    public bool AddKeyDoubleTapListener(KeyCode code, UnityAction action)
    {
        if (_keys.ContainsKey(code)) 
        {
            _keys[code].liftEvent.RemoveListener(action);
            _keys[code].doubleTapEvent.AddListener(action);
            return true;
        } else {
            return false;
        }
    }

}
