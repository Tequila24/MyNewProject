using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonkeSpawner : MonoBehaviour
{

    [SerializeField]
    GameObject monkePrefab;


    GameObject currentMonke = null;

    private float baseScale = 10;
    private float scaleModifier = 20;

    void Update()
    {
        if (currentMonke == null)
            CreateMonke();

        if (Input.GetKeyDown(KeyCode.T)) {
            RemoveMonke();
        }
    }

    public void RemoveMonke()
    {
        Object.Destroy(currentMonke);
        currentMonke = null;
    }

    void CreateMonke()
    {
        currentMonke = GameObject.Instantiate(monkePrefab);
        float generatedScale = baseScale + Random.value * scaleModifier;
        currentMonke.transform.localScale = new Vector3(generatedScale, generatedScale, generatedScale);

        Vector3 newPosition = Vector3.zero;
        if ((Random.value -0.5f) > 0) {
            // generate in vertical lane
            newPosition = new Vector3(  Random.value * 600 - 300,
                                        3 * generatedScale,
                                        Random.value * 200 - 100);
        } else {
            //generate in horizontal lane
            newPosition = new Vector3(  Random.value * 200 - 100,
                                        3 * generatedScale,
                                        Random.value * 600 - 300);
        }

        currentMonke.transform.position = newPosition;

        Quaternion newRotation = Quaternion.AngleAxis(Random.value * 360, Vector3.up);
        currentMonke.transform.rotation = newRotation;
    }
}
