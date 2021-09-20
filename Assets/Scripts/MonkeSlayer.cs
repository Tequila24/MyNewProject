using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonkeSlayer : MonoBehaviour
{
    MonkeSpawner mSpawner;

    void Start()
    {
        mSpawner = GameObject.Find("MonkeSpawner").GetComponent<MonkeSpawner>();
    }

    public void OnTriggerEnter(Collider other) 
    {
        Debug.Log(other.gameObject.name);
        mSpawner.RemoveMonke();
    }
}
