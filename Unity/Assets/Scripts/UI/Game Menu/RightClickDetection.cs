using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RightClickDetection : MonoBehaviour, IPointerClickHandler
{
    public GameMenu gameMenu;
    public GameObject targetMenu;
    public GameObject canvas;

    NetworkManager networkManager;

    GameObject instantiatedMenu;
    // Start is called before the first frame update
    void Start()
    {
        networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (eventData.pointerCurrentRaycast.gameObject.name == "btnFrameClickText")
            {
                if (instantiatedMenu != null)
                {
                    DestroyMenu();
                }

                gameMenu.rightClickMenuOpen = true;
                instantiatedMenu = GameObject.Instantiate(targetMenu);

                string playerName = transform.parent.parent.Find("playerName").GetComponent<Text>().text;

                TargetRightClick rightClickMenu = instantiatedMenu.GetComponent<TargetRightClick>();
                rightClickMenu.SetName(playerName);
                rightClickMenu.SetNetworkObject(networkManager);
                rightClickMenu.SetGameMenu(gameMenu);

                // Convert screen position to canvas position
                RectTransform canvasRectTransform = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
                RectTransform instantiatedMenuRect = instantiatedMenu.GetComponent<RectTransform>();

                instantiatedMenuRect.SetParent(canvasRectTransform, false);

                Vector2 canvasPosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, eventData.position, eventData.pressEventCamera, out canvasPosition);

                // Adjust the position so the top-left corner of the panel is at the clicked position
                canvasPosition.x += instantiatedMenuRect.sizeDelta.x * 0.5f;
                canvasPosition.y -= instantiatedMenuRect.sizeDelta.y * 0.5f;

                instantiatedMenuRect.anchoredPosition = canvasPosition;
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (instantiatedMenu != null)
            {
                DestroyMenu();
            }
        }
    }

    public void DestroyMenu()
    {
        GameObject.Destroy(instantiatedMenu);
        instantiatedMenu = null;
    }
}
