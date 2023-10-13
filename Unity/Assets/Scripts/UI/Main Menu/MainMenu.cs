using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MainMenu : MonoBehaviour
{
    public GameObject MainMenuContainer;
    public GameObject LogInContainer;
    public GameObject ServerStuff;
    public GameObject CharacterSelect;

    public GameObject characterButton;

    public TextMeshProUGUI txtStatus;

    public List<TMP_InputField> inputFields;

    NetworkManager networkManager;

    public bool awaitingCharSelect;
    private void Start()
    {
        MainMenuContainer.SetActive(true);
        LogInContainer.SetActive(false);
        ServerStuff.SetActive(false);
        CharacterSelect.SetActive(false);

        networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
        awaitingCharSelect = false;
    }

    private void Update()
    {
        if (LogInContainer.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                SelectNextInputField();
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                GameObject.Find("Database").GetComponent<LoginScript>().OnLoginButtonClicked();
            }
        }
    }

    void SelectNextInputField()
    {
        EventSystem currentEventSystem = EventSystem.current;
        GameObject currentSelected = currentEventSystem.currentSelectedGameObject;

        if (currentSelected == null) return;

        TMP_InputField currentInputField = currentSelected.GetComponent<TMP_InputField>();
        if (currentInputField == null) return;

        int currentIndex = inputFields.IndexOf(currentInputField);
        if (currentIndex == -1) return;

        // Calculate the next index (loop around if needed)
        int nextIndex = (currentIndex + 1) % inputFields.Count;
        currentEventSystem.SetSelectedGameObject(inputFields[nextIndex].gameObject);
    }

    public void EnableCharacterSelect()
    {
        ServerStuff.SetActive(false);
        CharacterSelect.SetActive(true);
    }
    public void PopulateCharacterSelect(PacketSerialization.CharacterData[] characterData)
    {
        int characterCount = 0;
        int posX = -10;
        int posY = -10;

        foreach (PacketSerialization.CharacterData charData in characterData)
        {
            characterCount++;

            GameObject charButton = GameObject.Instantiate(characterButton);
            charButton.transform.SetParent(CharacterSelect.transform, false);
            charButton.transform.name = "Character Button " + characterCount.ToString();

            charButton.GetComponent<RectTransform>().anchoredPosition = new Vector3(posX, posY, 0);  

            CharacterButton cb = charButton.GetComponent<CharacterButton>();
            cb.SetChildren(charData);
            cb.charName.text = charData.name;
            cb.charClass.text = "Level " + charData.combatLevel.ToString() + " " + charData.classType;
            cb.charTotalLevel.text = "Total Level 1";

            posY -= 100;
        }

        awaitingCharSelect = true;
    }

    public void NoCharacters()
    {
        
    }

    public void CharacterButtonClicked(GameObject clickedCharacter)
    {
        CharacterButton cb = clickedCharacter.GetComponent<CharacterButton>();
        networkManager.ChooseCharacter(cb.charName.text, cb.charData);
    }

    public void LogInMenuClick()
    {
        // Disable the other buttons
        MainMenuContainer.SetActive(false);
        LogInContainer.SetActive(true);
        EventSystem.current.SetSelectedGameObject(inputFields[0].gameObject);
    }
    
    public void OnLogIn()
    {
        LogInContainer.SetActive(false);
        ServerStuff.SetActive(true);
    }

    public void LogInAttemptFailed()
    {
        LogInContainer.SetActive(true);
        SetStatusText("Log In Attempt Failed");
    }

    public void LogInFailed()
    {
        LogInContainer.SetActive(true);
        SetStatusText("Unable to connect to server");
    }

    public void AlreadyLoggedIn()
    {
        LogInContainer.SetActive(true);
        SetStatusText("This account is already logged in");
    }

    public void HandshakeFailed()
    {
        LogInContainer.SetActive(true);
        SetStatusText("Unable to contact server on UDP");
    }

    public void IncorrectLogIn()
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            LogInContainer.SetActive(true);
        });
        
        SetStatusText("Incorrect username or password");
    }

    public void SetStatusText(string statusText)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
            txtStatus.text = statusText;
        });
    }
}