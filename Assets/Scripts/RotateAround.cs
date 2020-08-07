using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateAround : MonoBehaviour
{
    public Transform centreObject;
    public float speed = 1.0f;
    public float distance = 60.0f;

    void Awake()
    {
        Setup();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.RotateAround(centreObject.position, Vector3.up, speed * Time.deltaTime);
    }

    void Setup()
    {
        transform.position = new Vector3(centreObject.position.x + distance, centreObject.position.y, centreObject.position.z);
    }

    void OnEnable()
    {
        Setup();
    }
}
