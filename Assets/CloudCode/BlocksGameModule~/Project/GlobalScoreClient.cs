using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Model;
using Unity.Services.Leaderboards.Model;

namespace BlocksGameModule.PlayerAccount;

public class GlobalScoreClient
{
    const string globalScoreRow = "globalScoreRow";
    const string globalScoreKey = "globalScoreKey";
    [CloudCodeFunction(nameof(UpdateGlobalScore))]
    public async Task<int> UpdateGlobalScore(
        IExecutionContext ctx,
        IGameApiClient client,
        ILogger<GlobalScoreClient> logger,
        int increment)
    {
        ApiResponse<SetItemResponse> write = null;
        int tries = 3;
        var cloudSave = client.CloudSaveData;
        int newValue = 0;
        // Use logger to get logs on your modules
        logger.LogInformation("Update was called");
        for (int i = 0; i < tries; ++i)
        {
            // Custom items or Game Data are not tied to a player, and can be used to store anything
            // in the given ID (like a row), and the keys of that row.
            // Something like a global score, or clan data, or changes to the world state could be saved
            // here as long as it does not exceed limitations.
            var res = await cloudSave.GetCustomItemsAsync(
                ctx,
                ctx.ServiceToken,
                ctx.ProjectId,
                globalScoreRow,
                new List<string>{globalScoreKey});
            var kvp = res.Data.Results.FirstOrDefault();
            newValue = kvp == null ? increment : (int)(long)kvp.Value + increment;
            write = await cloudSave.SetCustomItemAsync(
                ctx,
                ctx.ServiceToken,
                ctx.ProjectId,
                globalScoreRow,
                new SetItemBody(globalScoreKey, newValue, kvp?.WriteLock!), CancellationToken.None);
            if (write.StatusCode == HttpStatusCode.Conflict)
            {
                //wait and try again
                 await Task.Delay(0);
                 continue; // try again, write lock failed
            }
            break; //either it was successful or something went wrong
        }

        if (write!.StatusCode != HttpStatusCode.OK)
        {
            // Was never successful indicate error
            throw new Exception("Failed to update global score!");
        }

        return newValue;
    }

    [CloudCodeFunction(nameof(GetGlobalScore))]
    public async Task<int> GetGlobalScore(
        IExecutionContext ctx,
        IGameApiClient client)
    {
        var cloudSave = client.CloudSaveData;
        var response = await cloudSave.GetCustomItemsAsync(
            ctx,
            ctx.ServiceToken,
            ctx.ProjectId,
            globalScoreRow,
            new List<string>{globalScoreKey});
        // Using cloud-code is not necessary to read "Default" access class game data
        // but it is necessary write it. In this case, we initialize it if its not there.
        var res = response.Data.Results.FirstOrDefault();
        if (res == null)
            return 0;
        return (int)(long)res.Value;
    }
}
