
using UnityEngine;

using Photon.Pun;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityStandardAssets.CrossPlatformInput;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class ChatManager : MonoBehaviourPunCallbacks
    {
        private PhotonView PV;

        public GamePlayFabController GPFC;

        public GameObject CB;

        public string username;

        public int maxMessages = 25;

        public bool setToDisable = false;

        public GameObject chatPanel, textObject;
        public InputField chatBox;

        public Color playerMessage, info;

        //Use of PM is to disable movement when chatting. This is a later feature.
        //public PlayerManager PM;


        [SerializeField]
        List<Message> messageList = new List<Message>();


        public override void OnEnable()
        {
            base.OnEnable();
            PhotonNetwork.AddCallbackTarget(this);
        }

        public override void OnDisable()
        {
            base.OnDisable();
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        // Start is called before the first frame update
        void Start()
        {
            PV = GetComponent<PhotonView>();
        }

        // Update is called once per frame
        void Update()
        {
            if (!CB.activeSelf) {
                if (Input.GetKeyDown(KeyCode.Return) || CrossPlatformInputManager.GetButtonDown("Chat"))
                {
                    CB.SetActive(true);
                    chatBox.ActivateInputField();
                    //PM.chatting = true;
                }
            }
            if (chatBox.text != "")
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    username = GPFC.username;
                    string test = username + ": " + chatBox.text;
                    var type = Message.MessageType.playerMessage;
                    PV.RPC("RPC_SendMessageToChat", RpcTarget.All, test, type);
                    chatBox.text = "";
                    chatBox.DeactivateInputField();
                    setToDisable = false;
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Return) && !setToDisable)
                {
                    chatBox.ActivateInputField();
                    setToDisable = true;
                }

                else if ((Input.GetKeyDown(KeyCode.Return) && setToDisable ) || CrossPlatformInputManager.GetButtonUp("Chat"))
                {
                    setToDisable = false;
                    CB.SetActive(false);
                }
            }

            if (!chatBox.isFocused)
            {
                //PM.chatting = false;
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    string test = "This is a system message. " + GPFC.username + " just jumped!";
                    var type = Message.MessageType.info;
                    PV.RPC("RPC_SendMessageToChat", RpcTarget.All, test, type);
                    chatBox.DeactivateInputField();
                }
            }
        }


        Color MessageTypeColor(Message.MessageType messageType)
        {
            Color color = info;
            switch (messageType)
            {
                case Message.MessageType.playerMessage:
                    color = playerMessage;
                    break;
            }
            return color;
        }




        [PunRPC]
        public void RPC_SendMessageToChat(string text, Message.MessageType messageType)
        {
            if (messageList.Count > maxMessages)
            {
                Destroy(messageList[0].textObject.gameObject);
                messageList.Remove(messageList[0]);
                Debug.Log("Space");
            }
            Message newMessage = new Message();
            newMessage.text = text;

            GameObject newText = Instantiate(textObject, chatPanel.transform);

            newMessage.textObject = newText.GetComponent<Text>();
            newMessage.textObject.text = newMessage.text;
            newMessage.textObject.color = MessageTypeColor(messageType);
            messageList.Add(newMessage);
        }

    }

    [System.Serializable]
    public class Message
    {
        public string text;
        public Text textObject;
        public MessageType messageType;

        public enum MessageType { playerMessage, info }
    }

}

