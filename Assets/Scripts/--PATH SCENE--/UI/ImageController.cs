using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(Image), typeof(AspectRatioFitter))]
public class ImageController : MonoBehaviour {
    private Texture2D _imageAsset;

    private Image _image;

    private void Start() {
        this._image = GetComponent<Image>();
    }

    public void ShowImage(Texture2D tx) {
        this._image.enabled = true;
        this._imageAsset = tx;
        
        // create sprite form texture
        var newSprite = Sprite.Create(_imageAsset, new Rect(
            0.0f, 0.0f, _imageAsset.width, _imageAsset.height), new Vector2(0.5f, 0.5f), 100.0f);
        // set sprite
        this._image.sprite = newSprite;
    }
    

    public void HideImage() {
        this._image.enabled = false;
    }
    
}