using System.IO;

using YamlDotNet.Serialization;

namespace EmpyrionModdingFramework
{
  public class ConfigManager
  {
    public ConfigClass Config = new ConfigClass();
    private readonly object documentLock = new object();

    public void LoadConfiguration<T>(StreamReader document, out T refConfig)
    {
      try
      {
          IDeserializer deserializer = new DeserializerBuilder()
              .IgnoreUnmatchedProperties()
              .Build();

          refConfig = deserializer.Deserialize<T>(document);
      }
      catch
      {
          throw;
      }
    }

    public void SaveConfiguration<T>(StreamWriter document, T config)
    {
      try
      {
          ISerializer serializer = new SerializerBuilder().Build();
          lock (documentLock)
          {
              serializer.Serialize(document, config);
          }
      }
      catch
      {
          throw;
      }
    }

    public class ConfigClass
    {
      public class ModBaseConfigClass
      {
        public string LogPrefix { get; set; }
        public string DedicatedYaml { get; set; }
      }

      public class DedicatedConfigClass
      {
        public string ConfigFileName { get; set; }
        public string SaveGameName { get; set; }
      }

      public ModBaseConfigClass ModBaseConfig { get; set; }
      public DedicatedConfigClass DedicatedConfig { get; set; }
    }
  }
}