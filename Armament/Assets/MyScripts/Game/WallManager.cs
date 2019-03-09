using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class WallManager : MonoBehaviour
    {

        #region MonoBehaviour CallBacks

        // Start is called before the first frame update
        void Start()
        {
            // Put the dividing wall gameobject in the correct place in the transform hierarchy 
            // #Important: Without it, JumboTronDisplay cannot find the wall to get its wallDropTimer component!
            transform.SetParent(GameManager.Instance.environment.transform.Find("Scene Props").transform);
        }

        #endregion MonoBehaviour CallBacks
    }
}