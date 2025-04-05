using Robust.Shared.Serialization;

namespace Content.Shared._RMC.Admin;

[Serializable, NetSerializable]
public sealed record SpawnAsJobDialogEvent(NetEntity User, NetEntity Target, string JobId);
