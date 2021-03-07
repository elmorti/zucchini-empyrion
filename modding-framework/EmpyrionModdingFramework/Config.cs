namespace EmpyrionModdingFramework
{
  public class FrameworkConfig
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
