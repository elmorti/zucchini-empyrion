using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Eleon;
using Eleon.Modding;

using EmpyrionModdingFramework;

namespace Empyrion_Vault
{
  public class Main : EmpyrionModdingFrameworkBase
  {
    private bool Debug { get; set; }
    private string PlayerVaultPath { get; set; }
    private string FactionVaultPath { get; set; }

    protected override void Initialize()
    {
      Debug = true;
      PlayerVaultPath = ModAPI.Application.GetPathFor(AppFolder.SaveGame) + @"\Mods\" + $"{ModName}" + @"\Players\";
      FactionVaultPath = ModAPI.Application.GetPathFor(AppFolder.SaveGame) + @"\Mods\" + $"{ModName}" + @"\Factions\";
      CommandManager.CommandList.Add(new ChatCommand($"vault", (I) => VaultHelpCommand(I)));
    }

    // TODO: Needs to implement safe file sharing mechanism (i.e. for Faction Vaults)

    /*
     * Vault Help
     */

    private async Task VaultHelpCommand(MessageData data)
    {
      if (Debug)
      {
        await Task.Run(() => Log($"VaultHelpCommand called by {data.SenderEntityId} with: {data.Text} "));
      }

      var command = data.Text.Split(' ');
      if (command.Length == 1)
      {
        await VaultHelpDialog(data.SenderEntityId);
        return;
      }
      switch (command[1])
      {
        case "new":
          await VaultNewCommand(data);
          break;
        case "list":
          await VaultListCommand(data);
          break;
        case "open":
          await VaultOpenCommand(data);
          break;
        default:
          await VaultHelpDialog(data.SenderEntityId);
          break;
      }
    }

    private async Task VaultHelpDialog(int playerId)
    {
      StringBuilder sb = new StringBuilder();
      sb.Append("\nZucchini Vault:\n\n");
      sb.Append("@vault new [p|f] name\n");
      sb.Append("@vault list\n");
      sb.Append("@vault open [p|f] name\n\n");
      await Helpers.SendFeedbackMessage(sb.ToString(), playerId);
    }

    /*
     * Vault New
     */

    private async Task VaultNewCommand(MessageData data)
    {
      if (Debug)
      {
        await Task.Run(() => Log($"VaultNewCommand called by {data.SenderEntityId} with: {data.Text}."));
      }

      var command = data.Text.Split(' ');
      if (command.Length < 4)
      {
        await VaultNewDialog(data.SenderEntityId);
        return;
      }

      var player = await Helpers.GetPlayerInfo(data.SenderEntityId);
      switch (command[2])
      {
        case "p":
          Directory.CreateDirectory(PlayerVaultPath + $"{player.steamId}");
          var vaultFilePath = PlayerVaultPath + $"{player.steamId}" + $"\\{command[3]}.yaml";

          if (File.Exists(vaultFilePath))
          {
            await Helpers.SendFeedbackMessage($"A vault with the name {command[3]} already exists.", data.SenderEntityId);
            return;
          }
          using (StreamWriter writer = File.CreateText(vaultFilePath))
          {
            var vault = new Vault()
            {
              Id = System.Guid.NewGuid(),
              Name = command[3],
              Items = new List<ItemStack>()
            };
            ConfigManager.SerializeYaml<Vault>(writer, vault);
          }
          break;
        case "f":
          Directory.CreateDirectory(FactionVaultPath + $"{data.SenderFaction}");
          vaultFilePath = FactionVaultPath + $"{data.SenderFaction}" + $"\\{command[3]}.yaml";

          if (File.Exists(vaultFilePath))
          {
            await Helpers.SendFeedbackMessage($"A vault with the name {command[3]} already exists.", data.SenderEntityId);
            return;
          }
          using (StreamWriter writer = File.CreateText(vaultFilePath))
          {
            var vault = new Vault()
            {
              Id = System.Guid.NewGuid(),
              Name = command[3],
              Items = new List<ItemStack>()
            };
            ConfigManager.SerializeYaml<Vault>(writer, vault);
          }
          break;
        default:
          await VaultNewDialog(data.SenderEntityId);
          return;
      }
    }

    private async Task VaultNewDialog(int playerId)
    {
      StringBuilder sb = new StringBuilder();
      sb.Append("\nZucchini Vault:\n\n");
      sb.Append("@vault new [p|f] name\n");
      sb.Append("Please review your arguments.\n");
      await Helpers.SendFeedbackMessage(sb.ToString(), playerId);
    }

    /*
     * Vault List
     */

    private async Task VaultListCommand(MessageData data)
    {
      if (Debug)
      {
        await Task.Run(() => Log($"VaultListCommand called by {data.SenderEntityId} with: {data.Text} and Faction: {data.SenderFaction}"));
      }

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

      if (playerVaults.Count == 0 && factionVaults.Count == 0) {
        await Helpers.SendFeedbackMessage("No vaults found.", data.SenderEntityId);
        return;
      }

      StringBuilder message = new StringBuilder();
      message.AppendFormat("\nHello {0}, you have {1} personal and {2} faction vault(s)\n\n", data.SenderEntityId, playerVaults.Count, factionVaults.Count);

      foreach (var vault in playerVaults)
      {
        message.AppendFormat("Vault name: {0} has {1} item(s)\n", vault.Name, vault.Items.Count);
      }
      await Helpers.SendFeedbackMessage(message.ToString(), data.SenderEntityId);
    }

    /*
     * Vault Open
     */

    private async Task VaultOpenCommand(MessageData data)
    {
      var command = data.Text.Split(' ');
      if (command.Length < 4)
      {
        await Helpers.SendFeedbackMessage("You need arguments.", data.SenderEntityId);
        return;
      }

      var player = await Helpers.GetPlayerInfo(data.SenderEntityId);

      switch (command[2])
      {
        case "p":
          var vaultFilePath = PlayerVaultPath + $"{player.steamId}" + $"\\{command[3]}.yaml";
          if (!File.Exists(vaultFilePath))
          {
            await Helpers.SendFeedbackMessage($"Could not find a vault named {command[3]}.", data.SenderEntityId);
            return;
          }

          var vault = new Vault();
          using (StreamReader reader = File.OpenText(vaultFilePath))
          {
            vault = ConfigManager.DeserializeYaml<Vault>(reader);
          }
          var itemExchange = new ItemExchangeInfo()
          {
            id = data.SenderEntityId,
            buttonText = "Done",
            desc = $"Name: {vault.Name}",
            title = "Zucchini Vault",
            items = vault.Items.ToArray()
          };
          vault.Items.Clear();
          using (StreamWriter writer = File.CreateText(vaultFilePath))
          {
            ConfigManager.SerializeYaml<Vault>(writer, vault);
          }
          var result = (ItemExchangeInfo)await RequestManager.SendGameRequest(CmdId.Request_Player_ItemExchange, itemExchange);
          vault.Items = new List<ItemStack>(result.items);
          using (StreamWriter writer = File.CreateText(vaultFilePath))
          {
            ConfigManager.SerializeYaml<Vault>(writer, vault);
          }
          await Helpers.SendFeedbackMessage("Vault contents should have been successfully saved.", data.SenderEntityId);
          break;
        case "f":
          vaultFilePath = FactionVaultPath + $"{data.SenderFaction}" + $"\\{command[3]}.yaml";
          if (!File.Exists(vaultFilePath))
          {
            await Helpers.SendFeedbackMessage($"Could not find a vault named {command[3]}.", data.SenderEntityId);
            return;
          }

          vault = new Vault();
          using (StreamReader reader = File.OpenText(vaultFilePath))
          {
            vault = ConfigManager.DeserializeYaml<Vault>(reader);
          }
          itemExchange = new ItemExchangeInfo()
          {
            id = data.SenderEntityId,
            buttonText = "Done",
            desc = $"Name: {vault.Name}",
            title = "Zucchini Vault",
            items = vault.Items.ToArray()
          };
          vault.Items.Clear();
          using (StreamWriter writer = File.CreateText(vaultFilePath))
          {
            ConfigManager.SerializeYaml<Vault>(writer, vault);
          }
          result = (ItemExchangeInfo)await RequestManager.SendGameRequest(CmdId.Request_Player_ItemExchange, itemExchange);
          vault.Items = new List<ItemStack>(result.items);
          using (StreamWriter writer = File.CreateText(vaultFilePath))
          {
            ConfigManager.SerializeYaml<Vault>(writer, vault);
          }
          await Helpers.SendFeedbackMessage("Vault contents should have been successfully saved.", data.SenderEntityId);
          break;
        default:
          await VaultNewDialog(data.SenderEntityId);
          return;
      }
    }
  }
}