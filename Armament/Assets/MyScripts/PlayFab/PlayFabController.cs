using System.Collections.Generic;
using Photon.Pun;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayFabController : MonoBehaviour
{
    public static PlayFabController PFC;

    private string userEmail;
    private string userPassword;
    private string username;
    public GameObject loginPanel;
    public GameObject addLoginPanel;
    public GameObject recoverButton;
    public SceneManager SM;
    [Tooltip("The UI Label in the Authentication section to inform the user that the connection is in progress")]
    [SerializeField] private GameObject authProgressLabel;

    private void OnEnable() {
        if (PlayFabController.PFC == null) {
            PlayFabController.PFC = this;
        }
        else { 
            if (PlayFabController.PFC != this) {
                Destroy(this.gameObject);
            }
        }
        DontDestroyOnLoad(this.gameObject);
    }

    public void Start()
    {
        //Used currently to counteract the autologin setting
        PlayerPrefs.DeleteAll();

        //Note: Setting title Id here can be skipped if you have set the value in Editor Extensions already.
        if (string.IsNullOrEmpty(PlayFabSettings.TitleId))
        {
            PlayFabSettings.TitleId = "E5D9";
        }
        //var request = new LoginWithCustomIDRequest { CustomId = "GettingStartedGuide", CreateAccount = true };
        //PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);


        if (PlayerPrefs.HasKey("EMAIL"))
        {
            userEmail = PlayerPrefs.GetString("EMAIL");
            userPassword = PlayerPrefs.GetString("PASSWORD");
            var request = new LoginWithEmailAddressRequest { Email = userEmail, Password = userPassword };
            PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
        }
        else {
#if UNITY_ANDROID
            var requestAndroid = new LoginWithAndroidDeviceIDRequest { AndroidDeviceId = returnMobileID(), CreateAccount = true };
            PlayFabClientAPI.LoginWithAndroidDeviceID(requestAndroid, OnLoginMobileSuccess, OnLoginMobileFailure);
#endif
#if UNITY_IOS
            var requestIOS = new LoginWithIOSDeviceIDRequest { DeviceId = returnMobileID(), CreateAccount = true };
            PlayFabClientAPI.LoginWithIOSDeviceID(requestIOS, OnLoginMobileSuccess, OnLoginMobileFailure);
#endif
        }
              
    }

    #region Login
    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("Login Success!");
        PlayerPrefs.SetString("EMAIL", userEmail);
        PlayerPrefs.SetString("PASSWORD", userPassword);
        loginPanel.SetActive(false);
        recoverButton.SetActive(false);
        getStats();

        //Move the camera to reveal the new input options
        var moveScript = GameObject.Find("Main Camera").GetComponent<CameraMove>();
        moveScript.Move();
    }

    private void OnLoginMobileSuccess(LoginResult result)
    {
        Debug.Log("Mobile Login Success!");
        getStats();
        loginPanel.SetActive(false);

        //Move the camera to reveal the new input options
        var moveScript = GameObject.Find("Main Camera").GetComponent<CameraMove>();
        moveScript.Move();
    }

    private void onRegisterSuccess(RegisterPlayFabUserResult result) {
        Debug.Log("Registration Success!");
        PlayerPrefs.SetString("EMAIL", userEmail);
        PlayerPrefs.SetString("PASSWORD", userPassword);


        //Move the camera to reveal the new input options
        var moveScript = GameObject.Find("Main Camera").GetComponent<CameraMove>();
        moveScript.Move();
    }

    #region Login
    private void OnLoginSuccess(LoginResult result)
    {
        authProgressLabel.GetComponent<Text>().text = "Login Success!";
        Debug.Log("Login Success!");
        PlayerPrefs.SetString("EMAIL", userEmail);
        PlayerPrefs.SetString("PASSWORD", userPassword);
        loginPanel.SetActive(false);
        getStats();

        //Move the camera to reveal the new input options
        //var moveScript = GameObject.Find("Main Camera").GetComponent<CameraMove>();
        //moveScript.MoveBack();
        //instead, we will launch our Launcher scene
        PhotonNetwork.LoadLevel("Launcher");//TODO: change hardcoded string
    }

    private void OnLoginMobileSuccess(LoginResult result)
    {
        authProgressLabel.GetComponent<Text>().text = "Mobile Login Success!";
        Debug.Log("Mobile Login Success!");
        getStats();
        loginPanel.SetActive(false);

        //Move the camera to reveal the new input options
        var moveScript = GameObject.Find("Main Camera").GetComponent<CameraMove>();
        moveScript.MoveBack();
    }

    private void onRegisterSuccess(RegisterPlayFabUserResult result) {
        authProgressLabel.GetComponent<Text>().text = "Registration Success!";
        Debug.Log("Registration Success!");
        PlayerPrefs.SetString("EMAIL", userEmail);
        PlayerPrefs.SetString("PASSWORD", userPassword);

        PlayFabClientAPI.UpdateUserTitleDisplayName(new UpdateUserTitleDisplayNameRequest { DisplayName = username }, OnDisplayName, OnLoginMobileFailure);
        getStats();
        loginPanel.SetActive(false);

        //Move the camera to reveal the new input options
        var moveScript = GameObject.Find("Main Camera").GetComponent<CameraMove>();
        moveScript.MoveBack();
    }

    void OnDisplayName(UpdateUserTitleDisplayNameResult result) {
        Debug.Log(result.DisplayName + " is your new display name");
    }

    #region PlayerStats

    public int playerKillCount;

    public void setStats() { 
        PlayFabClientAPI.UpdatePlayerStatistics( new UpdatePlayerStatisticsRequest {
            // request.Statistics is a list, so multiple StatisticUpdate objects can be defined if required.
            Statistics = new List<StatisticUpdate> {
            new StatisticUpdate { StatisticName = "PlayerKillCount", Value = playerKillCount }
            }
        },
        result => { Debug.Log("User statistics updated"); },
        error => { Debug.LogError(error.GenerateErrorReport()); });
    }

    void getStats() {
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
            switch(eachStat.StatisticName) {
                case "PlayerKillCount":
                    playerKillCount = eachStat.Value;
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
            FunctionParameter = new { pKillCount = playerKillCount}, // The parameter provided to your function
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

    #endregion PlayerStats

    private void onRegisterFailure(PlayFabError error) {
        Debug.LogError(error.GenerateErrorReport());
    }

    public void GetUserEmail(string emailIn) {
        userEmail = emailIn;
    }

    public void getUserPassword(string passwordIn) {
        userPassword = passwordIn;
    }

    public void getUsername(string usernameIn) {
        username = usernameIn;
    }

    public void onClickLogin() {
        authProgressLabel.GetComponent<Text>().text = "Authenticating...";
        var request = new LoginWithEmailAddressRequest { Email = userEmail, Password = userPassword };
        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
    }

    public static string returnMobileID() {
        string deviceID = SystemInfo.deviceUniqueIdentifier;
        return deviceID;
    }

    public void openAddLogin() {
        addLoginPanel.SetActive(true);

        //Move the camera to reveal the new input options
        var moveScript = GameObject.Find("Main Camera").GetComponent<CameraMove>();
        moveScript.MoveForward();
    }

    public void onClickAddLogin() {
        addLoginPanel.SetActive(false);
        var request = new LoginWithEmailAddressRequest { Email = userEmail, Password = userPassword };
        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnLoginFailure);
    }

    private void onAddLoginSuccess(AddUsernamePasswordResult result)
    {
        authProgressLabel.GetComponent<Text>().text = "Login Success!";
        Debug.Log("Login Success!");
        PlayerPrefs.SetString("EMAIL", userEmail);
        PlayerPrefs.SetString("PASSWORD", userPassword);
        getStats();
        addLoginPanel.SetActive(false);

        //Move the camera to reveal the new input options
        var moveScript = GameObject.Find("Main Camera").GetComponent<CameraMove>();
        moveScript.MoveBack();
    }
    #endregion Login

    #region PlayerStats

    public int playerKillCount;

    public void setStats() { 
        PlayFabClientAPI.UpdatePlayerStatistics( new UpdatePlayerStatisticsRequest {
            // request.Statistics is a list, so multiple StatisticUpdate objects can be defined if required.
            Statistics = new List<StatisticUpdate> {
            new StatisticUpdate { StatisticName = "PlayerKillCount", Value = playerKillCount }
            }
        },
        result => { Debug.Log("User statistics updated"); },
        error => { Debug.LogError(error.GenerateErrorReport()); });
    }

    void getStats() {
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
            switch(eachStat.StatisticName) {
                case "PlayerKillCount":
                    playerKillCount = eachStat.Value;
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
            FunctionParameter = new { pKillCount = playerKillCount}, // The parameter provided to your function
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

    #endregion PlayerStats


    public GameObject leaderboardPanel;
    public GameObject listingPrefab;
    public Transform listingContainer;

    #region Leaderboard
    public void GetLeaderboard() {
        var requestLeaderboard = new GetLeaderboardRequest { StartPosition = 0, StatisticName = "PlayerKillCount", MaxResultsCount = 20 };
        PlayFabClientAPI.GetLeaderboard(requestLeaderboard, onGetLeaderboard, onErrorLeaderboard);
    }

    void onGetLeaderboard(GetLeaderboardResult result) {
        leaderboardPanel.SetActive(true);
        //Debug.Log(result.Leaderboard[0].StatValue);
        foreach(PlayerLeaderboardEntry player in result.Leaderboard) {
            GameObject tempListing = Instantiate(listingPrefab, listingContainer);
            LeaderboardListing LL = tempListing.GetComponent<LeaderboardListing>();
            LL.playerNameText.text = player.DisplayName;
            LL.playerScoreText.text = player.StatValue.ToString();
            Debug.Log(player.DisplayName + ": " + player.StatValue);
        }
    }

    public void closeLeaderboardPanel() {
        leaderboardPanel.SetActive(false);
        for(int i = listingContainer.childCount - 1; i >= 0; i--) {
            Destroy(listingContainer.GetChild(i).gameObject);
        }
    }

    void onErrorLeaderboard(PlayFabError error) {
        Debug.LogError(error.GenerateErrorReport());
    }

    #endregion Leaderboard
}