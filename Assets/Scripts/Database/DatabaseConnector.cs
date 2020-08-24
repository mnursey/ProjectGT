using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Data.SqlClient;

public class DatabaseConnector : MonoBehaviour
{
    public bool test;
    public string host;
    public string user;
    public string password;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(test)
        {
            string testQuery = "SELECT TOP (1000) * FROM[ProjectGT].[dbo].[Accounts]";
            Query(testQuery);
            test = false;
        }
    }

    public bool Query(string cmd)
    {
        try
        {
            // Build connection string
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = host;
            builder.UserID = user;
            builder.Password = password;
            builder.InitialCatalog = "master";

            // Connect to SQL
            Debug.Log("Connecting to SQL Server ... ");
            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                connection.Open();
                Debug.Log("Done.");

                using (SqlCommand command = new SqlCommand(cmd, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Debug.Log("Row: " + reader.GetInt32(0) + " AccountID: " + reader.GetInt32(1));
                        }
                    }
                }
            }
        }
        catch (SqlException e)
        {
            Debug.LogWarning(e.ToString());
            return false;
        }

        return true;
    }

    public void AddAccount(int accountID, int accountType)
    {
        string cmd = String.Format("INSERT INTO [ProjectGT].[dbo].[Accounts] (AccountID, AccountType) VALUES ({0}, {1});", accountID, accountType);
        Query(cmd);
    }
}
