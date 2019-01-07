using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sim
{
    public class UserInputGenerator : MonoBehaviour
    {
        public string m_strVertical;
        public string m_strHorizontal;
        public string m_strQuickAttack;
        public string m_strSlowAttack;
        public string m_strBlock;

        public string m_strStartGameShortcut;

        public bool m_bStartGame;

        public bool m_bGenerateRandomInput;

        public float m_fInputChangeRate = 0.2f;

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
        public void Update()
        {
            if(m_bGenerateRandomInput)
            {
                GenerateRandomInput();
            }
            else
            {
                FetchInputs();
            }


        }

        public void FetchInputs()
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


            //get attack and block commands
            if (Input.GetKey(m_strQuickAttack))
            {
                bNewInput = (byte)(bNewInput | (byte)InputKeyFrame.Input.QuickAttack);
            }


            if (Input.GetKey(m_strSlowAttack))
            {
                bNewInput = (byte)(bNewInput | (byte)InputKeyFrame.Input.SlowAttack);
            }


            if (Input.GetKey(m_strBlock))
            {
                bNewInput = (byte)(bNewInput | (byte)InputKeyFrame.Input.Block);
            }

            if (bNewInput != m_bCurrentInput)
            {
                m_bCurrentInput = bNewInput;
            }
        }

        public void GenerateRandomInput()
        {
            byte bflipper = 0;
            
            for(int i = 0; i < 8; i++)
            {
                bflipper = (byte)(bflipper << 1);

                float fRandomValue = Random.Range(0.0f, 1.0f);

                //check if this value should be flipped 
                if (fRandomValue < (m_fInputChangeRate * Time.deltaTime))
                {
                    bflipper += 1;
                }               
            }

            //flip the input bits 
            m_bCurrentInput = (byte)(m_bCurrentInput ^ bflipper);

            //turn off attacks 
            byte bAttackMask = ((byte)InputKeyFrame.Input.Block | (byte)InputKeyFrame.Input.SlowAttack | (byte)InputKeyFrame.Input.QuickAttack);

            bAttackMask = (byte)~bAttackMask;

            m_bCurrentInput = (byte)(m_bCurrentInput & bAttackMask);
        }

        public void UpdateInputState()
        {
            m_bOldInput = m_bCurrentInput;
        }
    }
}