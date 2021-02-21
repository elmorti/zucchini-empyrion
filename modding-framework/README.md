![Build EmpyrionModdingFramework](https://github.com/Zucchini-Universe/empyrion-modding-framework/workflows/Build%20EmpyrionModdingFramework/badge.svg)

# Zucchini Empyrion Modding Framework

A small and simple framework to help server owners create Empyrion GS server side mods to improve the gameplay.
It provides some extra basic functionality such as a manager class for chat commands.

Includes support for ModAPI therefore it allows to write pure Client mods as well as using the Server side improved API.

## Why

The Empyrion API for dedicated server (aka LegacyAPI) works by handling game events and sending requests to the game API, these events and requests share common data:

```
cmdId -> An enum value representing a particular Event or Request exposed by Eleon's API
seqNr -> A ushort value used to track Events and Requests
data -> The message data that is used as a request data on Requests and as a response data on Events
```

Usually a dedicated server mod for Empyrion will be an implementation of the ModInterface, without going into many details, the core of the events and requests is done through 2 calls:

```csharp
bool ModInterface.Game_Request(CmdId reqId, ushort seqNr, object data) -> This will be your way to send a request to the game server
void ModInterface.Game_Event(CmdId eventId, ushort seqNr, object data) -> This will be called to your mod with the event data that comes from the game server
```

The "problem" of this is simple, for every request you make, the game will call you back via ```Game_Event``` using the same ```seqNr``` provided and you would have to figure it out manually what type of object is coming within ```data``` (which is listed in the API), and as you only have one point of entry for this, you would have to figure it out a way to filter the events you want every time, this can become very cumbersome and prone to errors and not efficient for a mid sized mod, let's see an example of why:

Let's look at the "Playfield Load" event, every time a playfield is requested to be loaded, either manually or by a player entering into it, your Game_Event will be called like 
this:

```
Game_Event(CmdId CmdId.Event_Playfield_Loaded, ushort seqNr, object data)
```

```CmdId eventId``` the type of the event in this case will be "Event_Playfield_Loaded". For more info look at [this](https://empyrion.gamepedia.com/Game_API_CmdId).
```ushort seqNr``` this is 0 for events that don't respond to any of our requests, or the same seqNr that we sent on our request.
```object data``` in this case it will be a ```PlayfieldLoad``` object, you would need to cast it properly to access its properties.

Let's say you would like to do something when that event happens, you would have to write something similar to the following:

```csharp
void ModInterface.Game_Event(CmdId eventId, ushort seqNr, object data)
{
  if(eventId == CmdId.Event_Playfield_Loaded)
  {
    //do something with data, where data would be casted as (PlayfieldLoad)data
  }
}
```

The above example would allow you to catch _all_ the ```Event_Playfield_Loaded``` events, since we are not checking ```seqNr```. If we wanted to respond to an specific event that we requested you would have to do something like this:
```csharp
// Assume we sent a request like ModInterface.Game_Event(CmdId.Request_Load_Playfield, (ushort)12345, PlayfieldLoad playfieldData)
void ModInterface.Game_Event(CmdId eventId, ushort seqNr, object data)
{
  if(eventId == CmdId.Event_Playfield_Loaded && seqNr == (ushort)12345)
  {
    //do something with my response to the request number 12345
  }
}
```

There is nothing wrong with this and you can make your mods pretty well without anything else, but when it grows in features, handling the requests and events will become ugly.

If you are happy with that, you probably can skip reading now, but if you want to focus more on your mod features rather than the game API, read on!

## How

### Requirements

The framework is targeted to .NET Framework v4.7.2 and not looking at being backwards compatible, though the code is very simple and most probably would work with other .NET versions, i.e. it builds on .NET Core v3.1, however it is not supported at runtime yet.

The recommended tool for developing with it is Visual Studio Community edition, it is free and has the best support for .NET Framework projects. You could also use Visual Studio Code, but they support the more modern .NET Core and developing .NET Framework there might be more tedious.

_It is expected that you get familiar with the Eleon API, C# language and particularly its async/await TAP model (just a basic understanding is more than enough), plus all the tools and infrastructure around coding the mod._

### Using the framework

First you will need to add the framework DLL assembly as a reference to your project, you can compile it yourself out of this repository or you can use the latest automated build release from here. We always recommend that you compile your own source code for safety reasons and it is pretty straightforward. You can even add this repository as a dependency in your project and always compile it, even with your own additions/customizations.

You would create a new Class Library project for .NET Framework (we won't go in details on how to use .NET), this class will inherit from ```EmpyrionModdingFramework```.
As a minimum, you will have to implement one method, ```Initialize()``` and probably read your configuration file if you will be using one.

```csharp
using Eleon.Modding;
using EmpyrionModdingFramework;

namespace EmpyrionVoteRewards
{
  public partial class EmpyrionVoteRewards : EmpyrionModdingFramework
  {
    public Config Configuration = new Config();
    protected override void Initialize()
    {
      using (StreamReader reader = File.OpenText(configFilePath))
      {
        ConfigManager.LoadConfiguration<Config>(reader, out Configuration);
      }
    ...
    }
  ...
  }
 ...
}
```
The rest is up to your imagination!

### The ConfigManager

The framework requires as a minimum a __config.yaml__ with some information about your game server installation and your mod, it also supports extending your mod with an extra configuration file per "save game".

A template __config.yaml__ is provided, but for your mod you would have to extend it yourself, it is usually as simple as creating a new class that represent the items in your YAML file and calling later ```ConfigManager.LoadConfiguration<T>``` and ```ConfigManager.SaveConfiguration<T>``` to manipulate it.

The name of the config file is hardcoded due to technical details on how the Assembly finds the files but it may change in the future.

As we use Assembly and Reflection to guess where the files are for the mod, it is necessary that the directory of the mod is named the same as the DLL file.

### The CommandManager

A very simple chat command processor is included, it works by simply mapping an expected text from the chat to a handler. To use it you can do the following:

```csharp
CommandManager.CommandList.Add(new ChatCommand(@"sping", (I) => ServerPing(I)));
// ServerPing would be a handler that accepts a MessageData object that you can use to process the command and work your logic.
```

### Requests (only for LegacyAPI)

This is probably the key of the framework, right now is quite simple, to make a request you would do the following:

```csharp
PlayerInfo player = (PlayerInfo)await requestTracker.SendGameRequest(CmdId.Request_Player_Info, new Id() { id = chatInfo.playerId });
// We call requestTracker.SendGameRequest, who will setup the async Task for us and get our response back for us.
```

__IMPORTANT__: ```requestTracker.SendGameRequest``` is the key that handles the request/event response from the API, it uses async/await therefore your method will need to be async as well.

### ModAPI Events

In ModAPI most of the logic works by subscribing to Events and writing Delegates that should handle it.

For instance, an event that is available both in Dedicated and Client is used in the framework to process chat messages:

```csharp
  ModAPI.Application.ChatMessageSent += CommandManager.ProcessChatMessage;
```

That means: Subscribe to __ChatMessageSent event__ with a handler Delegate called __CommandManager.ProcessChatMessage(MessageData data)__.

When available, is way much easier to use ModAPI events rather than LegacyAPI request/event loop, check out in the Modding Docs what is available.

### Link Assemblies

Finally, depending on your project, you may end with more than one assembly. The game server can only load one DLL per mod so you will need to merge them together.

We recommend that you install ILMerge and any of the most common MSBuild Tasks for ILMerge (do this via nuget in your project), make sure in Visual Studio that only the framework DLL and any other external assembly/reference you add is set to Copy Local = true, this would make ILMerge identify what assemblies to merge as only one per mod is supported right now. The game mod API assemblies should be Copy Local = false (i.e. mif.dll).

### Examples

We have written a simple mod that uses the framework and show how to use it, check out the [Skeleton](https://github.com/elmorti/TBD).

### Contributions

Hopefully there will be some interest out there to improve this over time and share new mods together, so PRs are more than welcome, just try to keep them small for quick review and merge as usual!

Please also see the [LICENSE](https://github.com/Zucchini-Universe/empyrion-modding-framework/blob/master/LICENSE) if you plan to fork this project or use it privately.

### Acknowledges

Initially this project was a fork of ASTIC/TC [EmpyrionNetAPIAccess](https://github.com/GitHub-TC/EmpyrionNetAPIAccess) and researching around I believe that most of this work was initiated by Chris Wheeler [EmpyrionNetAPITools](https://github.com/lostinplace/EmpyrionAPITools) and I also believe there is a lot of shared knowledge coming from the good [Jascha](http://empyriononline.com/members/jascha.8396/), so I believe those would be the main folks to say hi and thanks for their original contributions :)
