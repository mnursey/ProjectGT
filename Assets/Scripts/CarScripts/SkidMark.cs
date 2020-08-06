using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkidMark : MonoBehaviour
{
    public float width = 1.0f;
    public float life = 1.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void Idle()
    {
        gameObject.SetActive(false);
    }

    public void Place(Vector3 pos, Quaternion rot, float width, float life)
    {
        transform.position = pos;
        transform.rotation = rot;

        transform.eulerAngles = new Vector3(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + 0.001f, transform.rotation.eulerAngles.z);

        this.life = life;
        this.width = width;

        transform.localScale = new Vector3(width, transform.lossyScale.y, width);

        gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {

        // Auto remove if old
        if(life < 0.0f)
        {
            gameObject.SetActive(false);
        }

        life -= Time.deltaTime;
    }
}
