using UnityEngine;
using Random = UnityEngine.Random;

namespace Roguelike2D
{
    public class FoodFishObject : CellObject
    {
        public AudioClip[] PickedUpAudio;
        public int AmountGranted = 10;

        private void OnDestroy()
        {
            RemoveFromBoard();
        
        }

        public override void PlayerEntered()
        {

            FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/Interactables/Food/Desert Fish");

            Destroy(gameObject);
        
            //increase food
            GameManager.Instance.ChangeFood(AmountGranted);
        

        }
    }
}