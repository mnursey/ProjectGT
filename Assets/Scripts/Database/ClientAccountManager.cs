using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientAccountManager : MonoBehaviour
{
    public int accountType; // 0 -> steam, 1 -> local
    public int accountID; 
    public bool loggedIn;

    // Detect if has steam account -> if true use steam account (When logging into steam account, if new account register account)
    // Detect if has local account -> if true use local account 
    // Else -> Create local account -> log into new account

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadAccount()
    {
        bool result = LoadSteamAccount();

        if(!result)
        {
            result = LoadLocalAccount();

            if(!result)
            {
                CreateLocalAccount();
                LoadLocalAccount();
            }
        }

        Login();
    }

    bool LoadLocalAccount()
    {
        return false;
    }

    bool LoadSteamAccount()
    {
        return false;
    }

    bool CreateLocalAccount()
    {
        return false;
    }

    void Login()
    {

    }
}
