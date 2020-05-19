using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public enum AIGANN_STATE { IDLE, TRAINING, PLAY };

[Serializable]
public class AIGANN
{
    public AIGANN_STATE state = AIGANN_STATE.IDLE;

    List<Weights> subjects = new List<Weights>();

    public string weightsSaveName = "/aiWeights.dat";

    public int generation = 0;
    public int populationSize = 20;
    public int currentSubject = 0;

    // Chance for mutation per weight is 1 / mutation race
    public int mutationRate = 100;
    public float timeSinceTest = 0.0f;
    public float maxTestTime = 60.0f;

    public float lowVelocityTime = 0.0f;
    public float lowVelocity = 1.0f;
    public float lowVelocityMaxTime = 2.0f;

    public float maxImpact = 300.0f;

    public float visionDistance = 20.0f;

    public bool saveWeights = false;

    bool CheckFail(CarController car, ref int reason)
    {
        bool result = false;

        timeSinceTest += Time.fixedDeltaTime;

        if(timeSinceTest > maxTestTime)
        {
            result = true;
            reason = 1;
        }

        if(car.rb.velocity.magnitude < lowVelocity)
        {
            lowVelocityTime += Time.fixedDeltaTime;

            if(lowVelocityTime > lowVelocityMaxTime)
            {
                result = true;
                reason = 2;
            }
        } else
        {
            lowVelocityTime = 0.0f;
        }

        if(car.largestHitImpact > maxImpact)
        {
            result = true;
            reason = 3;
        }

        return result;
    }

    Weights SelectParent(List<Weights> parents)
    {
        float totalScore = 0.0f;

        foreach(Weights p in parents)
        {
            // Sum scores;
            totalScore += p.score;
        }

        float targetScore = UnityEngine.Random.Range(0.0f, totalScore);

        float addedScore = 0.0f;

        for(int i = 0; i < parents.Count; ++i)
        {
            addedScore += parents[i].score;

            if(parents[i].score <= targetScore)
            {
                return parents[i];
            }
        }

        return parents[0];
    }

    public void Update(CarController car, CheckPoint targetCheckpoint, CheckPoint startingCheckpoint, PlayerEntity e, List<CheckPoint> checkpoints, out float[] carInput)
    {
        if(saveWeights)
        {
            SaveWeights();
            saveWeights = false;
        }

        carInput = null;
        if(state == AIGANN_STATE.TRAINING)
        {
            if(subjects.Count != populationSize)
            {
                Debug.Log("Creating new subjects");
                subjects = new List<Weights>(populationSize);

                for(int i = 0; i < populationSize; ++i)
                {
                    subjects.Add(new Weights());
                }
            }

            int failReason = 0;
            if(CheckFail(car, ref failReason))
            {
                //subjects[currentSubject].score = e.lapScore;
                float disCheckpointToCheckPoint = Vector3.Distance(checkpoints[e.checkpoint].t.position, checkpoints[(e.checkpoint + 1) % checkpoints.Count].t.position);
                float disCheckpointToCar = Vector3.Distance(car.transform.position, checkpoints[(e.checkpoint + 1) % checkpoints.Count].t.position);

                subjects[currentSubject].score = (e.checkpoint + checkpoints.Count * e.lap) + Mathf.Max((disCheckpointToCheckPoint - disCheckpointToCar) / disCheckpointToCheckPoint, 0.0f);

                if(failReason == 1)
                {
                    subjects[currentSubject].score *= 0.5f;
                }

                // reset car

                car.rb.velocity = new Vector3();
                car.rb.angularVelocity = new Vector3();

                car.transform.rotation = startingCheckpoint.t.rotation;
                car.transform.position = startingCheckpoint.t.position;
                car.largestHitImpact = 0.0f;

                // reset fail timers
                timeSinceTest = 0.0f;
                lowVelocityTime = 0.0f;

                // reset lap & checkpoint state
                e.checkpoint = 0;
                e.lap = 0;
                e.lapScore = 0;

                currentSubject++;

                if(currentSubject == populationSize)
                {
                    subjects.Sort((x, y) => y.score.CompareTo(x.score));

                    foreach(Weights s in subjects)
                    {
                        // Make all scores positive
                        s.score += Mathf.Abs(subjects[subjects.Count - 1].score);
                        Debug.Log(s.score);
                    }

                    Debug.Log("Generation " + generation + " completed play...");

                    List<Weights> children = new List<Weights>();

                    while (children.Count != subjects.Count) {
                        Weights parentA = SelectParent(subjects);
                        Weights parentB = SelectParent(subjects);

                        children.Add(Weights.Mutate(Weights.Mix(parentA, parentB), mutationRate));
                    }

                    subjects = children;

                    currentSubject = 0;
                    generation++;
                }
            }

            float[] input = new float[16];
            GenerateInput(car, targetCheckpoint, input);

            carInput = NeuralNetwork.Forward(subjects[currentSubject], input);
        }
    }

    public void GenerateInput(CarController car, CheckPoint targetCheckpoint, float[] input)
    {
        int inputIndex = 0;

        /*
        // Car vel
        input[inputIndex++] = car.rb.velocity.x;
        input[inputIndex++] = car.rb.velocity.y;
        input[inputIndex++] = car.rb.velocity.z;

        // Car rot vel
        input[inputIndex++] = car.rb.angularVelocity.x;
        input[inputIndex++] = car.rb.angularVelocity.y;
        input[inputIndex++] = car.rb.angularVelocity.z;

        */

        // 9 raycasts
        int layerMask = ~LayerMask.GetMask("Water");
        for (int i = 0; i < 9; ++i)
        {
            RaycastHit hit;
            Vector3 direction = car.transform.TransformDirection(Quaternion.AngleAxis(i * (180.0f / 8.0f) - 90, Vector3.up) * Vector3.forward);

            if (car.physicsScene.Raycast(car.transform.position, direction, out hit, visionDistance, layerMask, QueryTriggerInteraction.Ignore))
            {
                Debug.DrawRay(car.transform.position, direction * hit.distance, Color.yellow);
                input[inputIndex++] = hit.distance / visionDistance;
            }
            else
            {
                input[inputIndex++] = 1.0f;
                Debug.DrawRay(car.transform.position, direction * visionDistance, Color.yellow);
            }
        }

        // Angle to next checkpoint
        Vector3 targetDir = targetCheckpoint.t.position - car.transform.position;
        Vector3 forward = car.transform.forward;

        input[inputIndex++] = Vector3.SignedAngle(targetDir, forward, Vector3.up) / 180f;
        //Debug.Log("Angle: " + input[input.Length - 1]);
    }

    public void LoadWeights()
    {
        string destination = Application.persistentDataPath + weightsSaveName;
        FileStream file;

        if (File.Exists(destination)) file = File.OpenRead(destination);
        else
        {
            Debug.LogError("AI Weights File not found");
            return;
        }

        BinaryFormatter bf = new BinaryFormatter();
        subjects = (List<Weights>)bf.Deserialize(file);
        file.Close();
    }

    public void SaveWeights()
    {
        string destination = Application.persistentDataPath + weightsSaveName;
        FileStream file;

        if (File.Exists(destination))
        {
            file = File.OpenWrite(destination);
        }
        else
        {
            file = File.Create(destination);
        }

        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(file, subjects);
        file.Close();

        Debug.Log("Saved AI Weights at " + destination);
    }

    public static float Sigmoid(float value)
    {
        return (1.0f / (1.0f + Mathf.Exp(-value))) - 0.5f;
    }
}

[Serializable]
public class Weights
{
    // Must match layers in NeuralNetwork class
    static int[] layers = { 16, 9, 5, 3 };
    public float[] weights;
    static float weightRange = 1000.0f;
    public float score = 0.0f;

    public Weights()
    {
        int numberOfWeights = 0;

        for(int i = 0; i < layers.Length; ++i)
        {
            if(i != layers.Length - 1)
            {
                numberOfWeights += layers[i] * layers[i + 1];
            }
        }

        weights = new float[numberOfWeights];

        for(int i = 0; i < numberOfWeights; ++i)
        {
            weights[i] = UnityEngine.Random.Range(-weightRange, weightRange);
        }
    }

    public static Weights Mix(Weights a, Weights b)
    {
        Weights child = new Weights();
        System.Random r = new System.Random();

        for(int i = 0; i < a.weights.Length; ++i)
        {
            if (r.Next(0, 2) == 0)
            {
                child.weights[i] = a.weights[i];
            } else
            {
                child.weights[i] = b.weights[i];
            }
        }

        return child;
    }

    public static Weights Mutate(Weights w, int rate)
    {
        System.Random r = new System.Random();

        for (int i = 0; i < w.weights.Length; ++i)
        {
            if (r.Next(0, rate) == 0)
            {
                w.weights[i] = UnityEngine.Random.Range(-weightRange, weightRange);
            }
        }

        return w;
    }
}

public static class NeuralNetwork
{
    // Must match layers in Weights class
    static int[] layers = { 16, 9, 5, 3 };

    static float GetWeight(Weights weights, ref int weightIndex)
    {
        return weights.weights[weightIndex++];
    }

    public static void ForwardLayer(float[] input, float[] output, Weights weights, ref int weightIndex)
    {
        for (int o = 0; o < output.Length; ++o)
        {
            for (int i = 0; i < input.Length; ++i)
            {
                output[o] += input[i] * GetWeight(weights, ref weightIndex);
            }

            output[o] = AIGANN.Sigmoid(output[o]);
        }
    }

    public static float[] Forward(Weights weights, float[] inputs)
    {
        int weightIndex = 0;

        // Input to hidden layer 1

        float[] layerOneValues = new float[layers[1]];

        ForwardLayer(inputs, layerOneValues, weights, ref weightIndex);

        // hidden layer 1 to 2

        float[] layerTwoValues = new float[layers[2]];

        ForwardLayer(layerOneValues, layerTwoValues, weights, ref weightIndex);

        // hidden layer 2 to 3

        float[] layerThreeValues = new float[layers[3]];

        ForwardLayer(layerTwoValues, layerThreeValues, weights, ref weightIndex);

        // hidden layer 3 to output

        return layerThreeValues;
    }
}