using System;
using System.IO;

using YamlDotNet.Serialization;

namespace EmpyrionModdingFramework
{
  public class ConfigManager
  {
    public string FilePath;
    public FrameworkConfig ModConfig { get; set; }
    private readonly object documentLock = new object();

    public ConfigManager(string filename, string pathname) // TODO: Change this to pass a stream object
    {
      try
      {
        using (StreamReader reader = File.OpenText(filename))
        {
          ModConfig = DeserializeYaml<FrameworkConfig>(reader);
        }
      }
      catch (Exception)
      {
        throw;
      }

      if (!Directory.Exists(pathname))
      {
        try
        {
          Directory.CreateDirectory(pathname);
          if (ModConfig.ModSubdirs != null)
          {
            foreach (string path in ModConfig.ModSubdirs)
            {
              Directory.CreateDirectory(pathname + @"\" + $"{path}");
            }
          }

          using (StreamWriter writer = File.CreateText(pathname + @"\" + ModConfig.ConfigFileName))
          { 
            SerializeYaml(writer, ModConfig);
          }
        }
        catch
        {
          throw;
        }
        try
        {
          using (StreamReader reader = File.OpenText(pathname + @"\" + ModConfig.ConfigFileName))
          {
            ModConfig = DeserializeYaml<FrameworkConfig>(reader);
          }
        }
        catch
        {
          throw;
        }
      }
    }

    public T DeserializeYaml<T>(StreamReader document)
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

    public void SerializeYaml<T>(StreamWriter document, T config)
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

    public class FrameworkConfig
    {
      public string LogPrefix { get; set; }
      public string ConfigFileName { get; set; }
      public string[] ModSubdirs { get; set; }
    }
  }
}