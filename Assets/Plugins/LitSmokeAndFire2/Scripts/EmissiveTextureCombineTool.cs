using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Emissive texture combiner", menuName = "LSAF2 Emissive texture combiner", order = 3)]
public class EmissiveTextureCombineTool : ScriptableObject
{
#if UNITY_EDITOR
    [System.Serializable]
    public class TextureToCombine
    {
        public Texture2D EmissiveTexture;
        public float EmissiveBlendPercent;
    }

    [SerializeField]
    [Help("Some of the source emissive sprite sheets are split into sparks and flame sheets. Use this tool to recombine the source emissive sprite sheets.")]
    protected bool _bDescription;

    [SerializeField]
    [Button("OnTextureMerge", "Build Emissive Texture")]
    protected bool _bTextureBuildButton;

    [SerializeField]
    [Help("This option converts the source emissive to grayscale for use with Unity’s built in sprite sheet recolouring tool. ")]
    protected bool _bHelp1;
    public bool _bConvertToGreyScale;

    [FileStructureString(FileStructureStringAttribute.FileAddressOptions.FOLDER_REFFERENCE_ONLY)]
    public string _strSaveDirectory;
    public string _strFileName;

    public TextureToCombine[] EmissiveTextures;

	public void OnTextureMerge()
    {
        //output texture
        Color[] colOutput = null;

        //width and height 
        int iWidth = 0;
        int iHeight = 0;

        //loop through all the textures
        for(int i = 0; i < EmissiveTextures.Length; i++)
        {
            Texture2D texEmissiveTexture = GetReadSafeVersionOfTexture(EmissiveTextures[i].EmissiveTexture);

            if(texEmissiveTexture != null)
            {
                iWidth = texEmissiveTexture.width;
                iHeight = texEmissiveTexture.height;

                Color[] colSourcePixels = texEmissiveTexture.GetPixels();
       


                if(colOutput == null)
                {
                    colOutput = new Color[colSourcePixels.Length];

                    for(int j = 0; j < colOutput.Length; j++ )
                    {
                        colOutput[j] = Color.black;
                    }
                }

                for(int j = 0; j < colOutput.Length && j < colSourcePixels.Length; j++)
                {
                    float fBlendPercent = EmissiveTextures[i].EmissiveBlendPercent;

                    colOutput[j] += new Color(colSourcePixels[j].r * fBlendPercent, colSourcePixels[j].g * fBlendPercent, colSourcePixels[j].b * fBlendPercent, 0);
                }

            }
        }

        Texture2D texOutput = new Texture2D(iWidth, iHeight);

        texOutput.SetPixels(colOutput);

        if(_bConvertToGreyScale)
        {
            texOutput = ConvertToGreyScale(texOutput);
        }

        SaveTexture(texOutput);

    }

    public Texture2D ConvertToGreyScale(Texture2D texSource)
    {
        Color[] colSource = texSource.GetPixels();
        
        for(int i = 0; i < colSource.Length; i++)
        {
            colSource[i] = new Color(colSource[i].maxColorComponent, colSource[i].maxColorComponent, colSource[i].maxColorComponent, 1);
        }

        Texture2D texOutput = new Texture2D(texSource.width, texSource.height);

        texOutput.SetPixels(colSource);

        return texOutput;
    }

    public void SaveTexture(Texture2D texTexture)
    {
        byte[] bytPNG = texTexture.EncodeToPNG();

        Debug.Log("Attempted save directory | " + Application.dataPath + "/" + _strSaveDirectory + "/" + _strFileName + ".png");

        File.WriteAllBytes(Application.dataPath + "/../" + _strSaveDirectory + "/" + _strFileName + ".png", bytPNG);

        AssetDatabase.Refresh();
    }

    public Texture2D GetReadSafeVersionOfTexture(Texture texSource)
    {
        if(texSource ==  null)
        {
            return null;
        }

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

#endif
}
