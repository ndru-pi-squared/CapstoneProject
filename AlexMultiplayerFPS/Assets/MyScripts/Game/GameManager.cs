using System;
using System.Collections;


using UnityEngine;
using UnityEngine.SceneManagement;


using Photon.Pun;
using Photon.Realtime;


namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    /// <summary>this is a sample summary created with GhostDoc</summary>
    public class GameManager : MonoBehaviourPunCallbacks
    {

        #region Public Fields 


        public static GameManager Instance;

        [Tooltip("The prefab to use for representing the local player")]
        public GameObject PlayerPrefab; // used to instantiate the player pref on PhotonNetwork

        [Tooltip("The prefab to use for representing the weapons")]
        public GameObject[] weapons; // used to instantiate the weapons on PhotonNetwork

        #endregion


        #region Private Fields 

        [SerializeField]
        private Transform[] playerSpawnPoints; // list of locations where a player can be spawned

        [SerializeField]
        private Transform[] weaponSpawnPoints; // list of locations where a weapon can be spawned

        #endregion


        #region Public Methods


        /// <Summary> 
        /// Should be called when a user chooses to leave the room 
        /// </Summary>
        public void LeaveRoom()
        {
            Debug.Log("GameManger: LeaveRoom() called.");
            
            // Leave the photon game room
            PhotonNetwork.LeaveRoom();

            // Make sure the cursor is visible again and not locked
            // This code did not work as expected... 
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }


        #endregion


        #region Private Methods


        private void Start()
        {
            Instance = this; if (PlayerPrefab == null)
            {
                Debug.LogError("<Color=Red><a>Missing</a></Color> PlayerPrefab Reference. Please set it up in GameObject 'Game Manager'", this);
            }
            else
            {
                /** Note from tutorial:
                 *  With this, we now only instantiate if the PlayerManager doesn't have a reference to an existing instance of localPlayer
                 */
                if (PlayerManager.LocalPlayerInstance == null)
                {
                    /** My comment: 
                     *   Below is the code from the tutorial but Application.loadLevelName is deprecated.
                     *   Debug.LogFormat("We are Instantiating LocalPlayer from {0}", Application.loadedLevelName);
                     *   I replaced it with a version that is not depricated.
                     */
                    Debug.LogFormat("We are Instantiating LocalPlayer from {0}", SceneManager.GetActiveScene().name);

                    // we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
                    // old spawn point: new Vector3(0f, 5f, 0f)
                    // old rotation: Quaternion.identity
                    Transform playerSpawnPoint = playerSpawnPoints[new System.Random().Next(playerSpawnPoints.Length)];
                    GameObject myPlayerGO = PhotonNetwork.Instantiate(this.PlayerPrefab.name, playerSpawnPoint.position, playerSpawnPoint.rotation, 0);
                    
                    // We need to enable all the controlling components for the local player so we're not controlling other players
                    myPlayerGO.GetComponent<Animator>().enabled = true;
                    myPlayerGO.GetComponent<CharacterController>().enabled = true;
                    myPlayerGO.GetComponent<AudioSource>().enabled = true;
                    //myPlayerGO.GetComponent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>().enabled = false;
                    myPlayerGO.GetComponent<Com.Kabaj.TestPhotonMultiplayerFPSGame.FirstPersonController>().enabled = true;
                    myPlayerGO.GetComponent<PlayerAnimatorManager>().enabled = true;
                    myPlayerGO.GetComponent<PlayerManager>().enabled = true;
                    myPlayerGO.GetComponentInChildren<Camera>().enabled = true;
                    myPlayerGO.GetComponentInChildren<AudioListener>().enabled = true;
                    myPlayerGO.GetComponentInChildren<FlareLayer>().enabled = true;
                    // Disable scene camera
                    Camera.main.enabled = false;

                    // Instantiate our two weapons at different spawn points for team A
                    PhotonNetwork.Instantiate(this.weapons[0].name, weaponSpawnPoints[0].position, weaponSpawnPoints[0].rotation, 0);
                    PhotonNetwork.Instantiate(this.weapons[1].name, weaponSpawnPoints[1].position, weaponSpawnPoints[1].rotation, 0);
                    // Instantiate our two weapons at different spawn points for team B
                    PhotonNetwork.Instantiate(this.weapons[0].name, weaponSpawnPoints[2].position, weaponSpawnPoints[2].rotation, 0);
                    PhotonNetwork.Instantiate(this.weapons[1].name, weaponSpawnPoints[3].position, weaponSpawnPoints[3].rotation, 0);

                }
                else
                {
                    Debug.LogFormat("Ignoring scene load for {0}", SceneManagerHelper.ActiveSceneName);
                }
            }
        }

        // Note: only called in OnPlayerEnteredRoom() and OnPlayerLeftRoom() by player on master client
        void LoadArena()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogError("PhotonNetwork : Trying to Load a level but we are not the master Client");
            }
            Debug.LogFormat("PhotonNetwork : Loading Level : {0}", PhotonNetwork.CurrentRoom.PlayerCount);
            // Excerpt from tutorial:
            // There are two things to watch out for here, it's very important.
            // - PhotonNetwork.LoadLevel() should only be called if we are the MasterClient. So we check first that we are the MasterClient using 
            //   PhotonNetwork.IsMasterClient. It will be the responsibility of the caller to also check for this, we'll cover that in the next part of this section.
            //
            // - We use PhotonNetwork.LoadLevel() to load the level we want, we don't use Unity directly, because we want to rely on Photon 
            //   to load this level on all connected clients in the room, since we've enabled PhotonNetwork.AutomaticallySyncScene for this Game.
            //PhotonNetwork.LoadLevel("Room for " + PhotonNetwork.CurrentRoom.PlayerCount);
            PhotonNetwork.LoadLevel("Room for 1");
        }


        #endregion


        #region Photon Callbacks


        ///<summary>
        /// Called when the local player left the room. We need to load the launcher scene.
        /// </summary>
        public override void OnLeftRoom()
        {
            // Load the launcher scene
            SceneManager.LoadScene(0);
        }

        // Note: I think this function is called on every computer when someone enters the room except the person who is entering the room
        public override void OnPlayerEnteredRoom(Player other)
        {
            Debug.LogFormat("OnPlayerEnteredRoom() {0}", other.NickName); // not seen if you're the player connecting

            // Note: I think this is only 
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.LogFormat("OnPlayerEnteredRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom
                
                //LoadArena();
            }
        }

        // Note: I think this function is called on every computer when someone leaves the room except the person who is leaves the room
        public override void OnPlayerLeftRoom(Player other)
        {
            Debug.LogFormat("OnPlayerLeftRoom() {0}", other.NickName); //seen when other disconnects

            if (PhotonNetwork.IsMasterClient)
            {
                Debug.LogFormat("OnPlayerLeftRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom

                //LoadArena();
            }
        }


        #endregion
    }
}
