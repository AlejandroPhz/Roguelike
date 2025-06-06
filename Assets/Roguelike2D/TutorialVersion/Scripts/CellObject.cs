using UnityEngine;

namespace TutorialVersion
{
    public class CellObject : MonoBehaviour
    {
        protected Vector2Int m_Cell;
        
        public virtual void Init(Vector2Int cell)
        {
            m_Cell = cell;
        }

        //Called when the player enter the cell in which that object is
        public virtual void PlayerEntered()
        {

        }
        
        public virtual bool PlayerWantsToEnter()
        {
            return true;
        }
    }
}