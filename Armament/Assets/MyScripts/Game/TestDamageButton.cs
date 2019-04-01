using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class TestDamageButton : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void TestDamage()
        {
            PlayerManager player = PlayerManager.LocalPlayerInstance.GetComponent<PlayerManager>();
            player.TakeDamage(10f, player);
        }
    }
}