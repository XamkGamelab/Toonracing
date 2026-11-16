using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class UserData
{
    public string userName;
    public string playerID;
    public Color playerColor;
    // Add other user data fields as needed
}

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    private UserData userData;

    private List<UserData> otherPlayers;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Singleton pattern to ensure only one instance of DataManager exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        userData = new UserData();
        otherPlayers = new List<UserData>();
    }

    public void SetUserName(string userName)
    {
        userData.userName = userName;
        PlayerPrefs.SetString("UserName", userName);
        PhotonNetwork.NickName = userName;
    }

    public string GetUsername()
    {
        if (userData != null) 
            return userData.userName;
        userData = new UserData { userName = PlayerPrefs.GetString("UserName", "Player") };
        return userData.userName;
    }
    
    public void SetPlayerColor(Color color)
    {
        userData.playerColor = color;
        PlayerPrefs.SetString("PlayerColor", ColorUtility.ToHtmlStringRGBA(color));
    }
    
    public Color GetPlayerColor()
    {
        if (userData != null) 
            return userData.playerColor;
        var colorHex = PlayerPrefs.GetString("PlayerColor", ColorUtility.ToHtmlStringRGBA(Color.white));
        ColorUtility.TryParseHtmlString("#" + colorHex, out var color);
        userData = new UserData { playerColor = color };
        return userData.playerColor;
    }
    public void AddPlayerData(string playerId, string playerName, Color playerColor)
    {
        var user = new UserData
        {
            playerID = playerId,
            userName = playerName,
            playerColor = playerColor
        };
        otherPlayers.Add(user);
    }

    public void RemovePlayerData(string playerId)
    {
        otherPlayers.RemoveAll(player => player.playerID == playerId);
    }

    public void ClearOthersPlayerData()
    {
        otherPlayers.Clear();
    }
    
    public List<UserData> GetOtherPlayersData()
    {
        return otherPlayers;
    }
}
