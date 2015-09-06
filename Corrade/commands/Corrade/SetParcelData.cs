using System;
using System.Collections.Generic;
using System.Linq;
using OpenMetaverse;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<Group, string, Dictionary<string, string>> setparceldata =
                (commandGroup, message, result) =>
                {
                    if (!HasCorradePermission(commandGroup.Name, (int) Permissions.Land))
                    {
                        throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                    }
                    Vector3 position;
                    if (
                        !Vector3.TryParse(
                            wasInput(
                                wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.POSITION)),
                                    message)),
                            out position))
                    {
                        position = Client.Self.SimPosition;
                    }
                    string region =
                        wasInput(wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.REGION)),
                            message));
                    Simulator simulator =
                        Client.Network.Simulators.FirstOrDefault(
                            o =>
                                o.Name.Equals(
                                    string.IsNullOrEmpty(region) ? Client.Network.CurrentSim.Name : region,
                                    StringComparison.InvariantCultureIgnoreCase));
                    if (simulator == null)
                    {
                        throw new ScriptException(ScriptError.REGION_NOT_FOUND);
                    }
                    Parcel parcel = null;
                    if (!GetParcelAtPosition(simulator, position, ref parcel))
                    {
                        throw new ScriptException(ScriptError.COULD_NOT_FIND_PARCEL);
                    }
                    if (!parcel.OwnerID.Equals(Client.Self.AgentID))
                    {
                        if (!parcel.IsGroupOwned && !parcel.GroupID.Equals(commandGroup.UUID))
                        {
                            throw new ScriptException(ScriptError.NO_GROUP_POWER_FOR_COMMAND);
                        }
                    }
                    wasCSVToStructure(
                        wasInput(wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.DATA)),
                            message)), ref parcel);
                    if (IsSecondLife())
                    {
                        if (parcel.OtherCleanTime > LINDEN_CONSTANTS.PARCELS.MAXIMUM_AUTO_RETURN_TIME ||
                            parcel.OtherCleanTime < LINDEN_CONSTANTS.PARCELS.MINIMUM_AUTO_RETURN_TIME)
                        {
                            throw new ScriptException(ScriptError.AUTO_RETURN_TIME_OUTSIDE_LIMIT_RANGE);
                        }
                        if (parcel.Name.Length > LINDEN_CONSTANTS.PARCELS.MAXIMUM_NAME_LENGTH)
                        {
                            throw new ScriptException(ScriptError.NAME_TOO_LARGE);
                        }
                        if (parcel.Desc.Length > LINDEN_CONSTANTS.PARCELS.MAXIMUM_DESCRIPTION_LENGTH)
                        {
                            throw new ScriptException(ScriptError.DESCRIPTION_TOO_LARGE);
                        }
                    }
                    parcel.Update(simulator, true);
                };
        }
    }
}