using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class JumboTronDisplay : MonoBehaviour
    {
        [SerializeField] WallDropTimer wallDropTimer;
        [SerializeField] TextMeshPro stageTextMeshPro;
        [SerializeField] TextMeshPro infoTextMeshPro;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            // If wall has not dropped... 
            if (!wallDropTimer.WallDropped)
            {
                // Display stage "Stage 1"
                stageTextMeshPro.text = "Stage 1";
                // Display Timer info
                infoTextMeshPro.text = "Time Left: " + wallDropTimer.TimeLeft.ToString();
            }
            else
            {
                // Display stage "Stage 2"
                stageTextMeshPro.text = "Stage 2";
                // Display "Fight!" instruction
                infoTextMeshPro.text = "Fight!";
            }
        }
    }
}