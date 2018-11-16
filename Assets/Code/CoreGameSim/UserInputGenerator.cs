using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserInputGenerator : MonoBehaviour
{
    public string m_strVertical;
    public string m_strHorizontal;
    public string m_strStartGameShortcut;

    public bool m_bStartGame;

    public byte m_bOldInput
    {
        get; private set;
    }

    public byte m_bCurrentInput
    {
        get; private set;
    }

    //has there been a change in inputs 
    public bool HasNewInputs
    {
        get
        {
            return (m_bCurrentInput != m_bOldInput);
        }
    }

    // Update is called once per frame
    void Update()
    {
        byte bNewInput = (byte)InputKeyFrame.Input.None;

        m_bStartGame = Input.GetKey(m_strStartGameShortcut);

        float fInput = Input.GetAxisRaw(m_strVertical);

        if (fInput > 0.5f)
        {
            bNewInput = (byte)(bNewInput | (byte)InputKeyFrame.Input.Up);
        }

        if (fInput < -0.5f)
        {
            bNewInput = (byte)(bNewInput | (byte)InputKeyFrame.Input.Down);
        }

        fInput = Input.GetAxisRaw(m_strHorizontal);


        if (fInput > 0.5)
        {
            bNewInput = (byte)(bNewInput | (byte)InputKeyFrame.Input.Right);
        }

        if (fInput < -0.5)
        {
            bNewInput = (byte)(bNewInput | (byte)InputKeyFrame.Input.Left);
        }

        if(bNewInput != m_bCurrentInput)
        {
            m_bCurrentInput = bNewInput;
        }
    }

    public void UpdateInputState()
    {
        m_bOldInput = m_bCurrentInput;
    }
}
