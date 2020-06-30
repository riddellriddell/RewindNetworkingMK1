using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ShaderBackgroundDecorator : MaterialPropertyDrawer
{
    public string _strBackgroundTextureName;
    public string _strHeaderTextureName;
    //public string _strShaderName = ;

    public float _fHeaderHeight = 128;
    public float _fStreatchUp = 5;
    public float _fStreachLeft = 15;
    public float _fSterachRight = 5;
    public Vector2 _vecBackgroundScale;
    public Vector2 _vecHeaderScale;
    public Texture2D _texBackground;
    public Texture2D _texHeader;

    void LoadTextures()
    {
        _texBackground = AssetDatabase.LoadAssetAtPath<Texture2D>(_strBackgroundTextureName);
        _texHeader = AssetDatabase.LoadAssetAtPath<Texture2D>(_strHeaderTextureName);
    }


    public bool CheckResources()
    {
        if(_texBackground == null || _texHeader)
        {
            LoadTextures();
        }


        if(_texBackground == null || _texHeader == null)
        {
            return false;
        }



        return true;
    }

    public ShaderBackgroundDecorator(string strBackgroundTexture, string strHeaderTexture)
    {
        _strBackgroundTextureName = strBackgroundTexture.Replace("..","\\");
        _strHeaderTextureName = strHeaderTexture.Replace("..", "\\");
        _vecBackgroundScale = new Vector2(1f, 1f);
        _vecHeaderScale = new Vector2(1f, 1f);
        //LoadBackgroundTexture();

        //LoadMaterial();
    }

    public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
    {
        return 0;

    }

    public void FillInBackgroundArea(Rect recBackground)
    {
        Vector2 vecTileSize = new Vector2(_texBackground.width * _vecBackgroundScale.x, _texBackground.height * _vecBackgroundScale.y);

        Rect recTile = new Rect(0, 0, _texBackground.width * _vecBackgroundScale.x, _texBackground.height * _vecBackgroundScale.y);


        while (recTile.x < recBackground.xMax)
        {
            recTile.width = vecTileSize.x;
            recTile.height = vecTileSize.y;
            recTile.y = recBackground.y;

            while (recTile.y < recBackground.yMax)
            {

                EditorGUI.DrawPreviewTexture(recTile, _texBackground);

                recTile.y += vecTileSize.y;
            }

            recTile.x += vecTileSize.x;
        }

    }

    public void FillInBar(Rect recBar)
    {
        Vector2 vecTileSize = new Vector2(_texHeader.width * _vecHeaderScale.x, recBar.height);

        Rect recTile = new Rect(recBar.x, recBar.y, _texHeader.width * _vecHeaderScale.x, recBar.height);


        while (recTile.x < recBar.xMax)
        {
            recTile.width = vecTileSize.x;
            recTile.y = recBar.y;

            EditorGUI.DrawPreviewTexture(recTile, _texHeader);    

            recTile.x += vecTileSize.x;
        }
    }

    public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
    {

        if(CheckResources() == false)
        {
            return;
        }

        Rect recStreachedTarget = new Rect(position);
        recStreachedTarget.yMin -= _fStreatchUp;
        recStreachedTarget.xMin -= _fStreachLeft;
        recStreachedTarget.xMax += _fSterachRight;
        recStreachedTarget.height = 900;

        Rect recHeder = new Rect(recStreachedTarget);
        recHeder.height = _fHeaderHeight;

        FillInBar(recHeder);


        Rect recBackground = new Rect(recStreachedTarget);

        recBackground.yMin -= _fStreatchUp;
        recBackground.xMin -= _fStreachLeft;
        recBackground.xMax += _fSterachRight;
        recBackground.height = 900;
        recBackground.y = recHeder.yMax;

        FillInBackgroundArea(recBackground);




    }

}
