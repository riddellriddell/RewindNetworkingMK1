using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CellDistanceKernal 
{
	public int[] x;
	public int[] y;
	public float[] Distance;
	public float[] NeighbourClosestDistance;
	//the size of the search volume
	public int _iXLength;
	public int _iYLength;
	
	//the size of the search kernal
	public float _fSearchSize;
	
	//the warp mode of the lookup
	public bool _bClamp;
	
	//is the search kernal setup
	protected bool _bIsSetup = false;
	
	public void SetupCheck(int iXLength, int iYLength, float fSearchSize,bool bClamp)
	{
		if(_bIsSetup == false)
		{
			InitalizeKernal(iXLength,iYLength,fSearchSize,bClamp);
			return;
		}
		
		if(iXLength != _iXLength|| iYLength != _iYLength || fSearchSize != _fSearchSize || bClamp != _bClamp)
		{
			InitalizeKernal(iXLength,iYLength,fSearchSize,bClamp);
		}
	}
	
	public void InitalizeKernal(int iXLength, int iYLength, float fSearchSize,bool bClamp)
	{
		//store values
		_iXLength = iXLength;
		_iYLength = iYLength;
		
		_fSearchSize = fSearchSize;
		
		_bClamp = bClamp;
		
		//the size of each pixle
		Vector2 vecPixleSize = new Vector2(1f/ _iXLength,1f/ _iYLength);
		
		
		
		//get the number of pixles accros the search zone 
		int iXPixSearchDist = (int)( _fSearchSize * 2 * _iXLength) + 2;
		int iYPixSearchDist = (int)(_fSearchSize * 2 * _iYLength) + 2;
		
		//	int iXPixSearchDist = 9;
		//	int iYPixSearchDist = 9;
		
		//create array to hold all of the addresses and distances
		List<int> intUnsortedXCord = new List<int>();
		List<int> intUnsortedYCord = new List<int>();
		List<float> fUnsortedDistance = new List<float>();
		List<float> fUnsortedNeighbourDistance = new List<float>();
		
		List<int> intSortedXCord = new List<int>();
		List<int> intSortedYCord = new List<int>();
		List<float> fSortedDistance = new List<float>();
		List<float> fSortedNeighbourDistance = new List<float>();
		
		//fill undorted lists
		//loop though pixles surounding the target pixle 
		for (int i = 0; i  < iXPixSearchDist; i++) 
		{
			for(int j = 0; j < iYPixSearchDist; j++)
			{
				//calculate cordinate as offset from search point
				int iX = i - (int)(((float)iXPixSearchDist / 2.0f));
				int iY = j - (int)(((float)iYPixSearchDist / 2.0f));
				
				float fXOffset = 0;
				float fYOffset = 0;

				if(iX < 0)
				{
					fXOffset = 1;
				}
				
				if(iX > 0)
				{
					fXOffset = -1;
				}
				
				if(iY < 0)
				{
					fYOffset = 1;
				}
				
				if(iY > 0)
				{
					fYOffset = -1;
				}
				
				float fDistance = ((new Vector2((float)iX * vecPixleSize.x,(float)iY * vecPixleSize.y)) ).magnitude;
				float NeighbourShortestDistance = ((new Vector2((float)(iX +fXOffset)  * vecPixleSize.x,(float)(iY + fYOffset)  * vecPixleSize.y)) ).magnitude;
				
				
				fDistance = fDistance / _fSearchSize;
				NeighbourShortestDistance = NeighbourShortestDistance / _fSearchSize;
				
				intUnsortedXCord.Add(iX);
				intUnsortedYCord.Add(iY);
				fUnsortedDistance.Add(fDistance);
				fUnsortedNeighbourDistance.Add(NeighbourShortestDistance);
			}
		}
		
		
		
		
		int iCordCount = fUnsortedDistance.Count;
		
		for (int i = 0; i < iCordCount; i++) 
		{
			float fShortestDistance = float.MaxValue;
			
			int iShortestIndex = int.MinValue;
			
			for(int j = 0; j  < fUnsortedDistance.Count; j++)
			{
				if(fUnsortedDistance[j] < fShortestDistance)
				{
					fShortestDistance = fUnsortedDistance[j];
					iShortestIndex = j;
				}
			}
			
			if(iShortestIndex != int.MinValue)
			{
				if(fUnsortedDistance[iShortestIndex] < 1 )
				{
					intSortedXCord.Add(intUnsortedXCord[iShortestIndex]);
					intSortedYCord.Add(intUnsortedYCord[iShortestIndex]);
					fSortedDistance.Add(fUnsortedDistance[iShortestIndex]);
					fSortedNeighbourDistance.Add (fUnsortedNeighbourDistance[iShortestIndex]);
				}
				intUnsortedXCord.RemoveAt(iShortestIndex);
				intUnsortedYCord.RemoveAt(iShortestIndex);
				fUnsortedDistance.RemoveAt(iShortestIndex);
				fUnsortedNeighbourDistance.RemoveAt(iShortestIndex);
			}
		}
		
		//save teh constructed kernal
		x = intSortedXCord.ToArray();
		y = intSortedYCord.ToArray();
		Distance = fSortedDistance.ToArray();
		NeighbourClosestDistance = fSortedNeighbourDistance.ToArray ();
		
		_bIsSetup = true;
	}
	
	public int GetXSearchCoordinate(int ixSearchCenter,int iSearchIndex)
	{
		//check bounds
		if (iSearchIndex >= x.Length) {
			return ixSearchCenter;
		}
		
		int iSearchCord = ixSearchCenter + x [iSearchIndex];
		
		if (_bClamp == false) 
		{
			iSearchCord = iSearchCord % _iXLength;
		} 
		else 
		{
			iSearchCord = Mathf.Clamp(iSearchCord,0,_iXLength);
			
		}
		
		return iSearchCord;
		
	}
	
	public int GetYSearchCoordinate(int iySearchCenter,int iSearchIndex)
	{
		//check bounds
		if (iSearchIndex >= y.Length) {
			return iySearchCenter;
		}
		
		int iSearchCord = iySearchCenter + y [iSearchIndex];
		
		if (_bClamp == false) 
		{
			iSearchCord = iSearchCord % _iYLength;
		} 
		else 
		{
			iSearchCord = Mathf.Clamp(iSearchCord,0,_iYLength);
			
		}
		
		return iSearchCord;
		
	}
	
	public float GetDistance(int iSearchIndex)
	{
		if (iSearchIndex >= Distance.Length) {
			return float.MaxValue;
		}
		
		return Distance [iSearchIndex];
	}
	
	public float GetNeighbourShortestDistance(int iSearchIndex)
	{
		if (iSearchIndex >= NeighbourClosestDistance.Length) 
		{
			return float.MaxValue;
		}
		
		return NeighbourClosestDistance [iSearchIndex];
	}
	
	public int KernalLength()
	{
		return Distance.Length;
	}
}

