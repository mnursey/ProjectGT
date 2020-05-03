using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class OnAwakeUpdateText : MonoBehaviour
{

    public ControlManager cm;

    public ControlAction ca;
    public int bindingID;

    void Awake()
    {
        cm.UpdateTextToControlName(GetComponent<TextMeshProUGUI>(), ca, bindingID);
    }

}
