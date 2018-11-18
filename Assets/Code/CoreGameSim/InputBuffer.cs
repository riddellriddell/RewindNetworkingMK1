using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
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

    public void GetHashCodeForInputs(byte[] bOutput, int iStartTick = 0, int iEndTick = int.MaxValue)
    {
        //loop through inputs untill the correct tick is found 
        int iStartIndex = -1;
        int iEndIndex = -1;

        for(int i = 0; i < m_ikfInputBuffer.Count; i++)
        {
            if(m_ikfInputBuffer[i].m_iTick >= iStartTick)
            {
                iStartIndex = iEndIndex = i;
                break;
            }
        }

        //no items are found in that range 
        if(iStartIndex < 0)
        {
            //clear hash code 
            for(int i = 0; i < bOutput.Length; i++)
            {
                bOutput[i] = 0;
            }

            return;
        }

        //get end index 
        for(;iEndIndex < m_ikfInputBuffer.Count; iEndIndex++)
        {
            if (m_ikfInputBuffer[iEndIndex].m_iTick > iEndTick)
            {
                break;
            }
        }

        //get size of each input entry 
        int iSizeOfInput = System.Runtime.InteropServices.Marshal.SizeOf<InputKeyFrame>();

        //calculate the number of inputs to hash
        int iInputsToHash = iEndIndex - iStartIndex;

        byte[] bDataToHash = new byte[iInputsToHash * iSizeOfInput];

        int iWriteHead = 0;

        //fill array to hash
        for(int i = 0; i < iInputsToHash; i++)
        {
            iWriteHead = m_ikfInputBuffer[iStartIndex + i].AddToByteArray(bDataToHash, iWriteHead);
        }

        MD5 md5 = MD5.Create();

        //generate the hash code 
        byte[] bHash = md5.ComputeHash(bDataToHash);

        //return output 
        for(int i = 0; i < bOutput.Length; i++)
        {
            bOutput[i] = bHash[i % bHash.Length];
        }
    }
}
