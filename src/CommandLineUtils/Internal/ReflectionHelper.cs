﻿// Copyright (c) Nate McMaster.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils.Abstractions;

namespace McMaster.Extensions.CommandLineUtils
{
    internal class ReflectionHelper
    {
        public static SetPropertyDelegate GetPropertySetter(PropertyInfo prop)
        {
            var setter = prop.GetSetMethod(nonPublic: true);
            if (setter != null)
            {
                return (obj, value) => setter.Invoke(obj, new object[] { value });
            }
            else
            {
                var backingFieldName = string.Format("<{0}>k__BackingField", prop.Name);
                var backingField = prop.DeclaringType.GetTypeInfo().GetDeclaredField(backingFieldName);
                if (backingField == null)
                {
                    throw new InvalidOperationException(
                        $"Could not find a way to set {prop.DeclaringType.FullName}.{prop.Name}");
                }

                return (obj, value) => backingField.SetValue(obj, value);
            }
        }

        public static object[] BindParameters(MethodInfo method, CommandLineContext context, BindResult bindResult)
        {
            var methodParams = method.GetParameters();
            var arguments = new object[methodParams.Length];

            for (var i = 0; i < methodParams.Length; i++)
            {
                var methodParam = methodParams[i];

                if (typeof(CommandLineApplication).GetTypeInfo().IsAssignableFrom(methodParam.ParameterType))
                {
                    arguments[i] = bindResult.Command;
                }
                else if (typeof(IConsole).GetTypeInfo().IsAssignableFrom(methodParam.ParameterType))
                {
                    arguments[i] = context.Console;
                }
                else if (typeof(ValidationResult).GetTypeInfo().IsAssignableFrom(methodParam.ParameterType))
                {
                    arguments[i] = bindResult.ValidationResult;
                }
                else if (typeof(CommandLineContext).GetTypeInfo().IsAssignableFrom(methodParam.ParameterType))
                {
                    arguments[i] = context;
                }
                else
                {
                    throw new InvalidOperationException(Strings.UnsupportedParameterTypeOnMethod(method.Name, methodParam));
                }
            }

            return arguments;
        }
    }
}
