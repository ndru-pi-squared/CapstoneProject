using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class WallDropAnimator : MonoBehaviour
    {
        [SerializeField] private WallDropTimer wallDropTimer;
        [Tooltip("Related to the time it takes for wall to drop the distance equal to its height")]
        [SerializeField] private float dropTime = 10;
        private Vector3 dropPosition; // stores the final position of the wall after it is dropped
        
        // Start is called before the first frame update
        void Start()
        {
            // Figure out what the final position of the wall should be after it is dropped
            float wallHeight = transform.localScale.y;
            dropPosition = transform.position - new Vector3(0f, wallHeight, 0f);
        }

        // Update is called once per frame
        void Update()
        {
            if (wallDropTimer.WallDropped)
            {
                // This code doesn't work as expected but I kind of like how the wall slows down as it drops...
                transform.position = Vector3.Lerp(transform.position, dropPosition, Time.deltaTime / dropTime);
            }
        }
    }
}