using UnityEngine;
using UnityEngine.Tilemaps;

namespace Roguelike2D
{
    public class ExitCellObject : CellObject
    {
        public Tile EndTile;
    
        public override void Init(Vector2Int coord)
        {
            base.Init(coord);
            GameManager.Instance.Board.SetCellTile(coord, EndTile);
        }

        public override void PlayerEntered()
        {
              FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/Interactables/Level Goal");
            GameManager.Instance.RequestNextLevel();
        }
    }
}