using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarModelManager : MonoBehaviour
{
    public List<CarModel> models = new List<CarModel>();
}

[Serializable]
public class CarModel
{
    public string name;
    public string description;

    public GameObject prefab;
}
