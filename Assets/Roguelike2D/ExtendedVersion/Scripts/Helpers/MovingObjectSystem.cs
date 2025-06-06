using System.Collections.Generic;
using UnityEngine;

namespace Roguelike2D
{
    public class MovingObjectSystem
    {

        public class MoveRequest
        {
            public delegate void MoveStartingCallback();
            public delegate void MoveFinishedCallback(bool midReturn);
        
            public Transform Target;
            public Vector3 Start;
            public Vector3 End;
            public float Speed;
            public bool Return;
            public bool Started = false;

            //called when the move start playing. Only called once! Returning will have FinishedCallback called with the
            //midway param set to true when starting the return.
            public MoveStartingCallback StartingCallback;
            //will receive true as param if we are just midway through a movement that return to start point
            public MoveFinishedCallback FinishedCallback;
        }
    
        public class MovingQueue
        {
            public int Priority;
            public List<MoveRequest> ObjectList;
        }
    
        private List<MovingQueue> m_MovingQueues = new();

        public MovingObjectSystem()
        {
        
        }

        public void Update()
        {
            if(m_MovingQueues.Count == 0)
                return;
        
            var currentList = m_MovingQueues[0].ObjectList;
            for (int i = 0; i < currentList.Count; ++i)
            {
                var req = currentList[i];

                if (req.Target == null)
                {
                    //target was destroyed
                    currentList.RemoveAt(i);
                    i--;
                    continue;
                }

                if (!req.Started)
                {
                    req.Started = true;
                    req.StartingCallback?.Invoke();
                }

                req.Target.position = Vector3.MoveTowards(req.Target.position, req.End, req.Speed * Time.deltaTime);

                if (req.Target.position == req.End)
                {
                    //move finished, if return is true, swap start and end and set return to false
                    if (req.Return)
                    {
                        req.Return = false;
                        (req.Start, req.End) = (req.End, req.Start);
                        req.FinishedCallback?.Invoke(true);
                    }
                    else
                    {
                        //no return needed, so we just call the callback and remove that entry from the list
                        req.FinishedCallback?.Invoke(false);
                        currentList.RemoveAt(i);
                        i--;
                    }
                }
            }

            if (currentList.Count == 0)
            {
                //we're done with that priority level, so remove it so next level get handled next
                m_MovingQueues.RemoveAt(0);
            }
        }

        public void Clean()
        {
            m_MovingQueues.Clear();
        }

        //will get all animation to the end and call the associated callback. Used to speed up game with user input so they
        //don't have to wait for all animation to play
        public void FlushAll()
        {
            foreach (var movingQueue in m_MovingQueues)
            {
                foreach (var moveRequest in movingQueue.ObjectList)
                {
                    if (moveRequest.Target == null)
                    {
                        //target was destroyed, ignore
                        continue;
                    }

                    moveRequest.Target.position = moveRequest.End;
                    //if that move is a return move, we call the callback once then return to start
                    if (moveRequest.Return)
                    {
                        moveRequest.FinishedCallback?.Invoke(true);
                        moveRequest.Target.position = moveRequest.Start;
                    }
                    moveRequest.FinishedCallback?.Invoke(false);
                }
            }

            Clean();
        }

        public void AddMoveRequest(Transform target, Vector3 end, float speed, bool isReturn, int priority, MoveRequest.MoveFinishedCallback finishedCallback = null, MoveRequest.MoveStartingCallback startedCallback = null)
        {
            var moveRequest = new MoveRequest()
            {
                Start = target.position,
                End = end,
                Return = isReturn,
                Speed = speed,
                Target = target,
                FinishedCallback = finishedCallback,
                StartingCallback = startedCallback
            };
        
            int idx = m_MovingQueues.FindIndex(queue => queue.Priority == priority);
            if (idx != -1)
            {
                m_MovingQueues[idx].ObjectList.Add(moveRequest);
            }
            else
            {
                int insertPos = 0;
                //find the insert index for that priority so the list stay in ascending priority order
                while (insertPos < m_MovingQueues.Count && m_MovingQueues[insertPos].Priority < priority)
                {
                    insertPos++;
                }
            
                m_MovingQueues.Insert(insertPos, new MovingQueue()
                {
                    ObjectList = new() { moveRequest },
                    Priority = priority
                });
            }
        }
    }
}