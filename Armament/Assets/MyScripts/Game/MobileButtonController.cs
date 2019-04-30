using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class MobileButtonController : MonoBehaviour
    {
        private bool isChatOpen = false;
        private bool isStatsOpen = false;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void ToggleChat()
        {
            if(isChatOpen)
            {
                // close chat
                CrossPlatformInputManager.SetButtonUp("Chat");
                isChatOpen = false;
            } else
            {
                // open chat
                CrossPlatformInputManager.SetButtonDown("Chat");
                isChatOpen = true;
            }
            
        }

        public void ToggleStats()
        {
            if (isStatsOpen)
            {
                // close stats
                CrossPlatformInputManager.SetButtonUp("Toggle Stats");
                isStatsOpen = false;
            }
            else
            {
                // open stats
                CrossPlatformInputManager.SetButtonDown("Toggle Stats");
                isStatsOpen = true;
            }
        }

        public void Jump()
        {
            CrossPlatformInputManager.SetButtonDown("Jump");
            CrossPlatformInputManager.SetButtonUp("Jump");
        }

        public void UseHealth()
        {
            CrossPlatformInputManager.SetButtonDown("Health");
            CrossPlatformInputManager.SetButtonUp("Health");
        }

        public void ThrowGrenade()
        {
            CrossPlatformInputManager.SetButtonDown("Grenade");
            CrossPlatformInputManager.SetButtonUp("Grenade");
        }

        public void ToggleAi()
        {
            CrossPlatformInputManager.SetButtonDown("Toggle AI");
            CrossPlatformInputManager.SetButtonUp("Toggle AI");
        }

        public void CycleGun()
        {
            CrossPlatformInputManager.SetButtonDown("Cycle Gun");
            CrossPlatformInputManager.SetButtonUp("Cycle Gun");
        }
    }
}