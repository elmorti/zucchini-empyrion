using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Text;

using Eleon;
using Eleon.Modding;

using EmpyrionModdingFramework;

namespace empyrion_vault
{
  public class Main : EmpyrionModdingFrameworkBase
  {
    public Config Configuration;
    public string GameModConfigPath;

    // Let's store an in memory copy to avoid too much concurrent access to the files.
    public ConcurrentDictionary<int, List<ItemStack>> Vaults;

    protected override void Initialize()
    {
      Vaults = new ConcurrentDictionary<int, List<ItemStack>>();

      CommandManager.CommandList.Add(new ChatCommand($"vault_help", (I) => Vault_Help(I)));
      CommandManager.CommandList.Add(new ChatCommand($"vault_recycle", (I) => Vault_Recycle(I)));
      CommandManager.CommandList.Add(new ChatCommand($"vault_struct", (I) => Vault_Structure(I)));

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

    private async Task Vault_Help(MessageData data)
    {
      DialogConfig dialogConfig = new DialogConfig();
      try
      {
        using (StreamReader reader = File.OpenText(ModAPI.Application.GetPathFor(AppFolder.Mod) + @"\" + $"{ModName}" + @"\dialogs\help.yaml"))
        {
          dialogConfig = ConfigManager.DeserializeYaml<DialogConfig>(reader);
        }
      }
      catch (Exception error)
      {
        Log($"error opening the dialog config");
        Log($"{error.Message}");
      }
      dialogConfig.BodyText = dialogConfig.BodyText.Replace("%%PLAYER%%", data.SenderEntityId.ToString());
      //await Helpers.ShowDialog(data, dlgConfig);
      await Task.Factory.StartNew(() =>
        ModAPI.Application.ShowDialogBox(data.SenderEntityId, dialogConfig, Vault_Help_Handler, 0)
      );
    }

    private async void Vault_Help_Handler(int buttonIdx, string linkId, string inputContent, int playerId, int customValue)
    {
      switch (linkId)
      {
        case "vault_new":
          await Vault_Add(playerId);
          return;
        case "vault_list":
          Log($"Player {playerId} clicked {linkId}");
          await Vault_List(playerId);
          return;
        default:
          break;
      }
    }

    private async void Vault_Open_Handler(int buttonIdx, string linkId, string inputContent, int playerId, int customValue)
    {
      await Vault_Open(playerId, linkId);
    }

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

    private async void Vault_New_Handler(int buttonIdx, string linkId, string inputContent, int playerId, int customValue)
    {
      if (inputContent == "")
      {
        Log($"No vault name provided");
        return;
      }
      await Vault_Create(playerId, inputContent);
    }

    private async Task Vault_Add(int playerId)
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
        ModAPI.Application.ShowDialogBox(playerId, dialogConfig, Vault_New_Handler, 0)
      );
      Log($"ASDASDASDASDASDSADA");
    }

    private async Task Vault_Create(int playerId, string vaultName)
    {
      PlayerInfo playerInfo = await Helpers.GetPlayerInfo(playerId);
      Vault test = new Vault()
      {
        Id = Guid.NewGuid(),
        Name = vaultName,
        Items = new List<ItemStack>()
      };

      if (File.Exists(GameModConfigPath + @"\Players\" + playerInfo.steamId + @"\" + vaultName + @".yaml")) {
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

    private async Task Vault_Open(int playerId, string vaultName)
    {
      PlayerInfo playerInfo = await Helpers.GetPlayerInfo(playerId);
      Vault test = new Vault();

      if (!File.Exists(GameModConfigPath + @"\Players\" + playerInfo.steamId + @"\" + vaultName + @".yaml"))
      {
        await RequestManager.SendGameRequest(CmdId.Request_InGameMessage_SinglePlayer, new IdMsgPrio()
        {
          msg = string.Format("Cannot find vault, please create one."),
          id = playerId,
          prio = 0,
          time = 1
        });
        return;
      }

      using (StreamReader reader = File.OpenText(GameModConfigPath + @"\Players\" + playerInfo.steamId + @"\" + vaultName + @".yaml"))
      {
        test = ConfigManager.DeserializeYaml<Vault>(reader);
      }
           
      ItemExchangeInfo vault_exchange = new ItemExchangeInfo()
      {
        title = "Vault",
        desc = $"Vault Name: {test.Name}",
        buttonText = "Close",
        items = test.Items.ToArray(),
        id = playerId
      };

      ///
      //// IF PLAYER DISCONNECTS HERE ITEMS CAN BE DUPLICATED!
      /// because test.Items will not get updated
      vault_exchange = (ItemExchangeInfo)await RequestManager.SendGameRequest(CmdId.Request_Player_ItemExchange, vault_exchange);

      test.Items = new List<ItemStack>(vault_exchange.items);

      using (StreamWriter writer = new StreamWriter(GameModConfigPath + @"\Players\" + playerInfo.steamId + @"\" + test.Name + @".yaml"))
      {
        ConfigManager.SerializeYaml(writer, test);
      }

      Log($"Vault SAVED!");
    }

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
