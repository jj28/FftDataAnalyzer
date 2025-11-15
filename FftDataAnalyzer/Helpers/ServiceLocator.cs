using System;
using System.Collections.Generic;

namespace FftDataAnalyzer.Helpers
{
    /// <summary>
    /// Simple Dependency Injection container / Service Locator
    /// </summary>
    public class ServiceLocator
    {
        private static ServiceLocator _instance;
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();

        public static ServiceLocator Instance => _instance ?? (_instance = new ServiceLocator());

        /// <summary>
        /// Register a singleton service instance
        /// </summary>
        public void RegisterSingleton<TInterface>(TInterface implementation)
        {
            _services[typeof(TInterface)] = implementation;
        }

        /// <summary>
        /// Register a singleton service with a factory function
        /// </summary>
        public void RegisterSingleton<TInterface>(Func<TInterface> factory)
        {
            _factories[typeof(TInterface)] = () => factory();
        }

        /// <summary>
        /// Register a transient service with a factory function (creates new instance each time)
        /// </summary>
        public void RegisterTransient<TInterface>(Func<TInterface> factory)
        {
            _factories[typeof(TInterface)] = () => factory();
        }

        /// <summary>
        /// Resolve a service from the container
        /// </summary>
        public T Resolve<T>()
        {
            var type = typeof(T);

            // Check if singleton instance exists
            if (_services.ContainsKey(type))
                return (T)_services[type];

            // Check if factory exists
            if (_factories.ContainsKey(type))
            {
                var instance = (T)_factories[type]();
                return instance;
            }

            throw new InvalidOperationException($"Service of type {type.Name} is not registered.");
        }

        /// <summary>
        /// Clear all registered services (useful for testing)
        /// </summary>
        public void Clear()
        {
            _services.Clear();
            _factories.Clear();
        }
    }
}
