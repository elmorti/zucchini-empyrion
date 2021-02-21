using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Eleon;
using Eleon.Modding;

using EmpyrionModdingFramework;

namespace ZucchiniSkeleton
{
  public class ZucchiniSkeleton : EmpyrionModdingFrameworkBase
  {
    public string configFilePath;
    public Config Configuration = new Config();

    protected override void Initialize()
    {
      ModAPI.Application.GameEntered += Application_GameEntered;

      CommandManager.CommandList.Add(new ChatCommand($"sping", (I) => ServerPing(I)));
    }

    private void Application_GameEntered(bool hasEntered)
    {
      ShowGamePaths();

      configFilePath = ModAPI.Application.GetPathFor(AppFolder.SaveGame) + @"\Mods\" + 
       ModName + @"\" + ConfigManager.Config.DedicatedConfig.ConfigFileName;
      try
      {
        using (StreamReader reader = File.OpenText(configFilePath))
        {
          ConfigManager.LoadConfiguration<Config>(reader, out Configuration);
        }
      }
      catch (Exception error)
      {
        if( error is FileNotFoundException)
        {
          try
          {
            GenerateEmptyConfig();
          }
          catch
          {
            throw;
          }
          
        }
      }

      Log($"SaveGame: {ModAPI.Application.GetPathFor(AppFolder.SaveGame)}");
      Log($"Game Entered event {hasEntered}");
    }

    public void GenerateEmptyConfig()
    {
      Configuration.SenderNameOverride = "Zucchi";
      using (StreamWriter writer = new StreamWriter(configFilePath))
      {
        ConfigManager.SaveConfiguration(writer, Configuration);
      }
    }

    private async Task ServerPing(MessageData data)
    {
      string msg;
      
      // If we are on dedicated server, use LegacyAPI and RequestManager to get information for the player
      if ( LegacyAPI != null )
      {
        PlayerInfo player = (PlayerInfo)await RequestManager.SendGameRequest(CmdId.Request_Player_Info, new Id() { id = data.SenderEntityId });
        msg = $"{player.playerName} said {data.Text}";

      }

      // If there is no RequestManager is because we are not in dedicated server, get the information from Client
      else
      {
        IPlayer player = ModAPI.ClientPlayfield.Players[data.SenderEntityId];
        msg = $"{player.Name} said {data.Text}";
      }
      await SendFeedbackMessage(msg, data.SenderEntityId);
    }

    async Task SendFeedbackMessage(string msgText, int entityID)
    {
      await Task.Factory.StartNew(() => Thread.Sleep(500));

      var chatMsg = new MessageData()
      {
        SenderType = Eleon.SenderType.Player,
        Channel = Eleon.MsgChannel.SinglePlayer,
        RecipientEntityId = entityID,
        SenderNameOverride = Configuration.SenderNameOverride,
        Text = msgText
      };
      ModAPI.Application.SendChatMessage(chatMsg);
    }

    public void ShowGamePaths()
    {
      // Useful Application Paths - Check the logs
      Log($"SaveGame: {ModAPI.Application.GetPathFor(AppFolder.SaveGame)}");
      Log($"ActiveScenario: {ModAPI.Application.GetPathFor(AppFolder.ActiveScenario)}");
      Log($"Cache: {ModAPI.Application.GetPathFor(AppFolder.Cache)}");
      Log($"Content: {ModAPI.Application.GetPathFor(AppFolder.Content)}");
      Log($"Dedicated: {ModAPI.Application.GetPathFor(AppFolder.Dedicated)}");
      Log($"Mod: {ModAPI.Application.GetPathFor(AppFolder.Mod)}");
      Log($"Root: {ModAPI.Application.GetPathFor(AppFolder.Root)}");
    }
  }
}