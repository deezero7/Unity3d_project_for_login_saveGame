
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine.SceneManagement;


public class accLogin : MonoBehaviour
{
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TextMeshProUGUI alert_text;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button createaccButton;

    private string loggedInUser; // Store the logged-in username here for profile picture upload or other uses
    [SerializeField] private RawImage userProfilePicRawImage;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI gemsText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI xpText;

    // #region // for local testing only
    // private string loginEndPoint = "http://localhost:3000/u3d/login"; // Replace with your server URL
    // private string createaccEndPoint = "http://localhost:3000/u3d/createacc"; // Replace with your server URL
    
    // private string userProfilePicEndPoint = "http://localhost:3000/u3d/uploadProfilePictureWeb"; // Replace with your server URL
    // #endregion

    #region // for production only
     private string loginEndPoint = "https://nodejs-server-for-unity3dgame-login-5vxc.onrender.com/u3d/login"; // Replace with your server URL
    private string createaccEndPoint = "https://nodejs-server-for-unity3dgame-login-5vxc.onrender.com/u3d/createacc"; // Replace with your server URL
    
    private string userProfilePicEndPoint = "https://nodejs-server-for-unity3dgame-login-5vxc.onrender.com/u3d/uploadProfilePictureWeb"; // Replace with your server URL
    private string autoLoginEndPoint = "https://nodejs-server-for-unity3dgame-login-5vxc.onrender.com/u3d/autoLogin"; // Replace with your server URL
    #endregion

    // define the pattern
    private static readonly Regex passwordRegex = new Regex(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)[A-Za-z\d]{6,25}$"); 
    

     private void Start() {
        // Check if the user is already logged in
        TryAutoLogin();
    }


    public void OnLoginClick()
    {
        alert_text.text = "Signing in";
        loginButton.interactable = false;

        StartCoroutine(Login());
    }
     public void OnCreateAccClick()
    {
        alert_text.text = "Creating account ";
        createaccButton.interactable = false;
        
        StartCoroutine(CreateAcc());
    }

    // auto login using jwt token
    /*Flow Recap
        First login → server issues JWT (168h expiry) → client stores it via SecurePrefs.

        App reopens → Unity reads the token → sends it to /autoLogin route.

        Server checks if token is valid:

        ✅ If valid: allows auto-login.

        ❌ If expired/invalid: responds with 401/403 → client clears token → shows login screen.

        New login replaces old token in SecurePrefs.

        So you're right — no need to track expiry on the client side unless you want to skip a round-trip to the server to check validity first.
        */
    public void TryAutoLogin()
    {
        string token = SecurePrefs.GetEncryptedToken();
        if (!string.IsNullOrEmpty(token))
        {
            StartCoroutine(PostAutoLogin(token));
        }
        else
        {
            Debug.Log("No token found. Showing login UI.");
            // Show login screen or fallback
        }
    }

    IEnumerator PostAutoLogin(string token)
    {
        UnityWebRequest request = new UnityWebRequest(autoLoginEndPoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(new byte[0]); // empty body
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Auto-login success: " + request.downloadHandler.text);
            // Proceed to game
            LoginResponseFromNodeServer loginResponse = JsonUtility.FromJson<LoginResponseFromNodeServer>(request.downloadHandler.text);
            loggedInUser = loginResponse.userData.username;
            var userData = loginResponse.userData;
            string adminText = loginResponse.userData.isAdmin ? " (Admin)" : "";
            alert_text.text = "Auto-login success " + "Welcome back " + adminText + loginResponse.userData.username + "!";;
            
            // jwt token save to securePrefs for auto refresh of jwt token
            SaveTokenAfterLogin(loginResponse.userData.token); // Store token
            
            // Show game data
                goldText.text = userData.gameData.gold.ToString();
                gemsText.text = userData.gameData.gems.ToString();
                levelText.text =  userData.gameData.level.ToString();
                xpText.text = userData.gameData.experiencePoints.ToString();

                // Show profile picture (already done)
                if (!string.IsNullOrEmpty(userData.userProfilePicture)) {
                    string base64 = userData.userProfilePicture;
                    if (base64.StartsWith("data:image"))
                        base64 = base64.Substring(base64.IndexOf(",") + 1);

                    byte[] imageBytes = Convert.FromBase64String(base64);
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(imageBytes);
                    userProfilePicRawImage.texture = tex;
                }
        }
        else
        {
            Debug.LogWarning("Auto-login failed: " + request.responseCode + " " + request.error);
            // Handle expired token or show login
        }
    }

    public void OnUserProfilePicUploadClick()
    {
        alert_text.text = "Selecting profile picture...";

        // Define allowed file types
        string[] allowedFileTypes = new string[] { "image/*" };

        NativeFilePicker.PickFile((path) =>
        {
            if (path == null)
            {
                alert_text.text = "File selection canceled.";
                return;
            }

            byte[] imageData = File.ReadAllBytes(path);

            if (imageData.Length > 200 * 1024)
            {
                alert_text.text = "Image too large. Must be under 200KB.";
                return;
            }

            //string base64Image = Convert.ToBase64String(imageData);
            StartCoroutine(UploadProfilePicture(loggedInUser, imageData));
        }, allowedFileTypes);
    }

    private IEnumerator UploadProfilePicture(string username, byte[] imageBytes)
    {
        // Create multipart form
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormDataSection("username", username));
        formData.Add(new MultipartFormFileSection("image", imageBytes, "profile.png", "image/png"));

        UnityWebRequest request = UnityWebRequest.Post(userProfilePicEndPoint, formData);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // ✅ Show the image in Unity UI immediately
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(imageBytes);
            userProfilePicRawImage.texture = tex;
            alert_text.text = "Profile picture uploaded successfully.";
        }
        else
        {
            alert_text.text = "Failed to upload profile picture: " + request.error;
            Debug.LogError("Upload error: " + request.error);
            Debug.LogError("Upload response: " + request.downloadHandler.text);
        }

        createaccButton.interactable = true;
    }
    
    private IEnumerator Login(){

        string username = usernameInput.text;
        string password = passwordInput.text;

        if(username.Length < 3 || username.Length > 25){
            alert_text.text = "Username must be between 5 and 20 characters long.";
            loginButton.interactable = true;
            createaccButton.interactable = true;
            yield break;
        }
         if(passwordRegex.IsMatch(password) == false){
            alert_text.text = "Invalid password.";
            loginButton.interactable = true;
            createaccButton.interactable = true;
            yield break;
        }

        
        string fullURL = $"{loginEndPoint}?username={UnityWebRequest.EscapeURL(username)}&password={UnityWebRequest.EscapeURL(password)}";
        // Debug: Print the full URL
        // Debug.Log($"Sending request to: {fullURL}");

        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("password", password);

        // Create the request with URL parameters
        UnityWebRequest request = UnityWebRequest.Post(fullURL, form);

        // Send the request
        float startTimer = 0.0f;
        yield return request.SendWebRequest();

        // Wait for the request to complete or timeout after 30 seconds
        while (!request.isDone)
        {
            startTimer += Time.deltaTime;
            if (startTimer > 30.0f)
            {
                Debug.LogError("Request timed out.");
                request.Abort(); // Cancel the request if it takes too long
                yield break;
            }
            yield return null;
        }

        LoginResponseFromNodeServer loginResponse = JsonUtility.FromJson<LoginResponseFromNodeServer>(request.downloadHandler.text);

        // Handle response
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log(request.downloadHandler.text);
            loginResponse = JsonUtility.FromJson<LoginResponseFromNodeServer>(request.downloadHandler.text);

            if(loginResponse.code == 0){
                loggedInUser = loginResponse.userData.username;
                var userData = loginResponse.userData;

                // jwt token save to securePrefs
                SaveTokenAfterLogin(loginResponse.userData.token); // Store token
                

                 // Show game data
                goldText.text = userData.gameData.gold.ToString();
                gemsText.text = userData.gameData.gems.ToString();
                levelText.text =  userData.gameData.level.ToString();
                xpText.text = userData.gameData.experiencePoints.ToString();

                // Show profile picture (already done)
                if (!string.IsNullOrEmpty(userData.userProfilePicture)) {
                    string base64 = userData.userProfilePicture;
                    if (base64.StartsWith("data:image"))
                        base64 = base64.Substring(base64.IndexOf(",") + 1);

                    byte[] imageBytes = Convert.FromBase64String(base64);
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(imageBytes);
                    userProfilePicRawImage.texture = tex;
                }

                //alert_text.text = "username and password are required";
                loginButton.interactable = false;
                createaccButton.interactable = false;
                string adminText = loginResponse.userData.isAdmin ? " (Admin)" : "";
                alert_text.text = "Welcome back " + adminText + loginResponse.userData.username + "!";
                
            }
            else{
                switch(loginResponse.code){
                    case 1:
                        alert_text.text = "Invalid Credentials";
                        loginButton.interactable = true;
                        createaccButton.interactable = true;
                        break;
                    case 3:
                        alert_text.text = "Password is too weak, please choose a stronger one";
                        loginButton.interactable = true;
                        createaccButton.interactable = true;
                        break;
                    case 98:
                        alert_text.text = "Account locked due to too many failed attempts. Try again later";
                        loginButton.interactable = false;
                        createaccButton.interactable = false;
                        break;
                    case 99:
                        alert_text.text = "Too many login attempts. Please try again later";
                        loginButton.interactable = false;
                        createaccButton.interactable = false;
                        break;
                    default:
                        alert_text.text = "Unknown error occurred or Corrupted data";
                        loginButton.interactable = false;
                        createaccButton.interactable = false;
                        break;
                }
                
                Debug.LogError($"Login failed! Error: {request.error}");
                // Handle login failure (e.g., show error message)
            }
            
            
            
        }
        else
        {
            alert_text.text = "Error connection to server.";
            Debug.LogError($"Request failed! Error: {request.error}");
            loginButton.interactable = true;
            createaccButton.interactable = true;
            
        }

        // Clear the input fields after login attempt
        usernameInput.text = string.Empty;
        passwordInput.text = string.Empty;
        yield return null;
    }
    
    private IEnumerator CreateAcc(){

        string username = usernameInput.text;
        string password = passwordInput.text;

        if(username.Length < 3 || username.Length > 25){
            alert_text.text = "Username must be between 5 and 20 characters long.";
            loginButton.interactable = true;
            createaccButton.interactable = true;
            yield break;
        }
        if(passwordRegex.IsMatch(password) == false){
            alert_text.text = "Invalid password.";
            loginButton.interactable = true;
            createaccButton.interactable = true;
            yield break;
        }

        
        string fullURL = $"{createaccEndPoint}?username={UnityWebRequest.EscapeURL(username)}&password={UnityWebRequest.EscapeURL(password)}";
        // Debug: Print the full URL
        Debug.Log($"Sending request to: {fullURL}");

        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("password", password);

        // Create the request with URL parameters
        UnityWebRequest request = UnityWebRequest.Post(fullURL, form);

        // Send the request
        float startTimer = 0.0f;
        yield return request.SendWebRequest();

        // Wait for the request to complete or timeout after 30 seconds
        while (!request.isDone)
        {
            startTimer += Time.deltaTime;
            if (startTimer > 30.0f)
            {
                Debug.LogError("Request timed out.");
                request.Abort(); // Cancel the request if it takes too long
                yield break;
            }
            yield return null;
        }

        // Handle response
        if (request.result == UnityWebRequest.Result.Success)
        {

            Debug.Log(request.downloadHandler.text);
            CreateResponseFromNodeServer createResponse = JsonUtility.FromJson<CreateResponseFromNodeServer>(request.downloadHandler.text);

            // response from nodejs server compare to do the following..
            if(createResponse.code == 0){
                GameAccount createUserData = new GameAccount();
                createUserData.gameData.gold = 7;
                createUserData.gameData.gems = 7;
                createUserData.gameData.level = 7;
                createUserData.gameData.experiencePoints = 7;
                // upload this manual filled data to the server for test purposes
                createResponse.userData = createUserData;
                // loginButton.interactable = true;
                // createaccButton.interactable = true;
                // GameAccount returnedAccount = JsonUtility.FromJson<GameAccount>(request.downloadHandler.text);
                alert_text.text = "Account created! Logg in...";
                
            }
            else{
                switch(createResponse.code){
                    case 1:
                        alert_text.text = "username and password are required";
                        loginButton.interactable = true;
                        createaccButton.interactable = true;
                        break;
                    case 2:
                        alert_text.text = "Username already exists, please choose another one";
                        loginButton.interactable = true;
                        createaccButton.interactable = true;
                        break;
                    case 3:
                        alert_text.text = "Password is too weak, please choose a stronger one";
                        loginButton.interactable = true;
                        createaccButton.interactable = true;
                        break;
                    case 99:
                        alert_text.text = "Too many login attempts. Please try again later";
                        loginButton.interactable = false;
                        createaccButton.interactable = false;
                        break;
                    default:
                        alert_text.text = "Unknown error occurred or Corrupted data";
                        loginButton.interactable = false;
                        createaccButton.interactable = false;
                        break;
                }
                
            }        
        }
        else
        {
            loginButton.interactable = true;
            createaccButton.interactable = true;
            alert_text.text = "Error connection to server.";
        }

        // Clear the input fields after login attempt
        usernameInput.text = string.Empty;
        passwordInput.text = string.Empty;
        yield return null;
    }

    public void SaveTokenAfterLogin(string jwtToken)
    {
        if (!string.IsNullOrEmpty(jwtToken)) {
            SecurePrefs.SetEncryptedToken(jwtToken);
        }
    }

    // This method is called when the user clicks the logout button
    // It clears the token 
    public void Logout()
    {
        PlayerPrefs.DeleteKey("authToken");
        Debug.Log("Logged out and token cleared.");
        // Redirect to login screen
        SceneManager.LoadScene("MainMenu");
    }

    #region // save game data to server
    // save data to server
    public TMP_InputField goldInputField;
    public TMP_InputField gemsInputField;
    private const string SaveGameDataUrl = "https://nodejs-server-for-unity3dgame-login-5vxc.onrender.com/u3d/saveGameData"; // Replace with your real endpoint

    
    public void OnSaveButtonClick()
    {
        // Check if the user is logged in
        if (string.IsNullOrEmpty(loggedInUser))
        {
            Debug.LogWarning("User not logged in. Please log in first.");
            return;
        }
        // Check if the token is valid
        string token = SecurePrefs.GetEncryptedToken();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogWarning("No valid auth token found. User may not be logged in.");
            return;
        }

        // Trim whitespace before parsing
        goldInputField.text = goldInputField.text.Trim();
        gemsInputField.text = gemsInputField.text.Trim();

        if (int.TryParse(goldInputField.text, out int gold) && int.TryParse(gemsInputField.text, out int gems))
        {
            if (gold > 100000 || gems > 100000)
            {
                Debug.LogWarning("Gold or Gems cannot exceed 100,000.");
                return;
            }

            GameData data = new GameData
            {
                gold = gold,
                gems = gems
            };

            SaveGameDataToServer(loggedInUser, data); // loggedInUser is the username
        }
        else
            {
                Debug.LogWarning("Please enter valid numbers for gold and gems.");
            }
    }

    private void SaveGameDataToServer(string username, GameData data)
    {
        GameAccount request = new GameAccount
        {
            username = username,
            gameData = data
        };

        string json = JsonUtility.ToJson(request);
        StartCoroutine(SendPutRequest(json, data));
    }

    private IEnumerator SendPutRequest(string json, GameData data)
    {
        UnityWebRequest request = new UnityWebRequest(SaveGameDataUrl, "PUT");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Game data saved: " + request.downloadHandler.text);
            // Parse the server response to get updated values
            LoginResponseFromNodeServer response = JsonUtility.FromJson<LoginResponseFromNodeServer>(request.downloadHandler.text);
            goldText.text = data.gold.ToString();
            gemsText.text = data.gems.ToString();

            // ✅ Clear inputs
            goldInputField.text = "";
            gemsInputField.text = "";
        }
        else
        {
            Debug.LogError("Error saving game data: " + request.error);
        }
    }

    #endregion

}

