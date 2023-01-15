using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if !UNITY_EDITOR && UNITY_WEBGL
using System.Runtime.InteropServices;
#endif

namespace GameViewUI
{

    public class MobileInputUI : MonoBehaviour
    {
        public bool m_bMobileOnly = true;

        public  bool m_bLeftPress = false;
        public  bool m_bRighPress = false;
        
        public  bool m_bLeftRelease = false;
        public  bool m_bRighRelease = false;

#if !UNITY_EDITOR && UNITY_WEBGL
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern bool IsMobile();
#endif

        private void Start()
        {
                bool isMobile = false;

#if !UNITY_EDITOR && UNITY_WEBGL
        isMobile = IsMobile();
#endif

            //check if this is open on a mobile device 
            if (!isMobile && m_bMobileOnly)
            {
                //disable mobile inputs on non mobile devices
                gameObject.SetActive(false);
            }
        }


        public void OnLeftPress()
        {
            m_bLeftPress = true;
        }

        public void OnRightPress()
        {
            m_bRighPress = true;
        }


        public void OnLeftRelease()
        {
            m_bLeftRelease = true;
        }

        public void OnRightRelease()
        {
            m_bRighRelease = true;
        }

        public void ResetPressState()
        {
             m_bLeftPress = false;
             m_bRighPress = false;
            
             m_bLeftRelease = false;
             m_bRighRelease = false;
        }
    }
}