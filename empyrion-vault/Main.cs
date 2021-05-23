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
    private bool debug;

    protected override void Initialize()
    {
      debug = true;

      CommandManager.CommandList.Add(new ChatCommand($"vault_help", (I) => VaultHelpCommand(I)));
      CommandManager.CommandList.Add(new ChatCommand($"vault_new", (I) => VaultNewCommand(I)));
      CommandManager.CommandList.Add(new ChatCommand($"vault_list", (I) => VaultListCommand(I)));
      CommandManager.CommandList.Add(new ChatCommand($"vault_open", (I) => VaultOpenCommand(I)));
      CommandManager.CommandList.Add(new ChatCommand($"vault_str", (I) => VaultStructureCommand(I)));
    }

    /*
     * Vault Help
     */

    private async Task VaultHelpCommand(MessageData data)
    {
      if (debug)
      {
        await Task.Run(() => Log($"VaultHelpCommand called by {data.SenderEntityId} with: {data.Text} "));
      }
      
      await VaultHelpDialog(data.SenderEntityId);
    }

    private async Task VaultHelpDialog(int playerId)
    {
      StringBuilder sb = new StringBuilder();
      sb.AppendFormat("This is the help dialog.");
      await Helpers.SendFeedbackMessage(sb.ToString(), playerId);
    }

    /*
     * Vault New
     */

    private async Task VaultNewCommand(MessageData data)
    {
      if (debug)
      {
        await Task.Run(() => Log($"VaultNewCommand called by {data.SenderEntityId} with: {data.Text} "));
      }

      var command = data.Text.Split(' ');
      if (command.Length == 1)
      {
        await Helpers.SendFeedbackMessage("You need arguments.", data.SenderEntityId);
        return;
      }

      await Helpers.SendFeedbackMessage($"Creating vault name {command[1]}", data.SenderEntityId);

      var player = await Helpers.GetPlayerInfo(data.SenderEntityId);
      var playerVaultPath = ModAPI.Application.GetPathFor(AppFolder.SaveGame) + @"\Mods\" + $"{ModName}" + @"\Players\" + $"{player.steamId}";
      var factionVaultPath = ModAPI.Application.GetPathFor(AppFolder.SaveGame) + @"\Mods\" + $"{ModName}" + @"\Factions\" + $"{data.SenderFaction}";

      if (File.Exists(playerVaultPath + $"\\{command[1]}.yaml"))
      {
        await Helpers.SendFeedbackMessage("File already exists.", data.SenderEntityId);
        return;
      }

      Directory.CreateDirectory(playerVaultPath);
      Directory.CreateDirectory(factionVaultPath);

      using (StreamWriter writer = File.CreateText(playerVaultPath + $"\\{command[1]}.yaml"))
      {
        var vault = new Vault()
        {
          Id = System.Guid.NewGuid(),
          Name = command[1],
          Items = new List<ItemStack>()
        };
        ConfigManager.SerializeYaml<Vault>(writer, vault);
      }
    }

    /*
     * Vault List
     */

    private async Task VaultListCommand(MessageData data)
    {
      if (debug)
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

      // TODO: This should be checking both faction and player vaults.
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
          buttonText = "Done",
          desc = $"Name: {vault.Name}",
          title = "Zucchini Vault",
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
        await Helpers.SendFeedbackMessage("Vault contents should have been successfully saved.", data.SenderEntityId);
      }
      catch
      {
        throw;
      }
    }

    private async Task VaultStructureCommand(MessageData data)
    {

      // TODO IMPORTANT CHECK IF THE PLAYER IS OWNER OF THE STRUCTURE!!

      var command = data.Text.Split(' ');
      if (command.Length == 1)
      {
        await Helpers.SendFeedbackMessage("You need arguments.", data.SenderEntityId);
        return;
      }

      // var player = await Helpers.GetPlayerInfo(data.SenderEntityId);
      var structure = (IdStructureBlockInfo)await RequestManager.SendGameRequest(CmdId.Request_Structure_BlockStatistics, new Id() { id = int.Parse(command[1]) });

      List<ItemStack> strBlocks = new List<ItemStack>();
      StringBuilder sb = new StringBuilder();
      sb.AppendFormat("\nStructure: {0}\n", structure.id);
      foreach (var block in structure.blockStatistics.Keys)
      {
        strBlocks.Add(new ItemStack()
        {
          id = block,
          count = structure.blockStatistics[block],
        });

        sb.AppendFormat("Block ID: {0} Quantity: {1}\n", block, structure.blockStatistics[block]);
        
        //Vaults[data.SenderEntityId] = new List<ItemStack>(strBlocks);
      }

      await RequestManager.SendGameRequest(CmdId.Request_Entity_Destroy, new Id() { id = int.Parse(command[1]) });

      await Helpers.SendFeedbackMessage(sb.ToString(), data.SenderEntityId);
    }
  }
}