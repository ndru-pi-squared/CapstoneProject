using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class AIEnemyWatcher : MonoBehaviour
    {
        private const bool DEBUG = true;
        private const bool DEBUG_OnBecameVisible = true;
        private const bool DEBUG_OnBecameInvisible = true;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// OnBecameVisible is called when the object became visible by any camera.
        /// This message is sent to all scripts attached to the renderer.OnBecameVisible and 
        /// OnBecameInvisible are useful to avoid computations that are only necessary when the object is visible.
        /// 
        /// Note that object is considered visible when it needs to be rendered in the Scene. 
        /// It might not be actually visible by any camera, but still need to be rendered for shadows for example. 
        /// Also, when running in the editor, the Scene view cameras will also cause this function to be called.
        /// </summary>
        private void OnBecameVisible()
        {
            // *** Note: We expect local player to be the only one with an active camera

            PlayerManager localPlayerPM = PlayerManager.LocalPlayerInstance.GetComponent<PlayerManager>();
            PlayerManager thisPM = transform.parent.parent.GetComponent<PlayerManager>();

            if (DEBUG && DEBUG_OnBecameVisible) Debug.LogFormat("AIEnemyWatcher: OnBecameVisible() this = [{0}]", thisPM);

            // If this player is not on the same team as the local player (the player with the camera)...
            if (!thisPM.GetTeam().Equals(localPlayerPM.GetTeam()))
            {
                if (DEBUG && DEBUG_OnBecameVisible) Debug.LogFormat("AIEnemyWatcher: OnBecameVisible() ADDED this = [{0}]", thisPM);
                localPlayerPM.EnemiesInView.Add(thisPM);
            }
        }

        /// <summary>
        /// OnBecameVisible is called when the object became visible by any camera.
        /// This message is sent to all scripts attached to the renderer.OnBecameVisible and 
        /// OnBecameInvisible are useful to avoid computations that are only necessary when the object is visible.
        /// 
        /// Note that object is considered visible when it needs to be rendered in the Scene. 
        /// It might not be actually visible by any camera, but still need to be rendered for shadows for example. 
        /// Also, when running in the editor, the Scene view cameras will also cause this function to be called.
        /// </summary>
        private void OnBecameInvisible()
        {
            // *** Note: We expect local player to be the only one with an active camera

            PlayerManager localPlayerPM = PlayerManager.LocalPlayerInstance.GetComponent<PlayerManager>();
            PlayerManager thisPM = transform.parent.parent.GetComponent<PlayerManager>();

            if (DEBUG && DEBUG_OnBecameVisible) Debug.LogFormat("AIEnemyWatcher: OnBecameVisible() this = [{0}]", thisPM);

            // If this player is not on the same team as the local player (the player with the camera)...
            if (!thisPM.GetTeam().Equals(localPlayerPM.GetTeam()))
            {
                if (DEBUG && DEBUG_OnBecameVisible) Debug.LogFormat("AIEnemyWatcher: OnBecameVisible() REMOVED this = [{0}]", thisPM);
                localPlayerPM.EnemiesInView.Remove(thisPM);
            }
        }
    }
}