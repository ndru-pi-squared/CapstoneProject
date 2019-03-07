using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class JumboTronDisplay : MonoBehaviour
    {
        [SerializeField] TextMeshPro stageTextMeshPro;
        [SerializeField] TextMeshPro infoTextMeshPro;
        
        // Update is called once per frame
        void Update()
        {
            // Find the wallDropTimer on the wall. 
            // transform.root will be the transform of the Environment gameobject because:
            // 1) We expect the jumbotron to be a child of Environment gameobject.
            // 2) We expect the team dividing wall to be a component in a child of Environment gameobject as well.
            WallDropTimer wallDropTimer = transform.root.GetComponentInChildren<WallDropTimer>();

            /*WallDropTimer wallDropTimer = null;
            // Find Original Dividing Wall GO transform
            foreach (GameObject rootGO in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (rootGO.name.Contains("Original Dividing Wall"))
                {
                    wallDropTimer = rootGO.transform.GetComponent<WallDropTimer>();
                    break;
                }
            }*/
            
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

        Transform findEnvironmentGOTransform()
        {
            return null;
        }
    }
}