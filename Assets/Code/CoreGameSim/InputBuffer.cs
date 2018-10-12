using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputBuffer
{
    public int m_iBufferHead = 0;

    public List<InputKeyFrame> m_ikfInputBuffer;

    public InputBuffer(int iSize)
    {
        m_ikfInputBuffer = new List<InputKeyFrame>(iSize);

        for(int i= 0; i < iSize; i++)
        {
            m_ikfInputBuffer.Add(new InputKeyFrame());
        }

    }

    public int GetMappedIndex(int iIndex)
    {
        return HelperFunctions.mod((m_iBufferHead + iIndex), m_ikfInputBuffer.Count);
    }

    public bool TryGetIndexOfInputForTick(int iTick, out int iIndex)
    {
        //start at the front of the buffer 
        for (int i = 0; i < m_ikfInputBuffer.Count; i++)
        {
            //convert to mapped index 
            int iMappedIndex = GetMappedIndex(i);

            //check if keyframe is older than current Keyframe 
            if (m_ikfInputBuffer[iMappedIndex].m_iTick <= iTick)
            {
                iIndex = iMappedIndex;

                return true;
            }
        }

        iIndex = GetMappedIndex(-1);
        return false;
    }

    public int AddKeyFrames(InputKeyFrame[] ikfInputsToAdd)
    {       

        //get latest input 
        InputKeyFrame ikfLastRecievedInput = m_ikfInputBuffer[m_iBufferHead];

        //get latest tick 
        int iLatestTick = ikfLastRecievedInput.m_iTick;

        int iFirstNewKeyFrame = iLatestTick;

        //loop through new inputs 
        for (int i = ikfInputsToAdd.Length - 1; i >= 0; i--)
        {
            //check if this input is newer than the latest input 
            if(ikfInputsToAdd[i].m_iTick > iLatestTick)
            {
                //check if this is the last recieved new input 
                if(iFirstNewKeyFrame == iLatestTick)
                {
                    iFirstNewKeyFrame = ikfInputsToAdd[i].m_iTick;
                }
                //add to head of buffer 
                AddKeyframeToEndOfInputBuffer(ikfInputsToAdd[i]);
            }
        }

        return iFirstNewKeyFrame;
    }

    public void GetKeyFrameData(int iStartTick, List<InputKeyFrame> output)
    {
        output.Clear();

        //start at the front of the buffer 
        for (int i = 0; i < m_ikfInputBuffer.Count; i++)
        {
            //convert to mapped index 
            int iMappedIndex = GetMappedIndex(i);

            //check if keyframe is older than current Keyframe 
            if (m_ikfInputBuffer[iMappedIndex].m_iTick > iStartTick)
            {
                output.Add(m_ikfInputBuffer[iMappedIndex]);
            }
            else
            {
                return;
            }
        }
    }

    public void AddKeyframeToEndOfInputBuffer(InputKeyFrame ikfItemToAdd)
    {
        //move buffer head
        m_iBufferHead = HelperFunctions.mod((m_iBufferHead - 1) , m_ikfInputBuffer.Count);

        //set item 
        m_ikfInputBuffer[m_iBufferHead] = ikfItemToAdd;
    }
}
