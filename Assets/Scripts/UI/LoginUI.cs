using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Network; 

public class LoginUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button loginButton;

    void Start()
    {
        loginButton.onClick.AddListener(OnLoginClicked);
    }

    private void OnLoginClicked()
    {
        string user = usernameInput.text;
        string pass = passwordInput.text;

        statusText.text = "Logging in...";

        
        StartCoroutine(APIManager.Instance.Login(user, pass, (success) => {
            if (success)
            {
                statusText.text = "Login Success! Joining game...";
                statusText.color = Color.green;

               
                JoinGame();
            }
            else
            {
                statusText.text = "Login Failed. Check credentials.";
                statusText.color = Color.red;
            }
        }));
    }

    private void JoinGame()
    {
       
        NetworkSessionManager.Instance.StartGame(Fusion.GameMode.AutoHostOrClient);
    }
}