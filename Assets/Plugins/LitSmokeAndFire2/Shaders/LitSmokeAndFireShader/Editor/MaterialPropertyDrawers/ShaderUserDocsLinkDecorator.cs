using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ShaderUserDocsLinkDecorator : MaterialPropertyDrawer
{
    public int _iTotalHeight = 80;

    public void DrawReviewPrompt(Rect recPos)
    {
        //EditorGUILayout.Space();
        //bool bCurrentWordWrap = EditorStyles.label.wordWrap;
        // EditorStyles.label.wordWrap = true;

        Rect recHeader = new Rect(recPos);
        Rect recButtonLeft = new Rect(recPos);
        Rect recButtonRight = new Rect(recPos);
       
        recHeader.height = recHeader.height * 0.66666f;

        recButtonLeft.yMin = recHeader.yMax;
        recButtonRight.yMin = recHeader.yMax;

        recButtonLeft.height = recPos.height * 0.333333f ;
        recButtonRight.height = recPos.height * 0.333333f;


        recButtonLeft.width = recButtonLeft.width * 0.5f;
        recButtonRight.xMin = recButtonLeft.xMax;
        recButtonRight.width = recButtonLeft.width;

       

        bool bExistingWarp = EditorStyles.label.wordWrap;

        TextAnchor txaAnchor = EditorStyles.label.alignment;

        EditorStyles.label.alignment = TextAnchor.UpperCenter;

        EditorStyles.label.wordWrap = true;

        EditorGUI.LabelField(recHeader, "Has this asset been useful, easy to use and met your expectations?");

        EditorStyles.label.wordWrap = bExistingWarp;

        EditorStyles.label.alignment = txaAnchor;

       if (GUI.Button(recButtonLeft, "Yes"))
       {
            RequestReviewWindow.CreateWindow(RequestReviewWindow.WindowType.REQUEST_REVIEW);
       }
       
       if (GUI.Button(recButtonRight, "No"))
       {
             RequestReviewWindow.CreateWindow(RequestReviewWindow.WindowType.DIRECT_TO_HELP);
       }

        // EditorStyles.label.wordWrap = bCurrentWordWrap;

    }

    public void DrawDocLink(Rect recPos)
    {
        if (GUI.Button(recPos, "User Documentation"))
        {
            Application.OpenURL("https://docs.google.com/document/d/1oON0eruQdmpZNi5yRBf1-v3cJl2xpxZpNczFLA_Vd-U/edit?usp=sharing");
        }
    }

    public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
    {

        return _iTotalHeight;
    }

    public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
    {

        Rect recReviewRequest = new Rect(position);

        Rect recDocLink = new Rect(position);

        recReviewRequest.height *= 0.7f;
        recDocLink.yMin = recReviewRequest.yMax;
        recDocLink.height = position.height * 0.2f;
       

        DrawReviewPrompt(recReviewRequest);

        DrawDocLink(recDocLink);

    }
}
