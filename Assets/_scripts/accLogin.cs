
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
    public NotificationPanelManager notificationManager;

    [SerializeField] private TMP_InputField usernameInputForLogin;
    [SerializeField] private TMP_InputField passwordInputForLogin;
    [SerializeField] private TMP_InputField usernameInputForCreateAcc;
    [SerializeField] private TMP_InputField passwordInputForCreateAcc;
    [SerializeField] private TMP_InputField emailInputForCreateAcc;
    [SerializeField] private TextMeshProUGUI alert_text;
    [SerializeField] private TextMeshProUGUI alert_text_forCreateAccPanel;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button createaccButton;

    private string loggedInUser; // Store the logged-in username here for profile picture upload or other uses
    [SerializeField] private RawImage userProfilePicRawImage;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI gemsText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI xpText;

    private AuthTokenManager tokenManager;

    [SerializeField] private TextMeshProUGUI playerNameText; // Text to display the player's name

    // Regular expression pattern for validating an email address.
    private const string EmailRegexPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

    // Call this method whenever a new message should pop up as pop up notification.
    public void OnNewMessageReceived(string messageContent)
    {
        if (notificationManager != null)
        {
            notificationManager.ShowNotification(messageContent);
        }
    }

    // Method to validate an email address using the defined regex pattern.
    public bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false; // Email cannot be empty or just whitespace
        }

        // Use Regex.IsMatch to check if the email matches the pattern
        return Regex.IsMatch(email, EmailRegexPattern);
    }


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
    private static readonly Regex passwordRegex = new Regex(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,25}$");


    private void Start()
    {
        tokenManager = UnityEngine.Object.FindFirstObjectByType<AuthTokenManager>();
        if (tokenManager == null)
        {
            Debug.LogError("AuthTokenManager not found in the scene. Please add it to the scene.");
            return;
        }
        // Check if the user is already logged in
        TryAutoLogin();
    }


    public void OnLoginClick()
    {
        alert_text.text = "Signing in";
        OnNewMessageReceived("Signing in...");

        loginButton.interactable = false;

        StartCoroutine(Login());
    }
    public void OnCreateAccClick()
    {
        alert_text.text = "Creating account ";
        OnNewMessageReceived("Creating account");

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
        string token = tokenManager.LoadDecryptedToken();
        if (!string.IsNullOrEmpty(token))
        {
            StartCoroutine(PostAutoLogin(token));
        }
        else
        {
            Debug.Log("No token found. Showing login UI.");
        }
    }

    IEnumerator PostAutoLogin(string token)
    {
        UnityWebRequest request = new UnityWebRequest(autoLoginEndPoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(new byte[0]);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {

            Debug.Log("Auto-login success: " + request.downloadHandler.text);
            string responseText = request.downloadHandler.text;

            // // Extract newToken manually
            // string newToken = null;
            // try
            // {
            //     int tokenIndex = responseText.IndexOf("\"newToken\":\"") + 12;
            //     int endIndex = responseText.IndexOf("\"", tokenIndex);
            //     newToken = responseText.Substring(tokenIndex, endIndex - tokenIndex);
            //     Debug.Log("Parsed newToken: " + newToken);
            // }
            // catch
            // {
            //     Debug.LogWarning("Failed to extract newToken from response.");
            // }

            // Extract the full response safely
            LoginResponseFromNodeServer loginResponse = JsonUtility.FromJson<LoginResponseFromNodeServer>(responseText);

            if (loginResponse != null && loginResponse.userData != null)
            {
                loggedInUser = loginResponse.userData.username;
                var userData = loginResponse.userData;
                string adminText = userData.isAdmin ? " (Admin)" : "";
                alert_text.text = "Auto-login success. Welcome back " + adminText + loggedInUser + "!";
                string name = "Auto-login success. Welcome back " + adminText + loggedInUser + "!";
                OnNewMessageReceived(name);
                playerNameText.text = loggedInUser; // Update player name text



                // ✅ Save the new token
                if (!string.IsNullOrEmpty(loginResponse.newToken))
                {
                    Debug.Log("Saving refreshed token");
                    tokenManager.SaveEncryptedToken(loginResponse.newToken);
                }
                else
                {
                    Debug.LogWarning("newToken is missing in response, cannot save.");
                }

                // Update game UI
                goldText.text = userData.gameData.gold.ToString();
                gemsText.text = userData.gameData.gems.ToString();
                levelText.text = userData.gameData.level.ToString();
                xpText.text = userData.gameData.experiencePoints.ToString();

                // Profile picture
                if (!string.IsNullOrEmpty(userData.userProfilePicture))
                {
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
                Debug.LogWarning("Auto-login succeeded but response was incomplete.");
            }
        }
        else
        {
            Debug.LogWarning("Auto-login failed: " + request.responseCode + " " + request.error);
        }
    }

    public void OnUserProfilePicUploadClick()
    {
        alert_text.text = "Selecting profile picture...";
        OnNewMessageReceived("Selecting profile picture...");

        // Define allowed file types
        string[] allowedFileTypes = new string[] { "image/*" };

        NativeFilePicker.PickFile((path) =>
        {
            if (path == null)
            {
                alert_text.text = "File selection canceled.";
                OnNewMessageReceived("File selection canceled.");
                return;
            }

            byte[] imageData = File.ReadAllBytes(path);

            if (imageData.Length > 200 * 1024)
            {
                alert_text.text = "Image too large. Must be under 200KB.";
                OnNewMessageReceived("Image too large. Must be under 200KB.");
                return;
            }

            //string base64Image = Convert.ToBase64String(imageData);
            StartCoroutine(UploadProfilePicture(loggedInUser, imageData));
        }, allowedFileTypes);
    }

    private IEnumerator UploadProfilePicture(string username, byte[] imageBytes)
    {
        string token = tokenManager.LoadDecryptedToken();


        if (string.IsNullOrEmpty(token))
        {
            alert_text.text = "No token found. Please log in again.";
            OnNewMessageReceived("No token found. Please log in again.");
            yield break;
        }

        // Use WWWForm so Unity properly sets multipart boundary
        WWWForm form = new WWWForm();
        form.AddBinaryData("image", imageBytes, "profile.png", "image/png");

        UnityWebRequest request = UnityWebRequest.Post(userProfilePicEndPoint, form);
        request.SetRequestHeader("Authorization", "Bearer " + token);  // ✅ send token in header

        Debug.Log("Uploading profile picture with token...");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success || request.responseCode == 200)
        {
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(imageBytes);
            userProfilePicRawImage.texture = tex;
            alert_text.text = "Profile picture uploaded successfully.";
            OnNewMessageReceived("Profile picture uploaded successfully.");
        }
        else
        {
            alert_text.text = $"Failed to upload image: {request.error}";
            OnNewMessageReceived($"Failed to upload image: {request.error}");
            Debug.LogError("Upload failed: " + request.downloadHandler.text);
        }

        createaccButton.interactable = true;
    }


    private IEnumerator Login()
    {

        string username = usernameInputForLogin.text;
        string password = passwordInputForLogin.text;

        if (username.Length < 3 || username.Length > 25)
        {
            alert_text.text = "Username must be between 5 and 20 characters long.";
            OnNewMessageReceived("Username must be between 5 and 20 characters long.");
            loginButton.interactable = true;
            createaccButton.interactable = true;
            yield break;
        }
        if (passwordRegex.IsMatch(password) == false)
        {
            alert_text.text = "Invalid password.";
            OnNewMessageReceived("Invalid password. Password must be between 6 and 25 characters long, contain at least one uppercase letter, one lowercase letter, one digit, and one special character.");
            loginButton.interactable = true;
            createaccButton.interactable = true;
            yield break;
        }

        // // Create the full URL with parameters for debugging ONLY
        // // Note: In production, you have to use POST with a form body instead of URL
        // string fullURL = $"{loginEndPoint}?username={UnityWebRequest.EscapeURL(username)}&password={UnityWebRequest.EscapeURL(password)}";
        // // Debug: Print the full URL
        // Debug.Log($"Sending request to: {fullURL}");

        string fullURL = loginEndPoint; // Use the endpoint directly for POST request

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
            playerNameText.text = username; // Update player name text
            Debug.Log(request.downloadHandler.text);
            loginResponse = JsonUtility.FromJson<LoginResponseFromNodeServer>(request.downloadHandler.text);

            if (loginResponse.code == 0)
            {
                loggedInUser = loginResponse.userData.username;
                var userData = loginResponse.userData;

                // jwt token save to securePrefs
                SaveTokenAfterLogin(loginResponse.userData.token); // Store token


                // Show game data
                goldText.text = userData.gameData.gold.ToString();
                gemsText.text = userData.gameData.gems.ToString();
                levelText.text = userData.gameData.level.ToString();
                xpText.text = userData.gameData.experiencePoints.ToString();

                // Show profile picture (already done)
                if (!string.IsNullOrEmpty(userData.userProfilePicture))
                {
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
                OnNewMessageReceived("Welcome back " + adminText + loginResponse.userData.username + "!");

            }
            else
            {
                switch (loginResponse.code)
                {
                    case 1:
                        alert_text.text = "Invalid Credentials";
                        OnNewMessageReceived("Invalid Credentials");
                        loginButton.interactable = true;
                        createaccButton.interactable = true;
                        break;
                    case 3:
                        alert_text.text = "Password is too weak, please choose a stronger one";
                        OnNewMessageReceived("Password is too weak, please choose a stronger one");
                        loginButton.interactable = true;
                        createaccButton.interactable = true;
                        break;
                    case 4:
                        alert_text.text = "Please verify your email before logging in.";
                        OnNewMessageReceived("Please verify your email before logging in.");
                        loginButton.interactable = true;
                        createaccButton.interactable = true;
                        break;
                    case 98:
                        alert_text.text = "Account locked due to too many failed attempts. Try again later";
                        OnNewMessageReceived("Account locked due to too many failed attempts. Try again later");
                        loginButton.interactable = false;
                        createaccButton.interactable = false;
                        break;
                    case 99:
                        alert_text.text = "Too many login attempts. Please try again later";
                        OnNewMessageReceived("Too many login attempts. Please try again later");
                        loginButton.interactable = false;
                        createaccButton.interactable = false;
                        break;
                    default:
                        alert_text.text = "Unknown error occurred or Corrupted data";
                        OnNewMessageReceived("Unknown error occurred or Corrupted data");
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
            OnNewMessageReceived("Error connection to server.");
            Debug.LogError($"Request failed! Error: {request.error}");
            loginButton.interactable = true;
            createaccButton.interactable = true;

        }

        // Clear the input fields after login attempt
        usernameInputForLogin.text = string.Empty;
        passwordInputForLogin.text = string.Empty;
        yield return null;
    }

    private IEnumerator CreateAcc()
    {

        string username = usernameInputForCreateAcc.text;
        string password = passwordInputForCreateAcc.text;
        string email = emailInputForCreateAcc.text;

        if (!IsValidEmail(email))
        {
            alert_text_forCreateAccPanel.text = "Invalid email format.";
            OnNewMessageReceived("Invalid email format.");
            loginButton.interactable = true;
            createaccButton.interactable = true;
            yield break;
        }

        if (username.Length < 3 || username.Length > 25)
        {
            alert_text_forCreateAccPanel.text = "Username must be between 5 and 20 characters long.";
            OnNewMessageReceived("Username must be between 5 and 20 characters long.");
            loginButton.interactable = true;
            createaccButton.interactable = true;
            yield break;
        }
        if (passwordRegex.IsMatch(password) == false)
        {
            alert_text_forCreateAccPanel.text = "Invalid password format";
            OnNewMessageReceived("Invalid password. Password must be between 6 and 25 characters long, contain at least one uppercase letter, one lowercase letter, one digit, and one special character.");
            loginButton.interactable = true;
            createaccButton.interactable = true;
            yield break;
        }

        // // Create the full URL with parameters for debugging ONLY
        // // Note: In production, you have to use POST with a form body instead of URL
        // string fullURL = $"{createaccEndPoint}?username={UnityWebRequest.EscapeURL(username)}&password={UnityWebRequest.EscapeURL(password)}";
        // // Debug: Print the full URL
        // Debug.Log($"Sending request to: {fullURL}");

        string fullURL = createaccEndPoint;

        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("password", password);
        form.AddField("email", email);

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
            CreateAccResponseFromNodeServer createResponse = JsonUtility.FromJson<CreateAccResponseFromNodeServer>(request.downloadHandler.text);

            // response from nodejs server compare to do the following..
            if (createResponse.code == 0)
            {
                GameAccount createUserData = new GameAccount();
                createUserData.gameData.gold = 0;
                createUserData.gameData.gems = 0;
                createUserData.gameData.level = 0;
                createUserData.gameData.experiencePoints = 0;
                // upload this manual filled data to the server for test purposes
                createResponse.userData = createUserData;
                // loginButton.interactable = true;
                // createaccButton.interactable = true;
                // GameAccount returnedAccount = JsonUtility.FromJson<GameAccount>(request.downloadHandler.text);
                alert_text.text = "Account created! Logg in...";
                OnNewMessageReceived("Account created! Log in...");

            }
            else
            {
                switch (createResponse.code)
                {
                    case 1:
                        alert_text_forCreateAccPanel.text = "All fields are required";
                        OnNewMessageReceived("All fields are required");

                        loginButton.interactable = true;
                        createaccButton.interactable = true;
                        break;
                    case 2:
                        alert_text_forCreateAccPanel.text = "Username already exists, please choose another one";
                        OnNewMessageReceived("Username already exists, please choose another one");

                        loginButton.interactable = true;
                        createaccButton.interactable = true;
                        break;
                    case 3:
                        alert_text_forCreateAccPanel.text = "Password is too weak, please choose a stronger one";
                        OnNewMessageReceived("Password is too weak, please choose a stronger one");

                        loginButton.interactable = true;
                        createaccButton.interactable = true;
                        break;
                    case 99:
                        alert_text_forCreateAccPanel.text = "Too many login attempts. Please try again later";
                        OnNewMessageReceived("Too many login attempts. Please try again later");
                        loginButton.interactable = false;
                        createaccButton.interactable = false;
                        break;
                    default:
                        alert_text_forCreateAccPanel.text = "Unknown error occurred or Corrupted data";
                        OnNewMessageReceived("Unknown error occurred or Corrupted data");
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
            alert_text_forCreateAccPanel.text = "Error connection to server.";
            OnNewMessageReceived("Error connection to server.");
        }

        // Clear the input fields after login attempt
        usernameInputForCreateAcc.text = string.Empty;
        passwordInputForCreateAcc.text = string.Empty;
        emailInputForCreateAcc.text = string.Empty;
        yield return null;
    }

    public void SaveTokenAfterLogin(string jwtToken)
    {
        if (!string.IsNullOrEmpty(jwtToken))
        {
            tokenManager.SaveEncryptedToken(jwtToken);
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

    // take input from the user for saving it to the server
    public void OnSaveButtonClick()
    {
        // Check if the user is logged in
        if (string.IsNullOrEmpty(loggedInUser))
        {
            Debug.LogWarning("User not logged in. Please log in first.");
            return;
        }
        // Check if the token is valid
        string token = tokenManager.LoadDecryptedToken();
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

    // This method format the game data for the server in json 
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
    // This method sends the game data to the server using a PUT request
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

    #region // Forgot Password
    public TMP_InputField emailInputForForgotPassword;

    public void OnForgotPasswordClick()
    {
        string email = emailInputForForgotPassword.text.Trim();

        if (!IsValidEmail(email))
        {
            alert_text.text = "Invalid email format.";
            OnNewMessageReceived("Invalid email format.");
            return;
        }

        StartCoroutine(SendForgotPasswordRequest(email));
    }
    private IEnumerator SendForgotPasswordRequest(string email)
    {
        //endpoint for forgot password
        string forgotPasswordUrl = "https://nodejs-server-for-unity3dgame-login-5vxc.onrender.com/u3d/forgotPassword"; 

        WWWForm form = new WWWForm();
        form.AddField("email", email);

        UnityWebRequest request = UnityWebRequest.Post(forgotPasswordUrl, form);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            ResponseFromServer response = JsonUtility.FromJson<ResponseFromServer>(request.downloadHandler.text);
            Debug.Log("Forgot password request sent successfully.");
            // alert_text.text = "If the email exists, a reset link has been sent.";
            // alert_text.text = response.message; // Use the message from the server response
            OnNewMessageReceived(response.message);
        }
        else
        {
            ResponseFromServer response = JsonUtility.FromJson<ResponseFromServer>(request.downloadHandler.text);
            Debug.Log(response.message + " : Error sending forgot password request: " + request.error);
            // alert_text.text = "Error sending forgot password request.";
            OnNewMessageReceived(response.message); // Use the message from the server response
        }
    }
    #endregion

}

