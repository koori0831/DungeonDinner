using System;
using UnityEngine;
using Work.FSM.Code;
using System.Collections.Generic;

namespace Work.Entities.Code
{
    public class EntityStateModule : MonoBehaviour, IEntityModule, IAfterInitialize
    {
        public Entity Owner { get; protected set; }
        public StateMachine StateMachine { get; private set; }

        [SerializeField] private List<StateSO> stateDataList;

        public void Initialize(Entity entity)
        {
            Owner = entity;
            StateMachine = new StateMachine();
        }

        public void AfterInitialize()
        {
            foreach (var data in stateDataList)
            {
                Type type = Type.GetType(data.targetClass);
                if (type != null)
                {
                    try
                    {
                        int animationHash = data.animationHash;
                        State state = Activator.CreateInstance(type, StateMachine, Owner, animationHash, data.isSkillAnimation) as State;
                        StateMachine.AddState(data.stateName, state);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[StateCompo] Failed to create state {data.stateName}: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"[StateCompo] Class not found: {data.targetClass}");
                }
            }

            if (stateDataList.Count > 0)
            {
                StateMachine.ChangeState(stateDataList[0].stateName);
            }
        }

        private void Update()
        {
            StateMachine?.Update();
        }

        public void TriggerEvent(AnimationEventType eventType)
        {
            StateMachine?.TriggerEvent(eventType);
        }

        private void OnDestroy()
        {
            StateMachine?.DisposeAll();
        }
    }
}