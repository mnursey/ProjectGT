using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkippedTrackArrowController : MonoBehaviour
{

    public GameObject skippedTrackArrowPrefab;
    public GameObject skippedTrackArrow;
    public float heightAboveCheckpoint = 20.0f;
    Animator staAnim;

    void Awake()
    {
        if(skippedTrackArrow == null)
        {
            skippedTrackArrow = Instantiate(skippedTrackArrowPrefab, Vector3.zero, Quaternion.identity, transform);
        }

        staAnim = skippedTrackArrow.GetComponent<Animator>();
        DisableSkippedTrackArrow();
    }

    public void EnableSkippedTrackArrow(Vector3 checkpointPosition)
    {
        skippedTrackArrow.transform.position = checkpointPosition + (Vector3.up * heightAboveCheckpoint);
        skippedTrackArrow.SetActive(true);
    }

    public void DisableSkippedTrackArrow()
    {
        staAnim.SetTrigger("Disable");
    }
}
