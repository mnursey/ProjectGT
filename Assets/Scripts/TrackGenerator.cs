using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TrackPeiceType { EMPTY, START, STRAIGHT, TURN, PIT, CLIFF, CLIFF_CORNER, WATER, WATER_CLIFF, WATER_CORNER, WATER_NARROW, WATER_END, WATER_POND };

public class TrackGenerator : MonoBehaviour
{
    public GameObject waterObject;
    public float waterHeightMax;
    public float waterHeightMin;
    public float waterHeight;

    public List<TrackPeice> trackPeicePrefabs = new List<TrackPeice>();

    public int generationWidth = 6;
    public float defaultCheckpointRadius = 25f;
    public int holeChance = 2;
    public int landmarkChance = 15;

    public float biomeFrequency = 2.0f;
    public float waterRange = 0.5f;

    public float generationPowerA = 1.0f;
    public float generationScaleA = 1.0f;
    public float generationPowerB = 1.0f;
    public float generationScaleB = 1.0f;
    public float generationPowerC = 1.0f;
    public float generationScaleC = 1.0f;

    public float generationHeightOffset = 16.0f;
    public float trackTileHeight = 15f;

    public List<List<int>> worldCosts = new List<List<int>>();
    public List<List<TrackPeiceType>> worldType = new List<List<TrackPeiceType>>();

    public List<List<int>> rotations = new List<List<int>>();
    public List<List<int>> prefabIndex = new List<List<int>>();
    List<int[]> trackPath = new List<int[]>();

    public GameObject generatedTilePrefab;

    public List<List<GameObject>> trackPeices = new List<List<GameObject>>();
    public List<Transform> trackStarts = new List<Transform>();
    public List<CheckPoint> checkPoints = new List<CheckPoint>();

    public System.Random r = new System.Random();

    public RaceTrack raceTrackController;
    public RaceController rc;

    public bool serverMode = false;

    public bool generateTrack = false;

    public int targetX = 10;
    public int targetY = 10;

    public float trackSpawnWidth = 10.0f;

    public string serializedTrack = "";

    List<List<float>> noiseMap;
    public int noiseMapDensity = 3;

    public List<GameObject> treePrefabs = new List<GameObject>();
    public List<GameObject> treeInstances = new List<GameObject>();

    public int numTrees = 100;

    public float xOffsetA;
    public float yOffsetA;
    public float xOffsetB;
    public float yOffsetB;
    public float xOffsetC;
    public float yOffsetC;

    public void Start()
    {

    }

    public List<List<float>> CreateNoiseMap()
    {
        System.Random r = new System.Random();

        xOffsetA = (float)r.NextDouble();
        yOffsetA = (float)r.NextDouble();
        xOffsetB = (float)r.NextDouble();
        yOffsetB = (float)r.NextDouble();
        xOffsetC = (float)r.NextDouble();
        yOffsetC = (float)r.NextDouble();

        List<List<float>> map = new List<List<float>>();

        for(int x = 0; x < noiseMapDensity * generationWidth; ++x)
        {
            List<float> column = new List<float>();
            for(int y = 0; y < noiseMapDensity * generationWidth; ++y)
            {
                column.Add(CalculateHeight(x, y));
            }

            map.Add(column);
        }

        // Create flat quad areas
        for (int x = 0; x < map.Count - 1; x += 2)
        {
            for(int y = 0; y < map.Count - 1; y += 2)
            {
                float value = (map[x][y] + map[x + 1][y + 1] + map[x][y + 1]  + map[x + 1][y]) / 4f;
                map[x][y] = value;
                map[x][y + 1] = value;
                map[x + 1][y] = value;
                map[x + 1][y + 1] = value;
            }
        }

        // Set borders to be low
        for (int i = 0; i < map.Count; ++i)
        {
            map[0][i] = waterHeight - 20f;
            map[map.Count - 2][i] = waterHeight - 20f;
            map[i][0] = waterHeight - 20f;
            map[i][map.Count - 2] = waterHeight - 20f;
        }

        return map;
    }

    private float CalculateHeight(float x, float y)
    {
        float value = 0.0f;

        value += ((Mathf.PerlinNoise(x * generationScaleA + xOffsetA, y * generationScaleA + yOffsetA) - 0.5f) * generationPowerA);
        value += ((Mathf.PerlinNoise(x * generationScaleB + xOffsetB, y * generationScaleB + yOffsetB) - 0.5f) * generationPowerB);
        value += ((Mathf.PerlinNoise(x * generationScaleC + xOffsetC, y * generationScaleC + yOffsetC) - 0.5f) * generationPowerC);

        return value + generationHeightOffset;
    }

    public float GetHeightFromMap(float x, float y)
    {
        int mapX;
        int mapY;

        WorldPosToNoiseMapPos(x, y, out mapX, out mapY);

        return noiseMap[mapX][mapY];
    }

    public void WorldPosToNoiseMapPos(float x, float y, out int mapX, out int mapY)
    {
        x = Mathf.Clamp(x / trackSpawnWidth * noiseMapDensity, 0.0f, noiseMap.Count - 1);
        y = Mathf.Clamp(y / trackSpawnWidth * noiseMapDensity, 0.0f, noiseMap.Count - 1);

        // Todo decided to either floor or round
        mapX = Mathf.FloorToInt(x);
        mapY = Mathf.FloorToInt(y);
    }

    public void WorldToTrackPos(float x, float y, out int trackX, out int trackY)
    {
        x = Mathf.Clamp(x / trackSpawnWidth, 0.0f, prefabIndex.Count - 1);
        y = Mathf.Clamp(y / trackSpawnWidth, 0.0f, prefabIndex.Count - 1);

        // Todo decided to either floor or round
        trackX = Mathf.RoundToInt(x);
        trackY = Mathf.RoundToInt(y);
    }

    public bool IsTrackTile(int x, int y)
    {
        return trackPeicePrefabs[prefabIndex[x][y]].type == TrackPeiceType.START || trackPeicePrefabs[prefabIndex[x][y]].type == TrackPeiceType.STRAIGHT || trackPeicePrefabs[prefabIndex[x][y]].type == TrackPeiceType.TURN || trackPeicePrefabs[prefabIndex[x][y]].type == TrackPeiceType.PIT || HasTag(trackPeicePrefabs[prefabIndex[x][y]], TrackPeiceTags.BRIDGE);
    }

    void FlattenNoiseMap()
    {
        for (int x = 0; x < rotations.Count; ++x)
        {
            List<GameObject> c = new List<GameObject>();
            for (int y = 0; y < rotations[x].Count; ++y)
            {
                if (IsTrackTile(x, y))
                {
                    Vector2 worldPos = new Vector2(trackSpawnWidth * x, trackSpawnWidth * y);

                    int mapX;
                    int mapY;

                    WorldPosToNoiseMapPos(worldPos.x, worldPos.y, out mapX, out mapY);

                    for (int xHat = -2; xHat <= 1; ++xHat)
                    {
                        for (int yHat = -2; yHat <= 1; ++yHat)
                        {
                            noiseMap[mapX + xHat][mapY + yHat] = trackTileHeight;
                        }
                    }
                }
            }
        }
    }

    public void InstantiateTrack()
    {
        FlattenNoiseMap();

        for (int x = 0; x < rotations.Count; ++x)
        {
            List<GameObject> c = new List<GameObject>();
            for(int y = 0; y < rotations[x].Count; ++y)
            {
                if(!IsTrackTile(x, y))
                {
                    GameObject generatedTile = (GameObject)Instantiate(generatedTilePrefab, new Vector3(trackSpawnWidth * x, 0.0f, trackSpawnWidth * y), Quaternion.identity, this.transform);
                    c.Add(generatedTile);

                    GeneratedTrackTile gtt = generatedTile.GetComponent<GeneratedTrackTile>();
                    gtt.Setup(x, y, this);
                }
                else
                {
                    c.Add((GameObject)Instantiate(trackPeicePrefabs[prefabIndex[x][y]].prefab, new Vector3(trackSpawnWidth * x, 0.0f, trackSpawnWidth * y), Quaternion.Euler(-90f, 90f * rotations[x][y], 0f), this.transform));
                }
            }

            trackPeices.Add(c);
        }

        SetupWater();
        SetupTrees();

        EnableServerObjects(serverMode);
    }

    public void SetupTrees()
    {
        for(int i = 0; i < treeInstances.Count; ++i)
        {
            PlaceOnGround pog = treeInstances[i].GetComponent<PlaceOnGround>();
            pog.rc = rc;
            pog.minHeight = waterHeight;
            treeInstances[i].SetActive(true);
            pog.Ground();

            // Remove trees on racetrack
            int xTile;
            int yTile;
            WorldToTrackPos(treeInstances[i].transform.position.x, treeInstances[i].transform.position.z, out xTile, out yTile);

            if(IsTrackTile(xTile, yTile))
            {
                Destroy(treeInstances[i]);
            }
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

        // Select type instance
        for (int x = 0; x < worldType.Count; ++x)
        {
            for (int y = 0; y < worldType[x].Count; ++y)
            {
                HandleTileInstancePicking(x, y);
            }
        }

        // Add landmarks
        for (int x = 0; x < worldType.Count; ++x)
        {
            for (int y = 0; y < worldType[x].Count; ++y)
            {
                //if(worldType[x][y] == TrackPeiceType.EMPTY)
                {
                    int c = r.Next(0, 100);

                    if(c < landmarkChance)
                    {
                        List<TrackPeice> pt = trackPeicePrefabs.FindAll(j => j.type == worldType[x][y] && HasTag(j, TrackPeiceTags.LANDMARK) && !HasTag(j, TrackPeiceTags.BRIDGE));

                        if(pt.Count > 0)
                        {
                            int ptIndex = r.Next(0, pt.Count);
                            prefabIndex[x][y] = trackPeicePrefabs.FindIndex(j => j == pt[ptIndex]);
                        }
                    }
                }
            }
        }

        // Add holes after jump
        {
            List<TrackPeice> pt = trackPeicePrefabs.FindAll(j => j.type == TrackPeiceType.STRAIGHT && IsHole(j));
            for (int i = 0; i < path.Count - 1; ++i)
            {
                TrackPeice currentPeice = trackPeicePrefabs[prefabIndex[path[i][0]][path[i][1]]];
                TrackPeice nextPeice = trackPeicePrefabs[prefabIndex[path[i + 1][0]][path[i + 1][1]]];

                // if peice is straight && jump && next peice is straight
                if(currentPeice.type == TrackPeiceType.STRAIGHT && nextPeice.type == TrackPeiceType.STRAIGHT && HasTag(currentPeice, TrackPeiceTags.JUMP))
                {
                    // chance to make random hole...
                    // random chance
                    int c = r.Next(0, 100);

                    // postive chance -> make nextpeice a hole
                    if (c < holeChance)
                    {
                        int ptIndex = r.Next(0, pt.Count);
                        prefabIndex[path[i + 1][0]][path[i + 1][1]] = trackPeicePrefabs.FindIndex(j => j == pt[ptIndex]);
                    }
                }
            }
        }

        /*
        // Add water bridges on straights flanked by water
        {
            List<TrackPeice> pt = trackPeicePrefabs.FindAll(j => j.type == TrackPeiceType.WATER_NARROW && HasTag(j, TrackPeiceTags.BRIDGE));
            for (int i = 0; i < path.Count - 1; ++i)
            {
                TrackPeice currentPeice = trackPeicePrefabs[prefabIndex[path[i][0]][path[i][1]]];

                // if peice is straight && regular
                if (currentPeice.type == TrackPeiceType.STRAIGHT && currentPeice.tags.Count == 0)
                {
                    // check if peice could of been water && if flanked by water...
                    // gen water spot instead...
                    if (biomeCosts[path[i][0]][path[i][1]] < waterRange)
                    {
                        int oldRot = rotations[path[i][0]][path[i][1]];
                        worldType[path[i][0]][path[i][1]] = TrackPeiceType.WATER_NARROW;
                        bool isWater = HandleWaterTile(path[i][0], path[i][1]);

                        // Check if water is narrow...
                        if (isWater && worldType[path[i][0]][path[i][1]] == TrackPeiceType.WATER_NARROW)
                        {
                            int ptIndex = r.Next(0, pt.Count);
                            prefabIndex[path[i][0]][path[i][1]] = trackPeicePrefabs.FindIndex(j => j == pt[ptIndex]);

                            // Update neighbour water tiles to account for new water tile.
                            List<int[]> neighbours = GetNeighbours(new int[] { path[i][0], path[i][1] });

                            foreach (int[] n in neighbours)
                            {
                                bool neighbourIsWater = HandleWaterTile(n[0], n[1]);

                                if (neighbourIsWater)
                                {
                                    HandleTileInstancePicking(n[0], n[1]);
                                }
                            }

                        }
                        else
                        {
                            rotations[path[i][0]][path[i][1]] = oldRot;
                            worldType[path[i][0]][path[i][1]] = TrackPeiceType.STRAIGHT;
                        }
                    }
                }
            }
        }
        */

        // Spawn potential trees
        for(int i = 0; i < numTrees; ++i)
        {
            GameObject prefab = treePrefabs[r.Next(treePrefabs.Count)];
            GameObject tree = Instantiate(prefab, new Vector3(UnityEngine.Random.Range(0f, 454f), 2048f, UnityEngine.Random.Range(0f, 454f)), prefab.transform.rotation, transform);
            treeInstances.Add(tree);
            tree.transform.eulerAngles = new Vector3(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            treeInstances[i].SetActive(false);
        }
    }

    void HandleTileInstancePicking(int x, int y)
    {
        List<TrackPeice> pt = trackPeicePrefabs.FindAll(j => j.type == worldType[x][y] && !IsHole(j) && !HasTag(j, TrackPeiceTags.LANDMARK));
        int ptIndex = r.Next(0, pt.Count);
        prefabIndex[x][y] = trackPeicePrefabs.FindIndex(j => j == pt[ptIndex]);
    }

    bool HandleWaterTile(int x, int y)
    {
        if (worldType[x][y] == TrackPeiceType.WATER || worldType[x][y] == TrackPeiceType.WATER_CLIFF || worldType[x][y] == TrackPeiceType.WATER_CORNER || worldType[x][y] == TrackPeiceType.WATER_END || worldType[x][y] == TrackPeiceType.WATER_NARROW || worldType[x][y] == TrackPeiceType.WATER_POND)
        {
            int numberOfWaterNeighbours = 0;
            string neighboursPattern = "";
            List<int[]> neighbours = GetNeighbours(new int[] { x, y });

            foreach (int[] n in neighbours)
            {
                if (worldType[n[0]][n[1]] == TrackPeiceType.WATER || worldType[n[0]][n[1]] == TrackPeiceType.WATER_CLIFF || worldType[n[0]][n[1]] == TrackPeiceType.WATER_CORNER || worldType[n[0]][n[1]] == TrackPeiceType.WATER_NARROW || worldType[n[0]][n[1]] == TrackPeiceType.WATER_END || worldType[n[0]][n[1]] == TrackPeiceType.WATER_POND)
                {
                    numberOfWaterNeighbours++;
                    neighboursPattern += "W";
                }
                else
                {
                    neighboursPattern += "L";
                }
            }

            // If all neighbours water then regular water.
            // If 3 neighbours is water then cliff water.
            // If 2 neighbours is water then corner or narrow water.
            // If 1 neighbours is water then end water.
            // If 0 neighbours is water then pond water.

            switch (numberOfWaterNeighbours)
            {
                case 4:
                    worldType[x][y] = TrackPeiceType.WATER;
                    break;

                case 3:
                    worldType[x][y] = TrackPeiceType.WATER_CLIFF;

                    if (neighboursPattern == "LWWW")
                    {
                        rotations[x][y] = 1;
                    }

                    if (neighboursPattern == "WLWW")
                    {
                        rotations[x][y] = 3;
                    }

                    if (neighboursPattern == "WWLW")
                    {
                        rotations[x][y] = 0;
                    }

                    if (neighboursPattern == "WWWL")
                    {
                        rotations[x][y] = 2;
                    }

                    break;

                case 2:

                    if (neighboursPattern == "LLWW")
                    {
                        rotations[x][y] = 1;
                        worldType[x][y] = TrackPeiceType.WATER_NARROW;
                    }

                    if (neighboursPattern == "WWLL")
                    {
                        rotations[x][y] = 0;
                        worldType[x][y] = TrackPeiceType.WATER_NARROW;
                    }

                    if (neighboursPattern == "LWWL")
                    {
                        rotations[x][y] = 1;
                        worldType[x][y] = TrackPeiceType.WATER_CORNER;
                    }

                    if (neighboursPattern == "WLLW")
                    {
                        rotations[x][y] = 3;
                        worldType[x][y] = TrackPeiceType.WATER_CORNER;
                    }

                    if (neighboursPattern == "LWLW")
                    {
                        rotations[x][y] = 0;
                        worldType[x][y] = TrackPeiceType.WATER_CORNER;
                    }

                    if (neighboursPattern == "WLWL")
                    {
                        rotations[x][y] = 2;
                        worldType[x][y] = TrackPeiceType.WATER_CORNER;
                    }

                    break;

                case 1:

                    worldType[x][y] = TrackPeiceType.WATER_END;

                    if (neighboursPattern == "LLLW")
                    {
                        rotations[x][y] = 3;

                    }

                    if (neighboursPattern == "LLWL")
                    {
                        rotations[x][y] = 1;

                    }

                    if (neighboursPattern == "LWLL")
                    {
                        rotations[x][y] = 0;

                    }

                    if (neighboursPattern == "WLLL")
                    {
                        rotations[x][y] = 2;
                    }

                    break;

                case 0:
                    //worldType[x][y] = TrackPeiceType.WATER_POND;

                    // Remove single water spots
                    worldType[x][y] = TrackPeiceType.EMPTY;
                    rotations[x][y] = r.Next(4);

                    return false;

                default:
                    break;
            }

            return true;
        }

        return false;
    }

    bool IsHole(TrackPeice tp)
    {
        return HasTag(tp, TrackPeiceTags.HOLE);
    }

    bool HasTag(TrackPeice tp, TrackPeiceTags tag)
    {
        bool hasTag = false;

        foreach (TrackPeiceTags t in tp.tags)
        {
            if (t == tag)
            {
                hasTag = true;
                break;
            }
        }

        return hasTag;
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

        if (serverMode)
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

    void EnableServerObjects(bool enable)
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            if (child.name == "ServerObjects")
            {
                child.gameObject.SetActive(enable);
            }
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

        foreach (Collider mc in GetComponentsInChildren<Collider>())
        {
            mc.enabled = false;
        }

        worldCosts = new List<List<int>>();
        worldType = new List<List<TrackPeiceType>>();
        rotations = new List<List<int>>();
        prefabIndex = new List<List<int>>();
        trackPath = new List<int[]>();
        trackPeices = new List<List<GameObject>>();
        waterHeight = UnityEngine.Random.Range(waterHeightMin, waterHeightMax);
        float biomeXOffset = (float)r.NextDouble();
        float biomeYOffset = (float)r.NextDouble();

        noiseMap = CreateNoiseMap();
        treeInstances = new List<GameObject>();

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

        data.Serialize(rotations, prefabIndex, trackPath, waterHeight);

        return data;
    }

    public void LoadTrackData(GeneratedTrackData data)
    {
        InitializeTrack();

        data.Deserialize(out rotations, out prefabIndex, out trackPath, out waterHeight);

        InstantiateTrack();

        SetupStartPositions();
        SetupCheckpoints(trackPath);
        SetupRaceTrackController();
    }

    public void SetupWater()
    {
        if(waterObject != null)
        {
            waterObject.transform.position = new Vector3(waterObject.transform.position.x, waterHeight, waterObject.transform.position.z);
        }
    }
}

public enum TrackPeiceTags { HOLE, JUMP, OBSTACLE, BRIDGE, ANIMAL, LANDMARK };

[Serializable]
public class TrackPeice
{
    public TrackPeiceType type; 
    public GameObject prefab;
    public List<TrackPeiceTags> tags = new List<TrackPeiceTags>();
}

[Serializable]
public class GeneratedTrackData
{
    public List<IntList> rotations = new List<IntList>();
    public List<IntList> prefabIndex = new List<IntList>();
    public List<Point> trackPath = new List<Point>();
    public float waterHeight;

    public void Serialize(List<List<int>> rotations, List<List<int>> prefabIndex, List<int[]> trackPath, float waterHeight)
    {

        this.rotations = new List<IntList>();
        this.prefabIndex = new List<IntList>();
        this.trackPath = new List<Point>();
        this.waterHeight = waterHeight;

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

    public void Deserialize(out List<List<int>> rotations, out List<List<int>> prefabIndex, out List<int[]> trackPath, out float waterHeight)
    {

        rotations = new List<List<int>>();
        prefabIndex = new List<List<int>>();
        trackPath = new List<int[]>();
        waterHeight = this.waterHeight;

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