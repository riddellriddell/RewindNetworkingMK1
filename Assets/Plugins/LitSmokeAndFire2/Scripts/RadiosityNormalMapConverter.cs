using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "RadiosityNormalMapConverter", menuName = "Radiosity Converter", order = 3)]
public class RadiosityNormalMapConverter : ScriptableObject
{
#if UNITY_EDITOR
    [Button("OnConvertButtonClick" , "Convert")]
    public bool _bConvertTextureButton;

    [Button("OnConvertPlusAlphaButtonClick", "Convert and apply transparency")]
    public bool _bConvertTexturePlusAlphaButton;

    [Button("OnBuildEmissiveTextureButtonClick", "Build Lookup Emissive Texture")]
    public bool _bBuildEmissiveTextureButton;

    [Button("OnBlurTextureTestButtonClick", "Test Blur Function")]
    public bool _bOnBlurTestButtonClick;

    public bool _bEnableBluring = false;
        
    public int _iBlurDistance = 20;

    public Texture2D _texNormalTextureToConvert;

    public Texture2D _texTransparencyMap;

    public Texture2D _TexEmissiveIntensityMap;

    public Texture2D _texOcclusionMap;

    public Texture2D _texTextureToBlur;

    public AnimationCurve _amcEmissiveStrengthReMap;

    public AnimationCurve _amcEmissiveOcclusionEffect;

    public AnimationCurve _amcTransparencyRemap;

    public AnimationCurve _amcNormalTransparencyPackingErrorAllowence;

    public float fEmissionLookupEndPercent;
    public float fOcclusionLookupStartPercent;

    public float _fAlphaMultiplyer = 1;

    [FileStructureString(FileStructureStringAttribute.FileAddressOptions.FOLDER_REFFERENCE_ONLY)]
    public string _strSaveDirectory;

    public string _strFileName;

    public float _fMinNormalLength = 0.1f;

    public float _fMaxNormalLength = 0.866f;

    public float _fSourceMinLength = 0.1f;

    public float _fSourceMaxLength = 0.8f;

    public bool _bUseNormalLengthRemapping = true;

    public AnimationCurve _amcNormalLengthReMap;

    public void OnBuildEmissiveTextureButtonClick()
    {
        //convert normal to radiosity normal
        Texture2D texOutput = ConvertToRadiosityMap(_texNormalTextureToConvert);

        //blur the normal texture
        if (_bEnableBluring == true)
        {
            BlurTextureNormals(texOutput, 1, 0, _iBlurDistance);
            BlurTextureNormals(texOutput, 0, 1, _iBlurDistance);
            BlurTextureNormals(texOutput, 1, 1, _iBlurDistance);
            BlurTextureNormals(texOutput, 1, -1, _iBlurDistance);
        }

        //encode lookup value
        //texOutput = EncodeLookupValue(texOutput, _TexEmissiveIntensityMap, _texOcclusionMap);

        //normalize values
        // texOutput = NormalizeValues(texOutput);

        //equalise brightness 
        texOutput = SaturateNormalLenghts(texOutput, _fMinNormalLength, _fMaxNormalLength, _fSourceMinLength, _fSourceMaxLength);

        //apply transparency 
        texOutput = ApplyTransparencyTexture(texOutput, _texTransparencyMap);

        texOutput = EncodeEmissiveAlphaLookup(texOutput, _TexEmissiveIntensityMap);
        //texOutput = EncodeOcclusionAlpha(texOutput, _texOcclusionMap);

        texOutput = EncodeNormalTransparencyPacking2(texOutput, _texTransparencyMap);

        //save texture
        SaveTexture(texOutput);
    }

    public void OnBlurTextureTestButtonClick()
    {
        Texture2D texBlured = new Texture2D( _texTextureToBlur.width, _texTextureToBlur.height);
        texBlured.SetPixels(_texTextureToBlur.GetPixels());

        BlurTextureNormals(texBlured, 1, 0, _iBlurDistance);
        BlurTextureNormals(texBlured, 0, 1, _iBlurDistance);
        //BlurTextureNormals(texBlured, 1, 1, _iBlurDistance);
        //BlurTextureNormals(texBlured, 1, -1, _iBlurDistance);

        texBlured.Apply();

        SaveTexture(texBlured);
    }

    public void BlurTextureNormals(Texture2D texTarget, int iBlurXStep , int iBlurYStep , int iBlurWidth)
    {
        //build arays

        //output texture
        Texture2D texOutput = new Texture2D(texTarget.width, texTarget.height,TextureFormat.ARGB32 , false);

        //array to hold the current blur addresses
        int[] iCurrentXBlurCord;
        int[] iCurrentYBlurCord;

        //number of steps needed to blur the entire image
        int iTotalBlurSteps = 0;

        //array to hold the current blur colours
        Color[] colCurrentBlurColour;

        //build start cords;
        if(iBlurXStep == 0)
        {
            iCurrentXBlurCord = new int[texTarget.width];
            iCurrentYBlurCord = new int[texTarget.width];

            iTotalBlurSteps = texTarget.height;

            for (int i = 0; i < texTarget.width; i++)
            {
                iCurrentXBlurCord[i] = i;
                iCurrentYBlurCord[i] = 0;
            }

        }
        else
        {
            iCurrentXBlurCord = new int[texTarget.width];
            iCurrentYBlurCord = new int[texTarget.width];

            iTotalBlurSteps = texTarget.width;

            for (int i = 0; i < texTarget.height; i++)
            {
                iCurrentXBlurCord[i] = 0;
                iCurrentYBlurCord[i] = i;
            }

        }

        //colour Scaler
        float fColourScale = 1 / (float)((iBlurWidth * 2) + 1);

        //build colour list
        colCurrentBlurColour = new Color[iCurrentXBlurCord.Length];

        //fill inital colour list
        for(int i = 0; i< iCurrentXBlurCord.Length; i++)
        {
            //set inital colour
            colCurrentBlurColour[i] = Color.clear;

            //loop through to front cords
            for (int j = -iBlurWidth; j <= iBlurWidth; j++)
            {
                int iXBlurCord = iCurrentXBlurCord[i] + (iBlurXStep * j);
                int iYBlurCord = iCurrentYBlurCord[i] + (iBlurYStep * j);

                //get colour from source texture
                Color colSourceColour = texTarget.GetPixel(iXBlurCord, iYBlurCord);

                //add to  start blur colour
                colCurrentBlurColour[i] += (colSourceColour * fColourScale);
            }
        }

        //apply the inital blur colours
        for(int i = 0; i < iCurrentXBlurCord.Length; i++)
        {
            texOutput.SetPixel(iCurrentXBlurCord[i], iCurrentYBlurCord[i], colCurrentBlurColour[i]);
        } 

        //loop though all the pixels in a row / columb based on the blur direction and apply the blur 
        for(int i = 1; i < iTotalBlurSteps; i++)
        {
            //loop though all the blur columbs/rows
            for(int j = 0; j < iCurrentXBlurCord.Length; j++)
            {
                //get the old blur start cords 
                int iXBlurStart = iCurrentXBlurCord[j] + (iBlurXStep * -iBlurWidth);
                int iYBlurStart = iCurrentYBlurCord[j] + (iBlurYStep * -iBlurWidth);

                //get old blur colour
                Color colBlurStartCol = texTarget.GetPixel(iXBlurStart, iYBlurStart);

                //move blur along one step
                iCurrentXBlurCord[j] += iBlurXStep;
                iCurrentYBlurCord[j] += iBlurYStep;

                //calculate blur end 
                int iXBlurEnd = iCurrentXBlurCord[j] + (iBlurXStep * iBlurWidth);
                int iYBlurEnd = iCurrentYBlurCord[j] + (iBlurYStep * iBlurWidth);

                //get new blur colour
                Color colBlurEndCol = texTarget.GetPixel(iXBlurEnd, iYBlurEnd);

                //apply blur change
                colCurrentBlurColour[j] += (colBlurEndCol * fColourScale);
                colCurrentBlurColour[j] -= (colBlurStartCol * fColourScale);

                //store result in output texture
                texOutput.SetPixel(iCurrentXBlurCord[j], iCurrentYBlurCord[j], colCurrentBlurColour[j]);
            }

            
        }

        //apply the blured
        texTarget.SetPixels(texOutput.GetPixels());
    }

    public Texture2D SaturateNormalLenghts(Texture2D texNormals, float fMinLength, float fMaxLength , float fExcludeValuesLessThan, float fExcludeValuesMoreThan)
    {
        Color[] colNormals = texNormals.GetPixels();

        float _fMinBrightness = float.MaxValue;
        float _fMaxBrightness = float.MinValue;
        
        //get min max brightness
        foreach(Color colColour in colNormals)
        {
            //convert to vector
            float fBrightness =( new Vector3(colColour.r, colColour.g, colColour.b)).magnitude;

            if (fBrightness > fExcludeValuesLessThan && fBrightness < fExcludeValuesMoreThan)
            {

                _fMinBrightness = Mathf.Min(_fMinBrightness, fBrightness);
                _fMaxBrightness = Mathf.Max(_fMaxBrightness, fBrightness);
            }

        }

        float fSourceScale = (1 / (_fMaxBrightness - _fMinBrightness));

        float fDestRange = fMaxLength - fMinLength;

        float fScaleValue = fSourceScale * fDestRange;

        float fSubValue = (-_fMinBrightness * fSourceScale * fDestRange) + fMinLength;


        Debug.Log(" Scale " + fScaleValue + " Sub Value" + fSubValue + " Min Brightness " + _fMinBrightness + " Max Brightness" + _fMaxBrightness);

        //apply min max brightness to colour range
        for(int i = 0; i < colNormals.Length; i++)
        {
            if (_bUseNormalLengthRemapping == false)
            {
                colNormals[i] = new Color((colNormals[i].r * fScaleValue) + fSubValue, (colNormals[i].g * fScaleValue) + fSubValue, (colNormals[i].b * fScaleValue) + fSubValue, colNormals[i].a);
            }
            else
            {
                Vector3 vecNormal = new Vector3((colNormals[i].r - _fMinBrightness) * fSourceScale, (colNormals[i].g - _fMinBrightness) * fSourceScale, (colNormals[i].b - _fMinBrightness) * fSourceScale);

                float fRemapValue = _amcNormalLengthReMap.Evaluate(vecNormal.magnitude);

                vecNormal = vecNormal.normalized * fRemapValue;

                colNormals[i] = new Color(vecNormal.x, vecNormal.y, vecNormal.z, colNormals[i].a);
            }
        }

        Texture2D texOutput = new Texture2D(texNormals.width, texNormals.height);

        texOutput.SetPixels(colNormals);

        return texOutput;
    }

    public Texture2D EncodeNormalTransparencyPacking(Texture2D texSource, Texture2D texSourceTransparency )
    {

        //float fMaxDifference = 0.2f;
        Color[] colSource = texSource.GetPixels();
        Color[] colSourceTransparency = texSourceTransparency.GetPixels();
        
        for(int i = 0; i < colSource.Length; i++)
        {
            //calculate normal lenght for source transparency
            float fTransparencyNormalLength =  Mathf.Sqrt((colSourceTransparency[i].a - 1.3333333333333f) / -0.444444444444444f);

            //lerp normal towards 1,1,1
            Vector3 vecMaxNormalLength = new Vector3(1, 1, 1);

            //normal lerp lenght = 
            float fVectorDirectionLerp = (colSourceTransparency[i].a * -1.125f) + 1;

            //calculate normal
            Vector3 vecNormal = new Vector3(colSource[i].r, colSource[i].g, colSource[i].b).normalized;

            //lerp to capture the max ranges
            vecNormal = Vector3.Lerp(vecNormal, vecMaxNormalLength, fVectorDirectionLerp).normalized;

            vecNormal = vecNormal * fTransparencyNormalLength;

            //convert back into colour
            Color colNormalTransparency = new Color(vecNormal.x, vecNormal.y, vecNormal.z, colSource[i].a);

            //get the packed transparency 
            float fPackedTransparency = Mathf.Clamp01(colSource[i].a * 2);

            //get difference between packed alpha and source alpha 
            float fAlphaDif = Mathf.Clamp01(fPackedTransparency - colSourceTransparency[i].a);

            //get normal transparency mapping 
            float fNormalTransparencyMapping = Mathf.Clamp01(_amcNormalTransparencyPackingErrorAllowence.Evaluate(fAlphaDif));

            //apply normal transparency mapping 
            colSource[i] = Color.Lerp(colSource[i], colNormalTransparency, fNormalTransparencyMapping);
            // colSource[i] = Color.Lerp(colNormalTransparency,colSource[i],  fNormalTransparencyMapping);

             //colSource[i] = Color.Lerp(Color.black, Color.white, fNormalTransparencyMapping);

            //fAlphaDif *= 2;

            //colSource[i] = new Color(fAlphaDif, fAlphaDif, fAlphaDif, 1);

        }

        Texture2D texOutput = new Texture2D(texSource.width, texSource.height);

        texOutput.SetPixels(colSource);

        return texOutput;

    }

    public Texture2D EncodeNormalTransparencyPacking2(Texture2D texSource, Texture2D texSourceTransparency)
    {
        int iLayerDistance = 5;
        float fMaxError = 0.9f;

        //setup target texture
        TextureHandeler txhSource = new TextureHandeler(texSource);

        TextureHandeler txhTransparency = new TextureHandeler( texSourceTransparency);

        for (int i = 0; i < txhTransparency.GetLength(); i++)
        {
        
           //distance to the nearest emissive pixle 
           float fDistancePercentToEmissive = float.MaxValue;
           
           //loop through all the layers
           for(int iLayer = 0; iLayer < iLayerDistance; iLayer++)
           {
           
               List<Color> colSourceLayerPixles = txhSource.GetNeighbours(i, iLayer);
               List<Color> colAlphaLayerPixels = txhTransparency.GetNeighbours(i, iLayer);
           
               //loop through pixles in layer
               for (int j = 0; j < colSourceLayerPixles.Count; j++)
               {
                   //check if pixel is emissive
                   if(colSourceLayerPixles[j].a > 0.5f && colAlphaLayerPixels[j].a < fMaxError)
                   {
                        //calc distance to emissive
                        fDistancePercentToEmissive = Mathf.Clamp01((float)(iLayer -1) / (float)(iLayerDistance -3));
                       // fDistancePercentToEmissive = 0;

                       break;
                   }
               }
           
               //check if distance was found 
               if(fDistancePercentToEmissive != float.MaxValue)
               {
                   break;
               }
           }
           
           if(fDistancePercentToEmissive == float.MaxValue)
           {
               fDistancePercentToEmissive = 1;
           }

            //calculate all the emissive stuff 

            //calculate the normal length and direction to get the correct alpha

            //calculate normal lenght for source transparency
            float fTransparencyNormalLength = Mathf.Sqrt((txhTransparency.GetPixle(i).a - 1.3333333333333f) / -0.444444444444444f);

            //lerp normal towards 1,1,1
            Vector3 vecMaxNormalLength = new Vector3(1, 1, 1);

            //normal lerp lenght = 
            float fVectorDirectionLerp = Mathf.Clamp01((txhTransparency.GetPixle(i).a * 1.2f )-0.2f);

            //calculate normal
            Vector3 vecNormal = new Vector3(txhSource.GetPixle(i).r, txhSource.GetPixle(i).g, txhSource.GetPixle(i).b).normalized;

            //lerp to capture the max ranges
            vecNormal = Vector3.Lerp(vecMaxNormalLength , vecNormal, fVectorDirectionLerp).normalized;

            //vecNormal = vecNormal * fTransparencyNormalLength;
            //vecNormal = (new Vector3(1, 1, 1)).normalized * fTransparencyNormalLength;
            vecNormal = vecNormal.normalized * fTransparencyNormalLength;

            //convert back into colour
            Color colNormalTransparency = new Color(vecNormal.x, vecNormal.y, vecNormal.z, txhSource.GetPixle(i).a);

            //apply normal transparency mapping 
            // txhSource.SetPixle(i,Color.Lerp(colNormalTransparency, txhSource.GetPixle(i), 0));
            txhSource.SetPixle(i, Color.Lerp(colNormalTransparency, txhSource.GetPixle(i), fDistancePercentToEmissive));
        }

        return txhSource.GenerateTexture();
    }

    public Texture2D EncodeLookupValue(Texture2D texSourceNormal,Texture2D texSourceEmissive, Texture2D texSourceOcclusion)
    {
        Color[] colSourceNormal = texSourceNormal.GetPixels();
        Color[] colSourceEmissive = texSourceEmissive.GetPixels();
       Color[] colSourceOcclusion = texSourceOcclusion.GetPixels();
       
       for(int i = 0; i < colSourceNormal.Length; i++)
       {
           float fLookupValue = CalcEmissiveOcclusionLookupValue(colSourceEmissive[i].maxColorComponent, colSourceOcclusion[i].maxColorComponent);
           colSourceNormal[i] = EncodeLookupValueInColor(colSourceNormal[i], fLookupValue);
       }

        Texture2D texOutput = new Texture2D(texSourceNormal.width, texSourceNormal.height);

        texOutput.SetPixels(colSourceNormal);

        return texOutput;
    }

    public Texture2D EncodeEmissiveAlphaLookup(Texture2D texSource, Texture2D texSourceEmissive)
    {
        Color[] colSource = texSource.GetPixels();
        Color[] colSourceEmissive = texSourceEmissive.GetPixels();

        for (int i = 0; i < colSource.Length; i++)
        {
            float fLookupValue = (colSource[i].a * 0.5f);

            float fEmissive = Mathf.Clamp01(_amcEmissiveStrengthReMap.Evaluate(colSourceEmissive[i].maxColorComponent));

            if(fEmissive > (3.0f / 256.0f))
            {
                fLookupValue = 0.5f + (fEmissive * 0.5f);
            }

            colSource[i].a = fLookupValue;
        }

        Texture2D texOutput = new Texture2D(texSource.width, texSource.height);

        texOutput.SetPixels(colSource);

        return texOutput;
    }

    public Texture2D NormalizeValues(Texture2D texSource)
    {
        Color[] colSource = texSource.GetPixels();

        for (int i = 0; i < colSource.Length; i++)
        {
            Vector3 vecNormal = new Vector3(colSource[i].r, colSource[i].g, colSource[i].b).normalized;

            colSource[i] = new Color(vecNormal.x, vecNormal.y, vecNormal.z, colSource[i].a);
        }

        Texture2D texOutput = new Texture2D(texSource.width, texSource.height);

        texOutput.SetPixels(colSource);

        return texOutput;
    }

    public float[] ExtractNormalLengths(Texture2D texSource)
    {
        //get source colours
        Color[] colSource = texSource.GetPixels();

        //get normal lengths
        float[] fNormalLenghts = new float[colSource.Length];

        for(int i = 0; i < colSource.Length; i++)
        {
            Vector3 vecNormal = new Vector3(colSource[i].r, colSource[i].g, colSource[i].b);

            fNormalLenghts[i] = vecNormal.magnitude;
        }

        return fNormalLenghts;
    }

    public Texture2D EncodeOcclusionAlpha(Texture2D texSource,Texture2D texOcclusion)
    {
        Color[] colSource = texSource.GetPixels();
        Color[] colOclusion = texSource.GetPixels();

        for(int i = 0; i < colSource.Length; i++)
        {
            float EncodedAlpha = EncodeOcclusionLookupInAlpha(colSource[i].a, colOclusion[i].maxColorComponent);

            colSource[i].a = EncodedAlpha;
        }

        Texture2D texOutput = new Texture2D(texSource.width, texSource.height);

        texOutput.SetPixels(colSource);

        return texOutput;
    }

    public float EncodeOcclusionLookupInAlpha(float fRawAlpha , float fOcclusion)
    {
        float fUpscaledAlpha = Mathf.Clamp01( fRawAlpha * 1.15f);
        float fOcclusionFadeOut = Mathf.Clamp01(((fRawAlpha * 1.15f) - 1) *40);
        float fFadedOccluson = Mathf.Clamp01(1 - fOcclusion) * fOcclusionFadeOut;

        if(fUpscaledAlpha < 1)
        {
            return fUpscaledAlpha * 0.5f;
        }
        else
        {
            return (fFadedOccluson * 0.5f) + 0.5f;
        }
    }

    public Color EncodeLookupValueInColor(Color colSourceColor, float LookupValue)
    {

        Vector3 vecColourNormal = (new Vector3(colSourceColor.r, colSourceColor.g, colSourceColor.b)).normalized;

        vecColourNormal = vecColourNormal * LookupValue;

        return new Color(vecColourNormal.x, vecColourNormal.y, vecColourNormal.z, colSourceColor.a);
    }

    //given an emissive value and an occlusion value what should the lookup value be 
    public float CalcEmissiveOcclusionLookupValue(float fEmissiveLevel, float fOcclusionAmount)
    {
        float fReMappedEmissive = _amcEmissiveStrengthReMap.Evaluate(fEmissiveLevel);
        float fEmissiveEffectedOcclusion = Mathf.Min(_amcEmissiveOcclusionEffect.Evaluate(fEmissiveLevel), fOcclusionAmount);
        if (fReMappedEmissive > 0)
        {
            return Mathf.Lerp(fEmissionLookupEndPercent, 0 , fReMappedEmissive);
        }
        else
        {
            return Mathf.Lerp( fOcclusionLookupStartPercent,1, fEmissiveEffectedOcclusion);
        }

    }

    public void OnConvertButtonClick()
    {
        Texture2D texConvertexTexture = ConvertToRadiosityMap(_texNormalTextureToConvert);

        SaveTexture(texConvertexTexture);
    }

    public void OnConvertPlusAlphaButtonClick()
    {
        Texture2D texConvertexTexture = ConvertToRadiosityMap(_texNormalTextureToConvert);
        texConvertexTexture = ApplyTransparencyTexture(texConvertexTexture, _texTransparencyMap);
        SaveTexture(texConvertexTexture);
    }

    public Texture2D ConvertToRadiosityMap(Texture2D texSource)
    {
        //get source pixels
        Color[] colSourcePixels = texSource.GetPixels();

        Vector3 vecTriAxisRight = new Vector3(0.81649658092f, 0, 0.57735026999f);
        Vector3 vecTriAxisUpLeft = new  Vector3(-0.40824829046f, 0.70710678118f, 0.57735026999f);
        Vector3 vecTriAxisDownLeft = new Vector3(-0.40824829046f, -0.70710678118f, 0.57735026999f);

        for (int i = 0; i < colSourcePixels.Length; i++)
        {
            //get normal
            Vector3 vecNormal = DecodeNormal(colSourcePixels[i]);

            Vector3 vecTriAxisWeights = Vector3.zero;

            vecTriAxisWeights.x = Mathf.Clamp01( Vector3.Dot(vecTriAxisRight, vecNormal));
            vecTriAxisWeights.y = Mathf.Clamp01(Vector3.Dot(vecTriAxisUpLeft, vecNormal));
            vecTriAxisWeights.z = Mathf.Clamp01(Vector3.Dot(vecTriAxisDownLeft, vecNormal));

            //apply change
            Color colOutput = new Color(vecTriAxisWeights.x, vecTriAxisWeights.y, vecTriAxisWeights.z, colSourcePixels[i].a);

            colSourcePixels[i] = colOutput;
        }

        //store result in new texture
        Texture2D texOutput = new Texture2D(texSource.width, texSource.height);

        texOutput.SetPixels(colSourcePixels);

        return texOutput;
    }

    public Texture2D ApplyTransparencyTexture(Texture2D texTargetTexture, Texture2D texTransparency)
    {
        //get all the target pixel colours 
        Color[] colTargetPixels = texTargetTexture.GetPixels();

        //get transparency pixeles 
        Color[] colTransparencyPixels = texTransparency.GetPixels();

        //loop through all teh pixels and apply effect
        for (int i = 0; i < colTargetPixels.Length && i < colTransparencyPixels.Length; i++)
        {
            colTargetPixels[i].a =  colTransparencyPixels[i].a * _fAlphaMultiplyer;

            //normal lerp value 
            float fNormalLerp = Mathf.Clamp01(colTargetPixels[i].a * 32);
           
            //flaten out low alpha normal
           
            colTargetPixels[i].r = Mathf.Lerp( 1f,colTargetPixels[i].r, fNormalLerp);
            colTargetPixels[i].g = Mathf.Lerp( 1f,colTargetPixels[i].g, fNormalLerp);
            colTargetPixels[i].b = Mathf.Lerp( 1f, colTargetPixels[i].b, fNormalLerp);

        }

        Texture2D texOutput = new Texture2D(texTargetTexture.width, texTargetTexture.height);

        //apply transparency effect of emissive mapt to texture

        texOutput.SetPixels(colTargetPixels);

        return texOutput;
    }
    public Vector3 DecodeNormal(Color colNormal)
    {
        Vector3 vecNormal = new Vector3(colNormal.r, colNormal.g, colNormal.b) * 2;

        vecNormal -= Vector3.one;

        return vecNormal;
    }

    public void SaveTexture(Texture2D texTexture)
    {
        byte[] bytPNG = texTexture.EncodeToPNG();

        Debug.Log("Attempted save directory | " + Application.dataPath + "/" + _strSaveDirectory + "/" + _strFileName + ".png");

        File.WriteAllBytes(Application.dataPath + "/../" + _strSaveDirectory + "/" + _strFileName + ".png", bytPNG);

        AssetDatabase.Refresh();
    }

#endif
}
