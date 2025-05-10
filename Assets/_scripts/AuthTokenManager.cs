using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.IO;

public class AuthTokenManager : MonoBehaviour
{
    private string validateUrl = "https://nodejs-server-for-unity3dgame-login-5vxc.onrender.com/u3d/validateToken"; // Replace with your backend URL
    private static readonly string key = "mine16byteAESkey"; // Must be 16 chars

    [System.Serializable]
    public class TokenPayload
    {
        public string token;
    }

    [System.Serializable]
    public class ServerResponse
    {
        public int code;
        public string message;
        public object userData;
    }

    void Start()
    {
        ValidateSavedToken();
    }

    public void ValidateSavedToken()
    {
        string token = LoadDecryptedToken();
        if (!string.IsNullOrEmpty(token))
        {
            StartCoroutine(ValidateTokenCoroutine(token));
        }
        else
        {
            Debug.Log("No saved token. Show login UI.");
        }
    }

    public void SaveEncryptedToken(string token)
    {
        string encrypted = Encrypt(token);
        PlayerPrefs.SetString("jwtToken", encrypted);
        PlayerPrefs.Save();
    }

    public string LoadDecryptedToken()
    {
        if (!PlayerPrefs.HasKey("jwtToken")) return null;
        string encrypted = PlayerPrefs.GetString("jwtToken");
        return Decrypt(encrypted);
    }

    public void DeleteToken()
    {
        PlayerPrefs.DeleteKey("jwtToken");
        PlayerPrefs.Save();
    }

    private IEnumerator ValidateTokenCoroutine(string token)
    {
        string jsonData = JsonUtility.ToJson(new TokenPayload { token = token });
        var request = new UnityWebRequest(validateUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Token valid: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogWarning("Token invalid or expired. Showing login.");
            DeleteToken();
        }
    }

    private string Encrypt(string plainText)
    {
        byte[] iv = new byte[16];
        byte[] array;

        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = iv;
            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (MemoryStream encryptStream = new MemoryStream())
            using (CryptoStream cryptoStream = new CryptoStream(encryptStream, encryptor, CryptoStreamMode.Write))
            using (StreamWriter writer = new StreamWriter(cryptoStream))
            {
                writer.Write(plainText);
                writer.Close();
                array = encryptStream.ToArray();
            }
        }

        return System.Convert.ToBase64String(array);
    }

    private string Decrypt(string cipherText)
    {
        byte[] iv = new byte[16];
        byte[] buffer = System.Convert.FromBase64String(cipherText);

        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = iv;
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using (MemoryStream decryptStream = new MemoryStream(buffer))
            using (CryptoStream cryptoStream = new CryptoStream(decryptStream, decryptor, CryptoStreamMode.Read))
            using (StreamReader reader = new StreamReader(cryptoStream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
