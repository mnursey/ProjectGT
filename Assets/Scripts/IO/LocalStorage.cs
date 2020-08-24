using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class LocalStorage
{

    public static void SaveFile(string fileName, string content)
    {
        string destination = Application.persistentDataPath + "/" + fileName;

        Debug.Log("Saving file to " + destination);

        FileStream file;

        if(File.Exists(destination))
        {
            file = File.OpenWrite(destination);
        } else
        {
            file = File.Create(destination);
        }

        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(file, content);
        file.Close();

    }

    public static bool LoadFile(string fileName, out string data)
    {
        string destination = Application.persistentDataPath + "/" + fileName;
        data = "";

        Debug.Log("Loading file from " + destination);

        FileStream file;

        if(File.Exists(destination))
        {
            file = File.OpenRead(destination);
        } else
        {
            return false;
        }

        BinaryFormatter bf = new BinaryFormatter();

        data = (string)bf.Deserialize(file);
        file.Close();

        return true;
    }
    
}
