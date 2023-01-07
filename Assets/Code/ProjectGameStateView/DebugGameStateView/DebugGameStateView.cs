using GameStateView;
using Sim;
using SimDataInterpolation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameStateView
{
    public class DebugGameStateView : MonoBehaviour, IGameStateView
    {
        public Color m_clrDrawColour = new Color(0,0,0,0);

        private ConstData m_cdaConstData;

        public void SetupConstDataViewEntities(ConstData cdaConstData)
        {
            m_cdaConstData = cdaConstData;

            if(m_clrDrawColour.a == 0)
            {
                m_clrDrawColour = new Color(Random.value, Random.value, Random.value, 1.0f);
            }
        }

        public void UpdateView(InterpolatedFrameDataGen ifdInterpolatedFrameData, SimProcessorSettings sdaSettingsData)
        {
            //draw all the asteroids
            DrawAsteroids();

            //draw space ships
            DrawSpaceShips(ifdInterpolatedFrameData, sdaSettingsData);

            //draw lasers
            DrawLasers(ifdInterpolatedFrameData, sdaSettingsData);
        }

        private void DrawAsteroids()
        {
            for (int i = 0; i < m_cdaConstData.m_fixAsteroidSize.Length; i++)
            {
                Vector3 center = new Vector3((float)m_cdaConstData.m_fixAsteroidPositionX[i], 0, (float)m_cdaConstData.m_fixAsteroidPositionY[i]);
                DrawCircle(center, (float)m_cdaConstData.m_fixAsteroidSize[i], m_clrDrawColour);
            }
        }

        private void DrawSpaceShips(InterpolatedFrameDataGen ifdInterpolatedFrameData, SimProcessorSettings sdaSettingsData)
        {
            for (int i = 0; i < ifdInterpolatedFrameData.m_fixShipPosX.Length; i++)
            {
                Vector3 center = new Vector3((float)ifdInterpolatedFrameData.m_fixShipPosX[i], 0, (float)ifdInterpolatedFrameData.m_fixShipPosY[i]);
                DrawCircle(center, (float)sdaSettingsData.ShipSize,m_clrDrawColour);
            }
        }

        private void DrawLasers(InterpolatedFrameDataGen ifdInterpolatedFrameData, SimProcessorSettings sdaSettingsData)
        {
            for (int i = 0; i < ifdInterpolatedFrameData.m_fixLazerPositionX.Length; i++)
            {
                Vector3 center = new Vector3((float)ifdInterpolatedFrameData.m_fixLazerPositionX[i], 0, (float)ifdInterpolatedFrameData.m_fixLazerPositionY[i]);
                DrawCircle(center, (float)sdaSettingsData.LazerSize, m_clrDrawColour);
            }
        }

        private void DrawCircle(Vector3 vecPos, float fRadius, Color colColour)
        {
            Vector3 veclastPoint = vecPos + new Vector3(fRadius, 0, 0);
            Vector3 vecNextPoint = Vector3.zero;

            for (var i = 0; i < 91; i++)
            {
                vecNextPoint.x = Mathf.Cos((i * 4) * Mathf.Deg2Rad) * fRadius;
                vecNextPoint.z = Mathf.Sin((i * 4) * Mathf.Deg2Rad) * fRadius;
                vecNextPoint.y = 0;

                vecNextPoint += vecPos;

                Debug.DrawLine(veclastPoint, vecNextPoint, colColour);
                veclastPoint = vecNextPoint;
            } 
        }
    }
}
