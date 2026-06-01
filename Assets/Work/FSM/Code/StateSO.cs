using System;
using UnityEngine;

namespace Work.FSM.Code
{
    [CreateAssetMenu(fileName = "StateData", menuName = "SO/FSM/StateData")]
    public class StateSO : ScriptableObject
    {
        public string stateName;
        public string targetClass;
        [SerializeField, HideInInspector] private int _animationHash;

        public int animationHash
        {
            get
            {
                if (_animationHash == 0 && !string.IsNullOrEmpty(stateName))
                {
                    _animationHash = Animator.StringToHash(stateName);
                }
                return _animationHash;
            }
        }

        public Type ResolveStateType()
        {
            if (string.IsNullOrWhiteSpace(targetClass))
            {
                return null;
            }

            Type type = Type.GetType(targetClass);
            if (IsValidStateType(type))
            {
                return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(targetClass);
                if (IsValidStateType(type))
                {
                    return type;
                }
            }

            return null;
        }

        private static bool IsValidStateType(Type type)
        {
            return type != null && type.IsAbstract == false && type.IsSubclassOf(typeof(State));
        }

        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(stateName))
                _animationHash = Animator.StringToHash(stateName);
        }
    }
}
