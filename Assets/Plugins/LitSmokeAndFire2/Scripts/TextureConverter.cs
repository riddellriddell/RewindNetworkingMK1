using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "TextureConverter", menuName = "LSAF2 Texture Converter", order = 1)]
public class TextureConverter : ScriptableObject
{
#if UNITY_EDITOR

    [SerializeField]
    [Help("This tool converts the raw normal and emissive maps into the texture format used by lit smoke and fire 2")]
    protected bool _bDescription;

    [SerializeField]
    [Help("Compiling the texture can take a while make sure you have the correct settings before you start")]
    protected bool _bHelp1;
    [SerializeField]
    [Button("ConvertTexture")]
    protected bool _bBuildButton;


    [SerializeField]
    [Help("The normal map texture format for lit smoke and fire 2 is different than standard normal maps check the user docs for extra details.This texture is required for conversion process to work.")]
    protected bool _bHelp2;
    [CleanInspectorName()]
    public Texture2D _texNormalMap;

    [SerializeField]
    [Help("This tool can convert 2 different normal formats into the correct lit smoke and fire texture. When using the textures included in this package leave this value disabled. For more details and a better description of the radiosity format check the user docs. ")]
    protected bool _bHelp3;
    [CleanInspectorName()]
    public bool _bAlreadyRadiosityFormat;

    [SerializeField]
    [Help("This is the texture that contains the transparency of particle sheet. This texture is required for conversion process to work. If the normal and transparency is in the same texture you must attach it to both values.")]
    protected bool _bHelp4;
    [CleanInspectorName()]
    public Texture2D _texOpacityMap;

    [SerializeField]
    [Help("This is an emissive INTENSITY texture(black and white flame texture) not the intended emissive colour texture. The brightness value of this texture is packed into a packed emissive texture and used later with a colour lookup texture to calculate the emissive color. This value is OPTIONAL if this value is not included a non emissive texture will be produced. If an emissive intensity texture is included a packed emissive texture will be produced. Making a packed emissive texture takes a lot of time make sure all the settings are correct before you start.")]
    protected bool _bHelp5;
    [CleanInspectorName()]
    public Texture2D _texEmissiveMap;

    [SerializeField]
    [Help("The length of the normals (color brightness) in the normal texture define the ambient occlusion (dark spots) in the final particle render. Remapping normal ranges changes the length of the normals and clamps them between a min and max value. Use this feature to increase or decrease the brightness or contrast in the smoke.")]
    protected bool _bHelp6;
    [CleanInspectorName()]
    public bool _bRemapNormalRange;

    [SerializeField]
    [Help("Normal lengths less than this (Darker) will be increased to this length", "_bRemapNormalRange")]
    protected bool _bHelp7;
    [CleanInspectorName("", "_bRemapNormalRange")]
    public float _fSourceMinLength;


    [SerializeField]
    [Help("Normal lengths longer than this (brighter) will be shortened to this length.", "_bRemapNormalRange")]
    protected bool _bHelp8;
    [CleanInspectorName("", "_bRemapNormalRange")]
    public float _fSourceMaxLenght;

    [SerializeField]
    [Help("Normals that have passed the source min length filter will be remapped to this length.", "_bRemapNormalRange")]
    protected bool _bHelp9;
    [CleanInspectorName("", "_bRemapNormalRange")]
    public float _fDestMinLength;

    [SerializeField]
    [Help("Normals that have passed the source max length filter will be remapped to this length.", "_bRemapNormalRange")]
    protected bool _bHelp10;
    [CleanInspectorName("", "_bRemapNormalRange")]
    public float _fDestMaxLength;

    [SerializeField]
    [Help("Blurs the normals. Use this feature to create smoother softer looking particles.")]
    protected bool _bHelp11;
    [CleanInspectorName("")]
    public bool _bBlurNormal;

    [SerializeField]
    [Help("The amount of blurring to apply." , "_bBlurNormal")]
    protected bool _bHelp12;
    [CleanInspectorName("", "_bBlurNormal")]
    public int _iNormalBlurWidth;

    [SerializeField]
    [Help("Blurs the transparency. Use this feature to reduce particle popping and create a smoother outline around the effect.")]
    protected bool _bHelp13;
    [CleanInspectorName("")]
    public bool _bBlurTransparency;

    [SerializeField]
    [Help("The amount of blurring to apply.", "_bBlurTransparency")]
    protected bool _bHelp14;
    [CleanInspectorName("", "_bBlurTransparency")]
    public int _iTransparencyBlurWidth;

    [FileStructureString(FileStructureStringAttribute.FileAddressOptions.FOLDER_REFFERENCE_ONLY)]
    public string _strSaveDirectory;

    public string _strFileName;

    public void ConvertTexture()
    {
        //work out what conversion type to perform
        if(_texEmissiveMap != null)
        {
            ConvertNormalToEmissive(_bAlreadyRadiosityFormat);
        }
        else
        {
            ConvertNormalToRadiosity(_bAlreadyRadiosityFormat);
        }

    }

    public void ProgressBar(float fTimeOfStart,float fProgress,string strHeading, string strinfo)
    {
       //float fProgress = 0.1f;

        float fTimeSpent = Time.realtimeSinceStartup - fTimeOfStart;

        float fTimeLeft = Mathf.Clamp((fTimeSpent * (1 / Mathf.Clamp01(fProgress) + 0.000001f)) * (1 - fProgress),0,86400f);


        TimeSpan t = TimeSpan.FromSeconds(fTimeLeft);

        string strTimeLeft = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms",
                        t.Hours,
                        t.Minutes,
                        t.Seconds,
                        t.Milliseconds);


        EditorUtility.DisplayProgressBar(strHeading, strinfo + " Time Left :" + strTimeLeft, fProgress);
    }

    public bool CancelableProgressBar(float fTimeOfStart, float fProgress, string strHeading, string strinfo)
    {
        //float fProgress = 0.1f;

        float fTimeSpent = Time.realtimeSinceStartup - fTimeOfStart;

        float fTimeLeft = Mathf.Clamp((fTimeSpent * (1 / Mathf.Clamp01(fProgress) + 0.000001f)) * (1 - fProgress), 0, 86400f);


        TimeSpan t = TimeSpan.FromSeconds(fTimeLeft);

        string strTimeLeft = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms",
                        t.Hours,
                        t.Minutes,
                        t.Seconds,
                        t.Milliseconds);


        bool bResult =  EditorUtility.DisplayCancelableProgressBar(strHeading, strinfo + " Time Left :" + strTimeLeft, fProgress);

        if (bResult == true)
        {
            EditorUtility.ClearProgressBar();

            Debug.LogWarning("Texture Build Stopped");
        }

        return bResult;
    }
   
    public void ConvertNormalToRadiosity(bool bRadiosityMap)
    {

        EditorUtility.ClearProgressBar();

        Texture2D texNormalMap = GetReadSafeVersionOfTexture(_texNormalMap);

        float fTimeOfStart = Time.realtimeSinceStartup;


        ProgressBar(fTimeOfStart, 0.1f, "LSAF2 Texture Convert ", "Encoding Radiosity,");



        if (!bRadiosityMap)
        {
            texNormalMap = ConvertToRadiosityMap(texNormalMap);
        }

        if(_bBlurNormal)
        {
            ProgressBar(fTimeOfStart, 0.2f, "LSAF2 Texture Convert ", "Bluring Normal,");
            texNormalMap = BlurTexture(texNormalMap, _iNormalBlurWidth);
        }

        ProgressBar(fTimeOfStart, 0.5f, "LSAF2 Texture Convert ", "Remapping Normal Range,");

        if (_bRemapNormalRange)
        {
            texNormalMap = SaturateNormalLenghts(texNormalMap, _fDestMinLength, _fDestMaxLength, _fSourceMinLength, _fSourceMaxLenght);
        }

       


        Texture2D texTransparencyTexture = GetReadSafeVersionOfTexture(_texOpacityMap);

        if(_bBlurTransparency)
        {
            ProgressBar(fTimeOfStart, 0.75f, "LSAF2 Texture Convert ", "Bluring Transparency,");

            texTransparencyTexture = BlurTexture(texTransparencyTexture, _iTransparencyBlurWidth);
        }

        ProgressBar(fTimeOfStart, 0.75f, "LSAF2 Texture Convert ", "Applying Transparency,");

        texNormalMap = ApplyTransparencyTexture(texNormalMap, texTransparencyTexture);

        ProgressBar(fTimeOfStart, 0.9f, "LSAF2 Texture Convert ", "Saving Texture,");

        SaveTexture(texNormalMap);

        EditorUtility.ClearProgressBar();
    }

    public void ConvertNormalToEmissive(bool bRadiosityMap)
    {

        EditorUtility.ClearProgressBar();

        Texture2D texNormalMap = GetReadSafeVersionOfTexture(_texNormalMap);

        Texture2D texOpacityMap = GetReadSafeVersionOfTexture(_texOpacityMap);

        if (!bRadiosityMap)
        {
            texNormalMap = ConvertToRadiosityMap(texNormalMap);
        }

        if (_bBlurNormal)
        {
            texNormalMap = BlurTexture(texNormalMap, _iNormalBlurWidth);
        }

        if (_bRemapNormalRange)
        {
            texNormalMap = SaturateNormalLenghts(texNormalMap, _fDestMinLength, _fDestMaxLength, _fSourceMinLength, _fSourceMaxLenght);
        }

        if (_bBlurTransparency)
        {

            texOpacityMap = BlurTexture(texOpacityMap, _iTransparencyBlurWidth);
        }

        texNormalMap = ApplyTransparencyTexture(texNormalMap, texOpacityMap);

        texNormalMap = EncodeEmissiveAlphaLookup(texNormalMap, GetReadSafeVersionOfTexture(_texEmissiveMap));

        texNormalMap = EncodeNormalTransparencyPacking(texNormalMap, texOpacityMap);

        //catch user cancel
        if(texNormalMap == null)
        {
            return;
        }

        SaveTexture(texNormalMap);
    }

    public Vector3 DecodeNormal(Color colNormal)
    {
        Vector3 vecNormal = new Vector3(colNormal.r, colNormal.g, colNormal.b) * 2;

        vecNormal -= Vector3.one;

        return vecNormal;
    }

    public Texture2D GetReadSafeVersionOfTexture(Texture texSource)
    {
        // Create a temporary RenderTexture of the same size as the texture
        RenderTexture tmp = RenderTexture.GetTemporary(
                            texSource.width,
                            texSource.height,
                            0,
                            RenderTextureFormat.Default,
                            RenderTextureReadWrite.Linear);

        // Blit the pixels on texture to the RenderTexture
        Graphics.Blit(texSource, tmp);

        // Backup the currently set RenderTexture
        RenderTexture previous = RenderTexture.active;

        // Set the current RenderTexture to the temporary one we created
        RenderTexture.active = tmp;

        // Create a new readable Texture2D to copy the pixels to it
        Texture2D myTexture2D = new Texture2D(texSource.width, texSource.height);

        // Copy the pixels from the RenderTexture to the new Texture
        myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
        myTexture2D.Apply();

        // Reset the active RenderTexture
        RenderTexture.active = previous;

        // Release the temporary RenderTexture
        RenderTexture.ReleaseTemporary(tmp);

        // "myTexture2D" now has the same pixels from "texture" and it's readable.
        return myTexture2D;
    }

    public Texture2D ConvertToRadiosityMap(Texture2D texSource)
    {
        //get source pixels
        Color[] colSourcePixels = texSource.GetPixels();

        Vector3 vecTriAxisRight = new Vector3(0.81649658092f, 0, 0.57735026999f);
        Vector3 vecTriAxisUpLeft = new Vector3(-0.40824829046f, 0.70710678118f, 0.57735026999f);
        Vector3 vecTriAxisDownLeft = new Vector3(-0.40824829046f, -0.70710678118f, 0.57735026999f);

        for (int i = 0; i < colSourcePixels.Length; i++)
        {
            //get normal
            Vector3 vecNormal = DecodeNormal(colSourcePixels[i]);

            Vector3 vecTriAxisWeights = Vector3.zero;

            vecTriAxisWeights.x = Mathf.Clamp01(Vector3.Dot(vecTriAxisRight, vecNormal));
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

    public Texture2D SaturateNormalLenghts(Texture2D texNormals, float fMinLength, float fMaxLength, float fMinSourceBrightness, float fMaxSourceBrightness)
    {
        Color[] colNormals = texNormals.GetPixels();

        float fSourceScale = (1 / (fMaxSourceBrightness - fMinSourceBrightness));

        float fDestRange = fMaxLength - fMinLength;

        float fScaleValue = fSourceScale * fDestRange;

        float fSubValue = (-fMinSourceBrightness * fSourceScale * fDestRange) + fMinLength;


        Debug.Log(" Scale " + fScaleValue + " Sub Value" + fSubValue + " Min Brightness " + fMinSourceBrightness + " Max Brightness" + fMaxSourceBrightness);

        //apply min max brightness to colour range
        for (int i = 0; i < colNormals.Length; i++)
        {

            colNormals[i] = new Color((colNormals[i].r * fScaleValue) + fSubValue, (colNormals[i].g * fScaleValue) + fSubValue, (colNormals[i].b * fScaleValue) + fSubValue, colNormals[i].a);

        }

        Texture2D texOutput = new Texture2D(texNormals.width, texNormals.height);

        texOutput.SetPixels(colNormals);

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
            colTargetPixels[i].a = colTransparencyPixels[i].a;// * _fAlphaMultiplyer;

            //normal lerp value 
            float fNormalLerp = Mathf.Clamp01(colTargetPixels[i].a * 32);

            //flaten out low alpha normal

            colTargetPixels[i].r = Mathf.Lerp(1f, colTargetPixels[i].r, fNormalLerp);
            colTargetPixels[i].g = Mathf.Lerp(1f, colTargetPixels[i].g, fNormalLerp);
            colTargetPixels[i].b = Mathf.Lerp(1f, colTargetPixels[i].b, fNormalLerp);

        }

        Texture2D texOutput = new Texture2D(texTargetTexture.width, texTargetTexture.height);

        //apply transparency effect of emissive mapt to texture

        texOutput.SetPixels(colTargetPixels);

        return texOutput;
    }

    public Texture2D EncodeEmissiveAlphaLookup(Texture2D texSource, Texture2D texSourceEmissive)
    {
        Color[] colSource = texSource.GetPixels();
        Color[] colSourceEmissive = texSourceEmissive.GetPixels();

        for (int i = 0; i < colSource.Length && i < colSourceEmissive.Length; i++)
        {
            float fLookupValue = (colSource[i].a * 0.5f);

            float fEmissive =  colSourceEmissive[i].maxColorComponent ;

            if (fEmissive > (3.0f / 256.0f))
            {
                fLookupValue = 0.5f + (fEmissive * 0.5f);
            }

            colSource[i].a = fLookupValue;
        }

        Texture2D texOutput = new Texture2D(texSource.width, texSource.height);

        texOutput.SetPixels(colSource);

        return texOutput;
    }

    public Texture2D EncodeNormalTransparencyPacking(Texture2D texSource, Texture2D texSourceTransparency)
    {

        float fTimeOfStart = Time.realtimeSinceStartup;

        //EditorApplication.RepaintProjectWindow();

        int iLayerDistance = 5;
        float fMaxError = 0.9f;

        //setup target texture
        TextureHandeler txhSource = new TextureHandeler(texSource);

        TextureHandeler txhTransparency = new TextureHandeler(texSourceTransparency);

        for (int i = 0; i < txhTransparency.GetLength(); i++)
        {
           
            if (i % 1000 == 0)
            {
                if(CancelableProgressBar(fTimeOfStart, (float)i / (float)txhTransparency.GetLength(), "LSAF2 Emissive Texture Convert", "Encoding Emissive Transprency,"))
                {
                    return null;
                }

            }

            

            //EditorApplication.RepaintProjectWindow();


            //distance to the nearest emissive pixle 
            float fDistancePercentToEmissive = float.MaxValue;

            //loop through all the layers
            for (int iLayer = 0; iLayer < iLayerDistance; iLayer++)
            {

                List<Color> colSourceLayerPixles = txhSource.GetNeighbours(i, iLayer);
                List<Color> colAlphaLayerPixels = txhTransparency.GetNeighbours(i, iLayer);

                //loop through pixles in layer
                for (int j = 0; j < colSourceLayerPixles.Count; j++)
                {
                    //check if pixel is emissive
                    if (colSourceLayerPixles[j].a > 0.5f && colAlphaLayerPixels[j].a < fMaxError)
                    {
                        //calc distance to emissive
                        fDistancePercentToEmissive = Mathf.Clamp01((float)(iLayer - 1) / (float)(iLayerDistance - 3));
                        // fDistancePercentToEmissive = 0;

                        break;
                    }
                }

                //check if distance was found 
                if (fDistancePercentToEmissive != float.MaxValue)
                {
                    break;
                }
            }

            if (fDistancePercentToEmissive == float.MaxValue)
            {
                fDistancePercentToEmissive = 1;
            }

            float fTargetTransparency = txhTransparency.GetPixle(i).a;

            Color colSourceColour = txhSource.GetPixle(i);

            //calculate all the emissive stuff 

            //calculate the normal length and direction to get the correct alpha

            //calculate normal lenght for source transparency
            float fTransparencyNormalLength = Mathf.Sqrt((fTargetTransparency - 1.3333333333333f) / -0.444444444444444f);

            //lerp normal towards 1,1,1
            Vector3 vecMaxNormalLength = new Vector3(1, 1, 1);

            //normal lerp length = 
            float fVectorDirectionLerp = Mathf.Clamp01((fTargetTransparency * 1.2f) - 0.2f);

            //calculate normal
            Vector3 vecRawNormal = new Vector3(colSourceColour.r, colSourceColour.g, colSourceColour.b);
            Vector3 vecNormal = vecRawNormal.normalized;

            //lerp to capture the max ranges
            vecNormal = Vector3.Lerp(vecMaxNormalLength, vecNormal, fVectorDirectionLerp).normalized;

            //vecNormal = vecNormal * fTransparencyNormalLength;
            //vecNormal = (new Vector3(1, 1, 1)).normalized * fTransparencyNormalLength;
            vecNormal = vecNormal.normalized * fTransparencyNormalLength;

            //convert back into colour
            Color colNormalTransparency = new Color(vecNormal.x, vecNormal.y, vecNormal.z, colSourceColour.a);

            //scale source normal into corect range
            vecRawNormal *= 0.8660254f;

            //get raw normal length
            float fRawLength = vecRawNormal.magnitude;

            //check if raw length is grater than limit
            if(fRawLength > 0.8660254f)
            {
                float fRescaleValue = 0.8660254f / fRawLength;

                //rescale normal 
                vecRawNormal = vecRawNormal * fRescaleValue;
            }

           

            Color colSourceNormalScaled = new Color(vecRawNormal.x, vecRawNormal.y, vecRawNormal.z, colSourceColour.a);

            //low alpha white lerp
            //normal lerp value 
            float fNormalLerp = Mathf.Clamp01(fTargetTransparency * 32);

            //flaten out low alpha normal
            colSourceNormalScaled.r = Mathf.Lerp(1f, colSourceNormalScaled.r, fNormalLerp);
            colSourceNormalScaled.g = Mathf.Lerp(1f, colSourceNormalScaled.g, fNormalLerp);
            colSourceNormalScaled.b = Mathf.Lerp(1f, colSourceNormalScaled.b, fNormalLerp);


            //apply normal transparency mapping 
            // txhSource.SetPixle(i,Color.Lerp(colNormalTransparency, txhSource.GetPixle(i), 0));
            txhSource.SetPixle(i, Color.Lerp(colNormalTransparency, colSourceNormalScaled, fDistancePercentToEmissive));



        }

        EditorUtility.ClearProgressBar();

        return txhSource.GenerateTexture();
    }

    public Texture2D BlurTexture(Texture2D texSource,int iBlurDistance)
    {
        Texture2D texBlured = new Texture2D(texSource.width, texSource.height);
        texBlured.SetPixels(texSource.GetPixels());

        SingleAxisBlurPass(texBlured, 1, 0, iBlurDistance);
        SingleAxisBlurPass(texBlured, 0, 1, iBlurDistance);
        SingleAxisBlurPass(texBlured, 1, 1, iBlurDistance);
        SingleAxisBlurPass(texBlured, 1, -1, iBlurDistance);

        return texBlured;
 
    }

    public void SingleAxisBlurPass(Texture2D texTarget, int iBlurXStep, int iBlurYStep, int iBlurWidth)
    {
        //build arays

        //output texture
        Texture2D texOutput = new Texture2D(texTarget.width, texTarget.height, TextureFormat.ARGB32, false);

        //array to hold the current blur addresses
        int[] iCurrentXBlurCord;
        int[] iCurrentYBlurCord;

        //number of steps needed to blur the entire image
        int iTotalBlurSteps = 0;

        //array to hold the current blur colours
        Color[] colCurrentBlurColour;

        //build start cords;
        if (iBlurXStep == 0)
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
        for (int i = 0; i < iCurrentXBlurCord.Length; i++)
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
        for (int i = 0; i < iCurrentXBlurCord.Length; i++)
        {
            texOutput.SetPixel(iCurrentXBlurCord[i], iCurrentYBlurCord[i], colCurrentBlurColour[i]);
        }

        //loop though all the pixels in a row / columb based on the blur direction and apply the blur 
        for (int i = 1; i < iTotalBlurSteps; i++)
        {
            //loop though all the blur columbs/rows
            for (int j = 0; j < iCurrentXBlurCord.Length; j++)
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




    public void SaveTexture(Texture2D texTexture)
    {
        byte[] bytPNG = texTexture.EncodeToPNG();

        Debug.Log("Attempted save directory | " + Application.dataPath + "/" + _strSaveDirectory + "/" + _strFileName + ".png");

        File.WriteAllBytes(Application.dataPath + "/../" + _strSaveDirectory + "/" + _strFileName + ".png", bytPNG);

        AssetDatabase.Refresh();
    }


#endif
}
