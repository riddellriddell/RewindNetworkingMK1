using Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameManagers
{
    public class ActiveGameManagerSceneTester : MonoBehaviour
    {
        public string m_strUniqueDeviceID;

        public WebInterface m_wbiWebInterface;

        public ActiveGameManager m_agmActiveGameManager;

        // Start is called before the first frame update
        void Start()
        {
            StartCoroutine(Test());
        }

        // Update is called once per frame
        void Update()
        {

        }

        protected IEnumerator Test()
        {
            yield return null;

            Debug.Log("Starting active game manager Test");

            m_wbiWebInterface = new WebInterface();

            m_wbiWebInterface.GetPlayerID(m_strUniqueDeviceID);
                        

            while (m_wbiWebInterface.PlayerIDCommunicationStatus.m_cmsStatus != WebInterface.WebAPICommunicationTracker.CommunctionStatus.Succedded )
            {
                m_wbiWebInterface.UpdateCommunication();

                yield return null;
            }

            m_agmActiveGameManager = new ActiveGameManager(m_wbiWebInterface);

            while(true)
            {
                m_wbiWebInterface.UpdateCommunication();

                m_agmActiveGameManager.UpdateGame(Time.deltaTime);

                yield return null;
            }

        }
    }
}