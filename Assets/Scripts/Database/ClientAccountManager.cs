﻿using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientAccountManager : MonoBehaviour
{
    public int accountType = -1; // 0 -> steam, 1 -> local
    public ulong accountID = 0; 
    public bool loggedIn;

    public AccountData accountData;

    private string localAccountDataLocation = "localAccount.data";
    public ClientController cc;
    public MenuController mc;
    public bool test;
    // Detect if has steam account -> if true use steam account (When logging into steam account, if new account register account)
    // Detect if has local account -> if true use local account 
    // Else -> Create local account -> log into new account

    // Start is called before the first frame update

    void Awake()
    {
        if(cc == null)
        {
            cc = GetComponent<ClientController>();
        }
    }

    void Start()
    {
        LoadAccount();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void LoadAccount()
    {
        bool result = LoadSteamAccount();
        bool newAccount = false;

        if(!result)
        {
            result = LoadLocalAccount();

            if(!result)
            {
                CreateLocalAccount();
                Debug.Log("Client Requesting new account");
                newAccount = true;
            }
        }

        if(!newAccount)
            Login();
    }

    bool LoadLocalAccount()
    {
        string data;
        bool result = LocalStorage.LoadFile(localAccountDataLocation, out data);

        if(result)
        {
            accountID = ulong.Parse(data);
            accountType = 1;
        }

        return result;
    }

    bool LoadSteamAccount()
    {
        CSteamID id;
        bool result = SteamScript.GetSteamLocalAccount(out id);

        if(result)
        {
            accountID = ulong.Parse(id.ToString());
            accountType = 0;
        }

        return result;
    }

    bool CreateLocalAccount()
    {
        cc.CreateNewAccount(CreateLocalAccountCallback);
        return true;
    }

    void CreateLocalAccountCallback(ulong accountID, int accountType)
    {
        this.accountID = accountID;
        this.accountType = accountType;
        LocalStorage.SaveFile(localAccountDataLocation, accountID.ToString());
        Login();
    }

    void Login()
    {
        cc.Login(accountID, accountType, LoginCallback);
    }

    void LoginCallback(AccountData ad)
    {
        if(ad != null)
        {
            Debug.Log("Logged in as " + ad.accountID + " " + ad.accountType + " Coins " + ad.coins);

            accountData = ad;

            // Todo
            // Refactor this...
            mc.usernameOption.text = ad.accountName;
            mc.rc.selectedCarModel = accountData.selectedCarID;

            loggedIn = true;
        } else
        {
            // Todo handle this...
            Debug.LogWarning("Account data was null...");
        }
    }
}
