using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AISequenceHandler : MonoBehaviour
{
    // A sequence is data for the AI to go from track tile A to track tile B.
    public List<AIBrainData> brains = new List<AIBrainData>();
    public bool createSequences = false;
    public TrackGenerator tg;

    public int currentBrainIndex = 0;
    public int populationSize = 10;
    public float mutationChance = 0.05f;

    void Awake()
    {
        if(tg == null)
        {
            Debug.LogError("SequenceHandler requires a TrackGenerator reference.");
        }

        if(brains.Count == 0)
            CreateBrains();
    }

    void Update()
    {

    }

    public void CreateBrains ()
    {
        brains = new List<AIBrainData>();

        for(int i = 0; i < populationSize; ++i)
        {
            brains.Add(new AIBrainData(tg));
        }
    }

    public void SaveSequences()
    {
        // Todo
    }

    public void LoadSequences()
    {
        // Todo
    }

    public AISequenceData GetSequenceFromWorldPos(Vector3 pos)
    {
        int trackX;
        int trackY;

        tg.WorldToTrackPos(pos.x, pos.z, out trackX, out trackY);

        int currentTilePrefabIndex = tg.prefabIndex[trackX][trackY];

        int currentTilePathIndex = 0;

        for (int i = 0; i < tg.trackPath.Count; ++i)
        {
            if(tg.trackPath[i][0] == trackX && tg.trackPath[i][1] == trackY)
            {
                currentTilePathIndex = i;
                break;
            }
        }

        int nextTilePathIndex = (currentTilePathIndex + 1) % tg.trackPath.Count;
        int nextTilePrefabIndex = tg.prefabIndex[tg.trackPath[nextTilePathIndex][0]][tg.trackPath[nextTilePathIndex][1]];

        return GetSequence(currentTilePrefabIndex, nextTilePrefabIndex);
    }

    public AISequenceData GetSequence(int firstIndex, int secondIndex)
    {
        return brains[currentBrainIndex].GetSequence(firstIndex, secondIndex);
    }

    public void SetScore(float score)
    {
        if (score < 0)
        {
            while(Mathf.Abs(score) > 0.1f)
            {
                score *= 0.5f;
            }
        }

        brains[currentBrainIndex].score = Mathf.Abs(score);
    }

    public bool NextSequence()
    {
        currentBrainIndex++;

        if(currentBrainIndex >= populationSize)
        {
            currentBrainIndex = 0;
            return true;
        }

        return false;
    }

    public int GetWeightedBrainIndex()
    {
        float scoreTotal = 0.0f;

        foreach (AIBrainData b in brains)
        {
            scoreTotal += b.score;
        }

        int index = -1;
        float r = Random.Range(0.0f, scoreTotal);
        float tally = 0.0f;

        foreach (AIBrainData b in brains)
        {
            tally += b.score;
            ++index;

            if (tally >= r)
            {

                break;
            }
        }

        return index;
    }

    public void CreateNextGeneration()
    {
        List<List<float>> parents = new List<List<float>>();
        List<List<float>> children = new List<List<float>>();

        foreach (AIBrainData b in brains)
        {
            parents.Add(b.ToFloatList());
        }

        for(int i = 0; i < populationSize; ++i)
        {
            // select parent a
            int parentAIndex = GetWeightedBrainIndex();
            List<float> parentA = parents[parentAIndex];

            // select parent b
            int parentBIndex = GetWeightedBrainIndex();
            List<float> parentB = parents[parentBIndex];

            Debug.Log(parentAIndex + " mate " + parentBIndex);

            // create new child
            List<float> child = CreateChild(parentA, parentB);

            // mutate child
            Mutate(child, mutationChance);

            children.Add(child);
        }

        // Set brain data to children data
        for (int i = 0; i < populationSize; ++i)
        {
            brains[i].LoadFloatData(children[i]);
            brains[i].score = 0;
        }
    }

    public List<float> CreateChild(List<float> a, List<float> b)
    {
        List<float> child = new List<float>(); 

        for(int i = 0; i < a.Count; ++i)
        {
            if(Random.value < 0.5f)
            {
                child.Add(a[i]);
            }
            else
            {
                child.Add(b[i]);
            }
        }

        return child;
    }

    public void Mutate(List<float> a, float rate)
    {
        for (int i = 0; i < a.Count; ++i)
        {
            if (Random.value < rate)
            {
                a[i] = Random.Range(-200f, 200f);
            }
        }
    }
}

[System.Serializable]
public class AISequenceData
{
    public Vector3 checkpointOffset = Vector3.zero;
    public float slowDistance = 15f;
    public float slowSpeed = 30f;
    public float fastSpeed = 40f;
    public float accelerationAngle = 40f;

    public AISequenceData()
    {

    }

    public AISequenceData(Vector3 checkpointOffset, float slowDistance, float slowSpeed, float fastSpeed, float accelerationAngle)
    {
        this.checkpointOffset = checkpointOffset;
        this.slowDistance = slowDistance;
        this.slowSpeed = slowSpeed;
        this.fastSpeed = fastSpeed;
        this.accelerationAngle = accelerationAngle;
    }

    public List<float> ToFloatList()
    {
        return new List<float>{
            checkpointOffset.x,
            checkpointOffset.y,
            checkpointOffset.z,
            slowDistance,
            slowSpeed,
            fastSpeed,
            accelerationAngle };
    }

    public void LoadFloatData(List<float> data)
    {
        checkpointOffset = new Vector3(data[0], data[1], data[2]);
        slowDistance = data[3];
        slowSpeed = data[4];
        fastSpeed = data[5];
        accelerationAngle = data[6];
    }
}

[System.Serializable]
public class AIBrainData
{
    List<List<AISequenceData>> sequences = new List<List<AISequenceData>>();

    public float score;

    public AIBrainData (TrackGenerator tg)
    {
        sequences = new List<List<AISequenceData>>();

        foreach (TrackPeice tp in tg.trackPeicePrefabs)
        {
            List<AISequenceData> s = new List<AISequenceData>();
            foreach (TrackPeice tpHat in tg.trackPeicePrefabs)
            {
                s.Add(new AISequenceData(new Vector3(Random.Range(-20f, 20f), Random.Range(-20f, 20f), Random.Range(-20f, 20f)), Random.Range(0.05f, 60f), Random.Range(0.1f, 200f), Random.Range(0.5f, 200f), Random.Range(-360f, 360f)));
            }

            sequences.Add(s);
        }
    }

    public AISequenceData GetSequence(int firstIndex, int secondIndex)
    {
        return sequences[firstIndex][secondIndex];
    }

    public List<float> ToFloatList()
    {
        List<float> data = new List<float>();

        foreach (List<AISequenceData> l in sequences)
        {
            foreach(AISequenceData d in l)
            {
                data.AddRange(d.ToFloatList());
            }
        }

        return data;
    }

    public void LoadFloatData(List<float> data)
    {
        int offset = 0;

        foreach (List<AISequenceData> l in sequences)
        {
            foreach (AISequenceData d in l)
            {
                d.LoadFloatData(data.GetRange(offset, 7));
                offset += 7;
            }
        }
    }
}
