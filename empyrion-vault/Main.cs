using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

using Eleon;
using Eleon.Modding;

using EmpyrionModdingFramework;

namespace Empyrion_Vault
{
  public class Main : EmpyrionModdingFrameworkBase
  {
    public Config Configuration;
    public string GameModConfigPath;

    protected override void Initialize()
    {
      CommandManager.CommandList.Add(new ChatCommand($"vault_help", (I) => VaultHelp(I)));

      LoadConfiguration();
    }

    // This should be moved to modding framwework
    public void LoadConfiguration()
    {
      if (!Directory.Exists(GameModConfigPath)) {
        try
        {
          foreach (string path in new string[] { "Players", "Factions" })
          {
            Directory.CreateDirectory(GameModConfigPath + @"\" + $"{path}");
          }

          Configuration.SenderNameOverride = "Vault";
          using (StreamWriter writer = File.CreateText(GameModConfigPath + @"\" + FrameworkConfig.ConfigFileName))
          {
            ConfigManager.SerializeYaml(writer, Configuration);
          }
        }
        catch (Exception error)
        {
          Log($"there was a problem creating the config file: {error.GetType()}");
          throw;
        }
      }

      try
      {
        using (StreamReader reader = File.OpenText(GameModConfigPath + @"\" + FrameworkConfig.ConfigFileName))
        {
          Configuration = ConfigManager.DeserializeYaml<Config>(reader);
        }
      }
      catch (Exception error)
      {
        Log($"there was a problem reading the mod save config file: {error.GetType()}");
        throw;
      }
    }

    private async Task _LoadFileFromYaml(string Path)
    {
      try
      {
        using (StreamReader reader = File.OpenText(path)
        {
          return ConfigManager.DeserializeYaml<T>(reader);
        }
      }
      catch (Exception error)
      {
        Log($"error opening the dialog config");
        Log($"{error.Message}");
      }
    }

    /// <summary>
    ///  VaultHelpCommadn
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>

    //
    // VAULT HELP
    // 

    // Delegate for CommandManager
    private async Task VaultHelpCommand(MessageData data)
    {
      Log($"VaultHelpCommand called by {data.SenderEntityId} with: {data.Text} ");
      await VaultHelpDialog(data);
    }

    private async Task VaultHelpDialog(MessageData data)
    {
      DialogConfig dialogConfig = _LoadFileFromYaml("adasd");
      dialogConfig.BodyText = dialogConfig.BodyText.Replace("%%PLAYER%%", data.SenderEntityId.ToString());
      await Task.Factory.StartNew(() =>
        ModAPI.Application.ShowDialogBox(data.SenderEntityId, dialogConfig, VaultHelpDialogHandler, 0)
      );
    }

    private async void VaultHelpDialogHandler(int buttonIdx, string linkId, string inputContent, int playerId, int customValue)
    {
      switch (linkId)
      {
        case "vault_new":
          await VaultNew(playerId);
          return;
        case "vault_list":
          Log($"Player {playerId} clicked {linkId}");
          await VaultList(playerId);
          return;
        default:
          break;
      }
    }
  }
}