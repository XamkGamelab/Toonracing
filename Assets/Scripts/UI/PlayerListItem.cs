using UnityEngine;
using UnityEngine.UI;

public class PlayerListItem : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI playerName;
    [SerializeField] private RawImage playerColor;
    [SerializeField] private string playerID;

    public void SetPlayerColor(Color color)
    {
        if(!playerColor)
            return;
        playerColor.color = color;
    }
    
    public void SetPlayerName(string name)
    {
        if(!playerName)
            return;
        playerName.text = name;
    }
    
    public void SetPlayerID(string id)
    {
        playerID = id;
    }
}
