
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text.RegularExpressions;


public class accLogin : MonoBehaviour
{
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TextMeshProUGUI alert_text;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button createaccButton;


    private string loginEndPoint = "http://localhost:3000/u3d/login"; // Replace with your server URL
    private string createaccEndPoint = "http://localhost:3000/u3d/createacc"; // Replace with your server URL
    
    // define the pattern
    private static readonly Regex passwordRegex = new Regex(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)[A-Za-z\d]{6,25}$"); 

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
            // LoginResponseFromNodeServer loginResponse = JsonUtility.FromJson<LoginResponseFromNodeServer>(request.downloadHandler.text);

            if(loginResponse.code == 0){
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
            alert_text.text = loginResponse.message;
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
}
