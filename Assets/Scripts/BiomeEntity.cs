using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiomeEntity : MonoBehaviour
{
    public int treeIndex = 0;
    public TrackGenerator tg;

    // Start is called before the first frame update
    void Start()
    {
        tg = TrackGenerator.Instance;

        if(tg != null)
        {
            GameObject prefab = tg.treePrefabs[tg.biomes[tg.currentBiomeIndex].treeIndicies[treeIndex]];
            GameObject go = Instantiate(prefab, transform.position, prefab.transform.rotation);
            PlaceOnGround pog = go.GetComponent<PlaceOnGround>();

            if(pog != null)
            {
                pog.rc = tg.rc;
                pog.minHeight = tg.waterHeight;
                pog.Ground();
            }

            go.transform.SetParent(tg.transform);
        }
    }
}
