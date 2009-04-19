using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using FizzWare.NBuilder.PropertyNaming;

namespace FizzWare.NBuilder.Implementation
{
    public class ObjectBuilder<T> : IObjectBuilder<T>
    {
        private readonly IReflectionUtil reflectionUtil;
        private IPropertyNamer propertyNamer;
        private object[] constructorArgs;

        private readonly List<MulticastDelegate> functions = new List<MulticastDelegate>();

        private readonly List<MultiFunction> multiFunctions = new List<MultiFunction>();

        public ObjectBuilder(IReflectionUtil reflectionUtil)
        {
            this.reflectionUtil = reflectionUtil;
        }

        public IObjectBuilder<T> WithConstructorArgs(params object[] args)
        {
            this.constructorArgs = args;
            return this;
        }

        public IObjectBuilder<T> With<TFunc>(Func<T, TFunc> func)
        {
            functions.Add(func);
            return this;
        }

        public IObjectBuilder<T> Do(Action<T> action)
        {
            functions.Add(action);
            return this;
        }

        public IObjectBuilder<T> DoMultiple<U>(Action<T, U> action, IList<U> list)
        {
            multiFunctions.Add(new MultiFunction(action, list));
            return this;
        }

        public IObjectBuilder<T> WithPropertyNamer(IPropertyNamer thePropertyNamer)
        {
            this.propertyNamer = thePropertyNamer;
            return this;
        }

        public T Build()
        {
            var obj = Construct();
            Name(obj);
            CallFunctions(obj);

            return obj;
        }

        public void CallFunctions(T obj)
        {
            for (int i = 0; i < functions.Count; i++)
            {
                var del = functions[i];
                del.DynamicInvoke(obj);
            }

            for (int i = 0; i < multiFunctions.Count; i++)
            {
                multiFunctions[i].Call(obj);
            }
        }

        public T Construct()
        {
            bool requiresArgs = reflectionUtil.RequiresConstructorArgs(typeof(T));

            T obj;

            if (requiresArgs && constructorArgs != null)
            {
                obj = reflectionUtil.CreateInstanceOf<T>(constructorArgs);
            }
            else if (constructorArgs != null)
            {
                obj = reflectionUtil.CreateInstanceOf<T>(constructorArgs);
            }
            else if (!requiresArgs)
            {
                obj = reflectionUtil.CreateInstanceOf<T>();
            }
            else
            {
                throw new TypeCreationException(
                    "Type does not have a default parameterless constructor but no constructor args were specified. Use WithConstructorArgs() method to supply some the required arguments.");
            }

            return obj;
        }

        public T Name(T obj)
        {
            if (!BuilderSetup.AutoNameProperties)
                return obj;

            propertyNamer.SetValuesOf(obj);
            return obj;
        }
    }
}