using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientAccountManager : MonoBehaviour
{
    public int accountType = -1; // 0 -> steam, 1 -> local
    public int accountID = -1; 
    public bool loggedIn;

    private string localAccountDataLocation = "localAccount.data";
    public ClientController cc;
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

    }

    // Update is called once per frame
    void Update()
    {
        if(test)
        {
            LoadAccount();
            test = false;
        }
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
            accountID = int.Parse(data);
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
            accountID = int.Parse(id.ToString());
            accountType = 0;
        }

        return result;
    }

    bool CreateLocalAccount()
    {
        cc.CreateNewAccount(CreateLocalAccountCallback);
        return true;
    }

    void CreateLocalAccountCallback(int accountID, int accountType)
    {
        this.accountID = accountID;
        this.accountType = accountType;
        LocalStorage.SaveFile(localAccountDataLocation, accountID.ToString());
        Login();
    }

    void Login()
    {
        // Todo
        // Send request to login server...
    }
}
