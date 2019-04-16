using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class AIGunWatcher : MonoBehaviour
    {
        private const bool DEBUG = false;
        private const bool DEBUG_OnBecameVisible = true;
        private const bool DEBUG_OnBecameInvisible = true;
        
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

            Debug.Log("---------A");
            if (PlayerManager.LocalPlayerInstance == null)
            {
                Debug.Log("---------B");
                return;
            }

            PlayerManager localPlayerPM = PlayerManager.LocalPlayerInstance.GetComponent<PlayerManager>();

            // Go up GO hierarchy looking for the first Gun script
            Gun thisGun = null;
            Transform parentTransform = transform.parent;
            while ((thisGun = parentTransform.GetComponent<Gun>()) == null && parentTransform.parent!=null)
            {
                parentTransform = parentTransform.parent;
            }

            if (DEBUG && DEBUG_OnBecameVisible) Debug.LogFormat("AIGunWatcher: OnBecameVisible() thisGun = [{0}]", thisGun);

            // Gets a vector that points from the player's position to the target's.
            var heading = thisGun.transform.position - localPlayerPM.gameObject.GetComponentInChildren<Camera>().transform.position;
            //heading.y = 0;  // This is the overground heading.
            var distance = heading.magnitude;
            var direction = heading / distance; // This is now a normalized direction

            bool objectIsInSightOfPlayer = false;

            // If cast hit something...
            if (Physics.Raycast(localPlayerPM.gameObject.GetComponentInChildren<Camera>().transform.position, direction, out RaycastHit hit, 1000))
            {
                //Debug.LogFormat("AIGunWatcher: OnBecameVisible() HIT thisGun.transform = [{0}], hit.transform = [{1}]", thisGun.transform, hit.transform);
                if (!hit.transform.Equals(GameManager.Instance.DividingWallGO.transform))
                {
                    objectIsInSightOfPlayer = true;
                }
            }



            // If this gun is not owned by a player AND not a showgun...
            if (!thisGun.IsOwned && !thisGun.IsShowGun && objectIsInSightOfPlayer)
            {
                localPlayerPM.UnclaimedGunsInView.Add(thisGun);
                //if (DEBUG && DEBUG_OnBecameVisible)
                Debug.LogFormat("AIGunWatcher: OnBecameVisible() ADDED thisGun = [{0}] localPlayerPM.UnclaimedGunsInView.Count = [{1}]", thisGun, localPlayerPM.UnclaimedGunsInView.Count);
            }
            else
                Debug.LogFormat("AIGunWatcher: OnBecameVisible() GUN ALREADY OWNED OR IS SHOWGUN thisGun = [{0}] OR objectIsInSightOfPlayer = [{1}]", thisGun, objectIsInSightOfPlayer);
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

            if (PlayerManager.LocalPlayerInstance == null)
                return;

            PlayerManager localPlayerPM = PlayerManager.LocalPlayerInstance.GetComponent<PlayerManager>();

            // Go up GO hierarchy looking for the first Gun script
            Gun thisGun = null;
            Transform parentTransform = transform.parent;
            while ((thisGun = parentTransform.GetComponent<Gun>()) == null && parentTransform.parent != null)
            {
                parentTransform = parentTransform.parent;
            }

            if (DEBUG && DEBUG_OnBecameVisible) Debug.LogFormat("AIGunWatcher: OnBecameInvisible() thisGun = [{0}]", thisGun);

            // If this gun is not owned by a player...
            if (!thisGun.IsOwned)
            {
                localPlayerPM.UnclaimedGunsInView.Remove(thisGun);
                //if (DEBUG && DEBUG_OnBecameVisible)
                Debug.LogFormat("AIGunWatcher: OnBecameInvisible() REMOVED thisGun = localPlayerPM.UnclaimedGunsInView.Count = [{1}]", thisGun, localPlayerPM.UnclaimedGunsInView.Count);
            }
        }
    }
}
