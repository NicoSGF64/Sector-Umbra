using Content.Shared._RMC.Admin;
using Content.Shared.Database;
using Robust.Shared.Player;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Systems;
using Content.Server.Mind;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Server.Station.Systems;

namespace Content.Server._RMC.Admin;

public sealed class RMCAdminSystem : SharedRMCAdminSystem
{
    [Dependency] private readonly AdminSystem _admin = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly JobSystem _job = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PlayTimeTrackingSystem _playTimeTracking = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public readonly Queue<(Guid Id, List<TacticalMapLine> Lines, string Actor, int Round)> LinesDrawn = new();
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpawnAsJobDialogEvent>(OnSpawnAsJobDialog);
    }

    private void OnSpawnAsJobDialog(SpawnAsJobDialogEvent ev)
    {
        if (GetEntity(ev.User) is not { Valid: true } user)
            return;

        if (GetEntity(ev.Target) is not { Valid: true } target ||
            !TryComp(target, out ActorComponent? actor) ||
            !_transform.TryGetMapOrGridCoordinates(target, out var coords))
        {
            _popup.PopupEntity(Loc.GetString("admin-player-spawn-failed"), user, user);
            return;
        }

        var player = actor.PlayerSession;
        var stationUid = _station.GetOwningStation(target);
        var profile = _gameTicker.GetPlayerProfile(actor.PlayerSession);

        var newMind = _mind.CreateMind(player.UserId, profile.Name);
        _mind.SetUserId(newMind, player.UserId);
        _playTimeTracking.PlayerRolesChanged(player);
        var mobUid = _stationSpawning.SpawnPlayerCharacterOnStation(stationUid, ev.JobId, profile);

        _mind.TransferTo(newMind, mobUid);
        _role.MindAddJobRole(newMind, jobPrototype: ev.JobId);

        var jobName = _job.MindTryGetJobName(newMind);
        _admin.UpdatePlayerList(player);

        if (mobUid != null)
            _transform.SetCoordinates(mobUid.Value, coords.Value);

        _adminLog.Add(LogType.RMCSpawnJob, $"{ToPrettyString(user)} spawned {ToPrettyString(mobUid)} as job {jobName}");
    }
}
