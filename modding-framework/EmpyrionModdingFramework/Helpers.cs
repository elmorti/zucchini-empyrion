using System.Threading.Tasks;

using Eleon;
using Eleon.Modding;

namespace EmpyrionModdingFramework
{
  public class Helpers
  {
    private readonly IModApi modAPI;
    private readonly RequestManager requestManager;

    public Helpers(in IModApi refModApi, in RequestManager refRequestManager)
    {
      modAPI = refModApi;
      requestManager = refRequestManager;
    }

    public async Task ShowDialog(MessageData data, DialogConfig dlgConfig)
    {
      await Task.Factory.StartNew(() => modAPI.Application.ShowDialogBox(data.SenderEntityId, dlgConfig, null, 0));
    }

    public async Task<PlayerInfo> GetPlayerInfo(int id)
    {
      return (PlayerInfo)await requestManager.SendGameRequest(CmdId.Request_Player_Info, new Id() { id = id });
    }

    public async Task<EntitySpawnInfo> GetEntityInfo(int id)
    {
      return (EntitySpawnInfo)await requestManager.SendGameRequest(CmdId.Request_Entity_Spawn, new Id() { id = id });
    }

    public async Task<GlobalStructureList> GetGlobalStructureList()
    {
      return (GlobalStructureList)await requestManager.SendGameRequest(CmdId.Request_GlobalStructure_List, null);
    }

    public async Task SendFeedbackMessage(string msgText, int entityID)
    {
      var chatMsg = new MessageData()
      {
        SenderType = Eleon.SenderType.Player,
        Channel = Eleon.MsgChannel.SinglePlayer,
        RecipientEntityId = entityID,
        SenderNameOverride = "SenderOverride",
        Text = msgText
      };
      await Task.Factory.StartNew(() => modAPI.Application.SendChatMessage(chatMsg));
    }
  }
}
