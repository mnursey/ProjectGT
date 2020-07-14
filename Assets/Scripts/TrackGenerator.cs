using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TrackPeiceType { EMPTY, START, STRAIGHT, TURN, PIT };

public class TrackGenerator : MonoBehaviour
{

    public List<TrackPeice> trackPeicePrefabs = new List<TrackPeice>();

    public int generationWidth = 6;

    public List<List<int>> worldCosts = new List<List<int>>();
    public List<List<TrackPeiceType>> worldType = new List<List<TrackPeiceType>>();
    public List<List<int>> rotations = new List<List<int>>();
    public List<List<int>> prefabIndex = new List<List<int>>();
    public List<List<GameObject>> trackPeices = new List<List<GameObject>>();
    public List<Transform> trackStarts = new List<Transform>();
    public List<CheckPoint> checkPoints = new List<CheckPoint>();

    public System.Random r = new System.Random();

    public RaceTrack raceTrackController;

    public bool serverMode = false;

    /*
    [TextArea(6, 20)]
    [Tooltip("WorldCosts View")]
    public string worldCostDebug = "";

    [TextArea(6, 20)]
    [Tooltip("WorldType View")]
    public string worldTypeDebug = "";
    */

    public bool generateTrack = false;

    public int targetX = 10;
    public int targetY = 10;

    public float trackSpawnWidth = 10.0f;
    public bool debugCheckPoints = false;

    public void Start()
    {

    }

    public void InstantiateTrack()
    {
        for(int x = 0; x < generationWidth; ++x)
        {
            List<GameObject> c = new List<GameObject>();
            for(int y = 0; y < generationWidth; ++y)
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

        List<int[]> trackPath = new List<int[]>();
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
            Debug.Log("Couldn't find a path back!");

            // Retry!
            GenerateTrack();
            return;
        }

        /*
        Debug.Log("--- Paths ---");
        for (int i = 0; i < trackPath.Count; ++i)
        {
            Debug.Log(trackPath[i][0] + " " + trackPath[i][1]);
        }
        */

        DesignRoad(trackPath);

        InstantiateTrack();

        /*
        DebugWorldCosts();
        DebugWorldType();
        */

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
        }
    }

    public void SetupStartPositions()
    {
        trackStarts = new List<Transform>();

        foreach(Transform t in trackPeices[generationWidth / 2][generationWidth / 2].transform)
        {
            if(t.name.ToLower() != "checkpoint")
            {
                trackStarts.Add(t);
            }
        }
    }

    public void SetupCheckpoints(List<int[]> trackPath)
    {
        checkPoints = new List<CheckPoint>();

        foreach (Transform t in trackPeices[generationWidth / 2][generationWidth / 2].transform)
        {
            if (t.name.ToLower() == "checkpoint")
            {
                float radius = trackSpawnWidth / 2f;

                if (t.localScale.x > 0.01f)
                {
                    radius = t.localScale.x;
                }

                checkPoints.Add(new CheckPoint(t, trackSpawnWidth / 2f));
            }
        }

        foreach (int[] tp in trackPath)
        {
            foreach (Transform t in trackPeices[tp[0]][tp[1]].transform)
            {
                if (t.name.ToLower() == "checkpoint")
                {
                    float radius = trackSpawnWidth / 2f;

                    if(t.localScale.x > 0.01f)
                    {
                        radius = t.localScale.x;
                    }

                    checkPoints.Add(new CheckPoint(t, trackSpawnWidth / 2f));
                }
            }
        }

        foreach (Transform t in trackPeices[generationWidth / 2][generationWidth / 2 - 1].transform)
        {
            if (t.name.ToLower() == "checkpoint")
            {
                float radius = trackSpawnWidth / 2f;

                if (t.localScale.x > 0.01f)
                {
                    radius = t.localScale.x;
                }

                checkPoints.Add(new CheckPoint(t, trackSpawnWidth / 2f));
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

        // Path from start to target
        // Convert all tiles on path from start to target into tiles...

        // Path from target to straight behind start
        // Convert all tiles on path from start to target into tiles...
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

        //int iter = 0;
        while(frontier.Count > 0)
        {
            /*
            string posOutput = "";
            string output = "";
            string simpleOutput = "";
            for (int x = 0; x < parentNodes.Count; ++x)
            {

                for (int y = 0; y < parentNodes.Count; ++y)
                {
                    output += parentNodes[y][x][0] + "," + parentNodes[y][x][1] + "    ";
                    posOutput += y + "," + x + "    ";

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

                    if(parentNodes[y][x][0] == -1)
                    {
                        simpleOutput += 0;

                    } else
                    {
                        //simpleOutput += ":" + (parentNodes[y][x][1] - x) + "," + (parentNodes[y][x][0] - y);
                    }

                    simpleOutput += "    ";
                }

                output += "\n";
                posOutput += "\n";
                simpleOutput += "\n";
            }

            Debug.Log(output);
            Debug.Log(posOutput);
            Debug.Log(simpleOutput);

            Debug.Log(iter++); */

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

            /*if(iter > 600)
            {
                break;
            }*/
        }

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

        return parentNodes;
    }

    /*
    public void DebugWorldCosts()
    {
        worldCostDebug = "";
        for (int x = 0; x < generationWidth; ++x)
        {
            for (int y = 0; y < generationWidth; ++y)
            {
                worldCostDebug += worldCosts[y][x] + " ";
            }

            worldCostDebug += "\n";
        }
    }

    public void DebugWorldType()
    {
        worldTypeDebug = "";
        for (int x = 0; x < generationWidth; ++x)
        {
            for (int y = 0; y < generationWidth; ++y)
            {
                worldTypeDebug += worldType[y][x] + " ";
            }

            worldTypeDebug += "\n";
        }
    }
    */

    public void LoadTrack(string data)
    {

    }

    void OnDrawGizmos()
    {
        if (debugCheckPoints)
        {
            foreach (CheckPoint c in checkPoints)
            {
                // Draw a yellow sphere at the transform's position
                Gizmos.color = new Color(242 / 255f, 245 / 255f, 66 / 255f, 190 / 255f);
                Gizmos.DrawSphere(c.t.position, c.raduis);
            }
        }
    }
}

[Serializable]
public class TrackPeice
{
    public TrackPeiceType type; 
    public GameObject prefab;
}
