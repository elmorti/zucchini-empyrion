using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Eleon;
using Eleon.Modding;

namespace ZucchiniSkeletonMod
{
    public class ZucchiniSkeletonMod : IMod, ModInterface
    {
        IModApi modApi;
        ModGameAPI dediApi;
        public string modLogPrefix = "";

        public void Init(IModApi modApi)
        {
            this.modApi = modApi;

            modApi.Log($"modApi initialized");
            modApi.Application.ChatMessageSent  += OnChatMessageSent;
        }

        public void Shutdown()
        {
            modApi.Application.ChatMessageSent -= OnChatMessageSent;
            modApi.Log($"modApi shutdown");
        }

        public void Game_Start(ModGameAPI legacyModApi)
        {
            this.dediApi = legacyModApi;
            dediApi.Console_Write($"dediApi initialized");
        }

        public void Game_Exit()
        {
            dediApi.Console_Write($"dediApi shutdown");
        }

        public void Game_Event(CmdId eventId, ushort seqNr, object data)
        {
        }

        public void Game_Update()
        {
        }

        void OnChatMessageSent(MessageData data)
        {
            modApi.Log($"Chat msg to {data.Channel}: {data.Text}");

            if (data.Channel == Eleon.MsgChannel.Server && data.Text == "zucchini:show")
            {
                ShowDialogToPlayer(data.SenderEntityId);
                modApi.Log($"Triggered answer...");
            }
        }

        void Handler(int buttonIdx, string linkId, string inputContent, int playerId, int customValue)
        {
            modApi.Log($"The dialog handler has been called: {buttonIdx} {linkId} {inputContent} {playerId} {customValue}");
            return;
        }

        void ShowDialogToPlayer(int entityId)
        {

            var dialogConfig = new DialogConfig
            {
                TitleText = "<i>Zucchini Universe</i>",
                BodyText = "<align=\"center\"><b>Hello World!</b>",
                CloseOnLinkClick = true,
                ButtonTexts = new string[]{ "Welcome!" },
                // ButtonIdxForEsc = -1,
                // ButtonIdxForEnter = -1,
                // MaxChars = 10
                // Placeholder = "Placeholder",
                // InitialContent = "InitialContent"
            };

            modApi.Application.ShowDialogBox(entityId, dialogConfig, Handler, 1);

            if (modApi.GUI != null)
            {
                modApi.GUI.ShowGameMessage("hello world"); // GUI seems not available on Dedi
            }
            


        }

        public void ExecCommand(List<string> args)
        {
            // This is to support console 'mod ex # args' command.
        }

        public void Log(string message)
        {
            if (modLogPrefix == "")
            {
                modApi.Log($"modApi: {message}");
                dediApi.Console_Write($"legacyApi: {message}");
                return;
            }
                modApi.Log($"{modLogPrefix} (modApi): {message}");
                dediApi.Console_Write($"{modLogPrefix} (legacyApi): {message}");
        }
    }
}
