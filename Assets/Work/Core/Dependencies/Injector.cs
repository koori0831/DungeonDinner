using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Work.Core.Dependencies
{
    [DefaultExecutionOrder(-10)] //모든 스크립트보다 빠르게 실행될 수 있도록 해야한다.
    public class Injector : MonoBehaviour
    {
        private const BindingFlags _bindingFlags = 
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        
        //의존성 주입을 위해 가지고 있는 Provide 객체들
        private readonly Dictionary<Type, object> _registry = new Dictionary<Type, object>();

        private void Awake()
        {
            IEnumerable<IDependencyProvider> providers = FindMonoBehaviours().OfType<IDependencyProvider>();
            foreach (var provider in providers)
            {
                RegisterProvider(provider);
            }
            
            IEnumerable<MonoBehaviour> injectables = FindMonoBehaviours().Where(IsInjectable);
            foreach (var injectable in injectables)
            {
                Inject(injectable);
            }

            
        }

        private void Inject(MonoBehaviour injectable)
        {
            Type type = injectable.GetType();
            IEnumerable<FieldInfo> injectableFields = type.GetFields(_bindingFlags)
                                .Where(f => Attribute.IsDefined(f, typeof(InjectAttribute)));

            foreach (var f in injectableFields)
            {
                Type fieldType = f.FieldType;
                object injectInstance = Resolve(fieldType);
                Debug.Assert(injectInstance != null, $"Inject instance not found for {fieldType.FullName}");
                f.SetValue(injectable, injectInstance);
            }
            
            IEnumerable<MethodInfo> injectableMethods = type.GetMethods(_bindingFlags)
                                .Where(m => Attribute.IsDefined(m, typeof(InjectAttribute)));
            foreach (var m in injectableMethods)
            {
                Type[] requiredParameters = m.GetParameters().Select(p => p.ParameterType).ToArray();
                object[] parameterInstances = requiredParameters.Select(Resolve).ToArray();
                m.Invoke(injectable, parameterInstances);
            }
        }

        private object Resolve(Type type)
        {
            _registry.TryGetValue(type, out object instance);
            return instance;
        }

        private bool IsInjectable(MonoBehaviour mono)
        {
            MemberInfo[] members = mono.GetType().GetMembers(_bindingFlags);
            return members.Any(member => Attribute.IsDefined(member, typeof(InjectAttribute)));
        }

        private void RegisterProvider(IDependencyProvider provider)
        {
            //클래스 그 자체가 Provide되는 경우라서 별도의 리플렉션 없이 가져오면 된다.
            if (Attribute.IsDefined(provider.GetType(), typeof(ProvideAttribute)))
            {
                _registry.Add(provider.GetType(), provider);
                return;
            }
            
            MethodInfo[] methods = provider.GetType().GetMethods(_bindingFlags);

            foreach (var method in methods)
            {
                if (!Attribute.IsDefined(method, typeof(ProvideAttribute))) continue;
                
                //정의되었다면 매서드를 실행해서 리턴타입을 레지스트리에 담아야 해.
                Type returnType = method.ReturnType;
                object providedInstance = method.Invoke(provider, null);
                Debug.Assert(providedInstance != null, $"provided instance is null : {method.Name}");
                _registry.Add(returnType, providedInstance);
            }
        }

        private static MonoBehaviour[] FindMonoBehaviours()
        {
            return FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None); //정렬없이 모든 모노 가져오기
        }
    }
}
