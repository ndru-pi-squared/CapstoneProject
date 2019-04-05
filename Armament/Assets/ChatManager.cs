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

        public string username;

        public int maxMessages = 25;

        public GameObject chatPanel, textObject;
        public InputField chatBox;

        public Color playerMessage, info;

        [SerializeField]
        List<Message> messageList = new List<Message>();

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (chatBox.text != "")
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    sendMessageToChat(username + ": " + chatBox.text, Message.MessageType.playerMessage);
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
                    sendMessageToChat("That's a spicy meat-a-ball", Message.MessageType.info);
                }
            }
        }

        public void sendMessageToChat(string text, Message.MessageType messageType)
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
