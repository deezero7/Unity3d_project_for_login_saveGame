using UnityEngine;

[System.Serializable]
public class GameAccount
{
    public string _id;
    public bool isAdmin;
    public string username;

    public string userProfilePicture;
    public GameData gameData;

    public GameAccount()
    {
        gameData = new GameData(); // Initialize here
    }

}
