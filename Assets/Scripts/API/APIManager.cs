using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

public class APIManager : MonoBehaviour
{
    public static APIManager Instance;

    public string AuthenticatedUsername { get; private set; }
    private string authToken;

    
    private string baseUrl = "http://127.0.0.1:3000/api/players";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Serializable]
    public class AuthData { public string username; public string email; public string password; }

    [Serializable]
    public class AuthResponse { public bool success; public string token; public PlayerData data; }

    [Serializable]
    public class PlayerData { public string id; public string username; public int score; }

    public IEnumerator Login(string username, string password, Action<bool> callback)
    {
        AuthData auth = new AuthData { username = username, password = password };
        string json = JsonUtility.ToJson(auth);

        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/login", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AuthResponse res = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                authToken = res.token;
                AuthenticatedUsername = res.data.username;
                Debug.Log($"Logged in as: {AuthenticatedUsername}");
                callback?.Invoke(true);
            }
            else
            {
                
                Debug.LogError($"Login Error {request.responseCode}: {request.error}");
                callback?.Invoke(false);
            }
        }
    }

    public IEnumerator Register(string username, string email, string password, Action<bool, string> callback)
    {
        AuthData auth = new AuthData { username = username, email = email, password = password };
        string json = JsonUtility.ToJson(auth);

        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/register", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                
                callback?.Invoke(true, "Registration Successful!");
            }
            else
            {
                Debug.LogError($"Register Error {request.responseCode}: {request.error}");
                callback?.Invoke(false, "Registration Failed: " + request.error);
            }
        }
    }

    public IEnumerator UpdatePlayerScore(int pointsEarned)
    {
        string url = baseUrl + "/score";
        string json = "{\"score\":" + pointsEarned + "}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        
        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/score", "PUT"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + authToken);
            request.SetRequestHeader("User-Agent", "Mozilla/5.0");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Score synced to MongoDB Atlas: " + request.downloadHandler.text);
            }
            else
            {
                
                Debug.LogError($"Score Sync Failed {request.responseCode}: {request.error}");
            }
        }
    }
}