using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetRightClick : MonoBehaviour
{
    GameMenu gameMenu;
    NetworkManager networkManager;

    string playerName;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InviteOnClick()
    {
        networkManager.InvitePlayer(playerName);
        gameMenu.DestroyRightClickMenu();
    }

    public void SetName(string n)
    {
        playerName = n;
    }

    public void SetNetworkObject(NetworkManager nm)
    {
        networkManager = nm;
    }

    public void SetGameMenu(GameMenu gm)
    {
        gameMenu = gm;
    }
}
