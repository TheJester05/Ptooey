using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

public class APIManager : MonoBehaviour
{
    public static APIManager Instance;

    // This property allows other scripts (like NetworkPlayer) to access the name
    public string AuthenticatedUsername { get; private set; }

    private string authToken;
    private string baseUrl = "http://localhost:3000/api/players";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Keeps the API Manager alive when moving from Login to Game scene
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

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AuthResponse res = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);

                // SAVE DATA HERE
                authToken = res.token;
                AuthenticatedUsername = res.data.username; // Save the name for NetworkPlayer to use

                Debug.Log($"Logged in as: {AuthenticatedUsername}");
                callback?.Invoke(true);
            }
            else
            {
                Debug.LogError("Login Error: " + request.error);
                callback?.Invoke(false);
            }
        }
    }

    public IEnumerator UpdatePlayerScore(int pointsEarned)
    {
        string json = "{\"score\":" + pointsEarned + "}";

        using (UnityWebRequest request = UnityWebRequest.Put(baseUrl + "/score", json))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + authToken);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Score synced to MongoDB Atlas");
            }
        }
    }
}