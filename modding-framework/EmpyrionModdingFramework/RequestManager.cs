using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Eleon.Modding;

namespace EmpyrionModdingFramework
{
    public class RequestManager
    {
        private readonly ModGameAPI modAPI;

        public RequestManager(in ModGameAPI refModApi)
        {
            modAPI = refModApi;
        }

        private static int nextSeqNr = new Random().Next(1025, ushort.MaxValue);
        
        private readonly ConcurrentDictionary<ushort, TaskCompletionSource<object>> taskTracker = new ConcurrentDictionary<ushort, TaskCompletionSource<object>>();

        public async Task<object> SendGameRequest(CmdId cmdID, object data)
        {
            if (cmdID == CmdId.Request_InGameMessage_SinglePlayer)
            {

            }
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            ushort seqNr = AddTaskCompletionSource(tcs);
            modAPI.Game_Request(cmdID, seqNr, data);
            return await tcs.Task;
        }


        private ushort AddTaskCompletionSource(TaskCompletionSource<object> tcs)
        {
            if (nextSeqNr == ushort.MaxValue)
            {
                Interlocked.Exchange(ref nextSeqNr, 1025);
            }

            ushort newSequenceNumber = (ushort)Interlocked.Increment(ref nextSeqNr);

            while (!taskTracker.TryAdd(newSequenceNumber, tcs))
            {
                newSequenceNumber = (ushort)Interlocked.Increment(ref nextSeqNr);
            }

            return newSequenceNumber;
        }

        public bool HandleRequestResponse(CmdId eventId, ushort seqNr, object data)
        {
            if (!taskTracker.TryRemove(seqNr, out TaskCompletionSource<object> taskCompletionSource))
                return false;

            if (eventId == CmdId.Event_Error && data is ErrorInfo eInfo)
            {
                taskCompletionSource.TrySetException(new Exception(eInfo.errorType.ToString()));
                return true;
            }
            else
            {
                try
                {
                    taskCompletionSource.TrySetResult(data);
                    return true;
                }
                catch (Exception error)
                {
                    taskCompletionSource.TrySetException(error);
                }
                return false;
            }
        }
    }
}
