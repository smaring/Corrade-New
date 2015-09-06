using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenMetaverse;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<Group, string, Dictionary<string, string>> deletepick =
                (commandGroup, message, result) =>
                {
                    if (
                        !HasCorradePermission(commandGroup.Name,
                            (int) Permissions.Grooming))
                    {
                        throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                    }
                    ManualResetEvent AvatarPicksReplyEvent = new ManualResetEvent(false);
                    string input =
                        wasInput(wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.NAME)),
                            message));
                    if (string.IsNullOrEmpty(input))
                    {
                        throw new ScriptException(ScriptError.EMPTY_PICK_NAME);
                    }
                    UUID pickUUID = UUID.Zero;
                    EventHandler<AvatarPicksReplyEventArgs> AvatarPicksEventHandler = (sender, args) =>
                    {
                        KeyValuePair<UUID, string> pick = args.Picks.AsParallel().FirstOrDefault(
                            o => o.Value.Equals(input, StringComparison.Ordinal));
                        if (!pick.Equals(default(KeyValuePair<UUID, string>)))
                            pickUUID = pick.Key;
                        AvatarPicksReplyEvent.Set();
                    };
                    lock (ClientInstanceAvatarsLock)
                    {
                        Client.Avatars.AvatarPicksReply += AvatarPicksEventHandler;
                        Client.Avatars.RequestAvatarPicks(Client.Self.AgentID);
                        if (!AvatarPicksReplyEvent.WaitOne((int) corradeConfiguration.ServicesTimeout, false))
                        {
                            Client.Avatars.AvatarPicksReply -= AvatarPicksEventHandler;
                            throw new ScriptException(ScriptError.TIMEOUT_GETTING_PICKS);
                        }
                        Client.Avatars.AvatarPicksReply -= AvatarPicksEventHandler;
                    }
                    if (pickUUID.Equals(UUID.Zero))
                    {
                        pickUUID = UUID.Random();
                    }
                    Client.Self.PickDelete(pickUUID);
                };
        }
    }
}