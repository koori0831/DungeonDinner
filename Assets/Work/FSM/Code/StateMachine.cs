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
            if (!states.TryGetValue(stateName, out State nextState))
            {
                Debug.LogError($"State '{stateName}' not found in the state machine.");
                return;
            }

            if (CurrentState != null && !isForcing && CurrentState == nextState)
                return;

            CurrentState?.Exit();
            PreviousState = CurrentState;
            CurrentState = nextState;
            CurrentState?.Enter();
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
            foreach (var kvp in states)
            {
                kvp.Value?.Dispose();
            }
        }
    }
}
