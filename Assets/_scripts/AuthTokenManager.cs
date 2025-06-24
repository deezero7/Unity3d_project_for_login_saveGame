using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class AuthTokenManager : MonoBehaviour
{
    private const string key = "mine16CharKey123"; // 16-char AES key
    private const string prefsKey = "authToken";   // PlayerPrefs key
    private const string validateUrl = "https://nodejs-server-for-unity3dgame-login-5vxc.onrender.com/u3d//validate-token";

    [System.Serializable]
    private class TokenPayload
    {
        public string token;
    }

    [System.Serializable]
    private class ServerResponse
    {
        public int code;
        public string message;
        public object userData;
    }

    void Start()
    {
        //ValidateSavedToken(); // Optional auto-validation on start
    }

    // Save token to PlayerPrefs (encrypted)
    public void SaveEncryptedToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogWarning("Attempted to save empty token");
            return;
        }

        string encrypted = Encrypt(token);
        PlayerPrefs.SetString(prefsKey, encrypted);
        PlayerPrefs.Save();
    }

    // Load token from PlayerPrefs (decrypted)
    public string LoadDecryptedToken()
    {
        if (!PlayerPrefs.HasKey(prefsKey)) return null;
        string encrypted = PlayerPrefs.GetString(prefsKey);
        return string.IsNullOrEmpty(encrypted) ? null : Decrypt(encrypted);
    }

    // Delete stored token
    public void DeleteToken()
    {
        PlayerPrefs.DeleteKey(prefsKey);
        PlayerPrefs.Save();
    }

    // Optional: Validate token with backend
    public void ValidateSavedToken()
    {
        string token = LoadDecryptedToken();
        if (!string.IsNullOrEmpty(token))
        {
            StartCoroutine(ValidateTokenCoroutine(token));
        }
        else
        {
            Debug.Log("No token found. Showing login screen.");
        }
    }

    private IEnumerator ValidateTokenCoroutine(string token)
    {
        string jsonData = JsonUtility.ToJson(new TokenPayload { token = token });

        UnityWebRequest request = new UnityWebRequest(validateUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Token validated: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogWarning("Token invalid or expired. Clearing.");
            DeleteToken();
        }
    }

    private string Encrypt(string plainText)
    {
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = new byte[16]; // Static IV (ok for non-sensitive token use)

        ICryptoTransform encryptor = aes.CreateEncryptor();
        byte[] input = Encoding.UTF8.GetBytes(plainText);
        byte[] encrypted = encryptor.TransformFinalBlock(input, 0, input.Length);
        return Convert.ToBase64String(encrypted);
    }

    private string Decrypt(string cipherText)
    {
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = new byte[16];

        ICryptoTransform decryptor = aes.CreateDecryptor();
        byte[] input = Convert.FromBase64String(cipherText);
        byte[] decrypted = decryptor.TransformFinalBlock(input, 0, input.Length);
        return Encoding.UTF8.GetString(decrypted);
    }
}
