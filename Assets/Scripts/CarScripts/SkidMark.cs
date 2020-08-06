using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkidMark : MonoBehaviour
{

    public MeshRenderer mr;
    public MeshFilter mf;

    public float life = 1.0f;

    void Awake()
    {
        mr = GetComponent<MeshRenderer>();
        mf = GetComponent<MeshFilter>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void Idle()
    {
        gameObject.SetActive(false);
    }

    public void Place(Mesh mesh, float life, Vector3 pos)
    {
        mf.mesh = mesh;
        this.life = life;
        transform.position = pos;
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
