using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Network; 

public class LoginManager : MonoBehaviour
{
    [Header("UI Panels")]
    
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject gameHUD;

    [Header("Input References")]
    [SerializeField] private TMP_InputField usernameField;
    [SerializeField] private TMP_InputField passwordField;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button loginButton;

    [Header("Register Fields")]
    [SerializeField] private TMP_InputField regUser;
    [SerializeField] private TMP_InputField regEmail;
    [SerializeField] private TMP_InputField regPass;
    [SerializeField] private Button finalizeRegisterButton;

    private void Start()
    {
        
        loginPanel.SetActive(true);
        gameHUD.SetActive(false);

        loginButton.onClick.AddListener(HandleLogin);
    }

    public void HandleLogin()
    {
        string user = usernameField.text;
        string pass = passwordField.text;

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            statusText.text = "Please enter credentials";
            return;
        }

        statusText.text = "Connecting...";
        loginButton.interactable = false;

        StartCoroutine(APIManager.Instance.Login(user, pass, (success) => {
            if (success)
            {
                
                loginPanel.SetActive(false);
                gameHUD.SetActive(true);

                
                NetworkSessionManager.Instance.StartGame(Fusion.GameMode.AutoHostOrClient);
            }
            else
            {
                statusText.text = "Login Failed!";
                loginButton.interactable = true;
            }
        }));
    }
    public void ShowRegisterPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
    }

    public void ShowLoginPanel()
    {
        registerPanel.SetActive(false);
        loginPanel.SetActive(true);
    }

    public void HandleRegister()
    {
        string user = regUser.text.Trim();
        string email = regEmail.text.Trim();
        string pass = regPass.text.Trim();

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(email))
        {
            statusText.text = "All fields are required!";
            return;
        }

        statusText.text = "Creating account...";

        StartCoroutine(APIManager.Instance.Register(user, email, pass, (success, message) => {
            if (success)
            {
                
                statusText.text = "Registration Successful! Please log in.";
                statusText.color = Color.green;

                
                ClearRegisterFields();

                
                ShowLoginPanel();
            }
            else
            {
                statusText.text = message;
                statusText.color = Color.red;
            }
        }));
    }

    private void ClearRegisterFields()
    {
        regUser.text = "";
        regEmail.text = "";
        regPass.text = "";
    }
}