using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine.Video;
using Object = UnityEngine.Object;



public abstract class SubPathInteractableComponent : MonoBehaviour, INteractable {
    // dict for UI
    
    

    #region EVENTS

    public static event EventHandler<OnInteractedEventArgs> OnInteracted;
    public class OnInteractedEventArgs : EventArgs {
        public Object Medium;
        public SubPath SubPath;
        public double RelStartTimeInSeconds;
        public int StartLocalIndex;
    }

    #endregion


    #region FIELDS

    public SubPath subPath;
    public double relStartTimeInSeconds = 0d;
    public int startLocalIndex = 0;
    
    #endregion


    #region METHODS
    
    
    protected void PlayerInteractionOnSubPathComponentSelected(object sender, PlayerInteraction.OnSubPathComponentSelectedEventArgs e) {
        if (e.Component.subPath != this.subPath) {
            SetSubPathVisualAsSelected(false);
            return;
        }
        SetSubPathVisualAsSelected(true);
    }

    protected void SetSubPathVisualAsSelected(bool b) {
        subPath.SetVisualAsSelected(b);
    }
    
    
    public void CollisionExitWithPlayer() {
        // when player exits collider of this dataspot -> set visual as unselected
        if (GameManager.Instance.gameState != GameManager.GameState.WalkAround) return;
        SetSubPathVisualAsSelected(false);
    }
    
    

    public void Interact() {
        // -> changes gamemode, shows video/image, calls followpath
        OnInteracted?.Invoke(this, new OnInteractedEventArgs {
            Medium = subPath.medium,
            SubPath = subPath,
            RelStartTimeInSeconds = relStartTimeInSeconds,
            StartLocalIndex = startLocalIndex,
        });
    }

    #endregion
    

}