using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
      CommandManager.CommandList.Add(new ChatCommand($"vault_recycle", (I) => VaultRecycle(I)));
      CommandManager.CommandList.Add(new ChatCommand($"vault_struct", (I) => VaultStructure(I)));

      LoadConfiguration();
    }

    // This should be moved to modding framwework
    public void LoadConfiguration()
    {
      if ( !Directory.Exists(GameModConfigPath)) {
        try
        {
          foreach (string path in new string[] {"Players", "Factions"})
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

    //
    // VAULT OPEN
    // 

    private async void VaultOpenHandler(int buttonIdx, string linkId, string inputContent, int playerId, int customValue)
    {
      await VaultOpen(playerId, linkId);
    }

    //
    // VAULT LIST
    // 

    private async Task Vault_List(int playerId)
    {
      PlayerInfo playerInfo = await Helpers.GetPlayerInfo(playerId);

      List<string> vaultFiles = new List<string>(Directory.GetFiles(GameModConfigPath + @"\Players\" + playerInfo.steamId, "*.yaml"));
      List<string> vaultNames = new List<string>();

      vaultFiles.ForEach(
        (file) => vaultNames.Add(Path.GetFileNameWithoutExtension(file))
      );
      
      try
      {
        // all this needs to be split better to avoid blocking!!
        using (StreamReader reader = File.OpenText(ModAPI.Application.GetPathFor(AppFolder.Mod) + @"\" + $"{ModName}" + @"\dialogs\vault_list.yaml"))
        {
          StringBuilder stringBuilder = new StringBuilder();
          var dlgConfig = ConfigManager.DeserializeYaml<DialogConfig>(reader);
          stringBuilder.Append(dlgConfig.BodyText);
          stringBuilder.AppendFormat("There are {0} vaults!\n", vaultNames.Count);

          vaultNames.ForEach(
            (vault) => stringBuilder.AppendFormat("<b><u><link={0}>Vault {0}</b></u></link>\n", vault, vault)
            );

          dlgConfig.BodyText = stringBuilder.ToString();
          await Task.Factory.StartNew(() =>
            ModAPI.Application.ShowDialogBox(playerId, dlgConfig, Vault_Open_Handler, 0) // we can use customvalue for steamID to avoid another request
          );
        }
      }

      catch (Exception error)
      {
        Log($"error opening the dialog config");
        Log($"{error.Message}");
      }
    }

    //
    // VAULT NEW
    // 

    private async Task VaultNewDialog(int playerId)
    {
      PlayerInfo playerInfo = await Helpers.GetPlayerInfo(playerId);
      DialogConfig dialogConfig = new DialogConfig()
      {
        TitleText = "ASdasd",
        BodyText = "asdasdasd",
        Placeholder = "vault name here",
        MaxChars = 20,
        ButtonTexts = new string[] { "Ok" }
      };

      if (!Directory.Exists(GameModConfigPath + @"\Players\" + playerInfo.steamId))
      {
        Directory.CreateDirectory(GameModConfigPath + @"\Players\" + playerInfo.steamId);
      }

      //dialogConfig.BodyText = "Enter vault name";
      await Task.Factory.StartNew(() =>
        ModAPI.Application.ShowDialogBox(playerId, dialogConfig, VaultNewDialogHandler, 0)
      );
      Log($"ASDASDASDASDASDSADA");
    }

    private async void VaultNewDialogHandler(int buttonIdx, string linkId, string inputContent, int playerId, int customValue)
    {
      if (inputContent == "")
      {
        Log($"No vault name provided");
        return;
      }
      await VaultNew(playerId, inputContent);
    }

    private async Task VaultNew(int playerId, string vaultName)
    {
      PlayerInfo playerInfo = await Helpers.GetPlayerInfo(playerId);
      Vault test = new Vault()
      {
        Id = Guid.NewGuid(),
        Name = vaultName,
        Items = new List<ItemStack>()
      };

      if (File.Exists(GameModConfigPath + @"\Players\" + playerInfo.steamId + @"\" + vaultName + @".yaml"))
      {
        await RequestManager.SendGameRequest(CmdId.Request_InGameMessage_SinglePlayer, new IdMsgPrio()
        {
          msg = string.Format("There is already a vault with that name."),
          id = playerId,
          prio = 1,
          time = 1
        });
        return;
      }

      using (StreamWriter writer = File.CreateText(GameModConfigPath + @"\Players\" + playerInfo.steamId + @"\" + test.Name + @".yaml"))
      {
        ConfigManager.SerializeYaml(writer, test);
      }

      await RequestManager.SendGameRequest(CmdId.Request_InGameMessage_SinglePlayer, new IdMsgPrio()
      {
        msg = string.Format("Vault Created - Prio 0 msg"), // RED
        id = playerId,
        prio = 0,
        time = 1
      });

      await Task.Delay(1000);
      await RequestManager.SendGameRequest(CmdId.Request_InGameMessage_SinglePlayer, new IdMsgPrio()
      {
        msg = string.Format("Vault Created - Prio 1 msg"), // YELLOW
        id = playerId,
        prio = 1,
        time = 1
      });

      await Task.Delay(1000);
      await RequestManager.SendGameRequest(CmdId.Request_InGameMessage_SinglePlayer, new IdMsgPrio()
      {
        msg = string.Format("Vault Created - Prio 2 msg"), // BLUE
        id = playerId,
        prio = 2,
        time = 1
      });

      await Task.Delay(1000);
      await RequestManager.SendGameRequest(CmdId.Request_InGameMessage_SinglePlayer, new IdMsgPrio()
      {
        msg = string.Format("Vault Created - Prio 3 msg"),
        id = playerId,
        prio = 3,
        time = 1

      });

      await Task.Delay(1000);
      await RequestManager.SendGameRequest(CmdId.Request_InGameMessage_SinglePlayer, new IdMsgPrio()
      {
        msg = string.Format("Vault Created - Prio 4 msg"),
        id = playerId,
        prio = 122,
        time = 1
      });
      await Task.Delay(1000);
      Log($"Safely leaving this task");
    }

    //
    // VAULT RENAME
    //

    private async Task Vault_Rename(MessageData data)
    {
      if (Vaults.TryAdd(data.SenderEntityId, new List<ItemStack>()))
      {
        await Helpers.SendFeedbackMessage("Vault created!", data.SenderEntityId);
      }
      else
      {
        await Helpers.SendFeedbackMessage("Could not create the vault.", data.SenderEntityId);
      }

      if (!Vaults.ContainsKey(data.SenderEntityId))
      {
        await Helpers.SendFeedbackMessage("No vault found", data.SenderEntityId);
        return;
      }

      ItemExchangeInfo vault_exchange = new ItemExchangeInfo()
      {
        title = "Vault",
        desc = "Vault Contents",
        buttonText = "Close",
        items = Vaults[data.SenderEntityId].ToArray(),
        id = data.SenderEntityId
      };

      vault_exchange = (ItemExchangeInfo)await RequestManager.SendGameRequest(CmdId.Request_Player_ItemExchange, vault_exchange);

      Vaults.TryUpdate(data.SenderEntityId, new List<ItemStack>(vault_exchange.items), Vaults[data.SenderEntityId]);

      Vaults[data.SenderEntityId].ForEach(item =>
      {
        Log($"Item {item.id}");
      });

      using (StreamWriter writer = new StreamWriter(GameModConfigPath + @"\" + "vault.yaml"))
      {
        ConfigManager.SerializeYaml(writer, Vaults);
      }
    }

    private async Task Vault_Recycle(MessageData data)
    {
      BlueprintResources resources = new BlueprintResources()
      {
        PlayerId = data.SenderEntityId,
        ItemStacks = Vaults[data.SenderEntityId],
        ReplaceExisting = true
      };

      var result = await RequestManager.SendGameRequest(CmdId.Request_Blueprint_Resources, resources);

      Log($"Result: {result}");
    }

    private async Task Vault_Structure(MessageData data)
    {

      DialogConfig dlgConfig = new DialogConfig()
      {
        TitleText = "Recycle Structure",
        BodyText = "Enter the structure ID<a href=\"asdasd\">asd</a>",
        Placeholder = "Enter the ID here",
        ButtonTexts = new string[] { "Ok", "Cancel" },
        ButtonIdxForEnter = 0,
        ButtonIdxForEsc = 1,
        CloseOnLinkClick = true,
        MaxChars = 40,
        InitialContent = "Initial Content",
      };

      void dialogActionHandler(int buttonIdx, string linkId, string inputContent, int playerId, int customValue)
      {
        Log($"buttonIdx {buttonIdx}");
        Log($"linkId {linkId}");
        Log($"inputContent {inputContent}");
        Log($"playerId {playerId}");
        Log($"customValue {customValue}");
      }

      ModAPI.Application.ShowDialogBox(data.SenderEntityId, dlgConfig, dialogActionHandler, 1234);

      var structure = (IdStructureBlockInfo)await RequestManager.SendGameRequest(CmdId.Request_Structure_BlockStatistics, new Id() { id = 37011 });

      List<ItemStack> strBlocks = new List<ItemStack>();
      foreach (var block in structure.blockStatistics.Keys)
      {
        strBlocks.Add(new ItemStack()
        {
          id = block,
          count = structure.blockStatistics[block],
        });
        Log($"Block ID: {block} Stats: {structure.blockStatistics[block]}");
        Vaults[data.SenderEntityId] = new List<ItemStack>(strBlocks);
      }

      Predicate<GlobalStructureInfo> predicate = new Predicate<GlobalStructureInfo>(str => str.id == 37011);

      var gsl = await Helpers.GetGlobalStructureList();
      var entity = gsl.globalStructures["Haven"].Find(predicate);

      Log($"Trying to delete the entity {entity.name}");

      EntityExportInfo exportInfo = new EntityExportInfo
      {
        id = entity.id,
        filePath = $"{entity.id}.exported.dat"
      };

      await RequestManager.SendGameRequest(CmdId.Request_Entity_Export, exportInfo);
      await RequestManager.SendGameRequest(CmdId.Request_Entity_Destroy, new Id() { id = entity.id });

      await Task.Delay(1000);

      Log($"Trying to recreate the entity {entity.name}");

      EntitySpawnInfo newEnt = new EntitySpawnInfo()
      {
        forceEntityId = entity.id,
        exportedEntityDat = $"{entity.id}.exported.dat"
      };

      await RequestManager.SendGameRequest(CmdId.Request_Entity_Spawn, newEnt);
    }
  }
}
