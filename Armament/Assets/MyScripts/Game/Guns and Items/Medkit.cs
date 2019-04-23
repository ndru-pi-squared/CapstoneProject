using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    [RequireComponent(typeof(AudioSource))]
    public class Medkit : MonoBehaviourPun
    {
        public float healthThisMedkitWillRestore = 30.0f;
        public bool medkitWasPickedUp;
        public bool playerUsedMedkit;
        public float timer = 1.0f;
        [Tooltip("Keeps track of the countdown")]
        public float countdown;

        [Tooltip("The player who is holding the medkit. **This implementation might need revision**")]
        public PlayerManager playerWhoOwnsThisMedkit;
        // Start is called before the first frame update
        void Start()
        {

        }

        private void Awake()
        {
            playerWhoOwnsThisMedkit = null;
            playerUsedMedkit = false;
            countdown = timer;
        }
        // Update is called once per frame
        void Update()
        {
            if (medkitWasPickedUp)
            {
               
                photonView.RPC("DestroyRPC", RpcTarget.All);
                
            }
            if (playerWhoOwnsThisMedkit != null && playerUsedMedkit == false)
            {
                GetComponent<BoxCollider>().isTrigger = false;
                Use();
            }
            // Debug.Log("Update() called");
            if (playerUsedMedkit == true)//after the grenade has been thrown
            {
                Debug.Log("used medkit == true");
                countdown -= Time.deltaTime;
                if (countdown <= 0)
                {
                    RestoreHealth();
                    //PhotonNetwork.Destroy(this);
                }
            }
        }

        [PunRPC]
        void DestroyRPC()
        {
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.Destroy(this.gameObject);
        }

        void RestoreHealth()
        {
            Debug.Log("RestoreHealth() called");
            playerWhoOwnsThisMedkit.RestoreHealth(healthThisMedkitWillRestore);
        }

        public void Use()//called from playermanager. pulling into here makes it more modular in PlayerManager since theres a lot of cod ethere. Similar to shoot. 
        {
            playerUsedMedkit = true;
            //add up and forward forces to lob it
        }

        public bool IsReadyToUse()
        {
            return false; //how often can he throw?
        }

    }
}