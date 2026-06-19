using System;
using System.Collections.Generic;

namespace Hadal.Core.DI
{
    /// <summary>
    /// Production DI container. Register interfaces to implementations at bootstrap.
    /// </summary>
    public sealed class GameServiceContainer
    {
        private readonly Dictionary<Type, object> _services = new();
        private bool _isLocked;

        public void Register<TService>(TService instance) where TService : class
        {
            if (_isLocked)
                throw new InvalidOperationException("Cannot register after container lock.");

            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            _services[typeof(TService)] = instance;
        }

        public void Register(Type serviceType, object instance)
        {
            if (_isLocked)
                throw new InvalidOperationException("Cannot register after container lock.");

            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));

            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (!serviceType.IsInstanceOfType(instance))
                throw new ArgumentException($"Instance does not implement {serviceType.Name}.");

            _services[serviceType] = instance;
        }

        public bool TryResolve<TService>(out TService service) where TService : class
        {
            if (_services.TryGetValue(typeof(TService), out var direct))
            {
                service = direct as TService;
                if (service != null)
                    return true;
            }

            foreach (var candidate in _services.Values)
            {
                if (candidate is TService typed)
                {
                    service = typed;
                    return true;
                }
            }

            service = null;
            return false;
        }

        public TService Resolve<TService>() where TService : class
        {
            if (TryResolve<TService>(out var service))
                return service;

            throw new InvalidOperationException($"Service not registered: {typeof(TService).Name}");
        }

        public void Lock() => _isLocked = true;

        public void Clear()
        {
            _services.Clear();
            _isLocked = false;
        }
    }
}
