using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity;
using UnityEngine.UI;
using System;

public class GameStartupUI : MonoBehaviour
{
    public Text m_txtGameStataOut;

    public void UpdateGameState(string strGameState)
    {       
        m_txtGameStataOut.text = strGameState + "/n" + m_txtGameStataOut.text;
    }

    public void OnEnterState()
    {
        gameObject.SetActive(true);
    }

    public void OnExitState()
    {
        gameObject.SetActive(false);
    }
}
