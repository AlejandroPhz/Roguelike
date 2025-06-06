using UnityEngine;
using Random = UnityEngine.Random;

namespace Roguelike2D
{
    public class FoodObject : CellObject
    {
        public AudioClip[] PickedUpAudio;
        public int AmountGranted = 10;

        private void OnDestroy()
        {
            RemoveFromBoard();
        
        }

        public override void PlayerEntered()
        {
            Destroy(gameObject);
        
            //increase food
            GameManager.Instance.ChangeFood(AmountGranted);
        
            if(PickedUpAudio.Length > 0)
                GameManager.Instance.PlayAudioSFX(PickedUpAudio[Random.Range(0, PickedUpAudio.Length)], transform.position);
        }
    }
}