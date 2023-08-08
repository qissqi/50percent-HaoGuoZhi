using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StartPanel : MonoBehaviour,IPointerClickHandler
{
    [SerializeField] private GameObject namePanel;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private GameObject mainMenu;
    
    public void NameConfirmClick()
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            return;
        }
        else
        {
            GameManager.Instance.playerName = inputField.text;
            namePanel.SetActive(false);
            EnterMainMenu();
        }
    }

    //点击屏幕，判断是否要输入名字
    public void OnPointerClick(PointerEventData eventData)
    {
        //已经弹出名字界面，返回
        if(namePanel.activeSelf)
            return;
        
        //已经输入名字，进入主界面
        if (!string.IsNullOrEmpty(GameManager.Instance.playerName))
        {
            EnterMainMenu();
        }
        else
        {
            namePanel.SetActive(true);
        }

    }

    private void EnterMainMenu()
    {
        mainMenu.SetActive(true);
        gameObject.SetActive(false);
    }
}
