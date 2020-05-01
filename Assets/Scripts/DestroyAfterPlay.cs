using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAfterPlay : MonoBehaviour
{
    ParticleSystem ps;

    void Awake()
    {
        if(ps == null)
        {
            ps = GetComponent<ParticleSystem>();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!ps.isPlaying || ps.isStopped)
        {
            Destroy(this.gameObject);
        }
    }
}
