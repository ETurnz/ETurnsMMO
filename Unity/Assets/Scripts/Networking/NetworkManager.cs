using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class NetworkManager : MonoBehaviour
{
    public TMP_InputField emailField;
    public TMP_InputField passwordField;

    public GameObject player;
    public GameObject networkPlayer;

    public PacketSerialization.CharacterData chosenCharacter;
    private Dictionary<string, GameObject> nameToNetworkObject = new Dictionary<string, GameObject>();


    GameClient2 gameClient2;
    public GameMenu gameMenu;
    MainMenu mainMenu;

    public bool assignedID;
    public bool isHandshakeSuccessful;

    public string uniqueID;
    private string email;
    private string hashedPW;

    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        Application.runInBackground = true;
        Application.backgroundLoadingPriority = ThreadPriority.High;

        assignedID = false;
        isHandshakeSuccessful = false;

        mainMenu = GameObject.Find("Canvas").GetComponent<MainMenu>();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    public void ParseTcpMessage(string message)
    {
        PacketSerialization.BaseMessage baseMessage = JsonConvert.DeserializeObject<PacketSerialization.BaseMessage>(message);

        if (!assignedID)
        {
            uniqueID = baseMessage.clientID;
            assignedID = true;

            mainMenu.SetStatusText("Handshaking");

            MainThreadDispatcher.Enqueue(() => StartCoroutine(TrySendUdpHandshake(uniqueID, 5)));
        }
        //Check if message was intended for us
        else if (baseMessage.clientID == uniqueID)
        {
            if (baseMessage.messageType == "PING")
            {
                PacketSerialization.BaseMessage pingResponse = new PacketSerialization.BaseMessage(uniqueID, "PONG");
                gameClient2.SendTcpMessageToServer(JsonConvert.SerializeObject(pingResponse));
            }
            else if (baseMessage.messageType == "UDP_HANDSHAKE_SUCCESS")
            {
                isHandshakeSuccessful = true;
                mainMenu.SetStatusText("Logging in...");

                PacketSerialization.LogInAttempt loginData = new PacketSerialization.LogInAttempt(uniqueID, "LOG_IN_ATTEMPT", email, hashedPW);
                gameClient2.SendTcpMessageToServer(JsonConvert.SerializeObject(loginData));
            }
            else if (baseMessage.messageType == "ALREADY_LOGGED_IN")
            {
                MainThreadDispatcher.Enqueue(() => {
                    mainMenu.AlreadyLoggedIn();
                    LogOut();
                });
            }
            else if (baseMessage.messageType == "LOG_IN_SUCCESS")
            {
                MainThreadDispatcher.Enqueue(() => {
                    mainMenu.txtStatus.text = "Downloading character data";
                });
            }
            else if (baseMessage.messageType == "LOG_IN_FAILED")
            {
                MainThreadDispatcher.Enqueue(() => {
                    mainMenu.IncorrectLogIn();
                    LogOut();
                });
            }
            else if (baseMessage.messageType == "LOGIN_ATTEMPT_FAILED")
            {
                MainThreadDispatcher.Enqueue(() => {
                    mainMenu.LogInAttemptFailed();
                    LogOut();
                });
            }
            else if (baseMessage.messageType == "CHAR_DOWNLOAD_SUCCESS")
            {
                PacketSerialization.CharacterDataResponse charData = JsonConvert.DeserializeObject<PacketSerialization.CharacterDataResponse>(message);

                MainThreadDispatcher.Enqueue(() => {
                    mainMenu.EnableCharacterSelect();
                    mainMenu.PopulateCharacterSelect(charData.characters);
                });
            }
            else if (baseMessage.messageType == "NO_CHAR_DATA_FOUND")
            {
                mainMenu.NoCharacters();
                Debug.Log(baseMessage.messageType);
            }
            else if (baseMessage.messageType == "CHARACTER_SELECT_SUCESS")
            {
                if (mainMenu.awaitingCharSelect)
                {
                    MainThreadDispatcher.Enqueue(() => {
                        SceneManager.LoadScene("game");
                    });
                }
            }
            else if (baseMessage.messageType == "LOAD_CHARACTER")
            {
                PacketSerialization.LoadCharacter characterToLoad = JsonConvert.DeserializeObject<PacketSerialization.LoadCharacter>(message);
                Debug.Log("Loading character, their zone ID is " + characterToLoad.characterData.zoneID);

                MainThreadDispatcher.Enqueue(() => {
                    GameObject g = GameObject.Instantiate(networkPlayer);
                    g.GetComponent<NetworkCharacter>().charName = characterToLoad.characterData.name;
                    g.GetComponent<NetworkCharacter>().zoneID = characterToLoad.characterData.zoneID;

                    Vector3 position = new Vector3(characterToLoad.characterData.posX, characterToLoad.characterData.posY, characterToLoad.characterData.posZ);
                    Quaternion rotation = new Quaternion(characterToLoad.characterData.rotX, characterToLoad.characterData.rotY, characterToLoad.characterData.rotZ, characterToLoad.characterData.rotW);

                    g.transform.position = position;
                    g.transform.rotation = rotation;

                    nameToNetworkObject[characterToLoad.characterData.name] = g;
                });
            }
            else if (baseMessage.messageType == "LOAD_COMPLETE")
            {
                PacketSerialization.BaseMessage toSend = new PacketSerialization.BaseMessage(uniqueID, "LOADED_WORLD");
                gameClient2.SendTcpMessageToServer(JsonConvert.SerializeObject(toSend));
            }
            else if (baseMessage.messageType == "UNLOAD_CHARACTER")
            {
                PacketSerialization.LoadCharacter characterToUnload = JsonConvert.DeserializeObject<PacketSerialization.LoadCharacter>(message);

                if (nameToNetworkObject.TryGetValue(characterToUnload.characterData.name, out GameObject networkObject))
                {
                    MainThreadDispatcher.Enqueue(() => {
                        Debug.Log("Attempting to destroy: " + characterToUnload.characterData.name);

                        GameObject.Destroy(networkObject);
                        nameToNetworkObject.Remove(characterToUnload.characterData.name);
                    });
                }
                else
                {
                    Debug.Log("Couldn't find person to destroy");
                }
            }
            else if (baseMessage.messageType == "DISCONNECT")
            {
                gameClient2.Disconnect();
            }
            else if (baseMessage.messageType == "INVITE_REQUEST")
            {
                if (gameMenu != null)
                {
                    PacketSerialization.PlayerInvite inviteRequest = JsonConvert.DeserializeObject<PacketSerialization.PlayerInvite>(message);
                    MainThreadDispatcher.Enqueue(() => {
                        gameMenu.InvitedByPlayer(inviteRequest.inviteName);
                    });
                }
                else
                {
                    Debug.LogError("Game menu null when processing invite request!");
                }
            }
            else if (baseMessage.messageType == "PARTY_UPDATE")
            {
                PacketSerialization.PartyUpdate info = JsonConvert.DeserializeObject<PacketSerialization.PartyUpdate>(message);
                MainThreadDispatcher.Enqueue(() => {
                    gameMenu.UpdateParty(info);
                });
            }
            else
            {
                Debug.LogWarning("Received a message we could not parse");
            }
        }
        else
        {
            Debug.LogWarning("Received a message that wasn't for us!");
        }
    }

    public void ParseUdpMessage(string message)
    {
        PacketSerialization.BaseMessage baseMessage = JsonConvert.DeserializeObject<PacketSerialization.BaseMessage>(message);

        //Check if message was intended for us
        if (baseMessage.clientID == uniqueID)
        {
            if (baseMessage.messageType == "UPDATE_PLAYER")
            {
                PacketSerialization.PlayerUpdate playerToUpdate = JsonConvert.DeserializeObject<PacketSerialization.PlayerUpdate>(message);

                if (nameToNetworkObject.TryGetValue(playerToUpdate.name, out GameObject networkObject))
                {
                    Vector3 pos = new Vector3(playerToUpdate.posX, playerToUpdate.posY, playerToUpdate.posZ);
                    Quaternion rot = new Quaternion(playerToUpdate.rotX, playerToUpdate.rotY, playerToUpdate.rotZ, playerToUpdate.rotW);

                    NetworkCharacter nc = networkObject.GetComponent<NetworkCharacter>();
                    nc.SetTarget(pos, rot);
                    nc.SetRunning(playerToUpdate.IsRunning);
                    nc.SetRolling(playerToUpdate.IsRolling);
                    nc.SetStrafing(playerToUpdate.IsStrafingLeft, playerToUpdate.IsStrafingRight);
                }
                else if (baseMessage.messageType == "DISCONNECT")
                {
                    gameClient2.Disconnect();
                }
                else
                {
                    //Debug.LogError("Matched character is null");
                }
            }

        }
        else
        {
            Debug.LogWarning("Received a message that wasn't for us!");
        }
    }

    public void SendPositionUpdate(Vector3 position, Quaternion rotation, bool IsRunning, bool IsRolling, bool IsStrafingLeft, bool IsStrafingRight)
    {
        PacketSerialization.PlayerUpdate toSend = new PacketSerialization.PlayerUpdate(uniqueID, "UPDATE_POSITION", chosenCharacter.name, position.x, position.y, position.z, rotation.x, rotation.y, rotation.z, rotation.w, IsRunning, IsRolling, IsStrafingLeft, IsStrafingRight);
        gameClient2.SendUdpMessage(JsonConvert.SerializeObject(toSend));
    }

    public void UpdateZone(int zoneID)
    {
        PacketSerialization.BaseMessage toSend = new PacketSerialization.BaseMessage(uniqueID, "ZONE_ID:" + zoneID);
        gameClient2.SendTcpMessageToServer(JsonConvert.SerializeObject(toSend));
    }

    public void LeaveZone(int zoneID)
    {
        PacketSerialization.BaseMessage toSend = new PacketSerialization.BaseMessage(uniqueID, "LEAVE_ZONE:" + zoneID);

        // Loop through the dictionary and unload players with matching zoneID
        var namesToRemove = new List<string>();
        foreach (var pair in nameToNetworkObject)
        {
            NetworkCharacter nc = pair.Value.GetComponent<NetworkCharacter>();
            if (nc.zoneID == zoneID)
            {
                namesToRemove.Add(pair.Key);
                GameObject.Destroy(pair.Value);
            }
        }

        foreach (var name in namesToRemove)
        {
            nameToNetworkObject.Remove(name);
        }

        gameClient2.SendTcpMessageToServer(JsonConvert.SerializeObject(toSend));
    }

    public void InvitePlayer(string toInvite)
    {
        PacketSerialization.PlayerInvite invitePacket = new PacketSerialization.PlayerInvite(uniqueID, "INVITE_PLAYER", toInvite);
        gameClient2.SendTcpMessageToServer(JsonConvert.SerializeObject(invitePacket));
    }

    public void AcceptInvite()
    {
        PacketSerialization.BaseMessage inviteResponse = new PacketSerialization.BaseMessage(uniqueID, "ACCEPT_INVITE");
        gameClient2.SendTcpMessageToServer(JsonConvert.SerializeObject(inviteResponse));
    }

    public void DeclineInvite()
    {
        PacketSerialization.BaseMessage inviteResponse = new PacketSerialization.BaseMessage(uniqueID, "DECLINE_INVITE");
        gameClient2.SendTcpMessageToServer(JsonConvert.SerializeObject(inviteResponse));
    }

    public void RequestDisconnect()
    {
        PacketSerialization.BaseMessage disconnectMessage = new PacketSerialization.BaseMessage(uniqueID, "DISCONNECT_ME");
        gameClient2.SendTcpMessageToServer(JsonConvert.SerializeObject(disconnectMessage));
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check the name of the loaded scene
        if (scene.name == "game")
        {
            //PacketSerialization.BaseMessage toSend = new PacketSerialization.BaseMessage(uniqueID, "LOADED_WORLD");
            //gameClient2.SendTcpMessageToServer(JsonConvert.SerializeObject(toSend));

            GameObject p = GameObject.Instantiate(player);

            Vector3 position = new Vector3(chosenCharacter.posX, chosenCharacter.posY, chosenCharacter.posZ);
            p.transform.Find("character").position = position;

            p.transform.Find("characterUICanvas/Player Unit Frame/playerName").GetComponent<Text>().text = chosenCharacter.name;
            p.transform.Find("characterUICanvas/Player Unit Frame/Level/playerLevel").GetComponent<Text>().text = chosenCharacter.combatLevel.ToString();
        }
    }

    IEnumerator TrySendUdpHandshake(string id, int maxAttempts)
    {
        PacketSerialization.BaseMessage toSend = new PacketSerialization.BaseMessage();
        toSend.clientID = id;
        toSend.messageType = "UDP_HANDSHAKE_ATTEMPT";

        int attempts = 0;
        float delay = 1f; // Initial delay (1 second)

        while (attempts < maxAttempts && !isHandshakeSuccessful)
        {
            gameClient2.SendUdpMessage(JsonConvert.SerializeObject(toSend));

            yield return new WaitForSeconds(delay);

            attempts++;
            delay *= 2; // Double the delay for the next attempt
        }

        // If handshake still failed after all attempts
        if (!isHandshakeSuccessful)
        {
            mainMenu.HandshakeFailed();
            LogOut();
        }
    }

    public void ChooseCharacter(string name, PacketSerialization.CharacterData cd)
    {
        PacketSerialization.BasicText chosenChar = new PacketSerialization.BasicText(uniqueID, "CHOOSE_CHARACTER", name);
        gameClient2.SendTcpMessageToServer(JsonConvert.SerializeObject(chosenChar));

        chosenCharacter = cd;
    }

    public void LogOut()
    {
        uniqueID = null;
        assignedID = false;
        MainThreadDispatcher.Enqueue(() => {
            gameClient2.Disconnect();
            GameObject.Destroy(gameClient2.gameObject);
        });
    }

    public void setGC2(GameClient2 gc2)
    {
        gameClient2 = gc2;
    }

    public void SetLogInInfo(string e, string hPW)
    {
        email = e;
        hashedPW = hPW;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
