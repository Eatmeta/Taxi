using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Ddd.Infrastructure
{
    /// <summary>
    /// Базовый класс для всех Value типов.
    /// </summary>
    public class ValueType<T> where T : class
    {
        private readonly PropertyInfo[] _properties;

        protected ValueType()
        {
            _properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        }

        public bool Equals(T other)
        {
            if (other == null || ReferenceEquals(this, other)) return false;

            var otherProperties = other
                .GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            for (var i = 0; i < _properties.Length; i++)
            {
                var thisPropertieValue = _properties[i].GetValue(this, null) as dynamic;
                var otherPropertieValue = otherProperties[i].GetValue(other, null) as dynamic;

                if (thisPropertieValue != null)
                {
                    if (!thisPropertieValue.Equals(otherPropertieValue)) return false;
                }
                else if (otherPropertieValue != null) return false;
            }
            return true;
        }

        public new bool Equals(object other)
        {
            return other is T ? Equals((T) other) : false;
        }

        public override int GetHashCode()
        {
            var result = 0;
            var some = 1113;
            foreach (var property in _properties)
            {
                var value = property.GetValue(this, null);
                if (value == null) continue;
                unchecked
                {
                    result += value.GetHashCode() * some;
                }
                some += 57;
            }
            return result;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            var propertiesAsString = new List<string>();
            builder.Append(GetType().Name);
            builder.Append("(");
            foreach (var property in _properties)
            {
                var value = property.GetValue(this, null);

                if (value == null)
                    propertiesAsString.Add(property.Name + ":" + " ");
                else propertiesAsString.Add(property.Name + ": " + property.GetValue(this, null));
            }
            propertiesAsString.Sort();

            for (var i = 0; i < propertiesAsString.Count; i++)
            {
                builder.Append(propertiesAsString[i]);
                if (i != propertiesAsString.Count - 1)
                    builder.Append("; ");
            }
            builder.Append(")");

            return builder.ToString();
        }
    }
}