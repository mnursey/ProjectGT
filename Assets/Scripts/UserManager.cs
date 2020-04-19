using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class UserManager : MonoBehaviour
{
    public List<UserReference> users = new List<UserReference>();

    public void AddUser(string username, int networkID)
    {
        users.Add(new UserReference(username, networkID));
    }

    public UserReference GetUserFromNetworkID(int networkID)
    {
        UserReference user = null;

        if(networkID > -1)
        {
            user = users.Find(x => x.networkID == networkID);
        }

        return user; 
    }

    public UserManagerState GetState()
    {
        return new UserManagerState(users);
    }

    public void SetState(UserManagerState ums)
    {
        users = ums.users;
    }
}

[Serializable]
public class UserReference
{
    public string username;
    public int networkID;

    public UserReference(string username, int networkID)
    {
        this.username = username;
        this.networkID = networkID;
    }
}

[Serializable]
public class UserManagerState
{
    public List<UserReference> users = new List<UserReference>();

    public UserManagerState (List<UserReference> users)
    {
        this.users = users;
    }
}
