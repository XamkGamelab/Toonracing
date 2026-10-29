using Photon.Pun;
using UnityEngine;

namespace Multiplayer
{
    public class CreateAndJoinRooms : MonoBehaviourPunCallbacks
    {
    
        public TMPro.TMP_InputField createInputField;
        public TMPro.TMP_InputField joinInputField;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Debug.Log("CreateAndJoinRooms script started");
            if(createInputField == null)
                Debug.LogError("Create input field is not assigned");
            if(joinInputField == null)
                Debug.LogError("Join input field is not assigned");
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void OnCreateRoom()
        {
            Debug.Log("Create room button pressed");
            if(string.IsNullOrEmpty(createInputField.text))
            {
                Debug.Log("Room name is empty");
                return;
            }
            else if(createInputField.text == "Room ID")
            {  
                Debug.Log("Room name is default text");
                return;
            }            
            PhotonNetwork.CreateRoom(createInputField.text);
        }
    
        public void OnJoinRoom()
        {
            if(string.IsNullOrEmpty(joinInputField.text))
            {
                Debug.Log("Room name is empty");
                return;
            }
            else if(joinInputField.text == "Room ID")
            {  
                Debug.Log("Room name is default text");
                return;
            }
            PhotonNetwork.JoinRoom(joinInputField.text);
        }
    
        public override void OnJoinedRoom()
        {
            Debug.Log("Joined Photon room.");
            PhotonNetwork.LoadLevel("Game");
        }
    }
}
