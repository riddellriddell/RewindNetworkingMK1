using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//this class is for texting the full cycle number generator
public class FullCycleUnitTest : MonoBehaviour 
{
	public TextureHandeler  _txhTextureHandeler;

	[Range(1,12)]
	public int TextureSizeX;

	[Range(1,12)]
	public int TextureSizeY;

	public int iSeed;

	public int _iNumberOfPointsToCheck;

	public Color _colNoVisitColour;

	public Color _colVisitOnceColour;

	public Color _colVisitTwiceColour;

	[ContextMenu("Full Cycle Test")]
	public void FullCycleTest()
	{
		int iTrueSizeX = (int) Mathf.Pow(2f,(float)TextureSizeX);
		int iTrueSizeY = (int) Mathf.Pow(2f,(float)TextureSizeY);
		//setup full cycle generator
		FullCycleGenerator fsgFullCycleGenerator = new FullCycleGenerator();

		iSeed = Random.Range(0,int.MaxValue);

		fsgFullCycleGenerator.Setup(iSeed, iTrueSizeX * iTrueSizeY);

		//setup taregt texture

		if(_txhTextureHandeler == null)
		{
			_txhTextureHandeler = new TextureHandeler();
		}

		//setup test texture
		_txhTextureHandeler.Initalise(iTrueSizeX,iTrueSizeY,_colNoVisitColour);

		//loop through all the texture accessed 
		for(int i = 0 ; i < _iNumberOfPointsToCheck; i++)
		{
			//get target address
			int iTargetAddress = fsgFullCycleGenerator.NextInt();

			//get pixle colour
			Color colTargetColour = _txhTextureHandeler.GetPixle(iTargetAddress);

			Color colColourToApply = _colVisitOnceColour;


			if(colTargetColour == _colNoVisitColour)
			{
				colColourToApply = _colVisitOnceColour;
			}

			if(colTargetColour == _colVisitOnceColour)
			{
				colColourToApply = _colVisitTwiceColour;
			}


			_txhTextureHandeler.SetPixle(iTargetAddress,colColourToApply);
		}

		//apply the colour changes
		_txhTextureHandeler.Apply();
	}

}
