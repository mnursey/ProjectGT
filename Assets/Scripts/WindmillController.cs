using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindmillController : MonoBehaviour
{
    public GameObject mast;
    public float mastSpeed = 0.5f;
    
    // Start is called before the first frame update
    void Start()
    {
        transform.eulerAngles = new Vector3(-90.0f, 0f, 45f);
    }

    // Update is called once per frame
    void Update()
    {
        mast.transform.localEulerAngles = new Vector3(0.0f, mast.transform.localEulerAngles.y + mastSpeed * Time.deltaTime, 0.0f);  
    }
}
