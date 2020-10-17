using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AINodeTags { REROUTE, SKIP };

public class AINode : MonoBehaviour
{
    public float slowDistance = 15f;
    public float slowSpeed = 30f;
    public float fastSpeed = 40f;
    public float accelerationAngle = 40f;
    public float radius = 2f;

    public List<AINodeTags> tags = new List<AINodeTags>();

    public AINode rerouteNode = null;
}
