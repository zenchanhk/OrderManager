using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using AmiBroker.OrderManager;
using System.Reflection;

namespace AmiBroker.Controllers
{   

    public class KnownTypesBinder : ISerializationBinder
    {
        public IList<Type> KnownTypes { get; set; }

        public Type BindToType(string assemblyName, string typeName)
        {
            return KnownTypes.SingleOrDefault(t => t.Name == typeName);
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.Name;
        }
    }
    public class SettingsSaveResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty prop = base.CreateProperty(member, memberSerialization);

            if (((prop.DeclaringType == typeof(IBController) ||
                prop.DeclaringType == typeof(FTController) ||
                prop.DeclaringType == typeof(IController)) &&
                prop.PropertyName != "DisplayName")
                ||
                (prop.DeclaringType == typeof(AccountInfo) &&
                prop.PropertyName != "Name" && prop.PropertyName != "Controller")
                ||
                (prop.DeclaringType != typeof(Script) &&
                prop.DeclaringType != typeof(Strategy) &&
                prop.DeclaringType != typeof(SymbolInAction) &&
                prop.DeclaringType != typeof(SymbolDefinition) &&
                prop.PropertyType != typeof(GoodTime) &&
                prop.PropertyType != typeof(TimeZone) &&
                prop.PropertyType.IsClass &&
                !prop.PropertyType.FullName.StartsWith("System")))
            {
                prop.ShouldSerialize = obj => false;
            }

            return prop;
        }
    }
    public class JSONConstants
    {
        static JSONConstants()
        {
            var types = typeof(BaseOrderType).Assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(BaseOrderType)));
            foreach (var t in types)
            {
                knownTypesBinder.KnownTypes.Add(t);
            }            
        }
        static KnownTypesBinder knownTypesBinder = new KnownTypesBinder
        {
            KnownTypes = new List<Type> { typeof(IBController), typeof(FTController), typeof(Script),
            typeof(SymbolInAction), typeof(Strategy), typeof(SymbolDefinition), typeof(ConnectionParam),
            typeof(AccountInfo), typeof(BaseOrderType), typeof(GoodTime), typeof(TimeZone)}
        };

        public static JsonSerializerSettings saveSerializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            ContractResolver = new SettingsSaveResolver(),
            SerializationBinder = knownTypesBinder
        };

        public static JsonSerializerSettings displaySerializerSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new SettingsSaveResolver(),
            SerializationBinder = knownTypesBinder
        };
    }
}
