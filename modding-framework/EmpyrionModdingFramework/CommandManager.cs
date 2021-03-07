using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Eleon;
using Eleon.Modding;

namespace EmpyrionModdingFramework
{
  public class CommandManager
  {
    private readonly IModApi modAPI;

    public CommandManager(in IModApi refModApi)
    {
      modAPI = refModApi;
    }

    public List<ChatCommand> CommandList { get; set; } = new List<ChatCommand>();

    public void ProcessChatMessage(MessageData data)
    {
      modAPI.Log($"Chat message to channel = {data.Channel} with text = {data.Text}");

      switch (data.Channel)
      {
        case Eleon.MsgChannel.Server:
          if (data.Text.StartsWith("@"))
          {
            ProcessChatCommand(data);
          }
          break;
        case Eleon.MsgChannel.Global:
          break;
        case Eleon.MsgChannel.Alliance:
          break;
        case Eleon.MsgChannel.Faction:
          break;
        case Eleon.MsgChannel.SinglePlayer:
          break;
        default:
          modAPI.Log($"Unknown Eleon.MsgChannel = {data.Channel}");
          break;
      }
    }

    public async void ProcessChatCommand(MessageData data)
    {
      ChatCommand chatCommand = CommandList.FirstOrDefault(C => data.Text.StartsWith("@" + C.cmdText));
      if ((chatCommand == null) || (chatCommand.cmdHandler == null))
      {
        return;
      }

      modAPI.Log($"ChatCommand found is {chatCommand.cmdText}");
      modAPI.Log($"ChatCommand has handler {chatCommand.cmdHandler}");
      modAPI.Log($"invoking handler for {data.Text}");
      await chatCommand.cmdHandler(data);
    }
  }

  public class ChatCommand
  {
    public delegate Task ChatCommandHandler(MessageData messageData);
    public readonly string cmdText;
    public ChatCommandHandler cmdHandler;

    public ChatCommand(string command, ChatCommandHandler handler)
    {
      cmdText = command;
      cmdHandler = handler;
    }
  }
}