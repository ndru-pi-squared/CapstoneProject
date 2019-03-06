using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class PlayerNetworkMover : MonoBehaviourPunCallbacks, IPunObservable
    {
        [Tooltip("Player camera whose vertical rotation we want to sync over network")]
        [SerializeField] Camera playerCamera;

        private Vector3 position;
        private Quaternion rotation; // holds camera rotation info from network
        private bool jump;
        private float smoothing = 10.0f;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (!photonView.IsMine)
            {
                //transform.position = Vector3.Lerp(transform.position, position, Time.deltaTime * smoothing);
                playerCamera.transform.rotation = Quaternion.Lerp(playerCamera.transform.rotation, rotation, Time.deltaTime * smoothing);
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            // I'm trying to sync the vertical rotation of the camera on the player over the network
            if (stream.IsWriting)
            {
                //stream.SendNext(transform.position);
                stream.SendNext(playerCamera.transform.rotation);
            }
            else
            {
                //position = (Vector3)stream.ReceiveNext();
                rotation = (Quaternion)stream.ReceiveNext();
            }
        }
    }
}