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
            foreach (IEntityModule module in _modules.Values)
            {
                if (module is IAfterInitialize afterInitModule)
                {
                    afterInitModule.AfterInitialize();
                }
            }
        }

        private void ModuleInit()
        {
            foreach (IEntityModule module in _modules.Values)
            {
                module.Initialize(this);
            }
        }

        private void AddModule()
        {
            _modules = GetComponentsInChildren<IEntityModule>(true).ToList().ToDictionary((IEntityModule item) => item.GetType());

            string m = $"이름 : {name} \n";
            foreach (KeyValuePair<Type, IEntityModule> kvp in _modules)
            {

                m += $"{kvp.Value.GetType().ToString()} \n";
            }
        }

        public T GetModule<T>(bool isAssignable = false) where T : class, IEntityModule
        {
            T module;

            if (TryGetModule<T>(out module, isAssignable) == true)
            {
                return module;
            }

            Debug.LogError($"Not Find {typeof(T)}");
            return null;
        }

        /// <summary>
        /// 등록된 엔티티 모듈 조회 시도.
        /// </summary>
        /// <param name="module">조회된 모듈.</param>
        /// <param name="isAssignable">상속 타입 포함 조회 여부.</param>
        /// <typeparam name="T">조회할 모듈 타입.</typeparam>
        /// <returns>조회 성공 여부.</returns>
        public bool TryGetModule<T>(out T module, bool isAssignable = false) where T : class, IEntityModule
        {
            if (_modules.TryGetValue(typeof(T), out IEntityModule exactModule) == true)
            {
                module = exactModule as T;
                return module != null;
            }

            if (isAssignable == true)
            {
                foreach (KeyValuePair<Type, IEntityModule> kvp in _modules)
                {
                    if (kvp.Value is T assignableModule)
                    {
                        module = assignableModule;
                        return true;
                    }
                }
            }

            module = null;
            return false;
        }
    }
}
