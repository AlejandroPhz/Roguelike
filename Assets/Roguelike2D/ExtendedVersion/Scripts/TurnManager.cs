using System.Collections.Generic;

namespace Roguelike2D
{
    public class TurnManager
    {
        public interface ITurnEntity
        {
            public int GetTurnEnergy();
        }

        public class TrackedEntity
        {
            public delegate int ActionRequestDelegate();
        
            public int RemainingEnergy;
            public ITurnEntity TurnEntity;
            public ActionRequestDelegate ActionRequest;

            public void NewTurn()
            {
                RemainingEnergy += TurnEntity.GetTurnEnergy();
            }
        }
    
        private int m_TurnCount;
        private TrackedEntity m_PlayerTrackedEntity;
        private List<TrackedEntity> m_TrackedEntities = new();

        public event System.Action OnTick;

        public TurnManager()
        {
            m_TurnCount = 1;
        }

        public void Tick()
        {
            m_TurnCount += 1;
            OnTick?.Invoke();
        
            //at the beginning of each new tick, every entity refill energy
            m_PlayerTrackedEntity.NewTurn();
            foreach (var entity in m_TrackedEntities)
            {
                entity.NewTurn();
            }

            //the player had a big enough energy "debt" that this turn didn't fill it back into positive value, so other
            //entities get to use all their energy and we trigger another Tick
            if (m_PlayerTrackedEntity.RemainingEnergy <= 0)
            {
                ExhaustEntitiesEnergy();
                Tick();
            }
        }

        public void RegisterPlayer(PlayerController player)
        {
            m_PlayerTrackedEntity = new TrackedEntity()
            {
                RemainingEnergy = player.GetTurnEnergy(),
                TurnEntity = player
            };
        }

        public void AddTrackEntity(ITurnEntity entity, TrackedEntity.ActionRequestDelegate actionRequestDelegate)
        {
            m_TrackedEntities.Add(new TrackedEntity()
            {
                ActionRequest = actionRequestDelegate,
                RemainingEnergy = entity.GetTurnEnergy(),
                TurnEntity = entity
            });
        }

        public void RemovedTrackedEntity(ITurnEntity entity)
        {
            //Looking every time is not very efficient but this will happen rarely (we won't remove entities every frame) so
            //this is Good Enough for our use case
            var idx = m_TrackedEntities.FindIndex(trackedEntity => trackedEntity.TurnEntity == entity);
            if (idx >= 0)
            {
                m_TrackedEntities.RemoveAt(idx);
            }
        }

        public void PlayerAct(int cost)
        {
            m_PlayerTrackedEntity.RemainingEnergy -= cost;

            //now the player used energy and took an action, we go over every entities and tick the one with remaining energy
            bool entitiesStillWaiting = false;
            foreach (var entity in m_TrackedEntities)
            {
                if (entity.RemainingEnergy > 0)
                {
                    entity.RemainingEnergy -= entity.ActionRequest();
                
                    //this will get flipped to true if any entity have remaining energy after acting
                    entitiesStillWaiting |= entity.RemainingEnergy > 0;
                }
            }

            if (m_PlayerTrackedEntity.RemainingEnergy <= 0)
            {
                //player don't have any more energy, we let any world entities with still some energy acts until they are out too
                if (entitiesStillWaiting)
                {
                    ExhaustEntitiesEnergy();
                }
            
                //everyone is out of energy now, so we actually increment the turn count by one
                Tick();
            }
        }

        void ExhaustEntitiesEnergy()
        {
            //Flush animation, this will in essence make the enemy "jump" to their last position.
            //A proper full game would probably either wait for the system to finish moving (but then need a way to skip
            //ahead if the player press a button before it finish) or would "combine" move so moving 2 cells in the same
            //direction wouldn't be flushed but combined, but an attack would flush to jump next to the player etc.
            GameManager.Instance.MovingObjectSystem.FlushAll();
        
            foreach (var entity in m_TrackedEntities)
            {
                while (entity.RemainingEnergy > 0)
                {
                    var energyCost = entity.ActionRequest();
                    if (energyCost <= 0)
                    {
                        //this is a safeguard. World entity should ALWAYS use energy when asked for an action
                        //if this return 0 or negative, this would loop forever. So empty energy
                        entity.RemainingEnergy = 0;
                    }
                    else
                    {
                        entity.RemainingEnergy -= energyCost;
                    }
                }
            }
        }
    }
}