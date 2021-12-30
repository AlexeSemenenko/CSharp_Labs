using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using YamlDotNet.Serialization;

namespace MainProccess
{
    class Program
    {
        class Data
        {
            public List<string> modules;
            public List<List<string>> sequence;
        }

        private static void Main(string[] args)
        {
            var deps = new Deserializer().Deserialize<Data>(File.ReadAllText("../../../deps.yml"));
            
            foreach (var className in deps.modules)
            {
                var lib = Assembly.LoadFrom($"../../../../{className}/bin/Debug/net5.0/{className}.dll");
                var types = lib.GetTypes();
                
                foreach (var method in deps.sequence.SelectMany(methods => methods))
                {
                    types[0].GetMethod(method)?.Invoke(null, null);
                }
            }
        }
    }
}
