using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class GLSLANMPShaderEditor : MaterialEditor
{
    //light count options 
    protected string[] _strOptimizedLightCountOptions = {"One Light","Two Lights", "Three Lights", "Four Lights"};
    protected string[] _strUnOptimizedLightCountOptions = { "One Light", "Two Lights", "Three Lights", "Four Lights", "Five Lights", "Six Lights", "Seven Lights", "Eight Lights" };
    protected string[] _strVertexLightCountOptions = { "No Vertex Lights", "One Light", "Two Lights", "Three Lights", "Four Lights", "Five Lights", "Six Lights", "Seven Lights", "Eight Lights" };

    protected string _strNormalMappedShader = "Encap/Mobile/GLSLNormalMapped";
    protected string _strOptimisedNormalMappedShader = "Encap/Mobile/GLSLNormalMappedFast";
    protected string _strEmissiveNormalMappedShader = "Encap/Mobile/GLSLNormalMappedEmissive";
    protected string _strOptimisedEmissiveNormalMappedShader = "Encap/Mobile/GLSLNormalMappedEmissiveFast";
    protected string _strFallBackNormalMappedShader = "Encap/Mobile/GLSLNormalMappedFallBack";
    protected string _strFallBackEmissiveShader = "Encap/Mobile/GLSLNormalMappedEmissiveFallBack";



    protected ShaderSpeed _enmShaderSpeed;
    protected LightingStyle _enmShaderStyle;
    protected enum ShaderSpeed
    {
        FAST,
        BEST,
        FALLBACK
    }

    protected enum LightingStyle
    {
        NORMAL,
        EMISSIVE
    }

    public enum BasicBlendModes
    {
        ADDITIVE,
        SUBTRACTIVE,
       // MULTIPLY,
        ALPHA_BLEND
    }


    public override void OnInspectorGUI()
    {
        // if we are not visible... return
        if (!isVisible)
        {
            return;
        }

        //check if bool exists
        if (EditorPrefs.HasKey("NormalShaderHelpHints") == false)
        {
            EditorPrefs.SetBool("NormalShaderHelpHints", true);
        }

        //check if shader is target shader
        if(ChackIfTargetMaterial() == false)
        {
            return;
        }

        //draw shader speed options
        DrawShaderSpeedOptions();

        //draw shader style options 
        DrawShaderStyleOptions();

        //apply shader selection
        ApplyShaderSelection();

        //draw blend mode options
        DrawBlendOptions();

        DrawOrthographicOptions();

        //draw normal map texture
        DrawSelectedTexture();

        //draw uv options
        // DrawNonSquareUVOptions();

        DrawFrameBlendingOptions();

        //add emissive shader options
        EmissiveShaderOptions();

        //draw colour clamping options
        DrawClampOptions();

        //draw pixel light options 
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Light Count Options");
        DrawPixelLightCountOptions();

        //draw vertex light options 
        DrawVertexLightCountOptions();

        //draw light calculation options
        DrawShadowCalculationOptions();

        //draw light scattering options 
        DrawBackScatterOptions();

        //draw spotlight support options 
        DrawSpotlightOptions();

        //draw soft particle options
        DrawSoftParticleOptions();

        //draw distance fade options
        DrawDistanceFadeOptions();

        //ask for reviews and or give help
        DrawReviewPrompt();

        //draw link to documentation
        DrawDocLink();
    }

    public bool ChackIfTargetMaterial()
    {
        if((target is  Material ) == false)
        {
            return false;
        }

        Material matTarget = target as Material;



        if (matTarget.shader.name == _strNormalMappedShader)
        {
            return true;
        }

        if (matTarget.shader.name == _strOptimisedNormalMappedShader)
        {
            return true;
        }
        if (matTarget.shader.name == _strEmissiveNormalMappedShader)
        {
            return true;
        }
        if (matTarget.shader.name == _strOptimisedEmissiveNormalMappedShader)
        {
            return true;
        }
        if (matTarget.shader.name == _strFallBackNormalMappedShader)
        {
            return true;
        }
        if (matTarget.shader.name == _strFallBackEmissiveShader)
        {
            return true;
        }

        return false;
    }


  // public void DrawNonSquareUVOptions()
  // {
  //     MaterialProperty UsingNonSquareUV = GetMaterialProperty(targets, "_UsingNonSquareUVs");
  //     MaterialProperty Columns = GetMaterialProperty(targets, "_UVColumns");
  //     MaterialProperty Rows = GetMaterialProperty(targets, "_UVRows");
  //     MaterialProperty NonSquareUVFix = GetMaterialProperty(targets, "_NonSquareUVFix");
  //
  //     if (UsingNonSquareUV == null || Columns == null || Rows == null || NonSquareUVFix == null )
  //     {
  //         return;
  //     }
  //
  //
  //     ShaderProperty(UsingNonSquareUV, "Using Non Square UVs");
  //
  //     if (UsingNonSquareUV.floatValue == 0)
  //     {
  //         NonSquareUVFix.vectorValue = Vector4.one;
  //     }
  //     else
  //     {
  //         Columns.floatValue = (float) EditorGUILayout.IntField("Columns in sprite sheet", (int)Columns.floatValue);
  //         Rows.floatValue = (float)EditorGUILayout.IntField("Rows in sprite sheet", (int)Rows.floatValue);
  //
  //         Vector4 vecRescale = new Vector4(Rows.floatValue, Columns.floatValue, 1, 1);
  //
  //         NonSquareUVFix.vectorValue = vecRescale;
  //     }
  //
  // }

    public void DrawBlendOptions()
    {
        MaterialProperty AdvancedBlendMode = GetMaterialProperty(targets, "_AdvancedBlendMode"); 
        MaterialProperty SelectedBasicBlendMode = GetMaterialProperty(targets, "_BasicMode");
        MaterialProperty BlendOP = GetMaterialProperty(targets, "_BlendOP");
        MaterialProperty SourceBlendOption = GetMaterialProperty(targets, "_SrcMode");
        MaterialProperty DestBlendOption = GetMaterialProperty(targets, "_DstMode");

        if (SourceBlendOption == null || DestBlendOption == null || AdvancedBlendMode == null || SelectedBasicBlendMode == null || BlendOP == null)
        {
            return;
        }

        //spotlight options 
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Blend Mode Options");

        ShaderProperty(AdvancedBlendMode, "Advanced Options");

        if (AdvancedBlendMode.floatValue == 0)
        {
            BasicBlendModes bbmBasicBlendMode = (BasicBlendModes)SelectedBasicBlendMode.floatValue;

            bbmBasicBlendMode = (BasicBlendModes)EditorGUILayout.EnumPopup("Blend Mode", bbmBasicBlendMode);

            SelectedBasicBlendMode.floatValue = (float)bbmBasicBlendMode;

            switch (bbmBasicBlendMode)
            {
                case BasicBlendModes.ADDITIVE:
                    BlendOP.floatValue = (float)UnityEngine.Rendering.BlendOp.Add;
                    SourceBlendOption.floatValue = (float)UnityEngine.Rendering.BlendMode.SrcAlpha;
                    DestBlendOption.floatValue = (float)UnityEngine.Rendering.BlendMode.One;
                    break;

                case BasicBlendModes.ALPHA_BLEND:
                    BlendOP.floatValue = (float)UnityEngine.Rendering.BlendOp.Add;
                    SourceBlendOption.floatValue = (float)UnityEngine.Rendering.BlendMode.SrcAlpha;
                    DestBlendOption.floatValue = (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                    break;

            //  case BasicBlendModes.MULTIPLY:
            //      BlendOP.floatValue = (float)UnityEngine.Rendering.BlendOp.Add;
            //      SourceBlendOption.floatValue = (float)UnityEngine.Rendering.BlendMode.DstColor;
            //      DestBlendOption.floatValue = (float)UnityEngine.Rendering.BlendMode.Zero;
            //      break;

                case BasicBlendModes.SUBTRACTIVE:
                    BlendOP.floatValue = (float)UnityEngine.Rendering.BlendOp.ReverseSubtract;
                    SourceBlendOption.floatValue = (float)UnityEngine.Rendering.BlendMode.SrcAlpha;
                    DestBlendOption.floatValue = (float)UnityEngine.Rendering.BlendMode.One;
                    break;
            }

        }
        else
        {

            //get current selection
            int iBlendOP = (int)BlendOP.floatValue;
            int iSourceSelection = (int)SourceBlendOption.floatValue;
            int iDestSelection = (int)DestBlendOption.floatValue;

            //draw material property
            UnityEngine.Rendering.BlendOp bloBlendOP = (UnityEngine.Rendering.BlendOp)iBlendOP;
            UnityEngine.Rendering.BlendMode blmSourceBlendMode = (UnityEngine.Rendering.BlendMode)iSourceSelection;
            UnityEngine.Rendering.BlendMode blmDestBlendMode = (UnityEngine.Rendering.BlendMode)iDestSelection;

            bloBlendOP = (UnityEngine.Rendering.BlendOp)EditorGUILayout.EnumPopup("Blend Operation", bloBlendOP);
            blmSourceBlendMode = (UnityEngine.Rendering.BlendMode)EditorGUILayout.EnumPopup("Source Blend Mode", blmSourceBlendMode);
            blmDestBlendMode = (UnityEngine.Rendering.BlendMode)EditorGUILayout.EnumPopup("Destination Blend Mode", blmDestBlendMode);

            BlendOP.floatValue = (float)bloBlendOP;
            SourceBlendOption.floatValue = (float)blmSourceBlendMode;
            DestBlendOption.floatValue = (float)blmDestBlendMode;
        }

    }

    public void DrawClampOptions()
    {

        MaterialProperty MinColourOutputs = GetMaterialProperty(targets, "_ParticleMinBrightnesCap");
        MaterialProperty MaxColourOutputs = GetMaterialProperty(targets, "_ParticleMaxBrightnesCap");

        if(MinColourOutputs == null || MaxColourOutputs == null)
        {
            return;
        }

        //spotlight options 
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Colour Clamping Options");

        //draw material property
        Color colMaxColour = MaxColourOutputs.colorValue;
        Color colMinColour = MinColourOutputs.colorValue;

        colMaxColour.a = 1;
        colMinColour.a = 0;

        colMaxColour = EditorGUILayout.ColorField(new GUIContent("Max Pixel Brightness","this is used to clamp how bright a pixel gets") , colMaxColour,true,false,true,new ColorPickerHDRConfig(0,100,0,100));
        colMinColour = EditorGUILayout.ColorField(new GUIContent("Min Pixel Brightness", "this is used to clamp how dark a pixel gets"), colMinColour, true, false, true, new ColorPickerHDRConfig(0, 100, 0, 100));

        MinColourOutputs.colorValue = colMinColour;
        MaxColourOutputs.colorValue = colMaxColour;
    }

    public void DrawReviewPrompt()
    {
        EditorGUILayout.Space();
        //bool bCurrentWordWrap = EditorStyles.label.wordWrap;
        // EditorStyles.label.wordWrap = true;

        EditorGUILayout.LabelField("Has this asset been useful, easy to use");


        EditorGUILayout.LabelField("and met your expectations?");

        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button("Yes") )
        {
            //RequestReviewWindow.CreateWindow(RequestReviewWindow.WindowType.REQUEST_REVIEW);
        }

        if(GUILayout.Button("No"))
        {
            //RequestReviewWindow.CreateWindow(RequestReviewWindow.WindowType.DIRECT_TO_HELP);
        }
        EditorGUILayout.EndHorizontal();

       // EditorStyles.label.wordWrap = bCurrentWordWrap;

    } 

    public void DrawDocLink()
    {
        if (GUILayout.Button("User Documentation"))
        {
            Application.OpenURL("https://docs.google.com/document/d/169qxcKAVB2PskHsQf8Y8MQ2PBKUGoi6Ho9OdO0xZDrg/edit?usp=sharing"); 
        }
    }

    public void DrawDistanceFadeOptions()
    {
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Distance fade options");

         MaterialProperty mprDistanceFadeStart = GetMaterialProperty(targets, "_DistanceFadeStart");
         MaterialProperty mprDistanceFadeEnd = GetMaterialProperty(targets, "_DistanceFadeEnd");
         MaterialProperty mprDistanceFadeRate = GetMaterialProperty(targets, "_DistanceFadeRate");

         ShaderProperty(mprDistanceFadeStart, "Fade Out Distance");

         ShaderProperty(mprDistanceFadeEnd, "Fade Start Distance");

         if ((mprDistanceFadeStart.floatValue - mprDistanceFadeEnd.floatValue) != 0)
         {
             mprDistanceFadeRate.floatValue = 1 / (mprDistanceFadeEnd.floatValue - mprDistanceFadeStart.floatValue);
         }
         else
         {
             mprDistanceFadeRate.floatValue = float.MaxValue;
         }

         EditorGUILayout.Space();
        
    }

    public void DrawSelectedTexture()
    {
        if(_enmShaderStyle == LightingStyle.EMISSIVE)
        {
            MaterialProperty mprTexture = GetMaterialProperty(targets, "_MainTex");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Normal + Emissive + Alpha Texture");
            ShaderProperty(mprTexture,"NNEA Tex");
        }

        if(_enmShaderStyle == LightingStyle.NORMAL)
        {
            MaterialProperty mprTexture = GetMaterialProperty(targets, "_MainTex");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Normal + Alpha Texture");
            ShaderProperty(mprTexture, "NNNA Tex");
        }
    }

    public void DrawShaderSpeedOptions()
    {
        //get current version of the shader
       string strCurrentShader = (target as Material).shader.name;

        //work out if it is high quality  low quality or fallback
        if(strCurrentShader == _strNormalMappedShader || strCurrentShader == _strEmissiveNormalMappedShader )
        {
            _enmShaderSpeed = ShaderSpeed.BEST;
        }
        else if(strCurrentShader == _strOptimisedNormalMappedShader || strCurrentShader == _strOptimisedEmissiveNormalMappedShader)
        {
            _enmShaderSpeed = ShaderSpeed.FAST;
        }
        else
        {
            _enmShaderSpeed = ShaderSpeed.FALLBACK;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shader Quality Options");
        _enmShaderSpeed = (ShaderSpeed)EditorGUILayout.EnumPopup(_enmShaderSpeed);
    }

    public void DrawShaderStyleOptions()
    {
        //get current version of the shader
        string strCurrentShader = (target as Material).shader.name;

        //work out if it is high quality or low quality
        if (strCurrentShader == _strEmissiveNormalMappedShader || strCurrentShader == _strOptimisedEmissiveNormalMappedShader || strCurrentShader == _strFallBackEmissiveShader)
        {
            _enmShaderStyle =  LightingStyle.EMISSIVE;
        }
        else
        {
            _enmShaderStyle = LightingStyle.NORMAL;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shader Style");
        _enmShaderStyle = (LightingStyle)EditorGUILayout.EnumPopup(_enmShaderStyle);
    }

    public void ApplyShaderSelection()
    {
        //get target material
        Material matTarget = target as Material;

        if(matTarget == null)
        {
            return;
        }

        if(_enmShaderSpeed == ShaderSpeed.BEST && _enmShaderStyle == LightingStyle.NORMAL)
        {
            matTarget.shader = Shader.Find(_strNormalMappedShader);
        }

        if (_enmShaderSpeed == ShaderSpeed.BEST && _enmShaderStyle == LightingStyle.EMISSIVE)
        {
            matTarget.shader = Shader.Find(_strEmissiveNormalMappedShader);
        }

        if (_enmShaderSpeed == ShaderSpeed.FAST && _enmShaderStyle == LightingStyle.NORMAL)
        {
            matTarget.shader = Shader.Find(_strOptimisedNormalMappedShader);
        }

        if (_enmShaderSpeed == ShaderSpeed.FAST && _enmShaderStyle == LightingStyle.EMISSIVE)
        {
            matTarget.shader = Shader.Find(_strOptimisedEmissiveNormalMappedShader);
        }

        if (_enmShaderSpeed == ShaderSpeed.FALLBACK && _enmShaderStyle == LightingStyle.NORMAL)
        {
            matTarget.shader = Shader.Find(_strFallBackNormalMappedShader);
        }

        if (_enmShaderSpeed == ShaderSpeed.FALLBACK && _enmShaderStyle == LightingStyle.EMISSIVE)
        {
            matTarget.shader = Shader.Find(_strFallBackEmissiveShader);
        }


        EditorUtility.SetDirty(target);

    }

    public void EmissiveShaderOptions()
    {
        if (_enmShaderStyle == LightingStyle.EMISSIVE)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Emissive Shading Options");

            MaterialProperty mprParticleColourTint = GetMaterialProperty(targets, "_ParticleTint");

            ShaderProperty(mprParticleColourTint, "Particle Colour Tint");

            MaterialProperty mprEmissiveShaderOptions = GetMaterialProperty(targets, "_EmissiveMultiplyer");

            ShaderProperty(mprEmissiveShaderOptions, "Self Lighting Multiplyer");
        }
    }

    public void DrawShadowCalculationOptions()
    {
        //check if the user wants to use different settings for the vertex lighting calculation
        MaterialProperty mprUseDifferentSettingsForVertexLights = GetMaterialProperty(targets, "_VertexLightOptionsToggle");

        //material propertys for the pixel lights
        MaterialProperty mprPixelLightWrapAround = GetMaterialProperty(targets, "_LightWrapAround");
        MaterialProperty mprPixelShadowMultiplyer = GetMaterialProperty(targets, "_ShadowEffectMultiplyer");

        MaterialProperty mprLightSharpness = GetMaterialProperty(targets, "_LightPower");

        //get material propertys for verex lights
        MaterialProperty mprVertexLightWrapAround = GetMaterialProperty(targets, "_VertexLightWrapAround");
        MaterialProperty mprVertexShadowMultiplyer = GetMaterialProperty(targets, "_VertexShadowEffectMultiplyer");
        MaterialProperty mprVertexLightBooster = GetMaterialProperty(targets, "_VertexLightBoost");

        //render all the pixel light options 
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pixel Light Options");

        ShaderProperty(mprPixelLightWrapAround, "Light Wrap Around");
        ShaderProperty(mprPixelShadowMultiplyer, "Shadow Multiplyer");
        ShaderProperty(mprLightSharpness, "Light Sharpness");


        //render the vertex settings toggle
        EditorGUILayout.Space();
        ShaderProperty(mprUseDifferentSettingsForVertexLights, "Diferent Vertex Light Settings");

        if(mprUseDifferentSettingsForVertexLights.floatValue == 1 || mprUseDifferentSettingsForVertexLights.hasMixedValue)
        {
            //render vertex settngs
            EditorGUILayout.LabelField("Vertex Light Options");
            ShaderProperty(mprVertexLightWrapAround, "Light Wrap Around");
            ShaderProperty(mprVertexShadowMultiplyer, "Shadow Multiplyer");
            ShaderProperty(mprVertexLightBooster, "Vertex Light Multiplyer");

        }
        else
        {
            //apply pixel settings to vertex
            mprVertexLightWrapAround.floatValue = mprPixelLightWrapAround.floatValue;
            mprVertexShadowMultiplyer.floatValue = mprPixelShadowMultiplyer.floatValue;
            mprVertexLightBooster.floatValue = 1;

        }

    }

    public void DrawSpotlightOptions()
    {
        //get spotlight options
        MaterialProperty mprSpotlightToggle = GetMaterialProperty(targets, "_SpotlightToggle");

        //spotlight options 
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spotlight Options");

        //draw material property
        ShaderProperty(mprSpotlightToggle, "Spotlights");
    }

    public void DrawOrthographicOptions()
    {
        //get spotlight options
        MaterialProperty mprSpotlightToggle = GetMaterialProperty(targets, "_OrthographicToggle");

        if(mprSpotlightToggle == null )
        {
            return;
        }

        //spotlight options 
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Orthographic Camera Projection");

        //draw material property
        ShaderProperty(mprSpotlightToggle, "orthographic camera");
    }

    public void DrawFrameBlendingOptions()
    {
        //get spotlight options
        MaterialProperty mprSpotlightToggle = GetMaterialProperty(targets, "_FrameBlendingToggle");

        //spotlight options 
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Frame Blending");

        //draw material property
        ShaderProperty(mprSpotlightToggle, "Blend sprite sheet frames");
    }

    public void DrawBackScatterOptions()
    {
        //get material property
        MaterialProperty mprLightScatterEnable = GetMaterialProperty(targets, "_LightScatteringToggle");

        if(mprLightScatterEnable == null)
        {
            return;
        }

        //draw material property
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Light Scatter Options");
        ShaderProperty(mprLightScatterEnable, "Light Scattering");

        //check if light scattering is enabled
        if(mprLightScatterEnable.floatValue == 1 || mprLightScatterEnable.hasMixedValue)
        {
            //draw all the light scattering options 
            MaterialProperty mprLightScatterSharpness = GetMaterialProperty(targets, "_LightScatterSharpness");
            //draw material property
            ShaderProperty(mprLightScatterSharpness, "Scatter Sharpness");


            MaterialProperty mprTransparencyScatterBrightness = GetMaterialProperty(targets, "_AlphaScatterEffect");
            //draw material property
            ShaderProperty(mprTransparencyScatterBrightness, "Transparency Blocking Effect");


            MaterialProperty mprTransparencyEffectOnSharpness = GetMaterialProperty(targets, "_AlphaScatterSharpness");
            //draw material property
            ShaderProperty(mprTransparencyEffectOnSharpness, "Transparency Scatter Sharpness");


            MaterialProperty mprLightScatterMultiplyer = GetMaterialProperty(targets, "_ScatterLightMultiplyer");
            //draw material property
            ShaderProperty(mprLightScatterMultiplyer, "Light Scatter Multiplyer");

        }

    }

    public void DrawPixelLightCountOptions()
    {
        //get the metadata for the current target materials
        
        MaterialProperty LightCountProperty = GetMaterialProperty(targets, "_LightCount");
        MaterialProperty FragLightCountProperty = GetMaterialProperty(targets, "_LightCountFrag");

        //list of all the options
        List<string> lstLightOptions;

        int iMaxNumberOfPixelLightsSupported = MaxPixelLightLimit(targets);

        if (iMaxNumberOfPixelLightsSupported < 8)
        {
            lstLightOptions = new List<string>(_strOptimizedLightCountOptions);
        }
        else
        {
            lstLightOptions = new List<string>(_strUnOptimizedLightCountOptions);
        }


        int iLightOptionsCurrentValue = 0;
        //if values are mixed add mixed option
        if (LightCountProperty.hasMixedValue)
        {
            lstLightOptions.Add("Mixed Values");
            iLightOptionsCurrentValue = lstLightOptions.Count - 1;
        }
        else
        {
            iLightOptionsCurrentValue = Mathf.Clamp((int)LightCountProperty.floatValue - 1, 0, lstLightOptions.Count - 1);
        }

        //get light options
        iLightOptionsCurrentValue = EditorGUILayout.Popup("Max Pixel lights", iLightOptionsCurrentValue, lstLightOptions.ToArray());

        //set selected value
        if (iLightOptionsCurrentValue < iMaxNumberOfPixelLightsSupported && ((LightCountProperty.hasMixedValue && iLightOptionsCurrentValue == lstLightOptions.Count - 1) == false))
        {
            LightCountProperty.floatValue = iLightOptionsCurrentValue + 1;
            FragLightCountProperty.floatValue = iLightOptionsCurrentValue + 1;
            SetMaterialPixelLightCount(iLightOptionsCurrentValue + 1, targets);

            for (int i = 0; i < targets.Length; i++)
            {
                EditorUtility.SetDirty(targets[i]);
            }
        }
    }

    public void DrawVertexLightCountOptions()
    {
        //get vertex and pixel light settings

        MaterialProperty LightCountProperty = GetMaterialProperty(targets, "_LightCount");
        MaterialProperty VertexLightCountProperty = GetMaterialProperty(targets, "_VertexLightCount");

        //get the number of vertex lights
        int iMaxVertexLights =  8 - (int)LightCountProperty.floatValue;

        //create options list for number of vertex lights 
        List<string> strOptions = new List<string>();

        //build options list
        for(int i = 0; i <= iMaxVertexLights; i++)
        {
            strOptions.Add(_strVertexLightCountOptions[i]);

        }

        //get the selected value
        int iSelectedVertexLightCount = Mathf.Clamp((int)VertexLightCountProperty.floatValue - (int)LightCountProperty.floatValue, 0, 8);

        if(VertexLightCountProperty.hasMixedValue)
        {
            strOptions.Add("Mixed Values");

            iSelectedVertexLightCount = strOptions.Count - 1;
        }

        //draw option popup
        iSelectedVertexLightCount = EditorGUILayout.Popup("Max vetex lights", iSelectedVertexLightCount, strOptions.ToArray());

        if ((VertexLightCountProperty.hasMixedValue && iSelectedVertexLightCount != (strOptions.Count - 1)) || !VertexLightCountProperty.hasMixedValue)
        {
            //set light value
            VertexLightCountProperty.floatValue = Mathf.Clamp( iSelectedVertexLightCount + (int)LightCountProperty.floatValue , 0,8);
        }
    }

    public void DrawSoftParticleOptions()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Soft Particle Options");
        MaterialProperty mprParticleSoftFactor = GetMaterialProperty(targets, "_InvFade");
        ShaderProperty(mprParticleSoftFactor, "Thickness");
    }

    public void SetMaterialPixelLightCount(int iTargetLightCount, Object[] Targets)
    {
        bool Option1 = false;
        bool Option2 = false;

        switch(Mathf.Clamp( iTargetLightCount,1,4))
        {
            //set which options need to be activated
            case 1:
                break;
            case 2:
                Option1 = true;
                break;
            case 3:
                Option2 = true;
                break;
            case 4:
                Option1 = true;
                Option2 = true;
                break;
        }

        //set the key words
        RunTimeChangeMaterialKeyWord(Targets, "PIXEL_LIGHT_OPTION_BIT_1", Option1);
        RunTimeChangeMaterialKeyWord(Targets, "PIXEL_LIGHT_OPTION_BIT_2", Option2);

    }

    public int MaxPixelLightLimit(Object[] targets)
    {
        int iMaxLightNumber = 8;

        foreach( Object objObject in targets)
        {
            if(objObject is Material)
            {
                //get material
                Material matTargetMaterial = objObject as Material;

                //get shader
                Shader shdMaterialShader = matTargetMaterial.shader;

                //check if shader is optimized 
                if (shdMaterialShader.name == "Encap/Mobile/GLSLNormalMappedEmissiveFast" || shdMaterialShader.name == "Encap/Mobile/GLSLNormalMappedFast")
                {
                    //reduce the max number of lights
                    iMaxLightNumber = Mathf.Clamp(iMaxLightNumber, 1, 4);
                }
            }
        }

        return iMaxLightNumber;
    }
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void RunTimeChangeMaterialKeyWord(Object[] Targets, string strTargetKeyWord, bool bEnable)
    {
        for(int i = 0; i < Targets.Length; i++)
        {
            if(Targets[i] != null && Targets[i] is Material)
            {
                RunTimeChangeMaterialKeyWord(Targets[i] as Material, strTargetKeyWord, bEnable);
            }
        }
    }
    

    public void RunTimeChangeMaterialKeyWord(Material matTarget, string strTargetKeyWord, bool bEnable)
    {
        List<string> keyWords = new List<string>(matTarget.shaderKeywords);
        for (int i = 0; i < keyWords.Count; i++)
        {
            if(keyWords[i] == strTargetKeyWord)
            {
                if(bEnable == true)
                {
                    //exit early so the add code is not run
                    return;
                }

                if(bEnable == false)
                {
                    //remove the target key word
                    keyWords.RemoveAt(i);
                    matTarget.shaderKeywords = keyWords.ToArray();
                    return;
                }
            }
           
        }

        //add key word in if it is not already set
        if(bEnable == true)
        {
            //add shader key word
            keyWords.Add(strTargetKeyWord);
            matTarget.shaderKeywords = keyWords.ToArray();
        }
        
    }
}
