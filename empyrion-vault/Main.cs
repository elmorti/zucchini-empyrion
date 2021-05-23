using System.IO;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Text;

using Eleon;
using Eleon.Modding;

using EmpyrionModdingFramework;

namespace Empyrion_Vault
{
  public class Main : EmpyrionModdingFrameworkBase
  {
    protected override void Initialize()
    {
      CommandManager.CommandList.Add(new ChatCommand($"vault_help", (I) => VaultHelpCommand(I)));
      CommandManager.CommandList.Add(new ChatCommand($"vault_new", (I) => VaultNewCommand(I)));
      CommandManager.CommandList.Add(new ChatCommand($"vault_list", (I) => VaultListCommand(I)));
      CommandManager.CommandList.Add(new ChatCommand($"vault_open", (I) => VaultOpenCommand(I)));
    }

    /*
     * Vault Help
     */

    private async Task VaultHelpCommand(MessageData data)
    {
      await Task.Run(() => Log($"VaultHelpCommand called by {data.SenderEntityId} with: {data.Text} "));
      await VaultHelpDialog(data.SenderEntityId);
    }

    private async Task VaultHelpDialog(int playerId)
    {
      DialogConfig dialogConfig = ConfigManager.DeserializeYaml<DialogConfig>(
        File.OpenText(ModAPI.Application.GetPathFor(AppFolder.Mod) + @"\" + $"{ModName}" + @"\dialogs\help.yaml"));

      dialogConfig.BodyText = dialogConfig.BodyText.Replace("%%PLAYER%%", playerId.ToString());
      await Task.Factory.StartNew(() =>
        ModAPI.Application.ShowDialogBox(playerId, dialogConfig, VaultHelpDialogHandler, 0)
      );
    }

    private async void VaultHelpDialogHandler(int buttonIdx, string linkId, string inputContent, int playerId, int customValue)
    {
      switch (linkId)
      {
        case "vault_new":
          await VaultNewDialog(playerId);
          return;
        case "vault_list":
          await Task.Run(
            () => Log($"Player {playerId} clicked {linkId}")
            );
          await Helpers.SendFeedbackMessage("You pressed vault_list.", playerId);
          // await VaultListDialog(playerId);
          return;
        default:
          break;
      }
    }

    /*
     * Vault New
     */

    private async Task VaultNewCommand(MessageData data)
    {
      await Task.Run(() => Log($"VaultNewCommand called by {data.SenderEntityId} with: {data.Text} "));
      await VaultNewDialog(data.SenderEntityId);
    }

    private async Task VaultNewDialog(int playerId)
    {
      DialogConfig dialogConfig = ConfigManager.DeserializeYaml<DialogConfig>(
        File.OpenText(ModAPI.Application.GetPathFor(AppFolder.Mod) + @"\" + $"{ModName}" + @"\dialogs\new.yaml"));

      dialogConfig.BodyText = dialogConfig.BodyText.Replace("%%PLAYER%%", playerId.ToString());
      await Task.Factory.StartNew(() =>
        ModAPI.Application.ShowDialogBox(playerId, dialogConfig, VaultNewDialogHandler, 0)
      );
    }

    // Async Void is correct?
    private async void VaultNewDialogHandler(int buttonIdx, string linkId, string inputContent, int playerId, int customValue)
    {
      var player = await Helpers.GetPlayerInfo(playerId);
      var playerVaultPath = ModAPI.Application.GetPathFor(AppFolder.SaveGame) + @"\Mods\" + $"{ModName}" + @"\Players\" + $"{player.steamId}";

      if (Directory.Exists(playerVaultPath))
      {
        try
        {
          // Catch file existing
          using (StreamWriter writer = File.CreateText(playerVaultPath + $"\\{inputContent}.yaml"))
          {
            ConfigManager.SerializeYaml<Vault>(writer, new Vault()); // Incoorect creation of object needs constructor or initialization.
          }
        }
        catch
        {
          throw;
        }
        await Helpers.SendFeedbackMessage($"Created vault {inputContent}", playerId);
        return;
      }
      await Helpers.SendFeedbackMessage($"Player directory not found", playerId);
    }

    /*
     * Vault List
     */

    private async Task VaultListCommand(MessageData data)
    {
      var player = await Helpers.GetPlayerInfo(data.SenderEntityId);

      var playerVaultPath = ModAPI.Application.GetPathFor(AppFolder.SaveGame) + @"\Mods\" + $"{ModName}" + @"\Players\" + $"{player.steamId}";
      var factionVaultPath = ModAPI.Application.GetPathFor(AppFolder.SaveGame) + @"\Mods\" + $"{ModName}" + @"\Factions\" + $"{data.SenderFaction}";

      var playerVaults = new List<Vault>();
      var factionVaults = new List<Vault>();

      if (Directory.Exists(playerVaultPath))
      {
        foreach (var file in Directory.GetFiles(playerVaultPath))
        {
          try
          {
            playerVaults.Add(ConfigManager.DeserializeYaml<Vault>(File.OpenText(file)));
          }
          catch
          {
            throw;
          }
          
        }
      }

      if (Directory.Exists(factionVaultPath))
      {
        foreach (var file in Directory.GetFiles(factionVaultPath))
        {
          try
          {
            factionVaults.Add(ConfigManager.DeserializeYaml<Vault>(File.OpenText(file)));
          }
          catch
          {
            throw;
          }

        }
      }

      if (playerVaults.Count == 0) {
        await Helpers.SendFeedbackMessage("No personal vaults for you.", data.SenderEntityId);
        return;
      }

      StringBuilder message = new StringBuilder();
      message.AppendFormat("Hello {0}, you have {1} personal and {2} faction vault(s)\n", data.SenderEntityId, playerVaults.Count, factionVaults.Count);

      foreach (var vault in playerVaults)
      {
        message.AppendFormat("Vault name: {0} has {1} item(s)\n", vault.Name, vault.Items.Count);
      }
      await Helpers.SendFeedbackMessage(message.ToString(), data.SenderEntityId);

      await Task.Run(
        () => Log($"VaultListCommand called by {data.SenderEntityId} with: {data.Text} and Faction: {data.SenderFaction}")
      );

      //await VaultListDialog(data.SenderEntityId);
    }

    // TODO: In general cancellation tokens!
    private async Task VaultListDialog(int playerId)
    {
      DialogConfig dialogConfig = ConfigManager.DeserializeYaml<DialogConfig>(
        File.OpenText(ModAPI.Application.GetPathFor(AppFolder.Mod) + @"\" + $"{ModName}" + @"\dialogs\list.yaml"));

      dialogConfig.BodyText = dialogConfig.BodyText.Replace("%%PLAYER%%", playerId.ToString());
      await Task.Factory.StartNew(() => // Task.Run is "shortcut"
        ModAPI.Application.ShowDialogBox(playerId, dialogConfig, null, 0)
      );
    }

    private async Task VaultListDialogHandler(int buttonIdx, string linkId, string inputContent, int playerId, int customValue)
    {
      await Task.Run(() => Log("Not implemented"));
    }

    /*
     * Vault Open
     */

    private async Task VaultOpenCommand(MessageData data)
    {
      var command = data.Text.Split(' ');
      if (command.Length == 1)
      {
        await Helpers.SendFeedbackMessage("You need arguments.", data.SenderEntityId);
        return;
      }

      var player = await Helpers.GetPlayerInfo(data.SenderEntityId);
      var playerVaultPath = ModAPI.Application.GetPathFor(AppFolder.SaveGame) + @"\Mods\" + $"{ModName}" + @"\Players\" + $"{player.steamId}";
      try
      {
        var vault = new Vault();
        using (StreamReader reader = File.OpenText(playerVaultPath + $"\\{command[1]}.yaml"))
        {
          vault = ConfigManager.DeserializeYaml<Vault>(reader);
        }
        var itemExchange = new ItemExchangeInfo()
        {
          id = data.SenderEntityId,
          buttonText = "KAKA",
          desc = "Description",
          title = "Title",
          items = vault.Items.ToArray()
        };
        vault.Items.Clear();
        using (StreamWriter writer = File.CreateText(playerVaultPath + $"\\{command[1]}.yaml"))
        {
          ConfigManager.SerializeYaml<Vault>(writer, vault);
        }

        var result = (ItemExchangeInfo) await RequestManager.SendGameRequest(CmdId.Request_Player_ItemExchange, itemExchange);
        vault.Items = new List<ItemStack>(result.items);
        using (StreamWriter writer = File.CreateText(playerVaultPath + $"\\{command[1]}.yaml"))
        {
          ConfigManager.SerializeYaml<Vault>(writer, vault);
        }
        StringBuilder message = new StringBuilder();
        message.AppendFormat("Property: {0} has value {1}\n", "id", result.id);
        message.AppendFormat("Property: {0} has value {1}\n", "buttonText", result.buttonText);
        message.AppendFormat("Property: {0} has value {1}\n", "desc", result.desc);
        message.AppendFormat("Property: {0} has value {1}\n", "title", result.title);
        message.AppendFormat("Property: {0} has value {1}\n", "items", result.items);
        foreach (var i in result.items)
        {
          message.AppendFormat("Item {0} count {1}\n", i.id, i.count);
        }
        await Helpers.SendFeedbackMessage(message.ToString(), data.SenderEntityId);
      }
      catch
      {
        throw;
      }
    }
  }
}