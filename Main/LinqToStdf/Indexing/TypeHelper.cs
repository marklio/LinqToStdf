// (c) Copyright Mark Miller.
// This source is subject to the Microsoft Public License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToStdf.Indexing
{

    /// <summary>
    /// Helper to find types for Linq stuff.  Most of this was yanked from System.Core code.
    /// </summary>
    internal static class TypeHelper
    {

        /// <summary>
        /// Finds a generic type
        /// </summary>
        /// <param name="definition">The open generic definition we're looking for.</param>
        /// <param name="type">The type we're inspecting</param>
        /// <returns></returns>
        internal static Type FindGenericType(Type definition, Type type)
        {
            //while the type isn't null and not object, keep looking up the type hierarchy
            while ((type != null) && (type != typeof(object)))
            {
                //If the type's generic definition is the one we're looking for, we found it
                if (type.IsGenericType && (type.GetGenericTypeDefinition() == definition))
                {
                    return type;
                }
                //if we're looking for an interface, check those too
                if (definition.IsInterface)
                {
                    foreach (Type type2 in type.GetInterfaces())
                    {
                        //recurse into the interfaces
                        Type innerType = FindGenericType(definition, type2);
                        if (innerType != null)
                        {
                            return innerType;
                        }
                    }
                }
                type = type.BaseType;
            }
            return null;
        }

        /// <summary>
        /// Attempts to find the "element type" of a query (T in IEnumerable of T)
        /// </summary>
        internal static Type GetElementType(Type enumerableType)
        {
            //Find the IEnumerable
            Type type = FindGenericType(typeof(IEnumerable<>), enumerableType);
            //if we found it, return the first generic argument
            if (type != null)
            {
                return type.GetGenericArguments()[0];
            }
            //otherwise, assume the type itself is the element type
            return enumerableType;
        }

        /// <summary>
        /// Strips off the nullable
        /// </summary>
        internal static Type GetNonNullableType(Type type)
        {
            if (IsNullableType(type))
            {
                return type.GetGenericArguments()[0];
            }
            return type;
        }

        internal static bool IsEnumerableType(Type enumerableType)
        {
            return (FindGenericType(typeof(IEnumerable<>), enumerableType) != null);
        }

        internal static bool IsKindOfGeneric(Type type, Type definition)
        {
            return (FindGenericType(definition, type) != null);
        }

        internal static bool IsNullableType(Type type)
        {
            return (((type != null) && type.IsGenericType) && (type.GetGenericTypeDefinition() == typeof(Nullable<>)));
        }
    }
}
