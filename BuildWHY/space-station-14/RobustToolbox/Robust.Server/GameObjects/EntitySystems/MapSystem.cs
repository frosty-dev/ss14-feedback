using System.Linq;
using Robust.Shared;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Events;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Server.GameObjects
{
    public sealed class MapSystem : SharedMapSystem
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private bool _deleteEmptyGrids;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MapGridComponent, EmptyGridEvent>(HandleGridEmpty);

            _cfg.OnValueChanged(CVars.GameDeleteEmptyGrids, SetGridDeletion, true);
        }

        protected override void OnMapAdd(EntityUid uid, MapComponent component, ComponentAdd args)
        {
            EnsureComp<PhysicsMapComponent>(uid);
        }

        private void SetGridDeletion(bool value)
        {
            _deleteEmptyGrids = value;

            // If we have any existing empty ones then cull them on setting the cvar
            if (_deleteEmptyGrids)
            {
                var toDelete = new ValueList<EntityUid>();

                var query = AllEntityQuery<MapGridComponent>();
                while (query.MoveNext(out var uid, out var grid))
                {
                    if (!GridEmpty(grid)) continue;
                    toDelete.Add(uid);
                }

                foreach (var uid in toDelete)
                {
                    MapManager.DeleteGrid(uid);
                }
            }
        }

        private bool GridEmpty(MapGridComponent grid)
        {
            return !(grid.GetAllTiles().Any());
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _cfg.UnsubValueChanged(CVars.GameDeleteEmptyGrids, SetGridDeletion);
        }

        private void HandleGridEmpty(EntityUid uid, MapGridComponent component, EmptyGridEvent args)
        {
            if (!_deleteEmptyGrids) return;
            if (!EntityManager.EntityExists(uid)) return;
            if (EntityManager.GetComponent<MetaDataComponent>(uid).EntityLifeStage >= EntityLifeStage.Terminating) return;

            MapManager.DeleteGrid(args.GridId);
        }
    }
}
