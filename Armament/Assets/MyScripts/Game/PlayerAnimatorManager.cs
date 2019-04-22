using UnityEngine;
using System.Collections;
using Photon.Pun;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class PlayerAnimatorManager : MonoBehaviourPun
    {

        #region Private Serializable Fields

        [SerializeField] private float directionDampTime = 0.25f;

        #endregion Private Serializable Fields

        #region Private Fields

        private Animator animator;

        #endregion Private Fields

        #if MOBILE_INPUT
            public LeftJoystick leftJoystick; // the game object containing the LeftJoystick script
            private Vector3 leftJoystickInput; // holds the input of the Left Joystick
        #endif

        #region MonoBehaviour Callbacks

        // Start is called before the first frame update
        void Start()
        {
            animator = GetComponent<Animator>();
            if (!animator)
            {
                Debug.LogError("PlayerAnimatorManager is Missing Animator Component", this);
            }
        }

        // Update is called once per frame
        void Update()
        {
            /** Notes from tutorial:
             * Ok, photonView.IsMine will be true if the instance is controlled by the 'client' application, meaning this 
             * instance represents the physical person playing on this computer within this application. So if it is false, 
             * we don't want to do anything and solely rely on the PhotonView component to synchronize the transform and 
             * animator components we've setup earlier. But, why having then to enforce PhotonNetwork.IsConnected == true 
             * in our if statement? eh eh :) because during development, we may want to test this prefab without being 
             * connected. In a dummy scene for example, just to create and validate code that is not related to networking 
             * features per se. And so with this additional expression, we will allow input to be used if we are not 
             * connected. It's a very simple trick and will greatly improve your workflow during development.
             */
            if (photonView && photonView.IsMine == false && PhotonNetwork.IsConnected == true)
            {
                return;
            }

            if (!animator)
            {
                return;
            }

            float h;
            float v;
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

#if MOBILE_INPUT
            if (!leftJoystick)
            {
                return;
            }
            // get input from left joystick
            leftJoystickInput = leftJoystick.GetInputDirection();
            if (stateInfo.IsName("Base Layer.Run"))
            {
                // When using trigger parameter
                //if (Input.GetButtonDown("Fire2"))
                //{
                //    animator.SetTrigger("Jump");
                //}
            }
            h = leftJoystickInput.x; // The horizontal movement from joystick 01
            v = leftJoystickInput.y; // The vertical movement from joystick 01
#endif

#if !MOBILE_INPUT
            // deal with Jumping
            // only allow jumping if we are running.
            if (stateInfo.IsName("Base Layer.Run"))
            {
                // When using trigger parameter
                if(Input.GetButtonDown("Fire2"))
                {
                    animator.SetTrigger("Jump");
                }
            }
            h = Input.GetAxis("Horizontal");
            v = Input.GetAxis("Vertical");
#endif
            /** My note:
             *   Without Animator>Apply Root Motion = true, the speed and direction properties of the animation don't affect position of transform
             */
            animator.SetFloat("Speed", h * h + v * v);
            animator.SetFloat("Direction_horizontal", h, directionDampTime, Time.deltaTime);
            animator.SetFloat("Direction_vertical", v, directionDampTime, Time.deltaTime);
        }

        #endregion

    }
}