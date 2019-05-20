using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using SharpWorker.WebApi;
using SharpWorker.WebApi.Description;

namespace SharpWorker.NetCoordinator.WebApi
{
    // ReSharper disable once ClassTooBig
    internal class WebApiProxyControllerService :
        IHttpControllerActivator,
        IHttpControllerTypeResolver,
        IAssembliesResolver,
        IWebApiWorkerService
    {
        private readonly DefaultHttpControllerActivator _activator = new DefaultHttpControllerActivator();
        private readonly DefaultAssembliesResolver _assembliesResolver = new DefaultAssembliesResolver();
        private readonly Dictionary<Type, Type> _controllerTypeProxyTypePairs = new Dictionary<Type, Type>();

        private readonly Dictionary<Type, List<WebApiController>> _proxyTypeControllerInstancesPairs =
            new Dictionary<Type, List<WebApiController>>();

        private readonly Dictionary<IWebApiWorker, Tuple<Type, WebApiController>[]> _workerControllerPair =
            new Dictionary<IWebApiWorker, Tuple<Type, WebApiController>[]>();

        private DefaultHttpControllerTypeResolver _typeResolver = new DefaultHttpControllerTypeResolver();

        public WebApiProxyControllerService(string basePath)
        {
            BasePath = basePath;
        }

        public string BasePath { get; }


        /// <inheritdoc />
        ICollection<Assembly> IAssembliesResolver.GetAssemblies()
        {
            return _assembliesResolver.GetAssemblies().Concat(GetProxyTypes().Select(type => type.Assembly)).ToArray();
        }

        /// <inheritdoc />
        IHttpController IHttpControllerActivator.Create(
            HttpRequestMessage request,
            HttpControllerDescriptor controllerDescriptor,
            Type proxyType)
        {
            return ActivateProxyType(proxyType) ?? _activator.Create(request, controllerDescriptor, proxyType);
        }

        /// <inheritdoc />
        ICollection<Type> IHttpControllerTypeResolver.GetControllerTypes(IAssembliesResolver assembliesResolver)
        {
            return _typeResolver.GetControllerTypes(assembliesResolver).Concat(GetProxyTypes()).ToArray();
        }

        /// <inheritdoc />
        WebApiControllerDescription[] IWebApiWorkerService.GetApiDescription()
        {
            return (this as IWebApiWorkerService)
                .GetControllers()
                .Select(controller => controller.GetType())
                .Distinct()
                .Select(type => new WebApiControllerDescription(type, GetControllerPath(type)))
                .ToArray();
        }

        /// <inheritdoc />
        WebApiController[] IWebApiWorkerService.GetControllers()
        {
            lock (_proxyTypeControllerInstancesPairs)
            {
                return _proxyTypeControllerInstancesPairs.Where(pair => pair.Value.Count > 0)
                    .Select(pair => pair.Value.LastOrDefault()).ToArray();
            }
        }

        /// <inheritdoc />
        IWebApiWorker[] IWebApiWorkerService.GetWorkers()
        {
            lock (_workerControllerPair)
            {
                return _workerControllerPair.Keys.ToArray();
            }
        }

        /// <inheritdoc />
        bool IWebApiWorkerService.RegisterController(WebApiController controller)
        {
            if (AddInstance(controller))
            {
                ClearResolverCache();

                return true;
            }

            return false;
        }

        /// <inheritdoc />
        bool IWebApiWorkerService.RegisterController<T>()
        {
            if (AddInstance(new T()))
            {
                ClearResolverCache();

                return true;
            }

            return false;
        }

        /// <inheritdoc />
        bool IWebApiWorkerService.RegisterWebApiWorker(IWebApiWorker worker)
        {
            if (AddWorker(worker))
            {
                ClearResolverCache();

                return true;
            }

            return false;
        }

        /// <inheritdoc />
        bool IWebApiWorkerService.UnRegisterController<T>()
        {
            if (RemoveInstance(typeof(T)))
            {
                ClearResolverCache();

                return true;
            }

            return false;
        }

        /// <inheritdoc />
        bool IWebApiWorkerService.UnRegisterController(WebApiController controller)
        {
            if (RemoveInstance(controller))
            {
                ClearResolverCache();

                return true;
            }

            return false;
        }

        /// <inheritdoc />
        bool IWebApiWorkerService.UnRegisterWebApiWorker(IWebApiWorker worker)
        {
            if (RemoveWorker(worker))
            {
                ClearResolverCache();

                return true;
            }

            return false;
        }

        public IEnumerable<Type> GetAllControllerTypes()
        {
            return ((IHttpControllerTypeResolver) this).GetControllerTypes(this);
        }

        private WebApiProxyControllerBase ActivateProxyType(Type proxyType)
        {
            if (proxyType == null || !typeof(WebApiProxyControllerBase).IsAssignableFrom(proxyType))
            {
                return null;
            }

            WebApiController instance;

            lock (_proxyTypeControllerInstancesPairs)
            {
                if (!_proxyTypeControllerInstancesPairs.ContainsKey(proxyType))
                {
                    return null;
                }

                lock (_proxyTypeControllerInstancesPairs[proxyType])
                {
                    instance = _proxyTypeControllerInstancesPairs[proxyType].LastOrDefault();
                }
            }

            if (instance == null)
            {
                return null;
            }

            return WebApiProxyControllerBuilder.ActivateProxyType(proxyType, instance);
        }


        private bool AddInstance(WebApiController controller)
        {
            var type = controller?.GetType();

            if (type == null)
            {
                return false;
            }

            var proxyType = GetProxyType(type);

            return AddInstance(controller, proxyType);
        }

        private bool AddInstance(WebApiController controller, Type proxyType)
        {
            if (proxyType == null ||
                controller == null ||
                !typeof(WebApiProxyControllerBase).IsAssignableFrom(proxyType))
            {
                return false;
            }

            lock (_proxyTypeControllerInstancesPairs)
            {
                if (!_proxyTypeControllerInstancesPairs.ContainsKey(proxyType))
                {
                    _proxyTypeControllerInstancesPairs.Add(proxyType, new List<WebApiController>());
                }

                _proxyTypeControllerInstancesPairs[proxyType].Add(controller);
            }

            return true;
        }

        private bool AddWorker(IWebApiWorker worker)
        {
            RemoveWorker(worker);

            var controllers = worker?.GetWebApiControllers();

            if (controllers == null)
            {
                return false;
            }

            var controllerTuples = new List<Tuple<Type, WebApiController>>();

            foreach (var controller in controllers)
            {
                var type = controller.GetType();
                var proxyType = GetProxyType(type);

                if (proxyType == null)
                {
                    return false;
                }

                if (AddInstance(controller, proxyType))
                {
                    controllerTuples.Add(new Tuple<Type, WebApiController>(type, controller));
                }
            }

            if (controllerTuples.Count > 0)
            {
                lock (_workerControllerPair)
                {
                    _workerControllerPair.Add(worker, controllerTuples.ToArray());
                }

                return true;
            }

            return false;
        }

        private void ClearResolverCache()
        {
            lock (this)
            {
                _typeResolver = new DefaultHttpControllerTypeResolver();
            }
        }

        private string GetControllerPath(Type type)
        {
            var controllerName = type.Name;

            if (controllerName.EndsWith("Controller"))
            {
                controllerName = controllerName.Substring(0, controllerName.Length - "Controller".Length);
            }


            string workerName;

            lock (_workerControllerPair)
            {
                workerName = _workerControllerPair.FirstOrDefault(pair => pair.Value.Any(t => t.Item1 == type)).Key
                    ?.GetType().Name;
            }

            var path = BasePath + "/";

            if (workerName != null)
            {
                if (workerName.EndsWith("Worker"))
                {
                    workerName = workerName.Substring(0, workerName.Length - "Worker".Length);
                }

                if (workerName.Equals(controllerName, StringComparison.InvariantCultureIgnoreCase))
                {
                    path = path + controllerName;
                }
                else
                {
                    path = path + workerName + "/" + controllerName;
                }
            }
            else
            {
                path = path + controllerName;
            }

            return path;
        }

        private Type GetProxyType(Type type)
        {
            lock (_controllerTypeProxyTypePairs)
            {
                if (_controllerTypeProxyTypePairs.ContainsKey(type))
                {
                    return _controllerTypeProxyTypePairs[type];
                }
            }


            var proxyType = WebApiProxyControllerBuilder.CreateProxyType(new WebApiControllerDescription(type, GetControllerPath(type)));

            if (proxyType != null)
            {
                lock (_controllerTypeProxyTypePairs)
                {
                    _controllerTypeProxyTypePairs.Add(type, proxyType);
                }
            }

            return proxyType;
        }

        private Type[] GetProxyTypes()
        {
            lock (_proxyTypeControllerInstancesPairs)
            {
                return _proxyTypeControllerInstancesPairs.Where(pair => pair.Value.Count > 0).Select(pair => pair.Key)
                    .ToArray();
            }
        }

        private bool RemoveInstance(WebApiController controller)
        {
            var type = controller?.GetType();

            if (type == null)
            {
                return false;
            }

            var proxyType = GetProxyType(type);

            return RemoveInstance(controller, proxyType);
        }

        private bool RemoveInstance(WebApiController controller, Type proxyType)
        {
            if (proxyType == null || controller == null)
            {
                return false;
            }

            lock (_proxyTypeControllerInstancesPairs)
            {
                if (!_proxyTypeControllerInstancesPairs.ContainsKey(proxyType))
                {
                    return false;
                }

                lock (_proxyTypeControllerInstancesPairs[proxyType])
                {
                    if (!_proxyTypeControllerInstancesPairs[proxyType].Contains(controller))
                    {
                        return false;
                    }

                    return _proxyTypeControllerInstancesPairs[proxyType].Remove(controller);
                }
            }
        }

        private bool RemoveInstance(Type type)
        {
            if (type == null || !typeof(WebApiController).IsAssignableFrom(type))
            {
                return false;
            }

            var proxyType = GetProxyType(type);

            if (proxyType == null)
            {
                return false;
            }

            lock (_proxyTypeControllerInstancesPairs)
            {
                if (!_proxyTypeControllerInstancesPairs.ContainsKey(proxyType))
                {
                    return false;
                }

                lock (_proxyTypeControllerInstancesPairs[proxyType])
                {
                    _proxyTypeControllerInstancesPairs[proxyType].Clear();
                }
            }

            return true;
        }

        private bool RemoveWorker(IWebApiWorker worker)
        {
            if (worker == null)
            {
                return false;
            }

            Tuple<Type, WebApiController>[] controllerPairs;

            lock (_workerControllerPair)
            {
                if (!_workerControllerPair.ContainsKey(worker))
                {
                    return false;
                }

                controllerPairs = _workerControllerPair[worker];
            }

            var removed = false;

            foreach (var controllerPair in controllerPairs)
            {
                var proxyType = GetProxyType(controllerPair.Item1);

                if (proxyType == null)
                {
                    continue;
                }

                removed = RemoveInstance(controllerPair.Item2, proxyType) || removed;
            }

            if (removed)
            {
                lock (_workerControllerPair)
                {
                    _workerControllerPair.Remove(worker);
                }
            }

            return removed;
        }
    }
}