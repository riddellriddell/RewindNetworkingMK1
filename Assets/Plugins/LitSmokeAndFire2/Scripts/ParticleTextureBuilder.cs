using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

[CreateAssetMenu(fileName = "Texture Setup", menuName = "Normal Map Texture", order = 1)]
public class ParticleTextureBuilder : ScriptableObject
{
    [System.Serializable]
    public class EmissiveMapLayer
    {
        public Texture2D _texLayer;

        public float _fAlphaAddMultplier;

        public bool _bReMapValues;

        public float _fBrightnessMultiplyer;
        
        public AnimationCurve _amcEmissiveValueRemap;

        public float[] GetEmissiveBrightness()
        {
            //get pixels
            Color[] colSourcePixels = _texLayer.GetPixels();

            float[] fBrightness = new float[colSourcePixels.Length];

            for(int i = 0; i < colSourcePixels.Length; i++)
            {
                //get raw brightness
                fBrightness[i] = colSourcePixels[i].b;

                if (_bReMapValues)
                {
                    fBrightness[i] = _amcEmissiveValueRemap.Evaluate(fBrightness[i]) * _fBrightnessMultiplyer;
                }
            }

            return fBrightness;
        }
    }

    public enum BuildAction
    {
        SUB_FRAME_SELECTION,
        NORMALIZE,
        TRANSPARENCY_NORMALIZE,
        TRANSPARENCY_OCCLUSION_NORMALIZE,
        TRANSPARENCY_EMISSIVE_NORMALIZE,
        TRANSPARENCY
    }

    [CleanInspectorName]
    public BuildAction _bacBuildAction;

    [Button("BuildTexture", "")]
    public bool _bBuildTextureButton;

    [CleanInspectorName()]
    public ParticleTextureBuilder[] _SubBuildActions;

    [CleanInspectorName()]
    public Texture2D _texRawNormalMap;

    [Range (0,1)]
    public float _fReduceNormalMapNoise;

    [CleanInspectorName()]
    public int iNumberOfNoiseReducePasses;

    [CleanInspectorName()]
    public float _fLowNormalAngleMultiplyer;

    [CleanInspectorName()]
    public Texture2D _texOcclusionMap;

    [CleanInspectorName()]
    public bool _bGetOcclusionMapFromNormals;

    [CleanInspectorName()]
    public float _fOcclusionIntensity;

    [CleanInspectorName()]
    public AnimationCurve _amcOcclusionRemapping;

    [CleanInspectorName()]
    public Texture2D _texOpacityMap;

    [CleanInspectorName()]
    public float _fOpacityMultiplyer = 0;

    [CleanInspectorName()]
    public AnimationCurve _amcOpacityRemapping;

    [SerializeField]
    public EmissiveMapLayer[] _emlEmissiveMaps;

    [Button("RunBuildSubFrameList", "Calculate frames using curve")]
    public bool _bBuildSubFrameList;

    [Button("SaveSubFrameSelection", "Save new image sequence")]
    public bool _bMakeSubFrameSelection;

    [CleanInspectorName("Sub Frame Settings")]
    public bool _bSubFrameSettings;
    [CleanInspectorName("", "_bSubFrameSettings")]
    public Texture2D _texSubFrameTargetTexture;

    [CleanInspectorName("", "_bSubFrameSettings")]
    public AnimationCurve _amcSubFrameOverTime;

    [CleanInspectorName("", "_bSubFrameSettings")]
    public int[] _iSubFrameSelection;

    [CleanInspectorName("", "_bSubFrameSettings")]
    public int _iSourceRows;

    [CleanInspectorName("", "_bSubFrameSettings")]
    public int _iSourceColumns;

    [CleanInspectorName("", "_bSubFrameSettings")]
    public int _iSubFrameRows;

    [CleanInspectorName("", "_bSubFrameSettings")]
    public int _iSubFrameColumns;

    [FileStructureString(FileStructureStringAttribute.FileAddressOptions.FOLDER_REFFERENCE_ONLY)]
    public string _strSaveDirectory;

    public string _strFileName;

    [Button("NormalizeAndSaveTexture", "Normalize texture")]
    public bool _bNormalizeTextureButton;

    [Button("TransparencyAndNormalizeTexture", "Apply transparency and normalize")]
    public bool _bTransparencyAndNormalizeButton;

    [Button("TransparencyOcclusionAndNormalizeTexture" , "Apply transparency, occlusion and normalization")]
    public bool _bTransparencyOcclusionAndNormalizationButton;

    [Button("TransparencyEmissiveAndNormalizeTexture", "Apply transparency, emissive and normalize")]
    public bool _bTransparencyEmissiveAndNormalizationButton;

    [Button("TransparencyTexture" , "Apple transparency to target")]
    public bool _bApplyTransparencyButton;



    public void Update()
    {
        if(_bNormalizeTextureButton == true)
        {
            _bNormalizeTextureButton = false;

            NormalizeAndSaveTexture();
        }

        if(_bTransparencyAndNormalizeButton == true)
        {
            _bTransparencyAndNormalizeButton = false;

            TransparencyAndNormalizeTexture();

        }
        
        if(_bTransparencyOcclusionAndNormalizationButton == true)
        {
            _bTransparencyOcclusionAndNormalizationButton = false;

            TransparencyOcclusionAndNormalizeTexture();

        }

        if(_bTransparencyEmissiveAndNormalizationButton == true)
        {
            _bTransparencyEmissiveAndNormalizationButton = false;


            TransparencyEmissiveAndNormalizeTexture();
        }

        if(_bApplyTransparencyButton == true)
        {
            _bApplyTransparencyButton = false;

            TransparencyTexture();
        }

        if(_bBuildSubFrameList == true)
        {
            _bBuildSubFrameList = false;

            RunBuildSubFrameList();
        }

        if(_bMakeSubFrameSelection == true)
        {
            _bMakeSubFrameSelection = false;

            SaveSubFrameSelection();
        }
    }

    public void BuildTexture()
    {
        switch(_bacBuildAction)
        {
            case BuildAction.SUB_FRAME_SELECTION:
                SaveSubFrameSelection();
                break;
            case BuildAction.NORMALIZE:
                NormalizeAndSaveTexture();
                break;
            case BuildAction.TRANSPARENCY:
                TransparencyTexture();
                break;
            case BuildAction.TRANSPARENCY_NORMALIZE:
                TransparencyAndNormalizeTexture();
                break;
            case BuildAction.TRANSPARENCY_OCCLUSION_NORMALIZE:
                TransparencyOcclusionAndNormalizeTexture();
                break;
            case BuildAction.TRANSPARENCY_EMISSIVE_NORMALIZE:
                TransparencyEmissiveAndNormalizeTexture();
                break;

        }

        if (_SubBuildActions != null)
        {
            for (int i = 0; i < _SubBuildActions.Length; i++)
            {
                if(_SubBuildActions[i] != null)
                {
                    _SubBuildActions[i].BuildTexture();
                }
            }
        }
    }

    public void NormalizeAndSaveTexture()
    {
        Texture2D texNoiseReducedTexture = ReduceNoise(_texRawNormalMap, _fReduceNormalMapNoise, iNumberOfNoiseReducePasses);

        Texture2D texNormalizedTexture = NormalizeTexture(_texRawNormalMap);

        SaveTexture(texNormalizedTexture);

        Resources.UnloadUnusedAssets();
    }

    public void TransparencyAndNormalizeTexture()
    {
        Texture2D texNoiseReducedTexture = ReduceNoise(_texRawNormalMap, _fReduceNormalMapNoise, iNumberOfNoiseReducePasses);

        Texture2D texNormalizedTransparentTexture = ApplyEmissiveTransparency(ApplyTransparencyTexture(NormalizeTexture(texNoiseReducedTexture), _texOpacityMap), _emlEmissiveMaps);


        SaveTexture(texNormalizedTransparentTexture);

        Resources.UnloadUnusedAssets();
    }

    public void TransparencyOcclusionAndNormalizeTexture()
    {
        Texture2D texNoiseReducedTexture = ReduceNoise(_texRawNormalMap, _fReduceNormalMapNoise, iNumberOfNoiseReducePasses);

        Texture2D texNormalizedTransparentTexture = ApplyOcclusionMap(ApplyEmissiveTransparency(ApplyTransparencyTexture(NormalizeTexture(texNoiseReducedTexture), _texOpacityMap), _emlEmissiveMaps), _texOcclusionMap);

        SaveTexture(texNormalizedTransparentTexture);

        Resources.UnloadUnusedAssets();
    }

    public void TransparencyEmissiveAndNormalizeTexture()
    {
        Texture2D texNoiseReducedTexture = ReduceNoise(_texRawNormalMap, _fReduceNormalMapNoise, iNumberOfNoiseReducePasses);

        Texture2D texNormalizedTransparentEmisiveTexture = ApplyEmissiveMap(ApplyEmissiveTransparency(ApplyTransparencyTexture(NormalizeTexture(texNoiseReducedTexture), _texOpacityMap), _emlEmissiveMaps), _emlEmissiveMaps);

        SaveTexture(texNormalizedTransparentEmisiveTexture);

        Resources.UnloadUnusedAssets();
    }

    public void TransparencyTexture()
    {

        Texture2D texTransparentTexture = ApplyEmissiveTransparency(ApplyTransparencyTexture(_texRawNormalMap, _texOpacityMap), _emlEmissiveMaps);

        SaveTexture(texTransparentTexture);

        Resources.UnloadUnusedAssets();
    }

    public void RunBuildSubFrameList()
    {
        _iSubFrameSelection = BuildSubFrameList(_iSubFrameRows * _iSubFrameColumns, _iSourceColumns * _iSourceRows, _amcSubFrameOverTime);
    }

    public void SaveSubFrameSelection()
    {
        Texture2D texSubFrameSelection = SepperateOutSubFrameSelection(_texSubFrameTargetTexture, _iSourceRows, _iSourceColumns, _iSubFrameRows, _iSubFrameColumns, new Color(0.5f, 0.5f, 0.5f, 0), _iSubFrameSelection);

        SaveTexture(texSubFrameSelection);
    }

    public Texture2D ReduceNoise(Texture2D texTargetTexture, float fNoiseReductionAmount, int iNumberOfPasses = 1)
    {
        if(iNumberOfNoiseReducePasses == 0)
        {
            return texTargetTexture;
        }

        //get all pixels
        Color[,] colPixels = ConvertPixelListTo2DArray(texTargetTexture.GetPixels(), texTargetTexture.width, texTargetTexture.height);

        //run through noise reducer
        return ReduceNoise(colPixels, fNoiseReductionAmount, iNumberOfPasses);

    }

    public Texture2D ReduceNoise(Color[,] colPixels, float fNoiseReductionAmount, int iNumberOfPasses = 1)
    {
        //make 
        if (true)
        {
            //clone values
            Color[,] colOldPixelValues = new Color[colPixels.GetLength(0), colPixels.GetLength(1)];

            for (int iX = 0; iX < colPixels.GetLength(0); iX++)
            {
                //loop through all y values
                for (int iY = 0; iY < colPixels.GetLength(1); iY++)
                {
                    colOldPixelValues[iX, iY] = new Color( colPixels[iX, iY].r, colPixels[iX, iY].g, colPixels[iX, iY].b, colPixels[iX, iY].a);
                }
            }

            //loop through all x values
            for (int iX = 0; iX < colPixels.GetLength(0); iX++)
            {
                //loop through all y values
                for (int iY = 0; iY < colPixels.GetLength(1); iY++)
                {
                    int iUpY = Mathf.Clamp(iY - 1, 0, colOldPixelValues.GetLength(1) - 1);
                    int iDownY = Mathf.Clamp(iY + 1, 0, colOldPixelValues.GetLength(1) - 1);
                    int iLeftX = Mathf.Clamp(iX - 1, 0, colOldPixelValues.GetLength(0) - 1);
                    int iRightX = Mathf.Clamp(iX + 1, 0, colOldPixelValues.GetLength(0) - 1);

                    //get surrounding values and the average between them
                    Color colVerticlAverage = ((colOldPixelValues[iX, iUpY] * 0.5f) + (colOldPixelValues[iX, iDownY] * 0.5f));
                    Color colHorizontalAverage = ((colOldPixelValues[iLeftX, iY] * 0.5f) + (colOldPixelValues[iRightX, iY] * 0.5f));
                    Color colDiagonalAverage1 = ((colOldPixelValues[iLeftX, iUpY] * 0.5f) + (colOldPixelValues[iRightX, iDownY] * 0.5f));
                    Color colDiagonalAverage2 = ((colOldPixelValues[iLeftX, iDownY] * 0.5f) + (colOldPixelValues[iRightX, iUpY] * 0.5f));

                    Color colSmoothValue = ((colVerticlAverage * 0.25f) + (colHorizontalAverage * 0.25f) + (colDiagonalAverage1 * 0.25f) + (colDiagonalAverage2 * 0.25f));

                    colPixels[iX, iY] = Color.Lerp(colPixels[iX, iY], colSmoothValue, fNoiseReductionAmount);
                }
            }
        }

        //reduce the number of passes 
        iNumberOfPasses--;

        if(iNumberOfPasses > 0)
        {
            return ReduceNoise(colPixels, fNoiseReductionAmount, iNumberOfPasses);
        }

        Texture2D texOutput = new Texture2D(colPixels.GetLength(0), colPixels.GetLength(1));

        texOutput.SetPixels(Convert2DArrayToPixelList(colPixels));

        return texOutput;
    }

    public Texture2D ApplyEmissiveTransparency(Texture2D texTargetTexture,EmissiveMapLayer[] emlEmmisiveLayers)
    {
        Texture2D texOutput = texTargetTexture;

        for(int i = 0; i < emlEmmisiveLayers.Length; i++)
        {
            texOutput = ApplyEmissiveTransparency(texOutput, emlEmmisiveLayers[i]);
        }

        return texOutput;
    }

    public Texture2D ApplyEmissiveTransparency(Texture2D texTargetTexture, EmissiveMapLayer emlEmissiveMap)
    {
        //get all pixels
        Color[] colTarget = texTargetTexture.GetPixels();

        //get all emissive map pixels
        float[] fEmissiveBrightness = emlEmissiveMap.GetEmissiveBrightness();

        for(int i = 0; i < colTarget.Length; i++)
        {
            colTarget[i].a += (fEmissiveBrightness[i] * emlEmissiveMap._fAlphaAddMultplier);
        }

        Texture2D texOutput = new Texture2D(texTargetTexture.width, texTargetTexture.height);

        texOutput.SetPixels(colTarget);

        return texOutput;
    }

    public Texture2D ApplyOcclusionMap(Texture2D texTargetTexture, Texture2D texOcclusionMap)
    {
        //get all the target pixels 
        Color[] colTargetPixels = texTargetTexture.GetPixels();

        //get all the occlusion pixels 
        Color[] colOcclusionPixels = texOcclusionMap.GetPixels();

        for(int i = 0; i < colTargetPixels.Length && i < colOcclusionPixels.Length; i++ )
        {
            float fOcclusion = 0;

            if(_bGetOcclusionMapFromNormals == true)
            {
                fOcclusion = 1 - DecodeNormal(colOcclusionPixels[i]).magnitude;
            }
            else
            {
                fOcclusion =  1 - colOcclusionPixels[i].maxColorComponent;
            }

            //remap occlusion
            fOcclusion = _amcOcclusionRemapping.Evaluate(fOcclusion) * _fOcclusionIntensity;

            //apply occlusion
            Vector3 vecTargetNormal = DecodeNormal(colTargetPixels[i]);

            vecTargetNormal = vecTargetNormal * ( 1 - fOcclusion);

            float fAlpha = colTargetPixels[i].a;

            colTargetPixels[i] = EncodeNormal(vecTargetNormal);

            colTargetPixels[i].a = fAlpha;
        }

        //convert output to texture

        Texture2D texOutput = new Texture2D(texTargetTexture.width, texTargetTexture.height);

        texOutput.SetPixels(colTargetPixels);

        return texOutput;
    }
    public Texture2D NormalizeTexture(Texture2D texNormal)
    {

        //get all the pixel colours 
        Color[] colSourcePixels = texNormal.GetPixels();

        //loop through all the pixels 
        for(int i = 0; i < colSourcePixels.Length; i++)
        {
            //get normal
            Vector3 vecNormal = DecodeNormal(colSourcePixels[i]);

            //get length
            float fLength = vecNormal.magnitude;

            //multiply low normal values
            float fAngleMultiplyer = ( Mathf.Clamp01(1 - fLength) * _fLowNormalAngleMultiplyer) + 1;
            vecNormal = new Vector3(vecNormal.x * fAngleMultiplyer, vecNormal.y * fAngleMultiplyer, vecNormal.z);

            //normalize pixel colour
            vecNormal.Normalize();

            //apply change to pixel colour
            colSourcePixels[i] = EncodeNormal(vecNormal);
        }

        Texture2D texOutput = new Texture2D(texNormal.width, texNormal.height);

        texOutput.SetPixels(colSourcePixels);

        return texOutput;
    }
    public Texture2D ApplyTransparencyTexture(Texture2D texTargetTexture, Texture2D texTransparency )
    {
        //get all the target pixel colours 
        Color[] colTargetPixels = texTargetTexture.GetPixels();

        //get transparency pixeles 
        Color[] colTransparencyPixels = texTransparency.GetPixels();

        //loop through all teh pixels and apply effect
        for(int i = 0; i < colTargetPixels.Length && i < colTransparencyPixels.Length; i++)
        {
            colTargetPixels[i].a = (_amcOpacityRemapping.Evaluate(colTransparencyPixels[i].a)) * _fOpacityMultiplyer;

            //flaten out low alpha normal
            if(colTargetPixels[i].a <= 0)
            {
                colTargetPixels[i].r = 0.5f;
                colTargetPixels[i].g = 0.5f;
                colTargetPixels[i].b = 0.5f;
            }
        }

        Texture2D texOutput = new Texture2D(texTargetTexture.width, texTargetTexture.height);

        //apply transparency effect of emissive mapt to texture

        texOutput.SetPixels(colTargetPixels);

        return texOutput;
    }
    public Texture2D ApplyEmissiveMap(Texture2D texTargetTexture, EmissiveMapLayer[] emlEmissiveMaps)
    {
        //get source pixels 
        Color[] colTargetPixels = texTargetTexture.GetPixels();

        //remove all the z normal values
        for(int i = 0; i < colTargetPixels.Length; i++)
        {
            colTargetPixels[i].b = 0;
        }

        //loop through all the emissive maps
        for(int i = 0;i < emlEmissiveMaps.Length; i++)
        {
            float[] fEmissiveValues = emlEmissiveMaps[i].GetEmissiveBrightness();

            for(int j = 0; j < colTargetPixels.Length && j < fEmissiveValues.Length; j++)
            {
                colTargetPixels[j].b += fEmissiveValues[j];
            }
        }

        //build output textures
        Texture2D texOutput = new Texture2D(texTargetTexture.width, texTargetTexture.height);

        texOutput.SetPixels(colTargetPixels);

        return texOutput;
    }
    public void SaveTexture( Texture2D texTexture)
    {
       byte[] bytPNG =  texTexture.EncodeToPNG();

        Debug.Log("Attempted save directory | " + Application.dataPath + "/" + _strSaveDirectory + "/" + _strFileName + ".png");

        File.WriteAllBytes(Application.dataPath + "/../" + _strSaveDirectory + "/"  + _strFileName + ".png", bytPNG);
    }
    public Vector3 DecodeNormal(Color colNormal)
    {
        Vector3 vecNormal = new Vector3(colNormal.r, colNormal.g, colNormal.b) * 2;

        vecNormal -= Vector3.one;

        return vecNormal;
    }
    public Color EncodeNormal(Vector3 vecNormal)
    {
        vecNormal += Vector3.one;

        vecNormal = vecNormal * 0.5f;

        return new Color(vecNormal.x, vecNormal.y, vecNormal.z);
    }
    public Color[,] ConvertPixelListTo2DArray(Color[] colPixels,int iImagetWidth,int iImageHeight)
    {
        Color[,] colOutput = new Color[iImagetWidth  ,iImageHeight  ];

        Debug.Log("Width " + iImagetWidth + " Height " + iImageHeight);

        //loop throug all the pixels
        for(int i = 0; i < colPixels.Length; i++)
        {
            //calculate coord in 2d array
            int iX = i % iImagetWidth;
            int iY = (i - iX ) / iImagetWidth;

            if(iX > iImagetWidth || iX < 0)
            {
                Debug.Log("Width Error");
                Debug.Log("target address ( X = " + iX + ", Y = " + iY + " ) at index " + i + " of " + colPixels.Length);
            }

            if (iY > (iImageHeight -1) || iY < 0)
            {
                Debug.Log("Height Error");
                Debug.Log("target address ( X = " + iX + ", Y = " + iY + " ) at index " + i + " of " + colPixels.Length);
            }

            if (i > colPixels.Length)
            {
                Debug.Log("source length error");
                Debug.Log("target address ( X = " + iX + ", Y = " + iY + " ) at index " + i + " of " + colPixels.Length);
            }

           // Debug.Log("target address ( X = " + iX + ", Y = " + iY + " )");

            //put value in array
            colOutput[iX, iY] = colPixels[i];


        }

        return colOutput;
    }
    public Color[] Convert2DArrayToPixelList(Color[,] col2DPixelList)
    {
        Color[] colOutput = new  Color[col2DPixelList.Length];

        for(int i = 0; i < col2DPixelList.Length; i++)
        {
            //calculate coord in 2d array
            int iX = i % col2DPixelList.GetLength(0);
            int iY = (i - iX) / col2DPixelList.GetLength(0);

            colOutput[i] = col2DPixelList[iX, iY];
        }

        return colOutput;
    }
    public Texture2D SepperateOutSubFrameSelection(Texture2D texSourceTexture, int iSourceRows, int iSourceColumns, int iDestRows, int iDestColumns,Color colDestStartColour,int[] iFrameIndexes,Func<int,Color,Color> fncPerFrameAction = null )
    {


        //calculate the size of each of the frames
        int iTileWidth = texSourceTexture.width / iSourceColumns;
        int iTileHeight = texSourceTexture.height / iSourceRows;

        //calculate output texture size
        int iWidth = iTileWidth * iDestColumns;
        int iHeight = iTileHeight * iDestRows;

        //create 2D texture array for source 
        Color[,] colSource = ConvertPixelListTo2DArray(texSourceTexture.GetPixels(), texSourceTexture.width, texSourceTexture.height);

        //create 2D textre array for dest
        Color[,] colDest = new Color[iWidth, iHeight];

        //set the inital values
        for(int iX = 0; iX < colDest.GetLength(0); iX++)
        {
            for(int iY = 0; iY < colDest.GetLength(1);iY++ )
            {
                colDest[iX, iY] = colDestStartColour;
            }
        }


        //loop through all output frames
        for (int i = 0; i < iFrameIndexes.Length; i++ )
        {
            //calc offsets for source and dest
            int iTargetSourceColumn = iFrameIndexes[i] % iSourceColumns;
            int iTargetSourceRow =   iSourceRows - (((iFrameIndexes[i] - iTargetSourceColumn) / iSourceColumns) + 1);


            int iTargetDestColumn = i % iDestColumns;
            int iTargetDestRow = iDestRows - (((i - iTargetDestColumn) / iDestColumns) + 1) ;



            //loop through all the pixels in the tile and applt the copy 
            for (int iX = 0; iX < iTileWidth; iX++)
            {
                for(int iY = 0; iY < iTileHeight; iY++)
                {
                    //get source colour
                    Color colTargetSource = colSource[iX + (iTargetSourceColumn * iTileWidth) , iY + (iTargetSourceRow * iTileHeight)];

                    //do per tile action
                    if(fncPerFrameAction != null)
                    {
                        colTargetSource = fncPerFrameAction.Invoke(i, colTargetSource);
                    }

                    //make per tile colouring 
                    colDest[iX + (iTargetDestColumn * iTileWidth), iY + (iTargetDestRow * iTileHeight)] = colTargetSource;
                }
            }
        }

        //convert back to texture 
        Texture2D texOutput = new Texture2D(colDest.GetLength(0), colDest.GetLength(1));

        //apply texture colours
        texOutput.SetPixels(Convert2DArrayToPixelList(colDest));

        return texOutput;
    }
    public int[] BuildSubFrameList(int iOutputFrameCount ,int iInputFrameCount, AnimationCurve amcFramesOverTime)
    {
        int[] iOutput = new int[iOutputFrameCount];

        for(int i = 0; i < iOutputFrameCount; i++)
        {
            float fPercentThroughRange = ((float)i) / iOutputFrameCount;

            float fFramePercent = amcFramesOverTime.Evaluate(fPercentThroughRange);

            iOutput[i] = (int)(fFramePercent * iInputFrameCount);
        }

        return iOutput;
    }
}
