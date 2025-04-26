using UnityEngine;

[System.Serializable]
public class LoginResponseFromNodeServer
{
    public int code;
    public string message;
    public GameAccount userData;
}
