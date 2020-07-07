using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TrackPeiceType { EMPTY, START, STRAIGHT, LEFT, RIGHT };

[ExecuteInEditMode]
public class TrackGenerator : MonoBehaviour
{

    public List<TrackPeice> trackPeicePrefabs = new List<TrackPeice>();

    public int generationWidth = 6;

    public List<List<int>> worldCosts = new List<List<int>>();
    public List<List<TrackPeiceType>> worldType = new List<List<TrackPeiceType>>();

    public System.Random r = new System.Random();

    [TextArea(6, 20)]
    [Tooltip("WorldCosts View")]
    public string worldCostDebug = "";

    [TextArea(6, 20)]
    [Tooltip("WorldType View")]
    public string worldTypeDebug = "";

    public bool generateTrack = false;

    public int targetX = 10;
    public int targetY = 10;

    public void Start()
    {

    }

    public void Update()
    {
        if(generateTrack)
        {
            GenerateTrack();

            List<List<int[]>> paths = GetMinPaths(new int[] { generationWidth / 2, generationWidth / 2 });

            int[] target = new int[] { 5, 5 };
            while(paths[target[0]][target[1]][0] != generationWidth / 2 && paths[target[0]][target[1]][1] != generationWidth / 2)
            {
                worldType[target[0]][target[1]] = TrackPeiceType.STRAIGHT;
                target = paths[target[0]][target[1]];
                Debug.Log("new : " + target[0] + " " + target[1]);
            }

            DebugWorldCosts();
            DebugWorldType();

            generateTrack = false;
        }
    }

    public void GenerateTrack()
    {
        for(int x = 0; x < generationWidth; ++x)
        {
            List<int> c = new List<int>();
            List<TrackPeiceType> t = new List<TrackPeiceType>();

            for (int y = 0; y < generationWidth; ++y)
            {
                c.Add(r.Next());
                t.Add(TrackPeiceType.EMPTY);
            }

            worldCosts.Add(c);
            worldType.Add(t);
        }

        worldType[generationWidth / 2][generationWidth / 2] = TrackPeiceType.START;
        worldType[generationWidth / 2][generationWidth / 2 - 1] = TrackPeiceType.STRAIGHT;

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

    int GetPathCost(int[] end, List<List<int[]>> parentNodes, int depth)
    {
        if(depth > 100)
        {
            Debug.Log("Deep depth");
            return depth;
        }

        if(parentNodes[end[0]][end[1]][0] == -1 && parentNodes[end[0]][end[1]][1] == -1)
        {
            return 0;
        } else
        {
            return worldCosts[end[0]][end[1]] + GetPathCost(parentNodes[end[0]][end[1]], parentNodes, depth + 1);
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

        int iter = 0;
        while(frontier.Count > 0)
        {

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

            Debug.Log(iter++);

            int lowestIndex = FindLowestPointInFrontier(frontier);
            int[] lowest = frontier[lowestIndex];
            frontier.RemoveAt(lowestIndex);

            neighbours = GetNeighbours(lowest);

            for(int i = 0; i < neighbours.Count; ++i)
            {
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
                        if (GetPathCost(neighbours[i], parentNodes, 0) > GetPathCost(lowest, parentNodes, 0) + worldCosts[neighbours[i][0]][neighbours[i][1]])
                        {
                            parentNodes[neighbours[i][0]][neighbours[i][1]] = new int[] { lowest[0], lowest[1] };
                        }
                    }
                }
            }

            if(iter > 600)
            {
                break;
            }
        }

        return parentNodes;
    }

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

    public void LoadTrack(string data)
    {

    }
}

[Serializable]
public class TrackPeice
{
    public TrackPeiceType type; 
    public GameObject prefab;
}
