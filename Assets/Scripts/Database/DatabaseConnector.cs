using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Data.SqlClient;
using System.Data;

public class DatabaseConnector : MonoBehaviour
{
    public string host;
    public string user;
    public string password;


    public bool Query(string cmd, out DataSet ds)
    {
        try
        {
            // Build connection string
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = host;
            builder.UserID = user;
            builder.Password = password;
            builder.InitialCatalog = "master";

            SqlDataAdapter adapter = new SqlDataAdapter();
            ds = new DataSet();

            // Connect to SQL
            Debug.Log("Connecting to SQL Server ... ");
            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                connection.Open();
                Debug.Log("Done.");

                using (SqlCommand command = new SqlCommand(cmd, connection))
                {
                    adapter.SelectCommand = command;
                    adapter.Fill(ds);
                    adapter.Dispose();
                }
            }
        }
        catch (SqlException e)
        {
            Debug.LogWarning(e.ToString());
            ds = null;
            return false;
        }

        return true;
    }

    public void AddAccount(ulong accountID, int accountType, string username)
    {
        string cmd = String.Format("INSERT INTO [ProjectGT].[dbo].[Accounts] (AccountID, AccountType, AccountName) VALUES ({0}, {1}, '{2}');", accountID, accountType, username);
        DataSet ds;

        Query(cmd, out ds);
    }

    public DataSet GetAccount(ulong accountID, int accountType)
    {
        string cmd = String.Format("select * from [ProjectGT].[dbo].[Accounts] where AccountID = {0} and AccountType = {1};", accountID, accountType);

        DataSet ds;

        Query(cmd, out ds);
        return ds;
    }

    public DataSet UpdateAccountStats(ulong accountID, int accountType, int numWinsDelta, int scoreDelta)
    {
        string cmd = String.Format("update [ProjectGT].[dbo].[Accounts] set NumRaces = NumRaces + 1, NumWins = NumWins + {2}, score = score + {3} where AccountID = {0} and AccountType = {1};", accountID, accountType, numWinsDelta, scoreDelta);

        DataSet ds;

        Query(cmd, out ds);
        return ds;
    }

    public DataSet UpdateSelectedCar(ulong accountID, int accountType, int carID)
    {
        string cmd = String.Format("update [ProjectGT].[dbo].[Accounts] set SelectedCarID = {2} where AccountID = {0} and AccountType = {1};", accountID, accountType, carID);

        DataSet ds;

        Query(cmd, out ds);
        return ds;
    }

    public DataSet GetUsersOrderedByScore(int row, int depth)
    {
        string cmd = String.Format("select * from[ProjectGT].[dbo].[Accounts] order by score desc offset {0} rows fetch next {1} rows only; ", row, depth);

        DataSet ds;

        Query(cmd, out ds);
        return ds;
    }
}
