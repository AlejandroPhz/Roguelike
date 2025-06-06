using UnityEngine;


namespace TutorialVersion
{
    public class FoodObject : CellObject
    {
        public int AmountGranted = 5;
        
        public override void PlayerEntered()
        {
            Destroy(gameObject);

            //increase food
            GameManager.Instance.ChangeFood(AmountGranted);
        }
    }
}