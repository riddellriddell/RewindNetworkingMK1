using UnityEngine;
using UnityEngine.Rendering;

public static class LightProbeUtility
{
    // Set SH coefficients to MaterialPropertyBlock
    public static void SetSHCoefficients(Vector3 position, MaterialPropertyBlock properties)
    {
        SphericalHarmonicsL2 sh;
        LightProbes.GetInterpolatedProbe(position, null, out sh);

        // Constant + Linear
        for (var i = 0; i < 3; i++)
            properties.SetVector(_idSHA[i], new Vector4(
                sh[i, 3], sh[i, 1], sh[i, 2], sh[i, 0] - sh[i, 6] 
            ));

        // Quadratic polynomials
        for (var i = 0; i < 3; i++)
            properties.SetVector(_idSHB[i], new Vector4(
                sh[i, 4], sh[i, 6], sh[i, 5] * 3, sh[i, 7]
            ));

        // Final quadratic polynomial
        properties.SetVector(_idSHC, new Vector4(
            sh[0, 8], sh[2, 8], sh[1, 8], 1
        ));
    }

    // Set SH coefficients to Material
    public static void SetSHCoefficients(Vector3 position, Material material)
    {
        SphericalHarmonicsL2 sh;
        LightProbes.GetInterpolatedProbe(position, null, out sh);

        // Constant + Linear
        for (var i = 0; i < 3; i++)
            material.SetVector(_idSHA[i], new Vector4(
                sh[i, 3], sh[i, 1], sh[i, 2], sh[i, 0] - sh[i, 6] 
            ));

        // Quadratic polynomials
        for (var i = 0; i < 3; i++)
            material.SetVector(_idSHB[i], new Vector4(
                sh[i, 4], sh[i, 6], sh[i, 5] * 3, sh[i, 7]
            ));

        // Final quadratic polynomial
        material.SetVector(_idSHC, new Vector4(
            sh[0, 8], sh[2, 8], sh[1, 8], 1
        ));
    }


    // Set SH coefficients to Material
    public static void SetParticleSHCoefficients(Vector3 position, Material material)
    {
        SphericalHarmonicsL2 sh;
        LightProbes.GetInterpolatedProbe(position, null, out sh);

        // Constant + Linear
        for (var i = 0; i < 3; i++)
            material.SetVector(Particle_idSHA[i], new Vector4(
                sh[i, 3], sh[i, 1], sh[i, 2], sh[i, 0] - sh[i, 6]
            ));

        // Quadratic polynomials
        for (var i = 0; i < 3; i++)
            material.SetVector(Particle_idSHB[i], new Vector4(
                sh[i, 4], sh[i, 6], sh[i, 5] * 3, sh[i, 7]
            ));

        // Final quadratic polynomial
        material.SetVector(Particle_idSHC, new Vector4(
            sh[0, 8], sh[2, 8], sh[1, 8], 1
        ));
    }

    // Set SH coefficients to Material
    public static void SetParticleSHCoefficientsWithAdustments(Vector3 position, Material material)
    {
        SphericalHarmonicsL2 sh;
        LightProbes.GetInterpolatedProbe(position, null, out sh);

        sh = RenderSettings.ambientProbe;

        float s_fSqrtPI = ((float)Mathf.Sqrt(Mathf.PI));
        float fC0 = 1.0f / (2.0f * s_fSqrtPI);
        float fC1 = (float)Mathf.Sqrt(3.0f) / (3.0f * s_fSqrtPI);
        float fC2 = (float)Mathf.Sqrt(15.0f) / (8.0f * s_fSqrtPI);
        float fC3 = (float)Mathf.Sqrt(5.0f) / (16.0f * s_fSqrtPI);
        float fC4 = 0.5f * fC2;

        // Constant + Linear
        for (var i = 0; i < 3; i++)
            material.SetVector(Particle_idSHA[i], new Vector4(
                -fC1 * sh[i, 3],
                -fC1 * sh[i, 1],
                fC1 * sh[i, 2],
                fC0 * sh[i, 0] - sh[i, 6]
            ));

        // Quadratic polynomials
        for (var i = 0; i < 3; i++)
            material.SetVector(Particle_idSHB[i], new Vector4(
                fC2 * sh[i, 4],
               -fC2 * sh[i, 6],
                3.0f * fC3 * sh[i, 5] * 3,
                -fC2 * sh[i, 7]
            ));

        // Final quadratic polynomial
        material.SetVector(Particle_idSHC, new Vector4(
             fC4 * sh[0, 8],
             fC4 * sh[2, 8],
             fC4 * sh[1, 8], 
             1

        ));
    }


    public static void SetParticleSHCoefficientsStraitPass(Vector3 position, Material material)
    {
        SphericalHarmonicsL2 sh;
        LightProbes.GetInterpolatedProbe(position, null, out sh);

        // Constant + Linear
        for (var i = 0; i < 3; i++)
            material.SetVector(Particle_idSHA[i], new Vector4(sh[i, 0], sh[i, 1], sh[i, 2], sh[i, 3]));

        // Quadratic polynomials
        for (var i = 0; i < 3; i++)
            material.SetVector(Particle_idSHB[i], new Vector4(sh[i, 4], sh[i, 5], sh[i,6] , sh[i, 7]));

        // Final quadratic polynomial
        material.SetVector(Particle_idSHC, new Vector4( sh[0, 8], sh[1, 8], sh[2, 8], 1 ));
    }

    static int[] _idSHA = {
        Shader.PropertyToID("unity_SHAr"),
        Shader.PropertyToID("unity_SHAg"),
        Shader.PropertyToID("unity_SHAb")
    };

    static int[] _idSHB = {
        Shader.PropertyToID("unity_SHBr"),
        Shader.PropertyToID("unity_SHBg"),
        Shader.PropertyToID("unity_SHBb")
    };

    static int _idSHC =
        Shader.PropertyToID("unity_SHC");


    static int[] Particle_idSHA = {
        Shader.PropertyToID("Particle_SHAr"),
        Shader.PropertyToID("Particle_SHAg"),
        Shader.PropertyToID("Particle_SHAb")
    };

    static int[] Particle_idSHB = {
        Shader.PropertyToID("Particle_SHBr"),
        Shader.PropertyToID("Particle_SHBg"),
        Shader.PropertyToID("Particle_SHBb")
    };

    static int Particle_idSHC =
        Shader.PropertyToID("Particle_SHC");
}
