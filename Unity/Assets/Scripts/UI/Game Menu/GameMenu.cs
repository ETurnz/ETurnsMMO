using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class GameMenu : MonoBehaviour
{
    public GameObject targetUnitFrame;
    public RightClickDetection rightClickDetection;

    public GameObject[] partyFrames;
    public Text[] partyNames;

    GameObject escapeMenu;
    GameObject inviteMenu;
    NetworkManager networkManager;
    Text targetNameText;

    bool EscapeMenuOpen;
    bool targetFrameEnabled;

    public bool rightClickMenuOpen;
    // Start is called before the first frame update
    void Start()
    {
        networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
        networkManager.gameMenu = this;

        escapeMenu = transform.Find("EscapeOptions").gameObject;
        inviteMenu = transform.Find("InviteMenu").gameObject;
        targetNameText = transform.Find("Target Unit Frame/playerName").GetComponent<Text>();
        EscapeMenuOpen = false;
        rightClickMenuOpen = false;
        targetFrameEnabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (rightClickMenuOpen)
            {
                DestroyRightClickMenu();
            }
            else if (targetFrameEnabled)
            {
                DisableTargetFrame();
            }
            else
                ToggleEscapeMenu();
        }
    }

    public void UpdateParty(PacketSerialization.PartyUpdate partyInfo)
    {
        int partyCount = partyInfo.partyMembers.Count;
        int j = 0;

        for (int i = 0; i < partyFrames.Length; i++)
        {
            // Activate the frame if the party count is greater than the current index
            partyFrames[i].SetActive(i < partyCount - 1);

            if (i < partyCount)
            {
                string partyName = partyInfo.partyMembers[i].playerName;

                if (partyName != networkManager.chosenCharacter.name)
                {
                    partyNames[j].text = partyName;
                    Debug.LogError(partyName);
                    j++;
                }
            }
        }
    }

    public void DestroyRightClickMenu()
    {
        rightClickDetection.DestroyMenu();
        rightClickMenuOpen = false;
    }

    public void InvitedByPlayer(string playerName)
    {
        inviteMenu.transform.Find("txtInvite").GetComponent<TextMeshProUGUI>().text = $"Player { playerName } invites you to join their group";
        inviteMenu.SetActive(true);
    }

    public void EnableTargetFrame(string targetName)
    {
        targetNameText.text = targetName;
        targetUnitFrame.SetActive(true);
        targetFrameEnabled = true;
    }

    public void DisableTargetFrame()
    {
        targetUnitFrame.SetActive(false);
        targetFrameEnabled = false;
    }

    public void AcceptInvite()
    {
        inviteMenu.SetActive(false);
        networkManager.AcceptInvite();
    }

    public void DeclineInvite()
    {
        inviteMenu.SetActive(false);
        networkManager.DeclineInvite();
    }

    void ToggleEscapeMenu()
    {
        EscapeMenuOpen = !EscapeMenuOpen;
        escapeMenu.SetActive(EscapeMenuOpen);
    }

    public void LogOutClicked()
    {
        networkManager.LogOut();
    }
}
