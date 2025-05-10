using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

// SecurePrefs is a simple class to store and retrieve encrypted data using AES encryption.
// It uses PlayerPrefs for storage, but the data is encrypted to provide a basic level of security.
//quick & simple way to protect the token (light obfuscation, not full security):
public static class SecurePrefs {
    private static readonly string key = "mine16CharKey123"; // Must be 16 chars for AES-128

    public static void SetEncryptedToken(string token) {

        if (string.IsNullOrEmpty(token)) {
            Debug.LogWarning("Tried to save a null or empty token.");
            return;
        }

        string encrypted = Encrypt(token, key);
        PlayerPrefs.SetString("authToken", encrypted);
    }

    public static string GetEncryptedToken() {
        string encrypted = PlayerPrefs.GetString("authToken", "");
        return string.IsNullOrEmpty(encrypted) ? "" : Decrypt(encrypted, key);
    }

    private static string Encrypt(string plainText, string key) {
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = new byte[16]; // All zero IV (for simplicity, not recommended for production)

        ICryptoTransform encryptor = aes.CreateEncryptor();
        byte[] input = Encoding.UTF8.GetBytes(plainText);
        byte[] encrypted = encryptor.TransformFinalBlock(input, 0, input.Length);
        return Convert.ToBase64String(encrypted);
    }

    private static string Decrypt(string cipherText, string key) {
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = new byte[16];

        ICryptoTransform decryptor = aes.CreateDecryptor();
        byte[] input = Convert.FromBase64String(cipherText);
        byte[] decrypted = decryptor.TransformFinalBlock(input, 0, input.Length);
        return Encoding.UTF8.GetString(decrypted);
    }
}
