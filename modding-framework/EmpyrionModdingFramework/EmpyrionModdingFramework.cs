using System;
using System.IO;
using System.Reflection;

using Eleon.Modding;

namespace EmpyrionModdingFramework
{
  public abstract class EmpyrionModdingFrameworkBase : IMod, ModInterface
  {
    protected static ModGameAPI LegacyAPI { get; private set; }
    protected static IModApi ModAPI { get; private set; }

    protected string ModName { get; private set; }

    protected ConfigManager ConfigManager { get; private set; }
    protected CommandManager CommandManager { get; private set; }
    protected RequestManager RequestManager { get; private set; }
    protected FrameworkConfig FrameworkConfig { get; private set; }
    protected Helpers Helpers { get; private set; }

    protected delegate void Game_EventHandler(CmdId eventId, ushort seqNr, object data);
    protected event Game_EventHandler Game_EventRaised;

    protected delegate void Game_ExitHandler();
    protected event Game_ExitHandler Game_ExitRaised;

    protected delegate void Game_UpdateHandler(ulong tick);
    protected event Game_UpdateHandler Game_UpdateRaised;

    protected abstract void Initialize();

    public void Init(IModApi modApi)
    {
      ModAPI = modApi;
      ModName = Assembly.GetExecutingAssembly().GetName().Name;
      ConfigManager = new ConfigManager();
      CommandManager = new CommandManager(ModAPI);
      Helpers = new Helpers(ModAPI, RequestManager);
      FrameworkConfig = new FrameworkConfig();

      ModAPI.Application.ChatMessageSent += CommandManager.ProcessChatMessage;
      try
      {
        using (StreamReader reader = File.OpenText(ModAPI.Application.GetPathFor(AppFolder.Mod) + @"\" + $"{ModName}" + @"\config.yaml"))
        {
          FrameworkConfig = ConfigManager.LoadConfiguration<FrameworkConfig>(reader);
        }
      }
      catch (Exception error)
      {
        Log($"error trying to load the main config file.");
        Log($"{error.Message}");
      }

      try
      {
        Initialize();
      }
      catch (Exception error)
      {
        Log($"initialization exception.");
        Log($"{error}");
      }
      Log($"ModAPI initialization complete!");
      if (LegacyAPI != null)
      {
        Log($"LegacyAPI initializacion complete!");
        return;
      }
      Log($"LegacyAPI not found. Only Client mods supported.");
    }

    public void Shutdown()
    {
      ModAPI.Application.ChatMessageSent -= CommandManager.ProcessChatMessage;
      Log($"shutting down");
    }

    public void Game_Start(ModGameAPI dediAPI)
    {
      if (dediAPI == null)
      {
        return;
      }
      LegacyAPI = dediAPI;
      RequestManager = new RequestManager(LegacyAPI);
    }

    public void Game_Event(CmdId eventId, ushort seqNr, object data)
    {
      try
      {
        if (RequestManager.HandleRequestResponse(eventId, seqNr, data))
        {
          Log($"RequestManager is handling Event {eventId} for the Request {seqNr}.");
        }
      }
      catch (Exception error)
      {
        Log($"Game_Event Exception: EventId: {eventId} SeqNr: {seqNr} Data: {data?.ToString()} Error: {error}");
      }

      switch (eventId)
      {
        case CmdId.Event_Ok:
          Log($"Game_Event OK for SeqNr: {seqNr} Data: {data?.ToString()}");
          break;
        case CmdId.Event_Error:
          ErrorInfo err = (ErrorInfo)data;
          Log($"Game_Event ERROR for SeqNr: {seqNr}");
          Log($"Game_Event ERROR  {err.errorType}");
          break;
        default:
          Log($"Game_Event {eventId} SeqNr: {seqNr} Data: {data?.ToString()}");
          break;
      }

      Game_EventRaised?.Invoke(eventId, seqNr, data);
    }

    public void Game_Exit()
    {
      Game_ExitRaised?.Invoke();
    }

    public void Game_Update()
    {
      Game_UpdateRaised?.Invoke(LegacyAPI.Game_GetTickTime());
    }

    public void Log(string msg)
    {
      ModAPI.Log($"{msg}");
    }
  }
}