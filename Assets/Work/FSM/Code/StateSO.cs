using UnityEngine;

namespace Work.FSM.Code
{
    [CreateAssetMenu(fileName = "StateData", menuName = "SO/FSM/StateData")]
    public class StateSO : ScriptableObject
    {
        public string stateName;
        public string targetClass;
        public bool isSkillAnimation = false;

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

        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(stateName))
                _animationHash = Animator.StringToHash(stateName);
        }
    }
}