using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Lookup Texture Builder", menuName = "LSAF2 Lookup Texture", order = 2)]
public class LookupTextureTool : ScriptableObject
{
#if UNITY_EDITOR
    [SerializeField]
    [Help("Use this tool to make lookup textures to use with packed emissive textures.")]
    protected bool _bDescription;

    public Gradient _grdGarient;

    [SerializeField]
    [Button("BuildTexture","Build Lookup Textuer")]
    protected bool _bTextureBuildButton;

    [FileStructureString(FileStructureStringAttribute.FileAddressOptions.FOLDER_REFFERENCE_ONLY)]
    public string _strSaveDirectory;
    public string _strFileName;

    public void SaveTexture(Texture2D texTexture)
    {
        byte[] bytPNG = texTexture.EncodeToPNG();

        Debug.Log("Attempted save directory | " + Application.dataPath + "/" + _strSaveDirectory + "/" + _strFileName + ".png");

        File.WriteAllBytes(Application.dataPath + "/../" + _strSaveDirectory + "/" + _strFileName + ".png", bytPNG);

        AssetDatabase.Refresh();
    }

    public void BuildTexture()
    {
        Texture2D texOutput = new Texture2D(256, 1);
        Color[] colColours = new Color[256];
        for(int i = 0; i < colColours.Length; i++)
        {
            float fPercentThroughRange = (float)i / (float)256;

            float fAlpha = Mathf.Clamp01( fPercentThroughRange * 2);

            Color colOutput = Color.black;

            if(fPercentThroughRange > 0.5f)
            {
                colOutput = _grdGarient.Evaluate(Mathf.Clamp01( (fPercentThroughRange * 2) - 1));
            }

            colOutput.a = fAlpha;

            colColours[i] = colOutput;
        }

        texOutput.SetPixels(colColours);

        SaveTexture(texOutput);

    }
#endif
}
