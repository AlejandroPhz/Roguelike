namespace Roguelike2D
{
    /// <summary>
    /// Base class for all CellObject that can be attacked. Set the IsUnique to true, as only one thing per cell should be
    /// attackable
    /// </summary>
    public abstract class AttackableCellObject : CellObject
    {
        public override bool IsUnique()
        {
            return true;
        }
    
        public abstract void Damaged(int amount);
    }
}