using System.IO;

using YamlDotNet.Serialization;

namespace EmpyrionModdingFramework
{
  public class ConfigManager
  {
    private readonly object documentLock = new object();

    public T LoadConfiguration<T>(StreamReader document)
    {
      try
      {
          IDeserializer deserializer = new DeserializerBuilder()
              .IgnoreUnmatchedProperties()
              .Build();

          return deserializer.Deserialize<T>(document);
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
  }
}