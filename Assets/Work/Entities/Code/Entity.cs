using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace Work.Entities.Code
{
    public class Entity : MonoBehaviour
    {
        public Transform Transform => gameObject != null ? transform : null;
        protected Dictionary<Type, IEntityModule> _modules = new Dictionary<Type, IEntityModule>();

        public virtual void Init()
        {
            AddModule();
            ModuleInit();
            ModuleAfterInit();
        }

        private void ModuleAfterInit()
        {
            foreach (var module in _modules.Values)
            {
                if (module is IAfterInitialize afterInitModule)
                {
                    afterInitModule.AfterInitialize();
                }
            }
        }

        private void ModuleInit()
        {
            foreach (var module in _modules.Values)
            {
                module.Initialize(this);
            }
        }

        private void AddModule()
        {
            _modules = GetComponentsInChildren<IEntityModule>(true).ToList().ToDictionary(item => item.GetType());

            string m = $"이름 : {name} \n";
            foreach (var kvp in _modules)
            {

                m += $"{kvp.Value.GetType().ToString()} \n";
            }
        }

        public T GetModule<T>(bool isAssignable = false) where T : class, IEntityModule
        {
            if (_modules.TryGetValue(typeof(T), out var module))
                return module as T;
            if (isAssignable == false)
            {
                Debug.LogError($"Not Find {typeof(T)}");
                return null;
            }

            foreach (var kvp in _modules)
            {
                if (kvp.Value is T tModule)
                    return tModule;
            }

            Debug.LogError($"Not Find {typeof(T)}");
            return null;
        }
    }
}