using UnityEngine;
using UnityEditor;
using System.Collections;

public class RequestReviewWindow : EditorWindow
{
    public WindowType _wtpWindowType;

    protected string _strBackgroundTextureAddress = "Assets\\EditorResources\\LitSmokeAndFire2\\ReviewRequestBackground.png";

    protected static Texture _texBackground;

    protected string _strRequestReviewText = "With as little as 2 clicks you can make this product better.\nHigh quality assets like this take a lot of time to produce and would not be possible without the support of users like yourself.\nBy rating, reviewing or posting your work on the forums you help expose this product to more people and help generate the resources needed to maintain and improve this asset.\nMore people = more resources = more features that can be added to the asset.\nIf you have the time please share this asset with as many people you can.";

    protected string _strHelpText = "If the product is not meeting your requirements or is falling short of your expected quality requirements please post your issue to the unity forum.\nIf you are having problems using the asset or don’t understand what a certan options does try reading the user documentation for this asset.\nIf you have a problem that is not covered in the user docs or on the online forums email you issue to encapgamedevelopment+lsaf2help@gmail.com and you should expect a reply within 2 working days.";

    public enum WindowType
    {
        REQUEST_REVIEW,
        DIRECT_TO_HELP
    }

    public static void CreateWindow(WindowType wtpWindowType)
    {
        //create rect for the popup window
        Rect recPopupWindow = new Rect();


        if(wtpWindowType == WindowType.REQUEST_REVIEW)
        {
            recPopupWindow.height = 320;
            recPopupWindow.width = 480;
        }

        if(wtpWindowType == WindowType.DIRECT_TO_HELP)
        {
            recPopupWindow.height = 320;
            recPopupWindow.width = 480;
        }


       // Rect bcup = new Rect(recPopupWindow);

        RequestReviewWindow rrwReviewPromptWindow = EditorWindow.GetWindowWithRect<RequestReviewWindow>(recPopupWindow);

        //reapply window pos
        rrwReviewPromptWindow.CenterOnMainWin();

        rrwReviewPromptWindow._wtpWindowType = wtpWindowType;
        
        rrwReviewPromptWindow.Show();
        rrwReviewPromptWindow.OnGUI();
    }

    void OnGUI()
    {

        if(_wtpWindowType == WindowType.REQUEST_REVIEW)
        {
            DrawReviewWindow();
        }

        if(_wtpWindowType == WindowType.DIRECT_TO_HELP)
        {
            DrawHelpWindow();
        }
    }

    void LoadBackgroundTexture()
    {
        _texBackground = AssetDatabase.LoadAssetAtPath<Texture2D>(_strBackgroundTextureAddress);
    }

    void DrawBackground()
    {
        if(_texBackground == null)
        {
            LoadBackgroundTexture();
        }

        if(_texBackground == null)
        {
            return;
        }

        EditorGUI.DrawPreviewTexture(new Rect(0, 0, 480, 320), _texBackground);
    }

    void DrawReviewWindow()
    {
        //draw background window
        DrawBackground();

        GUIStyle gusTextStyle = new GUIStyle();
        gusTextStyle.wordWrap = true;

        //draw label explaining need for reviews 
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(_strRequestReviewText, gusTextStyle);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
        EditorGUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        //draw button linking to reviews 
        if (GUILayout.Button("Review", GUILayout.Width(130), GUILayout.Height(25)))
        {
            Application.OpenURL("http://u3d.as/GzS");
        }
        GUILayout.FlexibleSpace();
        //draw button linking to forums
        if (GUILayout.Button("Forum", GUILayout.Width(130), GUILayout.Height(25)))
        {
            Application.OpenURL("https://forum.unity3d.com/threads/lit-smoke-and-fire-2-particle-package.463069/");
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Close", GUILayout.Width(130), GUILayout.Height(25)))
        {
            Close();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
    }

    void DrawHelpWindow()
    {
        //draw background
        DrawBackground();

        GUIStyle gusTextStyle = new GUIStyle();
        gusTextStyle.wordWrap = true;


        //draw label explaining need for reviews 
        EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
                EditorGUILayout.BeginVertical();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(_strHelpText, gusTextStyle);
            EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
        EditorGUILayout.EndHorizontal();


        GUILayout.FlexibleSpace();
         EditorGUILayout.BeginHorizontal();
         GUILayout.FlexibleSpace();
         //draw button linking to reviews 
         if (GUILayout.Button("User Documentation", GUILayout.Width(130), GUILayout.Height(25)))
         {
             Application.OpenURL("https://docs.google.com/document/d/1oON0eruQdmpZNi5yRBf1-v3cJl2xpxZpNczFLA_Vd-U/edit?usp=sharing");
         }
         GUILayout.FlexibleSpace();
         //draw button linking to forums
         if (GUILayout.Button("Forum", GUILayout.Width(130), GUILayout.Height(25)))
         {
             Application.OpenURL("https://forum.unity3d.com/threads/lit-smoke-and-fire-2-particle-package.463069/");
         }
         GUILayout.FlexibleSpace();
        if (GUILayout.Button("Close", GUILayout.Width(130), GUILayout.Height(25)))
        {
            Close();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
    }
}
