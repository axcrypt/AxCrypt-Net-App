using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace AxCrypt.Api.Implementation
{
    /// <summary>
    /// Ignore the Json Property attribute. This is usefull when you want to serialize or deserialize differently and not
    /// let the JsonProperty control everything.
    /// </summary>
    public class IgnoreJsonPropertyResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);
            foreach (var p in properties) { p.PropertyName = p.UnderlyingName; }
            return properties;
        }
    }
}