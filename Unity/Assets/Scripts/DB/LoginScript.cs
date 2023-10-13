using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Security.Cryptography;
using System.Text;
using TMPro;

public class LoginScript : MonoBehaviour
{
    public MainMenu mainMenu;
    public GameObject client;
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public Text resultText;
    NetworkManager networkManager;

    private const string loginURL = "https://localhost/GameLogIn.php";
    private void Start()
    {
        networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
    }
    public void OnLoginButtonClicked()
    {
        string email = emailField.text;
        string hashedPW = ComputeHash(passwordField.text);

        //GameClient gc = GameObject.Instantiate(client).GetComponent<GameClient>();
        GameClient2 gc2 = GameObject.Instantiate(client).GetComponent<GameClient2>();
        networkManager.setGC2(gc2);

        mainMenu.OnLogIn();
        networkManager.SetLogInInfo(email, hashedPW);
    }

    private string ComputeHash(string input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = sha256.ComputeHash(bytes);
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < hashBytes.Length; i++)
            {
                builder.Append(hashBytes[i].ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
