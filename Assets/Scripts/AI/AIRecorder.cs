using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[Serializable]
public class AIRecorder
{

    List<AIDatapoint> datapoints = new List<AIDatapoint>();

    public int numberOfPicks = 10;

    public float posWeight = 30.0f;
    public float posVelWeight = 5.0f;
    public float rotWeight = 10.0f;
    public float rotVelWeight = 5.0f;

    public float minVel = 5.0f;

    public void LoadDatapoints()
    {
        string destination = Application.persistentDataPath + "/aiDatapoints.dat";
        FileStream file;

        if (File.Exists(destination)) file = File.OpenRead(destination);
        else
        {
            Debug.LogError("AI Datapoints File not found");
            return;
        }

        BinaryFormatter bf = new BinaryFormatter();
        datapoints = (List<AIDatapoint>)bf.Deserialize(file);
        file.Close();
    }

    void ScoreDatapoint(AIDatapoint datapoint, Vector3 pos, Vector3 posVel, Vector3 rot, Vector3 rotVel)
    {
        float score = 0.0f;

        float posValue = Vector3.Distance(pos, datapoint.pos.GetValue()) * posWeight;
        float posVelValue = Vector3.Distance(posVel, datapoint.posVel.GetValue()) * posVelWeight;

        float rotValue = Vector3.Distance(rot, datapoint.rot.GetValue()) * rotWeight;
        float rotVelValue = Vector3.Distance(rotVel, datapoint.rotVel.GetValue()) * rotVelWeight;

        score = Mathf.Sqrt( Mathf.Pow(posValue, 2f) + Mathf.Pow(posVelValue, 2f) + Mathf.Pow(rotValue, 2f) + Mathf.Pow(rotVelValue, 2f));

        if(datapoint.posVel.GetValue().magnitude < minVel)
        {
            score = float.MaxValue;
        }

        datapoint.SetScore(score);
    }

    public void RetreiveInput(Vector3 pos, Vector3 posVel, Quaternion rot, Vector3 rotVel, out float steeringInput, out float accelerationInput, out float brakingInput)
    {
        Vector3 eulerRot = rot.eulerAngles;

        foreach(AIDatapoint dp in datapoints)
        {
            ScoreDatapoint(dp, pos, posVel, eulerRot, rotVel);
        }

        float minScore = datapoints.Min(dp => dp.GetScore());
        AIDatapoint bestDatapoint = datapoints.Find(dp => dp.GetScore() == minScore);

        steeringInput = bestDatapoint.steeringInput;
        accelerationInput = bestDatapoint.accelerationInput;
        brakingInput = bestDatapoint.brakingInput;
    }

    public void NewDatapoint(Vector3 pos, Vector3 posVel, Quaternion rot, Vector3 rotVel, float steeringInput, float accelerationInput, float brakingInput)
    {
        datapoints.Add(new AIDatapoint(pos, posVel, rot, rotVel, steeringInput, accelerationInput, brakingInput));
    }

    public void SaveDatapoints()
    {
        string destination = Application.persistentDataPath + "/aiDatapoints.dat";
        FileStream file;

        if (File.Exists(destination))  {
            file = File.OpenWrite(destination);
        } else
        {
            file = File.Create(destination);
        }

        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(file, datapoints);
        file.Close();

        Debug.Log("Saved AI datapoints at " + destination);
    }
}

[Serializable]
public class AIDatapoint
{
    public SVector3 pos;
    public SVector3 posVel;
    public SVector3 rot;
    public SVector3 rotVel;

    public float steeringInput = 0.0f;
    public float accelerationInput = 0.0f;
    public float brakingInput = 0.0f;

    float score = float.MaxValue;

    public AIDatapoint(Vector3 pos, Vector3 posVel, Quaternion rot, Vector3 rotVel, float steeringInput, float accelerationInput, float brakingInput)
    {
        this.pos = new SVector3(pos);
        this.posVel = new SVector3(posVel);
        this.rot = new SVector3(rot.eulerAngles);
        this.rotVel = new SVector3(rotVel);
        this.steeringInput = steeringInput;
        this.accelerationInput = accelerationInput;
        this.brakingInput = brakingInput;
    }

    public float GetScore()
    {
        return score;
    }

    public void SetScore(float score)
    {
        this.score = score;
    }
}