using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class MobileJumpButton : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Jump()
        {
            CrossPlatformInputManager.SetButtonDown("Jump");
            CrossPlatformInputManager.SetButtonUp("Jump");
        }
    }
}