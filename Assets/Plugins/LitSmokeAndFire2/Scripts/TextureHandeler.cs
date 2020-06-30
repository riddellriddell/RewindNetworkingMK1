using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//ths script handels the colour values from a texture
using System.IO;

[System.Serializable]
public class TextureHandeler 
{
    public TextureHandeler() : base()
    {
        
    }

    public TextureHandeler(Texture2D texTarget) : base()
    {
        _texTargetTexture = texTarget;
        _texPreviousTexture = texTarget;
        Initalise();
    }

    public int Width
	{
		get
		{
			if(_colTextureColours != null)
			{
				return _colTextureColours.Count;
			}

			return 0;

		}
	}

	public int Height
	{
		get
		{
			if(_colTextureColours != null)
			{
				if(_colTextureColours[0] != null)
				{
					return _colTextureColours[0].Count;
				}

			}
			
			return 0;
		}
	}

	public bool Clamp
	{
		get
		{
			if(_texTargetTexture == null)
			{
				return false;
			}

			if(_texTargetTexture.wrapMode == TextureWrapMode.Clamp)
			{
				return true;
			}
			
			return false;
		}
	}
//
//	public struct PixleCord
//	{
//		int x;
//		int y;
//		
//		public void ConvertUVToPix(Vector2 vecUV ,int iWidth ,int iHeight)
//		{
//			x = (int)(iWidth / vecUV.x);
//			y = (int)(iHeight / vecUV.y);
//			
//			x = (int)Mathf.Clamp (x, 0, iWidth);
//			y = (int)Mathf.Clamp (y, 0, iHeight);
//		}
//
//		public Vector2 PixleToUV(int iWidth ,int iHeight)
//		{
//			return new Vector2 ((float)x / iWidth, (float)y / iHeight);
//		}
//
//
//	}

	//the texture to base this off
	[SerializeField]
	public Texture2D _texTargetTexture;

	protected Texture2D _texPreviousTexture;

	//all the colours in the texture
	[SerializeField]
	protected List<List<Color>> _colTextureColours;

    int _iLastIndexLookUp = int.MaxValue;
    int _iLastX = int.MaxValue;
    int _iLastY = int.MaxValue;

    public void Initalise()
	{
		//perform the inital setup on the texture
		FetchPixleColours();
	}

	public void Initalise(int iWidth,int iHeight , Color colColour)
	{
		_colTextureColours = new  List<List<Color>>(iWidth);

		for(int i = 0; i < iWidth; i++)
		{
			List<Color> colColumbColours = new List<Color>(iHeight);

			for(int j = 0 ; j <iHeight; j++)
			{
				colColumbColours.Add(colColour);
			}

			_colTextureColours.Add(colColumbColours);
		}

	}

	//fetch all the pixle colours from teh source texture
	public void FetchPixleColours()
	{
		//check that source texture is attached
		if(_texTargetTexture == null)
		{
			//if there is no texture attached setup empty texture array
			_colTextureColours = new List<List<Color>>();
			return;
		}

		Color[] colSourceTextureColours = _texTargetTexture.GetPixels ();


		_colTextureColours = new List<List<Color>>(_texTargetTexture.width);

		//loop through texture pixles and put them in to the correct columbs 
		for(int i = 0 ; i < _texTargetTexture.width; i++)
		{
			//list to put the values into
			List<Color> colColumbColours = new List<Color>(_texTargetTexture.height);
		
			//loop through all the rows
			for(int j = 0 ; j < _texTargetTexture.height; j++)
			{
				//get the point to sample from
				int iSamplePoint = (_texTargetTexture.width * j) + i ;
			
				//check ranges
				if(iSamplePoint < colSourceTextureColours.Length)
				{
					//add the colour into the columb list
					colColumbColours.Add(colSourceTextureColours[iSamplePoint]);

				}
			}

			//add the colour colours into the colour list
			_colTextureColours.Add(colColumbColours);
		}
		
	}

	public Color[] GetTextureColourArray()
	{
		List<Color> colReturnColours = new List<Color>(_texTargetTexture.width * _texTargetTexture.height);

		if(_colTextureColours == null)
		{
			return null;
		}

		//loop through texture pixles and put them in to the correct columbs 
		for(int i = 0 ; i < Height; i++)
		{
			//loop through all the rows
			for(int j = 0 ; j < Width; j++)
			{
				if(_colTextureColours[j] != null)
				{
					//add the color to the list
					colReturnColours.Add(_colTextureColours[j][i]);
				}
			}

		}

		return colReturnColours.ToArray();

	}
	
	public void SaveTexture(string strSaveAddress)
	{
		CheckForTextureChange ();

		if (_colTextureColours == null) {
			return;
		}

		_texTargetTexture.SetPixels (GetTextureColourArray());
		_texTargetTexture.Apply ();

		
		byte[] bytes = _texTargetTexture.EncodeToPNG();

		// For testing purposes, also write to a file in the project folder
		Debug.Log ("Saving");

		File.WriteAllBytes(Application.dataPath  +"/"+ strSaveAddress + ".png", bytes);
		//File.WriteAllBytes(Application.dataPath + "/SavedScreen.png", bytes);
		
	}

	//apply the changes to the texture to the target texture
	public void Apply()
	{
		if (_texTargetTexture == null)
		{
			_texTargetTexture = new Texture2D (Width, Height);
				
		} 
		else 
		{
			_texTargetTexture.Resize (Width, Height);
		}

		_texTargetTexture.SetPixels(GetTextureColourArray());

		_texTargetTexture.Apply();
	}

	//get the number of pixles in the texture
	public int GetLength()
	{
		CheckForTextureChange ();

		if (_colTextureColours == null) 
		{
			FetchPixleColours ();

		}


		if (_colTextureColours == null) 
		{
			return 0;
		}

		if(_colTextureColours.Count == 0)
		{
			FetchPixleColours ();
		}

		return _colTextureColours.Count * _colTextureColours[0].Count;
	}
	
	//get the pixle colour at address
	public Color GetPixle(int iX, int iY)
	{
        if(iX == _iLastX && iY == _iLastY)
        {
            //unsafe pixle colour get
            return _colTextureColours[iX][iY];
        }



        while (iX < 0)
        {
            iX += _texTargetTexture.width;
        }

        while (iY < 0)
        {
            iY += _texTargetTexture.height;
        }

        CheckForTextureChange ();


		//check that source texture is attached
		if(_texTargetTexture == null)
		{
			return Color.black;
		}
		
		//clamp texture cords
		if (_texTargetTexture.wrapMode == TextureWrapMode.Clamp) 
		{
			iX = Mathf.Clamp (iX, 0, _texTargetTexture.width);
			iY = Mathf.Clamp (iY, 0, _texTargetTexture.height);
		} 
		else 
		{
			iX = iX % _texTargetTexture.width;
			iY = iY % _texTargetTexture.height;
		}

        //try
        //{
        _iLastX = iX;
        _iLastY = iY;

        return _colTextureColours[iX][iY];
       //}
       //catch
       //{
       //    Debug.Log("Texture Width " + _colTextureColours.Count + " Passed X Cord " + iX + " Height " + _colTextureColours[0].Count + " Passed YCord " + iY);
       //    return Color.black;
       //}

        //int iIndex = (_texTargetTexture.width * iY ) + iX;
		//
		//if(_colTextureColours == null)
		//{
		//	FetchPixleColours();
		//}
		//
		//if(iIndex >=_colTextureColours.Length )
		//{
		//	return Color.black;
		//}
		//
		//return _colTextureColours [iIndex];
	}
	
	public Color GetPixle(float fX, float fY)
	{

		
		int iX = (int) (_colTextureColours.Count * fX);
		int iY = (int) (_colTextureColours[0].Count * fY);
		
		//unsafe pixle colour get
		return _colTextureColours[iX][iY];

//
//		CheckForTextureChange ();
//
//		//check that source texture is attached
//		if(_texTargetTexture == null)
//		{
//			return Color.black;
//		}
//		
//		int iX = (int) (_texTargetTexture.width * fX);
//		int iY = (int) (_texTargetTexture.height * fY);
//
//		//unsafe pixle colour get
//		return _colTextureColours[iX,iY];
		
		//clamp texture cords
//		if (_texTargetTexture.wrapMode ==TextureWrapMode.Clamp) 
//		{
//			iX = Mathf.Clamp (iX, 0, _texTargetTexture.width);
//			iY = Mathf.Clamp (iY, 0, _texTargetTexture.height);
//		} 
//		else 
//		{
//			iX = iX % _texTargetTexture.width;
//			iY = iY % _texTargetTexture.height;
//		}
//		
//		int iIndex = (_texTargetTexture.width * iY ) + iX;
//		
//		if(_colTextureColours == null)
//		{
//			FetchPixleColours();
//		}
//		
//		if(iIndex >=_colTextureColours.Length )
//		{
//			return Color.black;
//		}
//		
//		return _colTextureColours [iIndex];
	}

	public Color GetPixle(int iPixleIndex)
	{
        if(iPixleIndex == _iLastIndexLookUp)
        {
            return _colTextureColours[_iLastX][_iLastY];
        }

		int iX = (int) ( iPixleIndex % _colTextureColours.Count);
		int iY = (int) ( (iPixleIndex - iX) / _colTextureColours.Count);

        _iLastIndexLookUp = iPixleIndex;
        _iLastX = iX;
        _iLastY = iY;
        //unsafe pixle colour get
        return _colTextureColours[iX][iY];

	}
	
	//get the pixle colour at address
	public void SetPixle(int iX, int iY,Color colColour)
	{

		_colTextureColours[iX][iY] = colColour;
	}
	
	public void SetPixle(float fX, float fY,Color colColour)
	{

		int iX = (int) (_colTextureColours.Count * fX);
		int iY = (int) (_colTextureColours[0].Count * fY);

		_colTextureColours[iX][iY] = colColour; 
	}

	public void SetPixle(int iPixleIndex,Color colColour)
	{
		int iX = (int) (iPixleIndex %_colTextureColours.Count );
		int iY = (int) ((iPixleIndex - iX) /_colTextureColours.Count);

		_colTextureColours[iX][iY] = colColour; 


		
	}

	//copy a bunch of pixles accross
	public void SetPixles(int iSourceXStart, int iSourceYStart, int iSourceXEnd, int iSourceYEnd, int iDestinationXStart,int iDestinationYStart, TextureHandeler txhTextureHandeler)
	{
		//check if source texture is null
		if(txhTextureHandeler == null)
		{
			return;
		}

		//check if the destination is out of bounds 
		if (iDestinationXStart < 0) 
		{
			//clip the source start point to the point that will be blitted
			iSourceXStart -= iDestinationXStart;

			iDestinationXStart = 0;
		}		
		if (iDestinationYStart < 0) 
		{
			iSourceYStart -= iDestinationYStart;

			iDestinationYStart = 0;
		}

		//get the width of the source pixls
		int iSourceWidth = iSourceXEnd - iSourceXStart;
		int iSourceHeight = iSourceYEnd - iSourceYStart;

		//check for bad texture cords
		if(iSourceWidth <= 0)
		{
				Debug.LogError ("Texture Coordinate Error");
			return;
		}

		if(iSourceHeight <= 0)
		{
			Debug.LogError ("Texture Coordinate Error");
			return;
		}

		//get the end points for the destination blit
		int iDestinationXEnd  =  iDestinationXStart + iSourceWidth;
		int iDestinationYEnd  =  iDestinationYStart + iSourceHeight;

		//check if the blit end is off the edge of the texture
		if (iDestinationXEnd >= this.Width) 
		{
			iSourceWidth -=  iDestinationXEnd - this.Width;

			if(iSourceWidth <= 0)
			{
				return;
			}

		}

		if (iDestinationYEnd >= this.Height) 
		{
			iSourceHeight -=  iDestinationYEnd - this.Height;
			
			if(iSourceHeight <= 0)
			{
				return;
			}
			
		}


		//copy ranges accross
		for(int i = 0 ; i < iSourceWidth ;i++)
		{
			for( int j = 0 ; j < iSourceHeight; j++)
			{
				//calculate columb
				int iDestColumb = iDestinationXStart + i;

				//calculate row
				int iDestRow = iDestinationYStart + j;

				//calculate source columb
				int iSourceColumb = iSourceXStart + i;

				//calculate source row
				int iSourceRow = iSourceYStart + j;
				//get 
				_colTextureColours[iDestColumb][iDestRow] = txhTextureHandeler._colTextureColours[iSourceColumb][iSourceRow];
			}
		}

	}

	public void ExpandUP(int iRowsToAdd, Color colExpansionColour)
	{
		//create array to add
		Color[] colAdditionalValues = new Color[iRowsToAdd];

		for (int i = 0; i < iRowsToAdd; i++) 
		{
			colAdditionalValues[i] = colExpansionColour;
		}

		//loop through all the rows 
		for (int i = 0; i < Width; i++) 
		{
			//add the additional pixled
			_colTextureColours[i].AddRange(colAdditionalValues);
		}

	}

	public void ExpandDown(int iRowsToAdd, Color colExpansionColour )
	{
		//create array that has the extra rows
		List<List<Color>> colResizedArray = new List<List<Color>>(Width);

		//loop through the columbs
		for(int i = 0; i < Width; i++)
		{
			///create array that holds the missing elements
			List<Color> colColumb = new List<Color>(iRowsToAdd + Height);

			for(int j = 0 ; j < iRowsToAdd; j++)
			{
				colColumb.Add(colExpansionColour);
			}

			//add the source color array
			colColumb.AddRange(_colTextureColours[i]);

			//put the resulting array into the new resized array
			colResizedArray.Add(colColumb);

		}

		//swap the arrays over
		_colTextureColours = colResizedArray;

	}

	public void ExpandLeft(int iRowsToAdd, Color colExpansionColour )
	{



		//create array that has the extra rows
		List<List<Color>> colResizedArray = new List<List<Color>>(Width + iRowsToAdd);

		//add the new columbs on the right
		Color[] colNewColumbColours = new Color[Height];
		
		for(int i = 0 ; i < colNewColumbColours.Length; i++)
		{
			colNewColumbColours[i] = colExpansionColour;
		}
		
		for(int i = 0 ; i < iRowsToAdd; i++)
		{
			List<Color> colColumbColor = new List<Color>(colNewColumbColours.Length);
			
			colColumbColor.AddRange(colNewColumbColours);
			
			colResizedArray.Add(colColumbColor);
		}

		//add existing columbs
		colResizedArray.AddRange(_colTextureColours);
		
		//swap the arrays over
		_colTextureColours = colResizedArray;

	}

	public void ExpandRight(int iRowsToAdd, Color colExpansionColour )
	{
		Color[] colNewColumbColours = new Color[Height];
		
		for(int i = 0 ; i < colNewColumbColours.Length; i++)
		{
			colNewColumbColours[i] = colExpansionColour;
		}
		
		for(int i = 0 ; i < iRowsToAdd; i++)
		{
			List<Color> colColumbColor = new List<Color>(colNewColumbColours.Length);
			
			colColumbColor.AddRange(colNewColumbColours);
			
			_colTextureColours.Add(colColumbColor);
		}
	}

	public void CheckForTextureChange()
	{
		if (_texPreviousTexture != _texTargetTexture) 
		{
			_texPreviousTexture = _texTargetTexture;

			if(_texTargetTexture != null)
			{
				FetchPixleColours();
			}
				
		}
	}

    public List<Color> GetNeighbours(int iX, int iLayer)
    {
        int X = (int)(iX % _colTextureColours.Count);
        int Y = (int)((iX - X) / _colTextureColours.Count);

        return GetNeighbours(X, Y, iLayer);
    }

    public List<Color> GetNeighbours(int iX , int iY , int iLayer )
    {
        int iNumberInLayer = 4 + (iLayer * 4);

        List<Color> colNeighbours = new List<Color>(iNumberInLayer);

        if (iLayer <= 0)
        {
            colNeighbours.Add(GetPixle(iX, iY));

            return colNeighbours;
        }

        //get top row
        for (int i = iX  - iLayer; i < iX + iLayer; i++)
        {
            colNeighbours.Add(GetPixle(i, iY - iLayer));
        }

        //right
        for (int i = iY - iLayer; i < iY + iLayer; i++)
        {
            colNeighbours.Add(GetPixle(iX + iLayer, i));
        }
        
        //bottom
        for(int i = iX + iLayer; i > iX - iLayer; i--)
        {
            colNeighbours.Add(GetPixle(i, iY + iLayer));
        }

        //left
        for (int i = iY + iLayer; i > iY - iLayer; i--)
        {
            colNeighbours.Add(GetPixle(iX - iLayer, i));
        }

        return colNeighbours;
    }

	public Texture2D GenerateTexture(Texture2D texTextureToOverride = null)
	{
		//create new texture
		if(texTextureToOverride == null)
		{
			texTextureToOverride = new Texture2D (Width, Height, TextureFormat.RGBA32, false);
		}
		else
		{
			texTextureToOverride.Resize(Width,Height);

			//texTextureToOverride. = TextureFormat.RGBA32;

			//texTextureToOverride
		}

		//fill the texture colours 
		texTextureToOverride.SetPixels (GetTextureColourArray ());

		texTextureToOverride.Apply (false);

		return texTextureToOverride;
	}
}
