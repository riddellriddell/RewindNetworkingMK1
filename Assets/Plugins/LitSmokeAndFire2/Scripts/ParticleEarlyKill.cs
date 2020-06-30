using UnityEngine;
using System.Collections;

public class ParticleEarlyKill : MonoBehaviour
{
    //get a list of all the particles
    ParticleSystem.Particle[] _parParticleList;

    //how long the particle will live for 
    public AnimationCurve _amcParticleLifeTime;

    [CleanInspectorName]
    public bool _bUseVariableFadeOut;

    [CleanInspectorName("", "_bUseVariableFadeOut")]
    public AnimationCurve _amcParticleFadeOutTime;

    

    //how a particle fades out
    public float _fParticleFadeOutRate;

    //the particle emitter
    protected ParticleSystem _parTargetParticleSystem;
    protected ParticleSystem TargetParticleSystem
    {
        get
        {
            if(_parTargetParticleSystem == null)
            {
                _parTargetParticleSystem = GetComponent<ParticleSystem>();
            }

            return _parTargetParticleSystem;
        }
    }

    // Update is called once per frame
    void Update ()
    {
       

        //get a list of all the particles
        if (_parParticleList == null || _parParticleList.Length != TargetParticleSystem.maxParticles)
        {
            _parParticleList = new ParticleSystem.Particle[TargetParticleSystem.maxParticles];
        }

        int iParticleCount = TargetParticleSystem.GetParticles(_parParticleList);

        //loop through all the particles
        for (int i = 0; i < iParticleCount; i++)
        {
            //convert there seed to a 0-1 range
            float fParticleSeed = (float)_parParticleList[i].randomSeed / (float)uint.MaxValue;

            //look up there target life time
            float fTargetLifeTime = ((1 - _amcParticleLifeTime.Evaluate(fParticleSeed)) * _parParticleList[i].startLifetime);

            //check if particle needs to be killed early
            if (fTargetLifeTime > _parParticleList[i].remainingLifetime)
            {

                // Debug.Log("killing Particle");

                //calc max alpha
                float fMaxAlpha = 1;
                if (_bUseVariableFadeOut)
                {
                    fMaxAlpha -= ((1 / (_fParticleFadeOutRate * _amcParticleFadeOutTime.Evaluate(fParticleSeed))) * (fTargetLifeTime - _parParticleList[i].remainingLifetime));
                }
                else
                {
                   
                   fMaxAlpha -= ((1 / _fParticleFadeOutRate) * (fTargetLifeTime - _parParticleList[i].remainingLifetime));
                }


                //check if particle alpha is 0
                if (fMaxAlpha < 0)
                {
                    //kill particle
                    _parParticleList[i].remainingLifetime = -1;
                    
                }

                Color colStartColour = _parParticleList[i].startColor;
                Color colFadeColour = new Color(colStartColour.r, colStartColour.g, colStartColour.b, Mathf.Clamp(colStartColour.a, -1, fMaxAlpha));

                // Debug.Log("Start " + colStartColour.ToString() + " Fade " + colFadeColour.ToString());

                //fade out particle 
                _parParticleList[i].startColor = colFadeColour;

               // Debug.Log("fade Colour " + parParticleList[i].startColor.ToString());

            } 
        }

        TargetParticleSystem.SetParticles(_parParticleList, iParticleCount);
    }
}
