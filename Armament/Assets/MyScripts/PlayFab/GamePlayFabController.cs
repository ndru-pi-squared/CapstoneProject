using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GamePlayFabController : MonoBehaviour
{
    public static GamePlayFabController GPFC;
    public SceneManager SM;

    public string username;

    private void OnEnable()
    {
        if (GamePlayFabController.GPFC == null)
        {
            GamePlayFabController.GPFC = this;
        }
        else
        {
            if (GamePlayFabController.GPFC != this)
            {
                Destroy(this.gameObject);
            }
        }
        DontDestroyOnLoad(this.gameObject);
    }

    public void Start()
    {
        //Note: Setting title Id here can be skipped if you have set the value in Editor Extensions already.
        if (string.IsNullOrEmpty(PlayFabSettings.TitleId))
        {
            PlayFabSettings.TitleId = "E5D9";
        }

        //Get the user's username from the account info and set the resultant string's username property to be this user's username
        GetAccountInfoRequest request = new GetAccountInfoRequest();
        PlayFabClientAPI.GetAccountInfo(request, OnGetAccountInfoSuccess, OnPlayFabCallbackError);

        playerKillCountThisGame = 0;
        playerDeathCountThisGame = 0;
        playerRoundWinsThisGame = 0;
        //setStats();
        //getStats();
    }

    //resultant method used to set the player's username
    public void OnGetAccountInfoSuccess(GetAccountInfoResult result)
    {
        username = result.AccountInfo.Username;
    }

    //error if request for account info fails
    public void OnPlayFabCallbackError(PlayFabError error)
    {
        Debug.Log(error);
    }

    #region PlayerStats

    private int playerKillCountThisGame;
    private int playerTotalKills;

    private int playerDeathCountThisGame;
    private int playerTotalDeaths;

    private int playerRoundWinsThisGame;
    private int playerTotalRoundWins;

    public void setStats()
    {
        PlayFabClientAPI.UpdatePlayerStatistics(new UpdatePlayerStatisticsRequest
        {
            // request.Statistics is a list, so multiple StatisticUpdate objects can be defined if required.
            Statistics = new List<StatisticUpdate> {
            new StatisticUpdate { StatisticName = "PlayerKillCount", Value = playerTotalKills + playerKillCountThisGame  },
            new StatisticUpdate { StatisticName = "PlayerDeathCount", Value = playerTotalDeaths + playerDeathCountThisGame  },
            new StatisticUpdate { StatisticName = "PlayerRoundWins", Value = playerTotalRoundWins + playerRoundWinsThisGame }
            }
        },
        result => { Debug.Log("User statistics updated"); },
        error => { Debug.LogError(error.GenerateErrorReport()); });
    }

    void getStats()
    {
        PlayFabClientAPI.GetPlayerStatistics(
            new GetPlayerStatisticsRequest(),
            OnGetStatistics,
            error => Debug.LogError(error.GenerateErrorReport())
        );
    }

    void OnGetStatistics(GetPlayerStatisticsResult result)
    {
        Debug.Log("Received the following Statistics:");
        foreach (var eachStat in result.Statistics)
        {
            Debug.Log("Statistic (" + eachStat.StatisticName + "): " + eachStat.Value);
            switch (eachStat.StatisticName)
            {
                case "PlayerKillCount":
                    playerTotalKills = eachStat.Value;
                    break;
                case "PlayerDeathCount":
                    playerTotalDeaths = eachStat.Value;
                    break;
                case "PlayerRoundWins":
                    playerTotalRoundWins = eachStat.Value;
                    break;
            }
        }
    }

    // Build the request object and access the API
    public void StartCloudUpdatePlayerStats()
    {
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
        {
            FunctionName = "UpdatePlayerStats", // Arbitrary function name (must exist in your uploaded cloud.js file)
            FunctionParameter = new { pKillCount = playerTotalKills + playerKillCountThisGame, 
                                      pDeathCount = playerTotalDeaths + playerDeathCountThisGame,
                                      pRoundWins = playerTotalRoundWins + playerRoundWinsThisGame }, // The parameter provided to your function
            GeneratePlayStreamEvent = true, // Optional - Shows this event in PlayStream
        }, OnCloudUpdateStats, OnErrorShared);
    }

    private static void OnCloudUpdateStats(ExecuteCloudScriptResult result)
    {
        // Cloud Script returns arbitrary results, so you have to evaluate them one step and one parameter at a time
        Debug.Log(JsonWrapper.SerializeObject(result.FunctionResult));
        JsonObject jsonResult = (JsonObject)result.FunctionResult;
        object messageValue;
        jsonResult.TryGetValue("messageValue", out messageValue); // note how "messageValue" directly corresponds to the JSON values set in Cloud Script
        Debug.Log((string)messageValue);
    }

    private static void OnErrorShared(PlayFabError error)
    {
        Debug.Log(error.GenerateErrorReport());
    }

    public void IncrementKillCount()
    {
        Debug.Log("Incrementing Kill Count...");
        playerKillCountThisGame++;
        StartCloudUpdatePlayerStats();
    }

    public void IncrementDeathCount()
    {
        Debug.Log("Incrementing Death Count...");
        playerDeathCountThisGame++;
        StartCloudUpdatePlayerStats();
    }

    public void IncrementRoundWins()
    {
        Debug.Log("Incrementing Round Wins...");
        playerRoundWinsThisGame++;
        StartCloudUpdatePlayerStats();
    }

    #endregion PlayerStats
}
