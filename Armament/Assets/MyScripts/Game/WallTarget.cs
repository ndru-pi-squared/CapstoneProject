using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class WallTarget : MonoBehaviourPunCallbacks, ITarget
    {

        #region Public Fields
        /// <summary>
        /// OnWallIsDead delegate.
        /// </summary>
        public delegate void WallIsDead();

        /// <summary>
        /// Called when the timer has expired.
        /// </summary>
        public static event WallIsDead OnWallIsDead;

        // Key references for the Room CustomProperties hash table (so we don't use messy string literals)
        public const string KEY_WALL_HEALTH_TEAM_A = "Wall Health for Team A";
        public const string KEY_WALL_HEALTH_TEAM_B = "Wall Health for Team B";

        #endregion PublicFields

        #region Private Fields

        [SerializeField] private float teamA_health = 100f;
        [SerializeField] private float teamB_health = 100f;
        [Tooltip("Wall Side is either 1 or 2")]
        [SerializeField] private int wallSide = 1; // set this manually in inspector for side 2


        private const bool DEBUG = true; // indicates whether we are debugging this class
        private const bool DEBUG_TakeDamage = true;

        MeshRenderer teamA_meshRenderer;
        MeshRenderer teamB_meshRenderer;
        private float teamA_originalHealth; // keeps track of original health value
        private float teamB_originalHealth; // keeps track of original health value

        private ArrayList teamA_playersWhoHitWall;
        private ArrayList teamB_playersWhoHitWall;
        private int teamA_numberOfPlayersWhoCanHitWall = 0;
        private int teamB_numberOfPlayersWhoCanHitWall = 0;

        #endregion Private Fields

        #region MonoBehaviour CallBacks

        void Awake()
        {
            teamA_originalHealth = teamA_health;
            teamB_originalHealth = teamB_health;

            teamA_playersWhoHitWall = new ArrayList();
            teamB_playersWhoHitWall = new ArrayList();
            teamA_meshRenderer = transform.Find("Side 2").gameObject.GetComponent<MeshRenderer>();
            teamB_meshRenderer = transform.Find("Side 1").gameObject.GetComponent<MeshRenderer>();
            SyncWallHealth();
        }

        #endregion MonoBehaviour CallBacks

        #region MonoBehaviourPun Callbacks

        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {   
            if (!PhotonNetwork.IsMasterClient)
            {
                object value;
                // Get the current health of the wall from Room CustomProperties
                if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_WALL_HEALTH_TEAM_A, out value))
                {
                    teamA_health = (float)value;
                    UpdateWallColor();
                }

                // Get the current health of the wall from Room CustomProperties
                if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_WALL_HEALTH_TEAM_B, out value))
                {
                    teamA_health = (float)value;
                    UpdateWallColor();
                }
            }
        }

        #endregion MonoBehaviourPun Callbacks

        #region Public Methods

        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name="amount"></param>
        public void TakeDamage(float amount){}

        /// <summary>
        /// Wall Target logs who hits it and health will represent 
        /// </summary>
        /// <param name="amount">Not used</param>
        /// <param name="player"></param>
        public void TakeDamage(float amount, PlayerManager player)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                // Try to log player hit. 
                // If player hits the wall for the first time...
                if (TryLogPlayerHit(player))
                {
                    if (DEBUG && DEBUG_TakeDamage) Debug.LogFormat("WallTarget: TakeDamage() LOGGED PLAYER HIT player = [{0}]", player);

                    if (player.GetTeam().Equals(PlayerManager.VALUE_TEAM_NAME_A))
                    {
                        double threshold = .5001f; // fudged the threshold number to make the math work
                        teamA_numberOfPlayersWhoCanHitWall = CountNumberOfPlayersWhoCanHitWallSide();
                        int numberOfPlayersWhoNeedToHitWallToKillIt = Convert.ToInt32(Math.Ceiling(teamA_numberOfPlayersWhoCanHitWall * threshold));

                        int numberOfPlayersWhoHitWall = teamA_playersWhoHitWall.Count;

                        teamA_health = Convert.ToInt32(100 * (numberOfPlayersWhoNeedToHitWallToKillIt - numberOfPlayersWhoHitWall) / numberOfPlayersWhoNeedToHitWallToKillIt);
                        UpdateWallColor();

                        SyncWallHealth();

                        if (teamA_health <= 0)
                            OnWallIsDead?.Invoke();
                    }

                    if (player.GetTeam().Equals(PlayerManager.VALUE_TEAM_NAME_B))
                    {
                        double threshold = .5001f; // fudged the threshold number to make the math work
                        teamB_numberOfPlayersWhoCanHitWall = CountNumberOfPlayersWhoCanHitWallSide();
                        int numberOfPlayersWhoNeedToHitWallToKillIt = Convert.ToInt32(Math.Ceiling(teamB_numberOfPlayersWhoCanHitWall * threshold));

                        int numberOfPlayersWhoHitWall = teamB_playersWhoHitWall.Count;

                        teamB_health = Convert.ToInt32(100 * (numberOfPlayersWhoNeedToHitWallToKillIt - numberOfPlayersWhoHitWall) / numberOfPlayersWhoNeedToHitWallToKillIt);
                        UpdateWallColor();

                        SyncWallHealth();

                        if (teamB_health <= 0)
                            OnWallIsDead?.Invoke();
                    }
                }
            }
        }
        
        /// <summary>
        /// Logs who hit this wall target
        /// </summary>
        /// <param name="player">player to log</param>
        /// <returns>true if was not already logged; false if player was already logged.</returns>
        bool TryLogPlayerHit (PlayerManager player)
        {
            if (player.GetTeam().Equals(PlayerManager.VALUE_TEAM_NAME_A))
            {
                if (teamA_playersWhoHitWall.Contains(player))
                    return false;
                teamA_playersWhoHitWall.Add(player);
                return true;
            }

            if (player.GetTeam().Equals(PlayerManager.VALUE_TEAM_NAME_B))
            {
                if (teamB_playersWhoHitWall.Contains(player))
                    return false;
                teamB_playersWhoHitWall.Add(player);
                return true;
            }

            return false;
        }

        int CountNumberOfPlayersWhoCanHitWallSide()
        {
            int count = 0;
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                string team = ((GameObject)player.TagObject).GetComponent<PlayerManager>().GetTeam();
                if ((wallSide == 1 && team.Equals(PlayerManager.VALUE_TEAM_NAME_A)) || 
                    (wallSide == 2 && team.Equals(PlayerManager.VALUE_TEAM_NAME_B)))
                    count++;
            }
            return count;
        }

        public void ResetHealth()
        {
            // Reset wall health
            teamA_health = teamA_originalHealth;
            teamB_health = teamB_originalHealth;
            teamA_playersWhoHitWall = new ArrayList();
            teamB_playersWhoHitWall = new ArrayList();
            UpdateWallColor();

            SyncWallHealth();
        }

        #endregion Public Methods

        #region Private Methods

        void SyncWallHealth()
        {
            // If we are the master client...
            if (PhotonNetwork.IsMasterClient)
            {
                // Sync current wall health in Room CustomProperties
                PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable {
                    { KEY_WALL_HEALTH_TEAM_A, teamA_health },
                    { KEY_WALL_HEALTH_TEAM_B, teamB_health } });
            }
        }

        void UpdateWallColor()
        {
            // As the wall takes damage the color changes from white to black
            //meshRenderer.material.color = Color.HSVToRGB(0, 0, Math.Max(health, 0) / originalHealth);
            
            // Trying to make wall more transparent as health decreases - not yet successful
            float tickleMeElmoFactor = Math.Max(teamA_health, 0) / teamA_originalHealth;
            Color color = Color.HSVToRGB(0, 0, tickleMeElmoFactor);
            color.a = tickleMeElmoFactor;
            teamA_meshRenderer.material.color = color;

            tickleMeElmoFactor = Math.Max(teamB_health, 0) / teamB_originalHealth;
            color = Color.HSVToRGB(0, 0, tickleMeElmoFactor);
            color.a = tickleMeElmoFactor;
            teamB_meshRenderer.material.color = color;
        }

        #endregion Private Methods
    }
}
