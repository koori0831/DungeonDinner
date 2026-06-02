using System.Collections.Generic;
using UnityEngine;

namespace Work.FSM.Code
{
    public class StateMachine
    {
        private Dictionary<string, State> states = new Dictionary<string, State>();

        public State CurrentState { get; private set; }
        public State PreviousState { get; private set; }

        public void AddState(string stateName, State state)
        {
            if (!states.ContainsKey(stateName))
            {
                states.Add(stateName, state);
            }
        }

        public void ChangeState(string stateName, bool isForcing = false)
        {
            if (TryChangeState(stateName, isForcing) == false)
            {
                Debug.LogError($"State '{stateName}' not found in the state machine.");
            }
        }

        /// <summary>
        /// 등록된 상태 존재 여부 반환.
        /// </summary>
        /// <param name="stateName">확인할 상태 이름.</param>
        /// <returns>상태 존재 여부.</returns>
        public bool HasState(string stateName)
        {
            return states.ContainsKey(stateName);
        }

        /// <summary>
        /// 등록된 상태로 전환 시도.
        /// </summary>
        /// <param name="stateName">전환할 상태 이름.</param>
        /// <param name="isForcing">동일 상태 강제 재진입 여부.</param>
        /// <returns>전환 성공 여부.</returns>
        public bool TryChangeState(string stateName, bool isForcing = false)
        {
            if (states.TryGetValue(stateName, out State nextState) == false)
            {
                return false;
            }

            if (CurrentState != null && !isForcing && CurrentState == nextState)
            {
                return true;
            }

            CurrentState?.Exit();
            PreviousState = CurrentState;
            CurrentState = nextState;
            CurrentState?.Enter();

            return true;
        }

        public void Update()
        {
            CurrentState?.Update();
        }

        public void TriggerEvent(AnimationEventType eventType)
        {
            CurrentState?.OnTriggerEnter(eventType);
        }

        public void DisposeAll()
        {
            foreach (KeyValuePair<string, State> kvp in states)
            {
                kvp.Value?.Dispose();
            }
        }
    }
}
