﻿//  This file is part of YamlDotNet - A .NET library for YAML.
//  Copyright (c) Antoine Aubry and contributors

//  Permission is hereby granted, free of charge, to any person obtaining a copy of
//  this software and associated documentation files (the "Software"), to deal in
//  the Software without restriction, including without limitation the rights to
//  use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
//  of the Software, and to permit persons to whom the Software is furnished to do
//  so, subject to the following conditions:

//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.

//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using YamlDotNet.Core;
using YamlDotNet.Helpers;
using YamlHash = YamlDotNet.Core.HashCode;

namespace YamlDotNet.Serialization
{
    /// <summary>
    /// Define a collection of YamlAttribute Overrides for pre-defined object types.
    /// </summary>
    public sealed class YamlAttributeOverrides
    {
        private struct AttributeKey
        {
            public readonly Type AttributeType;
            public readonly string PropertyName;

            public AttributeKey(Type attributeType, string propertyName)
            {
                AttributeType = attributeType;
                PropertyName = propertyName;
            }

            public override bool Equals(object obj)
            {
                var other = (AttributeKey)obj;
                return AttributeType.Equals(other.AttributeType)
                    && PropertyName.Equals(other.PropertyName);
            }

            public override int GetHashCode()
            {
                return YamlHash.CombineHashCodes(AttributeType.GetHashCode(), PropertyName.GetHashCode());
            }
        }

        private sealed class AttributeMapping
        {
            public readonly Type RegisteredType;
            public readonly Attribute Attribute;

            public AttributeMapping(Type registeredType, Attribute attribute)
            {
                this.RegisteredType = registeredType;
                Attribute = attribute;
            }

            public override bool Equals(object obj)
            {
                var other = obj as AttributeMapping;
                return other != null
                    && RegisteredType.Equals(other.RegisteredType)
                    && Attribute.Equals(other.Attribute);
            }

            public override int GetHashCode()
            {
                return YamlHash.CombineHashCodes(RegisteredType.GetHashCode(), Attribute.GetHashCode());
            }

            /// <summary>
            /// Checks whether this mapping matches the specified type, and returns a value indicating the match priority.
            /// </summary>
            /// <returns>The priority of the match. Higher values have more priority. Zero indicates no match.</returns>
            public int Matches(Type matchType)
            {
                var currentPriority = 0;
                var currentType = matchType;
                while (currentType != null)
                {
                    ++currentPriority;
                    if (currentType == RegisteredType)
                    {
                        return currentPriority;
                    }
                    currentType = currentType.BaseType();
                }

                if (matchType.GetInterfaces().Contains(RegisteredType))
                {
                    return currentPriority;
                }

                return 0;
            }
        }

        private readonly Dictionary<AttributeKey, List<AttributeMapping>> overrides = new Dictionary<AttributeKey, List<AttributeMapping>>();

        public T GetAttribute<T>(Type type, string member) where T : Attribute
        {
            List<AttributeMapping> mappings;
            if (overrides.TryGetValue(new AttributeKey(typeof(T), member), out mappings))
            {
                int bestMatchPriority = 0;
                AttributeMapping bestMatch = null;

                foreach (var mapping in mappings)
                {
                    var priority = mapping.Matches(type);
                    if (priority > bestMatchPriority)
                    {
                        bestMatchPriority = priority;
                        bestMatch = mapping;
                    }
                }

                if (bestMatchPriority > 0)
                {
                    return (T)bestMatch.Attribute;
                }
            }

            return null;
        }

        /// <summary>
        /// Adds a Member Attribute Override
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="member">Class Member</param>
        /// <param name="attribute">Overriding Attribute</param>
        public void Add(Type type, string member, Attribute attribute)
        {
            var mapping = new AttributeMapping(type, attribute);

            List<AttributeMapping> mappings;
            var attributeKey = new AttributeKey(attribute.GetType(), member);
            if (!overrides.TryGetValue(attributeKey, out mappings))
            {
                mappings = new List<AttributeMapping>();
                overrides.Add(attributeKey, mappings);
            }
            else if (mappings.Contains(mapping))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Attribute ({2}) already set for Type {0}, Member {1}", type.FullName, member, attribute));
            }

            mappings.Add(mapping);
        }

        /// <summary>
        /// Adds a Member Attribute Override
        /// </summary>
        public void Add<TClass>(Expression<Func<TClass, object>> propertyAccessor, Attribute attribute)
        {
            var property = propertyAccessor.AsProperty();
            Add(typeof(TClass), property.Name, attribute);
        }

        /// <summary>
        /// Creates a copy of this instance.
        /// </summary>
        public YamlAttributeOverrides Clone()
        {
            var clone = new YamlAttributeOverrides();
            foreach (var entry in overrides)
            {
                foreach (var item in entry.Value)
                {
                    clone.Add(item.RegisteredType, entry.Key.PropertyName, item.Attribute);
                }
            }
            return clone;
        }
    }
}
