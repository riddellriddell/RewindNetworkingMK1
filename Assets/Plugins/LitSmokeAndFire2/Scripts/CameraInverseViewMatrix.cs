using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class CameraInverseViewMatrix : MonoBehaviour
{
    protected Camera camCamera;
    public Camera TargetCamera
    {
        get
        {
            if(camCamera ==null)
            {
                camCamera = GetComponent<Camera>();
              
            }

            return camCamera;
        }
    }

    public void OnPreCull()
    {
        Shader.SetGlobalMatrix("_Camera2World", TargetCamera.cameraToWorldMatrix);
    }


#if UNITY_EDITOR
   public void Update()
   {
       Camera sceneCamera = null;
       if (SceneView.currentDrawingSceneView != null)
           sceneCamera = SceneView.currentDrawingSceneView.camera;
   
       if (sceneCamera != null)
       {
           if (sceneCamera.GetComponent<CameraInverseViewMatrix>() == null)
               sceneCamera.gameObject.AddComponent<CameraInverseViewMatrix>();
       }
   
   }
#endif
}
