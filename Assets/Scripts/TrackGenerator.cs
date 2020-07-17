using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TrackPeiceType { EMPTY, START, STRAIGHT, TURN, PIT, CLIFF, CLIFF_CORNER };

public class TrackGenerator : MonoBehaviour
{

    public List<TrackPeice> trackPeicePrefabs = new List<TrackPeice>();

    public int generationWidth = 6;
    public float defaultCheckpointRadius = 25f;
    public List<List<int>> worldCosts = new List<List<int>>();
    public List<List<TrackPeiceType>> worldType = new List<List<TrackPeiceType>>();

    public List<List<int>> rotations = new List<List<int>>();
    public List<List<int>> prefabIndex = new List<List<int>>();
    List<int[]> trackPath = new List<int[]>();

    public List<List<GameObject>> trackPeices = new List<List<GameObject>>();
    public List<Transform> trackStarts = new List<Transform>();
    public List<CheckPoint> checkPoints = new List<CheckPoint>();

    public System.Random r = new System.Random();

    public RaceTrack raceTrackController;

    public bool serverMode = false;

    public bool generateTrack = false;

    public int targetX = 10;
    public int targetY = 10;

    public float trackSpawnWidth = 10.0f;

    public string serializedTrack = "";

    public void Start()
    {

    }

    public void InstantiateTrack()
    {
        for(int x = 0; x < rotations.Count; ++x)
        {
            List<GameObject> c = new List<GameObject>();
            for(int y = 0; y < rotations[x].Count; ++y)
            {
                c.Add((GameObject)Instantiate(trackPeicePrefabs[prefabIndex[x][y]].prefab, new Vector3(trackSpawnWidth * x, 0.0f, trackSpawnWidth * y), Quaternion.Euler(-90f, 90f * rotations[x][y], 0f), this.transform));
            }

            trackPeices.Add(c);
        }
    }

    public void DesignRoad(List<int[]> path)
    {
        // Assume first road piece must start y+
        // Assume last road peice must end y+

        // find corners...

        for(int i = 0; i < path.Count; ++i)
        {
            int prevIndex = (i - 1) % path.Count;
            int nextIndex = (i + 1) % path.Count;

            if(prevIndex < 0)
            {
                prevIndex += path.Count;
            }

            if(!(path[prevIndex][0] == path[i][0] && path[nextIndex][0] == path[i][0]) && !(path[prevIndex][1] == path[i][1] && path[nextIndex][1] == path[i][1]))
            {
                // is corner
                worldType[path[i][0]][path[i][1]] = TrackPeiceType.TURN;

                // get correct rotation...

                if(path[prevIndex][0] < path[i][0] && path[nextIndex][1] < path[i][1])
                {
                    rotations[path[i][0]][path[i][1]] = 3;
                }

                if (path[prevIndex][1] < path[i][1] && path[nextIndex][0] > path[i][0])
                {
                    rotations[path[i][0]][path[i][1]] = 2;
                }

                if (path[prevIndex][0] > path[i][0] && path[nextIndex][1] > path[i][1])
                {
                    rotations[path[i][0]][path[i][1]] = 1;
                }

                if (path[prevIndex][0] > path[i][0] && path[nextIndex][1] < path[i][1])
                {
                    rotations[path[i][0]][path[i][1]] = 2;
                }

                if (path[prevIndex][1] < path[i][1] && path[nextIndex][0] < path[i][0])
                {
                    rotations[path[i][0]][path[i][1]] = 3;
                }

                if (path[prevIndex][1] > path[i][1] && path[nextIndex][0] > path[i][0])
                {
                    rotations[path[i][0]][path[i][1]] = 1;
                }

            } else
            {
                // is straight

                // get correct rotation...

                if(path[prevIndex][1] == path[i][1] && path[nextIndex][1] == path[i][1])
                {
                    if(path[prevIndex][0] < path[i][0])
                    {
                        rotations[path[i][0]][path[i][1]] = 3;
                    }
                    else
                    {
                        rotations[path[i][0]][path[i][1]] = 1;
                    }
                } else
                {
                    if (path[prevIndex][1] < path[i][1])
                    {
                        rotations[path[i][0]][path[i][1]] = 2;
                    }
                }
            }
        }

        // Design empty spots
        for(int x = 0; x < worldType.Count; ++x)
        {
            for(int y = 0; y < worldType[x].Count; ++y)
            {
                if(worldType[x][y] == TrackPeiceType.EMPTY)
                {
                    // Randomize rotation
                    rotations[x][y] = r.Next(4);
                }
            }
        }

        // Select type instance
        for (int x = 0; x < worldType.Count; ++x)
        {
            for (int y = 0; y < worldType[x].Count; ++y)
            {
                List<TrackPeice> pt = trackPeicePrefabs.FindAll(j => j.type == worldType[x][y]);
                int ptIndex = r.Next(0, pt.Count);
                prefabIndex[x][y] = trackPeicePrefabs.FindIndex(j => j == pt[ptIndex]);   
            }
        }
    }

    public List<int[]> TracePath(List<List<int[]>> paths, int[] t)
    {
        List<int[]> p = new List<int[]>();

        int[] target = new int[] { t[0], t[1] };

        while (!(target[0] == -1 && target[1] == -1))
        {
            p.Add(new int[] { target[0], target[1] });
            target = paths[target[0]][target[1]];
        }

        p.Reverse();

        return p;
    }

    public void GenerateTrack()
    {
        InitializeTrack();


        int[] pointA = new int[] { generationWidth / 2, (generationWidth / 2) + 1 };
        int[] pointB = new int[] { targetX, targetY };
        int[] pointC = new int[] { generationWidth / 2, (generationWidth / 2) - 2 };

        rotations[generationWidth / 2][generationWidth / 2] = 2;

        trackPath = new List<int[]>();

        // Get Path from A to B...
        List<List<int[]>> paths = GetMinPaths(pointA);
        trackPath.AddRange(TracePath(paths, pointB));

        // Mark Path from A to B as traveled...
        for (int i = 0; i < trackPath.Count; ++i)
        {
            if(worldType[trackPath[i][0]][trackPath[i][1]] == TrackPeiceType.EMPTY)
                worldType[trackPath[i][0]][trackPath[i][1]] = TrackPeiceType.STRAIGHT;
        }

        // Get Path from B to C, but not overlapping path A to B...
        paths = GetMinPaths(pointB);
        List<int[]> b_c_Path = TracePath(paths, pointC);
        b_c_Path.RemoveAt(0); // Remove Duplicate element... we already have B from path A to B
        trackPath.AddRange(b_c_Path);

        // Mark Path from B to C as traveled...
        for (int i = 0; i < b_c_Path.Count; ++i)
        {
            if (worldType[b_c_Path[i][0]][b_c_Path[i][1]] == TrackPeiceType.EMPTY)
                worldType[b_c_Path[i][0]][b_c_Path[i][1]] = TrackPeiceType.STRAIGHT;
        }

        if (b_c_Path.Count == 0 || b_c_Path[b_c_Path.Count - 1][0] != pointC[0] || b_c_Path[b_c_Path.Count - 1][1] != pointC[1])
        {
            if (b_c_Path.Count != 0) Debug.Log(b_c_Path[b_c_Path.Count - 1][0] + " " + b_c_Path[b_c_Path.Count - 1][1]);
            //Debug.Log("Couldn't find a path back!");

            // Retry!
            GenerateTrack();
            return;
        }

        DesignRoad(trackPath);

        InstantiateTrack();

        SetupStartPositions();
        SetupCheckpoints(trackPath);
        SetupRaceTrackController();

        if(serverMode)
        {
            GameObject track = this.gameObject;
            MeshRenderer[] meshRenders = track.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer mr in meshRenders)
            {
                mr.enabled = false;
            }

            ParticleSystem[] particleSystems = track.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particleSystems)
            {
                ps.Stop();
            }

            AudioSource[] audioSources = track.GetComponentsInChildren<AudioSource>();
            foreach (AudioSource ass in audioSources)
            {
                ass.Stop();
                ass.enabled = false;
            }

            serializedTrack = JsonUtility.ToJson(Serialize());
        }
    }

    public void SetupStartPositions()
    {
        trackStarts = new List<Transform>();

        foreach(Transform t in trackPeices[trackPeices.Count / 2][trackPeices[0].Count / 2].transform)
        {
            if(t.name.ToLower() != "checkpoint")
            {
                trackStarts.Add(t);
            }
        }
    }

    public void SetupCheckpoints(List<int[]> path)
    {
        checkPoints = new List<CheckPoint>();

        foreach (Transform t in trackPeices[trackPeices.Count / 2][trackPeices[0].Count / 2].transform)
        {
            if (t.name.ToLower() == "checkpoint")
            {
                float radius = defaultCheckpointRadius;

                if (t.localScale.x > 0.01f)
                {
                    //radius = t.localScale.x;
                }

                checkPoints.Add(new CheckPoint(t, radius));
            }
        }

        foreach (int[] tp in path)
        {
            foreach (Transform t in trackPeices[tp[0]][tp[1]].transform)
            {
                if (t.name.ToLower() == "checkpoint")
                {
                    float radius = defaultCheckpointRadius;

                    if(t.localScale.x > 0.01f)
                    {
                        //radius = t.localScale.x;
                    }

                    checkPoints.Add(new CheckPoint(t, radius));
                }
            }
        }

        foreach (Transform t in trackPeices[trackPeices.Count / 2][(trackPeices[0].Count / 2) - 1].transform)
        {
            if (t.name.ToLower() == "checkpoint")
            {
                float radius = defaultCheckpointRadius;

                if (t.localScale.x > 0.01f)
                {
                    //radius = t.localScale.x;
                }

                checkPoints.Add(new CheckPoint(t, radius));
            }
        }

        for(int i = 0; i < checkPoints.Count; ++i)
        {
            checkPoints[i].t.LookAt(checkPoints[(i + 1) % checkPoints.Count].t);
        }
    }

    public void SetupRaceTrackController()
    {
        raceTrackController.checkPoints = checkPoints;
        raceTrackController.carStarts = trackStarts;
        raceTrackController.track = this.gameObject;
    }

    public void Update()
    {
        if(generateTrack)
        {
            GenerateTrack();

            generateTrack = false;
        }
    }

    public void InitializeTrack()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        worldCosts = new List<List<int>>();
        worldType = new List<List<TrackPeiceType>>();
        rotations = new List<List<int>>();
        prefabIndex = new List<List<int>>();
        trackPath = new List<int[]>();
        trackPeices = new List<List<GameObject>>();

        for (int x = 0; x < generationWidth; ++x)
        {
            List<int> c = new List<int>();
            List<TrackPeiceType> t = new List<TrackPeiceType>();
            List<int> rots = new List<int>();
            List<int> index = new List<int>();

            for (int y = 0; y < generationWidth; ++y)
            {
                c.Add(r.Next());
                t.Add(TrackPeiceType.EMPTY);
                rots.Add(0);
                index.Add(0);
            }

            worldCosts.Add(c);
            worldType.Add(t);
            rotations.Add(rots);
            prefabIndex.Add(index);
        }

        worldType[generationWidth / 2][generationWidth / 2] = TrackPeiceType.START;
        worldType[generationWidth / 2][generationWidth / 2 - 1] = TrackPeiceType.PIT;

        // Create edge cliffs

        for (int x = 0; x < generationWidth; ++x)
        {
            worldType[x][0] = TrackPeiceType.CLIFF;

            worldType[x][generationWidth - 1] = TrackPeiceType.CLIFF;
            rotations[x][generationWidth - 1] = 2;
        }

        for (int y = 0; y < generationWidth; ++y)
        {
            worldType[0][y] = TrackPeiceType.CLIFF;
            rotations[0][y] = 1;

            worldType[generationWidth - 1][y] = TrackPeiceType.CLIFF;
            rotations[generationWidth - 1][y] = 3;
        }

        worldType[0][0] = TrackPeiceType.CLIFF_CORNER;
        rotations[0][0] = 0;

        worldType[0][generationWidth - 1] = TrackPeiceType.CLIFF_CORNER;
        rotations[0][generationWidth - 1] = 1;

        worldType[generationWidth - 1][0] = TrackPeiceType.CLIFF_CORNER;
        rotations[generationWidth - 1][0] = 3;

        worldType[generationWidth - 1][generationWidth - 1] = TrackPeiceType.CLIFF_CORNER;
        rotations[generationWidth - 1][generationWidth - 1] = 2;

    }

    List<int[]> GetNeighbours(int[] point) {

        List<int[]> neighbours = new List<int[]>();

        if(point[0] - 1 >= 0)
        {
            neighbours.Add(new int[] { point[0] - 1, point[1]});
        }

        if (point[0] + 1 < generationWidth)
        {
            neighbours.Add(new int[] { point[0] + 1, point[1] });
        }

        if (point[1] - 1 >= 0)
        {
            neighbours.Add(new int[] { point[0], point[1] - 1});
        }

        if (point[1] + 1 < generationWidth)
        {
            neighbours.Add(new int[] { point[0], point[1] + 1 });
        }

        return neighbours;
    }

    int FindLowestPointInFrontier(List<int[]> frontier)
    {
        int[] lowest = frontier[0];
        int index = 0;

        for(int i = 0; i < frontier.Count; ++i)
        {
            if(worldCosts[frontier[i][0]][frontier[i][1]] < worldCosts[lowest[0]][lowest[1]])
            {
                lowest = frontier[i];
                index = i;
            }
        }

        return index;
    }

    int GetPathCost(int[] end, List<List<int[]>> parentNodes)
    {
        if(parentNodes[end[0]][end[1]][0] == -1 && parentNodes[end[0]][end[1]][1] == -1)
        {
            return 0;
        } else
        {
            return worldCosts[end[0]][end[1]] + GetPathCost(parentNodes[end[0]][end[1]], parentNodes);
        }
    }

    public List<List<int[]>> GetMinPaths(int[] pointA)
    {
        List<List<int[]>> parentNodes = new List<List<int[]>>();
        List<List<bool>> seenNodes = new List<List<bool>>();

        for (int x = 0; x < generationWidth; ++x)
        {
            List<int[]> c = new List<int[]>();
            List<bool> s = new List<bool>();

            for (int y = 0; y < generationWidth; ++y)
            {
                c.Add(new int[] { -1, -1 });
                s.Add(false);
            }

            parentNodes.Add(c);
            seenNodes.Add(s);
        }

        List<int[]> frontier = new List<int[]>();

        List<int[]> neighbours;

        seenNodes[pointA[0]][pointA[1]] = true;

        frontier.Add(pointA);

        while(frontier.Count > 0)
        {

        int lowestIndex = FindLowestPointInFrontier(frontier);
            int[] lowest = frontier[lowestIndex];
            frontier.RemoveAt(lowestIndex);

            neighbours = GetNeighbours(lowest);

            for(int i = 0; i < neighbours.Count; ++i)
            {
                if(worldType[neighbours[i][0]][neighbours[i][1]] != TrackPeiceType.EMPTY)
                {
                    continue;
                }

                bool inFrontier = frontier.Exists(x => x[0] == neighbours[i][0] && x[1] == neighbours[i][1]);

                if (!inFrontier && !seenNodes[neighbours[i][0]][neighbours[i][1]])
                {
                    parentNodes[neighbours[i][0]][neighbours[i][1]] = new int[] { lowest[0], lowest[1] };
                    seenNodes[neighbours[i][0]][neighbours[i][1]] = true;
                    frontier.Add(new int[] { neighbours[i][0], neighbours[i][1]});
                } else
                {
                    if(inFrontier)
                    {
                        // if neighbour in frontier update it's parent if new cost is lower
                        if (GetPathCost(neighbours[i], parentNodes) > GetPathCost(lowest, parentNodes) + worldCosts[neighbours[i][0]][neighbours[i][1]])
                        {
                            parentNodes[neighbours[i][0]][neighbours[i][1]] = new int[] { lowest[0], lowest[1] };
                        }
                    }
                }
            }
        }

        /*
        string simpleOutput = "";
        for (int x = 0; x < parentNodes.Count; ++x)
        {

            for (int y = 0; y < parentNodes.Count; ++y)
            {
                if (parentNodes[y][x][0] < y && parentNodes[y][x][0] != -1)
                {
                    simpleOutput += "<";
                }

                if (parentNodes[y][x][0] > y && parentNodes[y][x][0] != -1)
                {
                    simpleOutput += ">";
                }

                if (parentNodes[y][x][1] < x && parentNodes[y][x][0] != -1)
                {
                    simpleOutput += "^";
                }

                if (parentNodes[y][x][1] > x && parentNodes[y][x][0] != -1)
                {
                    simpleOutput += "v";
                }

                if (parentNodes[y][x][0] == -1)
                {
                    simpleOutput += 0;

                }

                simpleOutput += "    ";
            }

            simpleOutput += "\n";
        }

        Debug.Log(simpleOutput);
        */

        return parentNodes;
    }

    public GeneratedTrackData Serialize()
    {
        GeneratedTrackData data = new GeneratedTrackData();

        data.Serialize(rotations, prefabIndex, trackPath);

        return data;
    }

    public void LoadTrackData(GeneratedTrackData data)
    {
        InitializeTrack();

        data.Deserialize(out rotations, out prefabIndex, out trackPath);

        InstantiateTrack();

        SetupStartPositions();
        SetupCheckpoints(trackPath);
        SetupRaceTrackController();
    }
}

[Serializable]
public class TrackPeice
{
    public TrackPeiceType type; 
    public GameObject prefab;
}

[Serializable]
public class GeneratedTrackData
{
    public List<IntList> rotations = new List<IntList>();
    public List<IntList> prefabIndex = new List<IntList>();
    public List<Point> trackPath = new List<Point>();

    public void Serialize(List<List<int>> rotations, List<List<int>> prefabIndex, List<int[]> trackPath)
    {

        this.rotations = new List<IntList>();
        this.prefabIndex = new List<IntList>();
        this.trackPath = new List<Point>();

        for (int x = 0; x < rotations.Count; ++x)
        {
            IntList yHat = new IntList();

            for(int y = 0; y < rotations[x].Count; ++y)
            {
                yHat.list.Add(rotations[x][y]);
            }

            this.rotations.Add(yHat);
        }

        for(int x = 0; x < prefabIndex.Count; ++x)
        {
            IntList yHat = new IntList();

            for (int y = 0; y < prefabIndex[x].Count; ++y)
            {
                yHat.list.Add(prefabIndex[x][y]);
            }

            this.prefabIndex.Add(yHat);
        }

        foreach(int[] a in trackPath)
        {
            this.trackPath.Add(new Point(a[0], a[1]));
        }
    }

    public void Deserialize(out List<List<int>> rotations, out List<List<int>> prefabIndex, out List<int[]> trackPath)
    {

        rotations = new List<List<int>>();
        prefabIndex = new List<List<int>>();
        trackPath = new List<int[]>();

        for (int x = 0; x < this.rotations.Count; ++x)
        {
            List<int> yHat = new List<int>();

            for (int y = 0; y < this.rotations[x].list.Count; ++y)
            {
                yHat.Add(this.rotations[x][y]);
            }

            rotations.Add(yHat);
        }

        for (int x = 0; x < this.prefabIndex.Count; ++x)
        {
            List<int> yHat = new List<int>();

            for (int y = 0; y < this.prefabIndex[x].list.Count; ++y)
            {
                yHat.Add(this.prefabIndex[x][y]);
            }

            prefabIndex.Add(yHat);
        }

        foreach (Point p in this.trackPath)
        {
            trackPath.Add(new int[] { p.x, p.y });
        }
    }
}

[Serializable]
public class IntList
{
    public List<int> list = new List<int>();

    public int this[int key]
    {
        get
        {
            return list[key];
        }
        set
        {
            list[key] = value;
        }
    }
}

[Serializable]
public class Point
{
    public int x = 0;
    public int y = 0;

    public Point() { }

    public Point(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}