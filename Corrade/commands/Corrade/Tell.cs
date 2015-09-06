using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using OpenMetaverse;
using Parallel = System.Threading.Tasks.Parallel;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<Group, string, Dictionary<string, string>> tell = (commandGroup, message, result) =>
            {
                if (!HasCorradePermission(commandGroup.Name, (int) Permissions.Talk))
                {
                    throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                }
                string data = wasInput(
                    wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.MESSAGE)),
                        message));
                List<string> myName =
                    new List<string>(
                        GetAvatarNames(string.Join(" ", Client.Self.FirstName, Client.Self.LastName)));
                switch (
                    wasGetEnumValueFromDescription<Entity>(
                        wasInput(
                            wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.ENTITY)),
                                message)).ToLowerInvariant()))
                {
                    case Entity.AVATAR:
                        UUID agentUUID;
                        if (
                            !UUID.TryParse(
                                wasInput(
                                    wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.AGENT)),
                                        message)), out agentUUID) && !AgentNameToUUID(
                                            wasInput(
                                                wasKeyValueGet(
                                                    wasOutput(
                                                        wasGetDescriptionFromEnumValue(ScriptKeys.FIRSTNAME)),
                                                    message)),
                                            wasInput(
                                                wasKeyValueGet(
                                                    wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.LASTNAME)),
                                                    message)),
                                            corradeConfiguration.ServicesTimeout,
                                            corradeConfiguration.DataTimeout,
                                            ref agentUUID))
                        {
                            throw new ScriptException(ScriptError.AGENT_NOT_FOUND);
                        }
                        if (string.IsNullOrEmpty(data) ||
                            (IsSecondLife() &&
                             Encoding.UTF8.GetByteCount(data) >
                             LINDEN_CONSTANTS.CHAT.MAXIMUM_MESSAGE_LENGTH))
                        {
                            throw new ScriptException(ScriptError.TOO_MANY_OR_TOO_FEW_CHARACTERS_IN_MESSAGE);
                        }
                        Client.Self.InstantMessage(agentUUID, data);
                        // Log instant messages,
                        if (corradeConfiguration.InstantMessageLogEnabled)
                        {
                            string agentName = string.Empty;
                            if (!AgentUUIDToName(
                                agentUUID,
                                corradeConfiguration.ServicesTimeout,
                                ref agentName))
                            {
                                throw new ScriptException(ScriptError.AGENT_NOT_FOUND);
                            }
                            List<string> fullName =
                                new List<string>(
                                    GetAvatarNames(agentName));
                            CorradeThreadPool[CorradeThreadType.LOG].SpawnSequential(() =>
                            {
                                try
                                {
                                    lock (InstantMessageLogFileLock)
                                    {
                                        using (
                                            StreamWriter logWriter =
                                                new StreamWriter(
                                                    wasPathCombine(
                                                        corradeConfiguration.InstantMessageLogDirectory,
                                                        string.Join(" ", fullName.First(), fullName.Last())) +
                                                    "." +
                                                    CORRADE_CONSTANTS.LOG_FILE_EXTENSION, true, Encoding.UTF8))
                                        {
                                            logWriter.WriteLine("[{0}] {1} {2} : {3}",
                                                DateTime.Now.ToString(CORRADE_CONSTANTS.DATE_TIME_STAMP,
                                                    CultureInfo.DefaultThreadCurrentCulture.DateTimeFormat),
                                                myName.First(),
                                                myName.Last(),
                                                data);
                                            //logWriter.Flush();
                                            //logWriter.Close();
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // or fail and append the fail message.
                                    Feedback(
                                        wasGetDescriptionFromEnumValue(
                                            ConsoleError.COULD_NOT_WRITE_TO_INSTANT_MESSAGE_LOG_FILE),
                                        ex.Message);
                                }
                            }, corradeConfiguration.MaximumLogThreads);
                        }
                        break;
                    case Entity.GROUP:
                        IEnumerable<UUID> currentGroups = Enumerable.Empty<UUID>();
                        if (
                            !GetCurrentGroups(corradeConfiguration.ServicesTimeout,
                                ref currentGroups))
                        {
                            throw new ScriptException(ScriptError.COULD_NOT_GET_CURRENT_GROUPS);
                        }
                        if (!currentGroups.ToList().Any(o => o.Equals(commandGroup.UUID)))
                        {
                            throw new ScriptException(ScriptError.NOT_IN_GROUP);
                        }
                        if (string.IsNullOrEmpty(data) || (IsSecondLife() &&
                                                           Encoding.UTF8.GetByteCount(data) >
                                                           LINDEN_CONSTANTS.CHAT.MAXIMUM_MESSAGE_LENGTH))
                        {
                            throw new ScriptException(ScriptError.TOO_MANY_OR_TOO_FEW_CHARACTERS_IN_MESSAGE);
                        }
                        if (!Client.Self.GroupChatSessions.ContainsKey(commandGroup.UUID))
                        {
                            if (
                                !HasGroupPowers(Client.Self.AgentID, commandGroup.UUID, GroupPowers.JoinChat,
                                    corradeConfiguration.ServicesTimeout, corradeConfiguration.DataTimeout))
                            {
                                throw new ScriptException(ScriptError.NO_GROUP_POWER_FOR_COMMAND);
                            }

                            if (!JoinGroupChat(commandGroup.UUID, corradeConfiguration.ServicesTimeout))
                            {
                                throw new ScriptException(ScriptError.UNABLE_TO_JOIN_GROUP_CHAT);
                            }
                        }
                        Client.Self.InstantMessageGroup(commandGroup.UUID, data);
                        Parallel.ForEach(
                            corradeConfiguration.Groups.AsParallel().Where(
                                o => o.UUID.Equals(commandGroup.UUID) && o.ChatLogEnabled),
                            o =>
                            {
                                CorradeThreadPool[CorradeThreadType.LOG].SpawnSequential(() =>
                                {
                                    // Attempt to write to log file,
                                    try
                                    {
                                        lock (GroupLogFileLock)
                                        {
                                            using (
                                                StreamWriter logWriter = new StreamWriter(o.ChatLog, true,
                                                    Encoding.UTF8))
                                            {
                                                logWriter.WriteLine("[{0}] {1} {2} : {3}",
                                                    DateTime.Now.ToString(CORRADE_CONSTANTS.DATE_TIME_STAMP,
                                                        CultureInfo.DefaultThreadCurrentCulture.DateTimeFormat),
                                                    myName.First(),
                                                    myName.Last(),
                                                    data);
                                                //logWriter.Flush();
                                                //logWriter.Close();
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        // or fail and append the fail message.
                                        Feedback(
                                            wasGetDescriptionFromEnumValue(
                                                ConsoleError.COULD_NOT_WRITE_TO_GROUP_CHAT_LOG_FILE),
                                            ex.Message);
                                    }
                                }, corradeConfiguration.MaximumLogThreads);
                            });
                        break;
                    case Entity.LOCAL:
                        if (string.IsNullOrEmpty(data) || (IsSecondLife() &&
                                                           Encoding.UTF8.GetByteCount(data) >
                                                           LINDEN_CONSTANTS.CHAT.MAXIMUM_MESSAGE_LENGTH))
                        {
                            throw new ScriptException(ScriptError.TOO_MANY_OR_TOO_FEW_CHARACTERS_IN_MESSAGE);
                        }
                        int chatChannel;
                        if (
                            !int.TryParse(
                                wasInput(
                                    wasKeyValueGet(
                                        wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.CHANNEL)),
                                        message)),
                                out chatChannel))
                        {
                            chatChannel = 0;
                        }
                        FieldInfo chatTypeInfo = typeof (ChatType).GetFields(BindingFlags.Public |
                                                                             BindingFlags.Static)
                            .AsParallel().FirstOrDefault(
                                o =>
                                    o.Name.Equals(
                                        wasInput(
                                            wasKeyValueGet(
                                                wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.TYPE)),
                                                message)),
                                        StringComparison.Ordinal));
                        ChatType chatType = chatTypeInfo != null
                            ? (ChatType)
                                chatTypeInfo
                                    .GetValue(null)
                            : ChatType.Normal;
                        Client.Self.Chat(
                            data,
                            chatChannel,
                            chatType);
                        // Log local chat,
                        if (corradeConfiguration.LocalMessageLogEnabled)
                        {
                            List<string> fullName =
                                new List<string>(
                                    GetAvatarNames(string.Join(" ", Client.Self.FirstName, Client.Self.LastName)));
                            CorradeThreadPool[CorradeThreadType.LOG].SpawnSequential(() =>
                            {
                                try
                                {
                                    lock (LocalLogFileLock)
                                    {
                                        using (
                                            StreamWriter logWriter =
                                                new StreamWriter(
                                                    wasPathCombine(
                                                        corradeConfiguration.LocalMessageLogDirectory,
                                                        Client.Network.CurrentSim.Name) + "." +
                                                    CORRADE_CONSTANTS.LOG_FILE_EXTENSION, true, Encoding.UTF8))
                                        {
                                            logWriter.WriteLine("[{0}] {1} {2} ({3}) : {4}",
                                                DateTime.Now.ToString(CORRADE_CONSTANTS.DATE_TIME_STAMP,
                                                    CultureInfo.DefaultThreadCurrentCulture.DateTimeFormat),
                                                fullName.First(),
                                                fullName.Last(), Enum.GetName(typeof (ChatType), chatType),
                                                data);
                                            //logWriter.Flush();
                                            //logWriter.Close();
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // or fail and append the fail message.
                                    Feedback(
                                        wasGetDescriptionFromEnumValue(
                                            ConsoleError.COULD_NOT_WRITE_TO_LOCAL_MESSAGE_LOG_FILE),
                                        ex.Message);
                                }
                            }, corradeConfiguration.MaximumLogThreads);
                        }
                        break;
                    case Entity.ESTATE:
                        Client.Estate.EstateMessage(data);
                        break;
                    case Entity.REGION:
                        Client.Estate.SimulatorMessage(data);
                        break;
                    default:
                        throw new ScriptException(ScriptError.UNKNOWN_ENTITY);
                }
            };
        }
    }
}