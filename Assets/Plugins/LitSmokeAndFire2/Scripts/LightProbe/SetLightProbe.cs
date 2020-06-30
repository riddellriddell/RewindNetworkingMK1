using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SetLightProbe : MonoBehaviour
{

    //is this particle system viible to the player
    protected bool _bIsVisible = false;

    //sample light probes or fall back on the global ambient sperical harmonics
    protected bool _bSampleLightProbes = true;

    //a fixed location in the world to sample the harmonnics from
    [CleanInspectorName]
    public Transform _trnOptionalFixedSamplePosition;

    //the frame the ambient light was last updated
    protected static int s_iAmbientlightLastUpdate;

    //the material ids
    protected static int[] s_iParticleAmbientSHA;
    protected static int[] s_iParticleAmbientSHB;
    protected static int s_iParticleAmbientSHC;

    protected static int[] s_iParticleProbeSHA;
    protected static int[] s_iParticleProbeSHB;
    protected static int s_iParticleProbeSHC;

    //have the ids been set up
    protected static bool _bIdsSetUp = false;

    //the lighting harmonics
    protected static SphericalHarmonicsL2 s_sphAmbientLightAtLastUpdate;

    //the material property block of this particle system
    private MaterialPropertyBlock _mpbMaterialPropertyBlock;
    //light probe
    private SphericalHarmonicsL2 _sphProbe;

    public bool _bLogDebugOutput = false;

    //public bool _bForceAmbientUpdateButton;

    public void OnBecameVisible()
    {
       if(_bLogDebugOutput) Debug.Log("Became Visible");
        _bIsVisible = true;
    }

    public void OnBecameInvisible()
    {
        if (_bLogDebugOutput) Debug.Log("Became Invisible");
        _bIsVisible = false;
    }

    public void Awake()
    {
        UpdateProbeSupport();
    }

    public void UpdateProbeSupport()
    {
        //get renderer key words
        string[] strKeyWords = GetComponent<Renderer>().sharedMaterial.shaderKeywords;

        //check if material is set to sample light probes
        _bSampleLightProbes = false;

        foreach (string strKey in strKeyWords)
        {
            if (strKey == "LIGHTPROBE")
            {
                _bSampleLightProbes = true;
                if (_bLogDebugOutput) Debug.Log("Setting Light Probes");
                return;
            }
        }

        if (_bLogDebugOutput) Debug.Log("Setting Ambient lighting only");
    }

    public void LateUpdate()
    {
        //if aplication is not running 
        if (Application.isPlaying == false)
        {
            //update light probe support
            UpdateProbeSupport();
        }

        //check if it can even be seen
        if (_bIsVisible == false && Application.isPlaying == true)
        {
            if (_bLogDebugOutput) Debug.Log("Not visible skipping probe set code");
            return;
        }


        //check if set to ambient only 
        if (_bSampleLightProbes == false)
        {
            //check if already set this frame 
            if(Time.frameCount == s_iAmbientlightLastUpdate)
            {
                //ambient light already updated so dont update more
                return;
            }

            //get light probe that is already set;
            SphericalHarmonicsL2 sphCurrentAmbinetLight = RenderSettings.ambientProbe;

            //--------- for testing -----------
            if (sphCurrentAmbinetLight == new SphericalHarmonicsL2())
            {
                if (_bLogDebugOutput) Debug.Log("No ambinet light found");
            }


            //check if there is a difference and this is not the first frame
            if (s_sphAmbientLightAtLastUpdate != null && sphCurrentAmbinetLight == s_sphAmbientLightAtLastUpdate && s_iAmbientlightLastUpdate != 0)
            {
                if (_bLogDebugOutput) Debug.Log("Ambient light has not changed skipping lighting update");

                //ambient light has not changes to no need to update the material
               return;
            }

           
            //protect against nulls
            if(sphCurrentAmbinetLight == null)
            {
                if (_bLogDebugOutput) Debug.Log("Ambient light is null");
                return;
            }

            //set the global ambient lighting 
            SetGlobalAmbientSHCoefficients(sphCurrentAmbinetLight);

            //update the set ambient lighting 
            if (s_sphAmbientLightAtLastUpdate == null)
            {
                s_sphAmbientLightAtLastUpdate = new SphericalHarmonicsL2();
            }

            //copy across coeficients
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 9; y++)
                {
                    s_sphAmbientLightAtLastUpdate[x,y] = sphCurrentAmbinetLight[x,y];
                }
            }


            //update the time of last update
            s_iAmbientlightLastUpdate = Time.frameCount;

            //logging
            if (_bLogDebugOutput) Debug.Log("Setting Ambient coeficients");

            return;
        }


        // If sampling light probes
        if (_bIsVisible || Application.isPlaying == false)
        {
            //get renderer
            Renderer psrParticleRenderer = GetComponent<Renderer>();

            if(_mpbMaterialPropertyBlock == null)
            {
                _mpbMaterialPropertyBlock = new MaterialPropertyBlock();
            }

            //get material property block
            psrParticleRenderer.GetPropertyBlock(_mpbMaterialPropertyBlock);

            //get sample point
            Vector3 vecCenter;

            if (_trnOptionalFixedSamplePosition != null)
            {
                vecCenter = _trnOptionalFixedSamplePosition.transform.position;
            }
            else
            {
                vecCenter = psrParticleRenderer.bounds.center;
            }

            //sample light probe
            LightProbes.GetInterpolatedProbe(vecCenter, psrParticleRenderer,out _sphProbe);

            //apply light probe
           SetParticleProbeSHCoefficients(_sphProbe, _mpbMaterialPropertyBlock);

            //for testint apply the ambient light probe
           // SetParticleProbeSHCoefficients(RenderSettings.ambientProbe, _mpbMaterialPropertyBlock);

            psrParticleRenderer.SetPropertyBlock(_mpbMaterialPropertyBlock);

            if (_bLogDebugOutput) Debug.Log("Setting light probe coeficients");
        }
    }

    public static void SetGlobalAmbientSHCoefficients(SphericalHarmonicsL2 sphHarmonic)
    {

        //make sure ids are set up
        InitaliseIDs();


        // Constant + Linear
        for (var i = 0; i < 3; i++)
            Shader.SetGlobalVector(s_iParticleAmbientSHA[i], new Vector4(
                sphHarmonic[i, 3], sphHarmonic[i, 1], sphHarmonic[i, 2], sphHarmonic[i, 0] - sphHarmonic[i, 6]
            ));

        // Quadratic polynomials
        for (var i = 0; i < 3; i++)
            Shader.SetGlobalVector(s_iParticleAmbientSHB[i], new Vector4(
                sphHarmonic[i, 4], sphHarmonic[i, 6], sphHarmonic[i, 5] * 3, sphHarmonic[i, 7]
            ));

        // Final quadratic polynomial
        Shader.SetGlobalVector(s_iParticleAmbientSHC, new Vector4(
            sphHarmonic[0, 8], sphHarmonic[2, 8], sphHarmonic[1, 8], 1
        ));
    }

    //public static void SetLocalAmbientSHCoefficients(SphericalHarmonicsL2 sphHarmonic, Material matMaterial)
    //{
    //
    //    //make sure ids are set up
    //    InitaliseIDs();
    //
    //
    //    // Constant + Linear
    //    for (var i = 0; i < 3; i++)
    //        matMaterial.SetVector(s_iParticleAmbientSHA[i], new Vector4(
    //            sphHarmonic[i, 3], sphHarmonic[i, 1], sphHarmonic[i, 2], sphHarmonic[i, 0] - sphHarmonic[i, 6]
    //        ));
    //
    //    // Quadratic polynomials
    //    for (var i = 0; i < 3; i++)
    //        matMaterial.SetVector(s_iParticleAmbientSHB[i], new Vector4(
    //            sphHarmonic[i, 4], sphHarmonic[i, 6], sphHarmonic[i, 5] * 3, sphHarmonic[i, 7]
    //        ));
    //
    //    // Final quadratic polynomial
    //    matMaterial.SetVector(s_iParticleAmbientSHC, new Vector4(
    //        sphHarmonic[0, 8], sphHarmonic[2, 8], sphHarmonic[1, 8], 1
    //    ));
    //
    //}
    //

    public static void SetParticleProbeSHCoefficients(SphericalHarmonicsL2 sphHarmonic, MaterialPropertyBlock mpbMaterialPropertyBlock)
    {

        //make sure ids are set up
        InitaliseIDs();

        // Constant + Linear
        for (var i = 0; i < 3; i++)
        {
            mpbMaterialPropertyBlock.SetVector(s_iParticleProbeSHA[i], new Vector4(
                sphHarmonic[i, 3], sphHarmonic[i, 1], sphHarmonic[i, 2], sphHarmonic[i, 0] - sphHarmonic[i, 6]
            ));
        }

        // Quadratic polynomials
        for (var i = 0; i < 3; i++)
        {
            mpbMaterialPropertyBlock.SetVector(s_iParticleProbeSHB[i], new Vector4(
                sphHarmonic[i, 4], sphHarmonic[i, 6], sphHarmonic[i, 5] * 3, sphHarmonic[i, 7]
            ));
        }

        // Final quadratic polynomial
        mpbMaterialPropertyBlock.SetVector(s_iParticleProbeSHC, new Vector4(
            sphHarmonic[0, 8], sphHarmonic[2, 8], sphHarmonic[1, 8], 1
        ));

    }



    static void InitaliseIDs()
    {
        //check if ids have been setup
        if(_bIdsSetUp)
        {
            return;
        }

        s_iParticleAmbientSHA = new int[]
        {
            Shader.PropertyToID("Particle_Ambient_SHAr"),
            Shader.PropertyToID("Particle_Ambient_SHAg"),
            Shader.PropertyToID("Particle_Ambient_SHAb")
        };

        s_iParticleAmbientSHB = new int[] 
        {
            Shader.PropertyToID("Particle_Ambient_SHBr"),
            Shader.PropertyToID("Particle_Ambient_SHBg"),
            Shader.PropertyToID("Particle_Ambient_SHBb")
        };

        s_iParticleAmbientSHC = 
            Shader.PropertyToID("Particle_Ambient_SHC");


        s_iParticleProbeSHA = new int[] 
        {
            Shader.PropertyToID("Particle_Probe_SHAr"),
            Shader.PropertyToID("Particle_Probe_SHAg"),
            Shader.PropertyToID("Particle_Probe_SHAb")
        };

       s_iParticleProbeSHB = new int[] 
       {
            Shader.PropertyToID("Particle_Probe_SHBr"),
            Shader.PropertyToID("Particle_Probe_SHBg"),
            Shader.PropertyToID("Particle_Probe_SHBb")
        };

        s_iParticleProbeSHC =
            Shader.PropertyToID("Particle_Probe_SHC");
    }
    
}
