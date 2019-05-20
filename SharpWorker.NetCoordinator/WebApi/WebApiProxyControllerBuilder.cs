using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Web.Http;
using NSwag.Annotations;
using SharpWorker.WebApi;
using SharpWorker.WebApi.Description;

namespace SharpWorker.NetCoordinator.WebApi
{
    internal static class WebApiProxyControllerBuilder
    {
        public static WebApiProxyControllerBase ActivateProxyType(
            Type proxyType,
            WebApiController instance)
        {
            return Activator.CreateInstance(proxyType, instance) as WebApiProxyControllerBase;
        }

        public static Type CreateProxyType(WebApiControllerDescription controllerDescription)
        {
            var typeName = "ProxyControllers." + controllerDescription.ControllerType.FullName;

            if (!typeName.EndsWith("Controller"))
            {
                typeName += "Controller";
            }

            var assemblyInfo = controllerDescription.ControllerType.Assembly.GetName(true);
            assemblyInfo.Name = "ProxyControllers." + assemblyInfo.Name;

            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                assemblyInfo,
                AssemblyBuilderAccess.RunAndCollect
            );

            var moduleBuilder = assemblyBuilder.DefineDynamicModule(controllerDescription.ControllerType.Module.Name,
                assemblyInfo.Name + ".dll");

            var typeBuilder = moduleBuilder.DefineType(typeName,
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit, typeof(WebApiProxyControllerBase));

            typeBuilder.SetCustomAttribute(GetAttributeBuilder(typeof(RoutePrefixAttribute), controllerDescription.ControllerPath));

            if (!string.IsNullOrWhiteSpace(controllerDescription.Description))
            {
                typeBuilder.SetCustomAttribute(GetAttributeBuilder(typeof(DescriptionAttribute),
                    controllerDescription.Description));
            }

            CreateConstructors(typeBuilder);

            foreach (var action in controllerDescription.Actions)
            {
                CreateMethod(typeBuilder, action);
            }

            var proxyType = typeBuilder.CreateType();

            return proxyType;
        }

        // ReSharper disable once TooManyDeclarations
        private static void CreateConstructors(TypeBuilder typeBuilder)
        {
            var baseClassConstructors =
                typeof(WebApiProxyControllerBase).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var baseClassConstructor in baseClassConstructors)
            {
                var parameters = baseClassConstructor.GetParameters();
                var constructorBuilder = typeBuilder.DefineConstructor(
                    MethodAttributes.Public |
                    MethodAttributes.SpecialName |
                    MethodAttributes.RTSpecialName |
                    MethodAttributes.HideBySig,
                    CallingConventions.HasThis,
                    parameters.Select(info => info.ParameterType).ToArray()
                );

                var ilGenerator = constructorBuilder.GetILGenerator();

                // Load `this` into stack
                ilGenerator.Emit(OpCodes.Ldarg_0);

                // Load arguments into stack
                for (var i = 0; i < parameters.Length; i++)
                {
                    // Loads argument value into stack
                    ilGenerator.Emit(OpCodes.Ldarg, i + 1);
                }

                // Call base constructor
                ilGenerator.Emit(OpCodes.Call, baseClassConstructor);

                // Return
                ilGenerator.Emit(OpCodes.Ret);
            }
        }

        private static void CreateMethod(TypeBuilder typeBuilder, WebApiControllerActionDescription action)
        {
            var invokeMethod = typeof(WebApiProxyControllerBase).GetMethod(nameof(WebApiProxyControllerBase.Invoke),
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (invokeMethod == null)
            {
                return;
            }

            var methodBuilder = typeBuilder.DefineMethod(
                action.Name,
                MethodAttributes.Public | MethodAttributes.HideBySig,
                typeof(IHttpActionResult),
                action.Parameters.Select(info => info.Type).ToArray());

            switch (action.RequestAttributes?.Method)
            {
                case WebApiRequestMethod.Ignore:
                    methodBuilder.SetCustomAttribute(GetAttributeBuilder(typeof(NonActionAttribute)));

                    break;
                case WebApiRequestMethod.Post:
                    methodBuilder.SetCustomAttribute(GetAttributeBuilder(typeof(HttpPostAttribute)));

                    break;
                case WebApiRequestMethod.Get:
                    methodBuilder.SetCustomAttribute(GetAttributeBuilder(typeof(HttpGetAttribute)));

                    break;
                case WebApiRequestMethod.Head:
                    methodBuilder.SetCustomAttribute(GetAttributeBuilder(typeof(HttpHeadAttribute)));

                    break;
                case WebApiRequestMethod.Put:
                    methodBuilder.SetCustomAttribute(GetAttributeBuilder(typeof(HttpPutAttribute)));

                    break;
                case WebApiRequestMethod.Delete:
                    methodBuilder.SetCustomAttribute(GetAttributeBuilder(typeof(HttpDeleteAttribute)));

                    break;
                case WebApiRequestMethod.Patch:
                    methodBuilder.SetCustomAttribute(GetAttributeBuilder(typeof(HttpPatchAttribute)));

                    break;
                case WebApiRequestMethod.Options:
                    methodBuilder.SetCustomAttribute(GetAttributeBuilder(typeof(HttpOptionsAttribute)));

                    break;
            }

            if (action.RequestAttributes?.RouteTemplate != null)
            {
                methodBuilder.SetCustomAttribute(GetAttributeBuilder(typeof(RouteAttribute),
                    action.RequestAttributes.RouteTemplate));
            }

            foreach (var response in action.ResponseAttributes)
            {
                methodBuilder.SetCustomAttribute(GetAttributeBuilder(typeof(SwaggerResponseAttribute),
                    response.ResponseCode, response.ResponseType ?? typeof(void)));
            }

            if (!string.IsNullOrWhiteSpace(action.Description))
            {
                methodBuilder.SetCustomAttribute(GetAttributeBuilder(typeof(DescriptionAttribute), action.Description));
            }

            for (var i = 0; i < action.Parameters.Length; i++)
            {
                var parameterBuilder =
                    methodBuilder.DefineParameter(i + 1, action.Parameters[i].ParameterInfo.Attributes,
                        action.Parameters[i].Name);

                parameterBuilder.SetCustomAttribute(
                    action.Parameters[i].IsFromBody
                        ? GetAttributeBuilder(typeof(FromBodyAttribute))
                        : GetAttributeBuilder(typeof(FromUriAttribute)));

                if (!string.IsNullOrWhiteSpace(action.Parameters[i].Description))
                {
                    parameterBuilder.SetCustomAttribute(GetAttributeBuilder(typeof(DescriptionAttribute),
                        action.Parameters[i].Description));
                }
            }

            var ilGenerator = methodBuilder.GetILGenerator();

            // Load `this` into stack
            ilGenerator.Emit(OpCodes.Ldarg_0);

            // Load method name into stack
            ilGenerator.Emit(OpCodes.Ldstr, action.Name);

            // Loads argument array length into stack
            ilGenerator.Emit(OpCodes.Ldc_I4, action.Parameters.Length);

            // Creates and put reference of the argument array into stack
            ilGenerator.Emit(OpCodes.Newarr, typeof(object));

            // Fill the array
            for (var i = 0; i < action.Parameters.Length; i++)
            {
                ilGenerator.Emit(OpCodes.Dup);

                // Load array's index into stack
                ilGenerator.Emit(OpCodes.Ldc_I4, i);

                // Loads argument value into stack
                ilGenerator.Emit(OpCodes.Ldarg, i + 1);

                // Box value types
                if (action.Parameters[i].Type.IsPrimitive)
                {
                    ilGenerator.Emit(OpCodes.Box, action.Parameters[i].Type);
                }

                // Copy to array
                ilGenerator.Emit(OpCodes.Stelem_Ref);
            }

            // Call base class `Invoke`
            ilGenerator.EmitCall(OpCodes.Call, invokeMethod, null);

            // Return
            ilGenerator.Emit(OpCodes.Ret);
        }

        private static CustomAttributeBuilder GetAttributeBuilder(Type attributeType, params object[] parameters)
        {
            var constructor = attributeType.GetConstructor(parameters.Select(o => o.GetType()).ToArray());

            if (constructor == null)
            {
                return null;
            }

            return new CustomAttributeBuilder(constructor, parameters);
        }
    }
}