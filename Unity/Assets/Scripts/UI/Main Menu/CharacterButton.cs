using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterButton : MonoBehaviour
{
    public TextMeshProUGUI charName;
    public TextMeshProUGUI charClass;
    public TextMeshProUGUI charTotalLevel;

    public PacketSerialization.CharacterData charData;
    
    MainMenu mainMenu;
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(CharacterButtonClicked);
        mainMenu = GameObject.Find("Canvas").GetComponent<MainMenu>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CharacterButtonClicked()
    {
        mainMenu.CharacterButtonClicked(this.gameObject);
    }

    public void SetChildren(PacketSerialization.CharacterData cd)
    {
        charData = cd;
        charName = transform.Find("txtCharName").GetComponent<TextMeshProUGUI>();
        charClass = transform.Find("txtCharClass").GetComponent<TextMeshProUGUI>();
        charTotalLevel = transform.Find("txtCharTotalLevel").GetComponent<TextMeshProUGUI>();
    }
}
