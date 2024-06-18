using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RainbowMage.OverlayPlugin
{
    /// <summary>
    /// Use this interface to specify that this class should be registered early in the
    /// before the initialization process
    /// 
    /// TinyIoC Auto Register and Auto Construct helpers cannot be used in the HtmlRenderer assembly
    /// due to its reliance on CefSharp
    /// </summary>
    /// <typeparam name="T">The type that should be registered for IoC resolution</typeparam>
    public interface ITinyIoCAutoRegisterPreInit<T> { }

    /// <summary>
    /// Use this interface to specify that this class should be registered early in the
    /// plugin initialization process, before actually initializing anything else
    /// 
    /// TinyIoC Auto Register and Auto Construct helpers cannot be used in the HtmlRenderer assembly
    /// due to its reliance on CefSharp
    /// </summary>
    /// <typeparam name="T">The type that should be registered for IoC resolution</typeparam>
    public interface ITinyIoCAutoRegisterBeforeInit<T> { }

    /// <summary>
    /// Use this interface to specify that this class should be registered during the
    /// plugin initialization process
    /// 
    /// TinyIoC Auto Register and Auto Construct helpers cannot be used in the HtmlRenderer assembly
    /// due to its reliance on CefSharp
    /// </summary>
    /// <typeparam name="T">The type that should be registered for IoC resolution</typeparam>
    public interface ITinyIoCAutoRegisterDuringInit<T> { }

    /// <summary>
    /// Use this interface to specify that this class should be registered after all other plugin
    /// types and GUI elements are initialized
    /// 
    /// TinyIoC Auto Register and Auto Construct helpers cannot be used in the HtmlRenderer assembly
    /// due to its reliance on CefSharp
    /// </summary>
    /// <typeparam name="T">The type that should be registered for IoC resolution</typeparam>
    public interface ITinyIoCAutoRegisterAfterInit<T> { }

    /// <summary>
    /// Use this interface to specify that this class should be registered and immediately constructed
    /// before the plugin initialization process
    /// 
    /// Note that for auto constructed types, construction is done as a second pass after
    /// IoC entries are registered for the stage
    /// 
    /// TinyIoC Auto Register and Auto Construct helpers cannot be used in the HtmlRenderer assembly
    /// due to its reliance on CefSharp
    /// </summary>
    /// <typeparam name="T">The type that should be registered for IoC resolution</typeparam>
    public interface ITinyIoCAutoConstructPreInit<T> : ITinyIoCAutoRegisterPreInit<T> { }

    /// <summary>
    /// Use this interface to specify that this class should be registered and immediately constructed
    /// early in the plugin initialization process, before actually initializing anything else
    /// 
    /// Note that for auto constructed types, construction is done as a second pass after
    /// IoC entries are registered for the stage
    /// 
    /// TinyIoC Auto Register and Auto Construct helpers cannot be used in the HtmlRenderer assembly
    /// due to its reliance on CefSharp
    /// </summary>
    /// <typeparam name="T">The type that should be registered for IoC resolution</typeparam>
    public interface ITinyIoCAutoConstructBeforeInit<T> : ITinyIoCAutoRegisterPreInit<T> { }

    /// <summary>
    /// Use this interface to specify that this class should be registered and immediately constructed
    /// during the plugin initialization process
    /// 
    /// Note that for auto constructed types, construction is done as a second pass after
    /// IoC entries are registered for the stage
    /// 
    /// TinyIoC Auto Register and Auto Construct helpers cannot be used in the HtmlRenderer assembly
    /// due to its reliance on CefSharp
    /// </summary>
    /// <typeparam name="T">The type that should be registered for IoC resolution</typeparam>
    public interface ITinyIoCAutoConstructDuringInit<T> : ITinyIoCAutoRegisterDuringInit<T> { }

    /// <summary>
    /// Use this interface to specify that this class should be registered and immediately constructed
    /// after all other plugin types and GUI elements are initialized
    /// 
    /// Note that for auto constructed types, construction is done as a second pass after
    /// IoC entries are registered for the stage
    /// 
    /// TinyIoC Auto Register and Auto Construct helpers cannot be used in the HtmlRenderer assembly
    /// due to its reliance on CefSharp
    /// </summary>
    /// <typeparam name="T">The type that should be registered for IoC resolution</typeparam>
    public interface ITinyIoCAutoConstructAfterInit<T> : ITinyIoCAutoRegisterAfterInit<T> { }

    public static class TinyIoCAutoHelper
    {
        private static List<Assembly> Assemblies = new List<Assembly>();

        public static void RegisterAssemblies(List<Assembly> assemblies)
        {
            Assemblies.AddRange(assemblies.Where(a => !Assemblies.Contains(a)));
        }

        public static void AutoRegisterPreInit()
        {
            AutoRegister(typeof(ITinyIoCAutoRegisterPreInit<>));
        }

        public static void AutoRegisterBeforeInit()
        {
            AutoRegister(typeof(ITinyIoCAutoRegisterBeforeInit<>));
        }

        public static void AutoRegisterDuringInit()
        {
            AutoRegister(typeof(ITinyIoCAutoRegisterDuringInit<>));
        }

        public static void AutoRegisterAfterInit()
        {
            AutoRegister(typeof(ITinyIoCAutoRegisterAfterInit<>));
        }

        public static void AutoRegister(Type t)
        {
            var container = TinyIoCContainer.Current;
            foreach (var asm in Assemblies)
            {
                var types = asm.SafeGetTypes().Where(type => type.IsClass && type.GetInterfaces().Any(type2 => type2.IsGenericType && type2.GetGenericTypeDefinition() == t));

                foreach (var type in types)
                {
                    var interfaces = type.GetInterfaces().Where(type2 => type2.IsGenericType && type2.GetGenericTypeDefinition() == t).ToList();
                    var registerType = interfaces[0].GenericTypeArguments[0];
                    var registerImplementation = type;
                    if (!container.CanResolve(registerType))
                    {
                        TinyIoCContainer.Current.Register(registerType, registerImplementation).AsSingleton();
                    }
                }
            }
        }
        public static void AutoConstructPreInit()
        {
            AutoConstruct(typeof(ITinyIoCAutoConstructPreInit<>));
        }


        public static void AutoConstructBeforeInit()
        {
            AutoConstruct(typeof(ITinyIoCAutoConstructBeforeInit<>));
        }

        public static void AutoConstructDuringInit()
        {
            AutoConstruct(typeof(ITinyIoCAutoConstructDuringInit<>));
        }

        public static void AutoConstructAfterInit()
        {
            AutoConstruct(typeof(ITinyIoCAutoConstructAfterInit<>));
        }

        public static void AutoConstruct(Type t)
        {
            foreach (var asm in Assemblies)
            {
                var types = asm.SafeGetTypes().Where(type => type.IsClass && type.GetInterfaces().Any(type2 => type2.IsGenericType && type2.GetGenericTypeDefinition() == t));

                foreach (var type in types)
                {
                    // We know that the type is already registered, so we just need to resolve it and let TinyIoC handle the construction itself
                    var interfaces = type.GetInterfaces().Where(type2 => type2.IsGenericType && type2.GetGenericTypeDefinition() == t).ToList();
                    var registerType = interfaces[0].GenericTypeArguments[0];
                    TinyIoCContainer.Current.Resolve(registerType);
                }
            }
        }
    }
}
