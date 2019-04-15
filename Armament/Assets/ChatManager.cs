using System.Collections;

using UnityEngine;

using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using System.Collections.Generic;
using ExitGames.Client.Photon;


namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class ChatManager : MonoBehaviourPunCallbacks
    {
        private PhotonView PV;

        public GamePlayFabController GPFC;

        public GameObject CB;

        public string username;

        public int maxMessages = 25;

        public GameObject chatPanel, textObject;
        public InputField chatBox;

        public Color playerMessage, info;

        public Toggle chatToggle;

        private Transform chatToggleStartPosition;

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
            //chatToggleStartPosition = chatToggle.transform.position;
        }

        // Update is called once per frame
        void Update()
        {
            if (chatBox.text != "")
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    username = GPFC.username;
                    string test = username + ": " + chatBox.text;
                    var type = Message.MessageType.playerMessage;
                    PV.RPC("RPC_SendMessageToChat", RpcTarget.All, test, type);
                    chatBox.text = "";
                }
            }

            else
            {
                if (!chatBox.isFocused && Input.GetKeyDown(KeyCode.Return))
                {
                    chatBox.ActivateInputField();
                }
            }

            if (!chatBox.isFocused)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    string test = "That's a spicy meat-a-ball";
                    var type = Message.MessageType.info;
                    PV.RPC("RPC_SendMessageToChat", RpcTarget.All, test, type);
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

        public void ToggleChatBox()
        {
            if (chatToggle.isOn)
            {
                Debug.Log("Chat turned on");
                //chatToggle.transform.position.y = chatToggleStartPosition.up;
                CB.SetActive(true);
            }

            else
            {
                Debug.Log("Chat turned off");
                //chatToggle.transform.position;
                CB.SetActive(false);
            }
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

