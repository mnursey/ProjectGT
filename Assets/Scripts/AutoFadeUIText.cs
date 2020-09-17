using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AutoFadeUIText : MonoBehaviour
{
    public float fadeCloseDistance = 15.0f;
    public float fadeFarDistance = 35.0f;

    public TextMeshProUGUI ui;

    Camera c;

    void Awake()
    {
        c = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if(c != null)
        {
            float d = Vector3.Distance(c.transform.position, transform.position);

            float p = (Mathf.Clamp(d, fadeCloseDistance, fadeFarDistance) - fadeCloseDistance) / (fadeFarDistance - fadeCloseDistance);

            ui.alpha = Mathf.Lerp(0.0f, 1.0f, p);
        } else
        {
            gameObject.SetActive(false);
        }
    }
}
