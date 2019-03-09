using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{ 
    
    public class Launcher : MonoBehaviourPunCallbacks
    {
        #region Private Serializable Fields

        // *This variable is temporary and just to be used during early development. 
        // I was running into long build times when including the original (very large) unity scene file in the build.
        // Using this variable to quickly change what scene the master client code will load and removing all scenes 
        // except for Launcher and Simple Room from the build will hopefully keep build times down and make debugging
        // multiplayer-related code (that requires running a built second instance of the game) a lot easier.
        // Currently, this information is also accessed in the GameManager class to determine the correct transform
        // for the team dividing wall prefab during instantiation in the Start method.
        [Tooltip("The game level (unity scene) to load. See comments in code for more information.")]
        [SerializeField] public static string developmentOnly_levelToLoad = "Simple Room";

        [Tooltip("The UI Panel to let the user enter name, connect and play")]
        [SerializeField] private GameObject controlPanel;
        [Tooltip("The UI Label to inform the user that the connection is in progress")]
        [SerializeField] private GameObject progressLabel;
        [SerializeField] private InputField roomNameInputField;
        [SerializeField] private ScrollRect existingRoomList;
        
        /// <summary>
        /// The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created.
        /// </summary>
        [Tooltip("The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created")]
        [SerializeField] private byte maxPlayersPerRoom = 4;

        #endregion Private Serializable Fields
            
        #region Private Fields

        /// <summary>
        /// This client's version number. Users are separated from each other by gameVersion (which allows you to make breaking changes).
        /// </summary>
        string gameVersion = "1";

        List<RoomInfo> _roomList;

        /// <summary>
        /// An InputField to store a reference to the players. There is no null checking, players can enter room w/ blank name. This may change when we implement playfab. 
        /// </summary>
        //private InputField PlayerName;//make read only?

        /// <summary>
        /// Keep track of the current process. Since connection is asynchronous and is based on several callbacks from Photon,
        /// we need to keep track of this to properly adjust the behavior when we receive call back by Photon.
        /// Typically this is used for the OnConnectedToMaster() callback.
        /// </summary>
        bool isConnecting;
        bool isJoiningNamedRoom = false;

        #endregion Private Fields


        #region MonoBehaviour CallBacks

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
        /// </summary>
        private void Awake()
        {
            // #Critical
            // this makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during initialization phase.
        /// </summary>
        void Start()
        {
            //progressLabel.SetActive(false);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;//frees up the cursor
            controlPanel.SetActive(true);
            
        }


        #endregion


        #region Public Methods

        /// <summary>
        /// Lists the existing rooms.
        /// </summary>
        public void ListExistingRooms()
        {
            Debug.Log("Launcher: ListExistingRooms()");
            
            // Join the Default TypedLobby
            PhotonNetwork.JoinLobby(TypedLobby.Default);

            // Get the contents of the existing rooms list
            RectTransform scrollableContent = existingRoomList.content;
            VerticalLayoutGroup verticalLayoutGroup = scrollableContent.GetComponent<VerticalLayoutGroup>();
            
            
        }


        /// <summary>
        /// Start the connection process.
        /// - If already connected, we attempt joining a random room
        /// - if not yet connected, Connect this application instance to Photon Cloud Network
        /// </summary>
        public void Connect()
        {
            Debug.Log("Launcher: Connect()");
            //keep track of the will to join a room, because when we come back from the game we will get a callback that we are connected, so we need to know what to do then
            isConnecting = true;

            progressLabel.GetComponent<Text>().text = "Connecting...";

            //progressLabel.SetActive(true);
            //controlPanel.SetActive(false);

            /** My Note:
             *   I'm going to try to force connection to 'us' region for testing purposes.
             *   If my two clients are in different regions they cannot enter the same room.
             *   ...
             *   Looks like I didn't need this code. The reason clients weren't ending up in the same rooms was because
             *   they were connecting to different regional servers. I changed the whitelist to "us;" only. This seems
             *   to be a good solution for testing but would need to change before publishing.
             */

            /*// we check if we are connected or not, we join if we are, else we initiate the connection to the server.
            if (PhotonNetwork.IsConnected)
            {
                // #Critical we need at this point to attempt joining a Random Room. If it fails, we'll get notified in OnJoinRandomFailed() and we'll create one.
                PhotonNetwork.JoinRandomRoom();
            }
            else
            {
                // #Critical, we must first and foremost connect to Photon Online Server.
                PhotonNetwork.GameVersion = gameVersion;
                PhotonNetwork.ConnectUsingSettings();
            }*/

            // #Critical, we must first and foremost connect to Photon Online Server.
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
        }

        public void JoinRandomRoom()
        {
            if (PhotonNetwork.IsConnected)
            {
                progressLabel.GetComponent<Text>().text = "Joining Random Room...";
                // #Critical we need at this point to attempt joining a Random Room. If it fails, we'll get notified in OnJoinRandomFailed() and we'll create one.
                PhotonNetwork.JoinRandomRoom();

                /*PlayerName = (InputField)GameObject.FindWithTag("PlayerName").GetComponent<InputField>();
                Debug.Log("Launcher: JoinRandomRoom()");
                if (PlayerName.text != "" || PlayerName.text != " ") //right now we're checking for either null or an accidental space. lets see if we can refine this
                {
                    
                }*/
                //else, tell user please enter name and reprompt?
            }
            else
            {
                Connect();
            }
        }

        /// <summary>
        /// Join or create room without expected users
        /// </summary>
        public void JoinOrCreateRoom()
        {
            // Join or create room (with no expected users)
            JoinOrCreateRoom(null);
        }

        /// <summary>
        /// Join or create room with expected users
        /// </summary>
        /// <param name="expectedUsers">The expected users.</param>
        public void JoinOrCreateRoom(string[] expectedUsers)
        {
            Debug.Log("Launcher: JoinOrCreateRoom(string[] expectedUsers)");
            progressLabel.GetComponent<Text>().text = "Joining or Creating Room...";

            // In case we are not yet connected, keep track of what we're trying to do with isJoiningNamedRoom...
            // We will try to connect first. Then this function will be called again.
            isJoiningNamedRoom = true;

            // Check that we are connected to master server before we attempt to join a room.
            // If we're not connected to a master server...
            if (!PhotonNetwork.IsConnected)
            {
                // 
                progressLabel.GetComponent<Text>().text = "Not yet connected!";
                Debug.Log("Launcher: JoinOrCreateRoom(string[] expectedUsers) Not yet connected! ");
                // Try to connect
                Connect();
                // Do not continue to trying to join a room. We will wait for the OnConnectedToMaster callback to call this function again
                return;
            }

            // Get the roomName from the GUI input field
            string roomName = roomNameInputField.text;
            
            // Create a new RoomOptions
            RoomOptions roomOptions = new RoomOptions();
            
            // Set room options
            //roomOptions.CustomRoomPropertiesForLobby = { "map", "ai" };
            //roomOptions.CustomRoomProperties = new Hashtable() { { "map", 1 } };
            //roomOptions.MaxPlayers = expectedMaxPlayers;
            
            // Join or create room (w/out expected users)
            PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default, expectedUsers);
        }

        #endregion


        #region MonoBehaviourPunCallbacks Callbacks

        public override void OnJoinedLobby()
        {
            Debug.Log("Launcher: OnJoinedLobby()");
            progressLabel.GetComponent<Text>().text = "Joined Lobby!";
            progressLabel.SetActive(true);
        }
        
        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            Debug.Log("Launcher: OnRoomListUpdate()");
            _roomList = roomList;
            foreach (RoomInfo game in _roomList)
            {
                Debug.LogFormat("Launcher: ListExisitingRooms() game.Name = {0}, game.PlayerCount = {1}, game.MaxPlayers{2}", game.Name, game.PlayerCount, game.MaxPlayers);
            }
        }

        public override void OnConnectedToMaster()
        {
            Debug.Log("PUN Basics Tutorial/Launcher: OnConnectedToMaster() was called by PUN");
            progressLabel.GetComponent<Text>().text = "Connected to Master server!";

            // we don't want to do anything if we are not attempting to join a room.
            // this case where isConnecting is false is typically when you lost or quit the game, when this level is loaded, OnConnectedToMaster will be called, in that case
            // we don't want to do anything.
            if (isConnecting)
            {
                // If user wanted to join a specific room but was not yet connected to master server...
                // (the JoinOrCreateRoom() function will have set isJoiningNamedRoom to true, tried to connect to master server, and then returned)
                if (isJoiningNamedRoom)
                {
                    // We need to call JoinOrCreateRoom back to finish its work
                    JoinOrCreateRoom();
                }
                // If user did not want to join a specific room...
                else
                {
                    //#Critical: The first we try to do is to join a potential existing room. If there is, good, else, we'll be called back with OnJoinRandomFailed()
                    PhotonNetwork.JoinRandomRoom();
                }
            }
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            //progressLabel.SetActive(false);
            //controlPanel.SetActive(true);

            Debug.LogWarningFormat("PUN Basics Tutorial/Launcher: OnDisconnect() was called by PUN with reason {0}", cause);
            progressLabel.GetComponent<Text>().text = string.Format("Disconnected: {0}", cause);
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log("PUN Basics Tutorial/Launcher:OnJoinRandomFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");
            progressLabel.GetComponent<Text>().text = "No random room available, so we create one....";
            
            // #Critical: we failed to join a random room, maybe none exists or they are all full. No worries, we create a new room.
            PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayersPerRoom });
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("PUN Basicis Tutorial/Launcher: OnJoinedRoom() called by PUN. Now this client is in a room.");
            progressLabel.GetComponent<Text>().text = "Now this client is in a room!";
            
            // #Critical: We only load if we are the first player, else we rely on 'PhotonNetwork.AutomaticallySyncScene' to sync our instance scene.
            if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
            {
                Debug.Log("We load the 'Room for 1'");
                progressLabel.GetComponent<Text>().text = "Loading the 'Room for 1' map...";

                // #Critical
                // Load the Room Level.
                PhotonNetwork.LoadLevel(developmentOnly_levelToLoad);
            }
        }

        #endregion
    }
}