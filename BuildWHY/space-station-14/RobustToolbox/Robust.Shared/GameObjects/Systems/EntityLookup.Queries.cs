using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Robust.Shared.Collections;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public sealed partial class EntityLookupSystem
{
    /*
     * There is no GetEntitiesInMap method as this should be avoided; anyone that really needs it can implement it themselves
     */

    // Internal API messy for now but mainly want external to be fairly stable for a while and optimise it later.

    #region Private

    private void AddEntitiesIntersecting(
        EntityUid lookupUid,
        HashSet<EntityUid> intersecting,
        Box2 worldAABB,
        LookupFlags flags)
    {
        var lookup = _broadQuery.GetComponent(lookupUid);
        var invMatrix = _transform.GetInvWorldMatrix(lookupUid);
        var localAABB = invMatrix.TransformBox(worldAABB);

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref intersecting,
                static (ref HashSet<EntityUid> state, in FixtureProxy value) =>
                {
                    state.Add(value.Entity);
                    return true;
                }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.Static) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref intersecting,
                static (ref HashSet<EntityUid> state, in FixtureProxy value) =>
                {
                    state.Add(value.Entity);
                    return true;
                }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            lookup.StaticSundriesTree.QueryAabb(ref intersecting,
                static (ref HashSet<EntityUid> state, in EntityUid value) =>
                {
                    state.Add(value);
                    return true;
                }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            lookup.SundriesTree.QueryAabb(ref intersecting,
                static (ref HashSet<EntityUid> state, in EntityUid value) =>
                {
                    state.Add(value);
                    return true;
                }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }
    }

    private void AddEntitiesIntersecting(
        EntityUid lookupUid,
        HashSet<EntityUid> intersecting,
        Box2Rotated worldBounds,
        LookupFlags flags)
    {
        var lookup = _broadQuery.GetComponent(lookupUid);
        var invMatrix = _transform.GetInvWorldMatrix(lookupUid);
        // We don't just use CalcBoundingBox because the transformed bounds might be tighter.
        var localAABB = invMatrix.TransformBox(worldBounds);

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref intersecting,
            static (ref HashSet<EntityUid> state, in FixtureProxy value) =>
            {
                state.Add(value.Entity);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.Static) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref intersecting,
            static (ref HashSet<EntityUid> state, in FixtureProxy value) =>
            {
                state.Add(value.Entity);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            lookup.StaticSundriesTree.QueryAabb(ref intersecting,
                static (ref HashSet<EntityUid> state, in EntityUid value) =>
                {
                    state.Add(value);
                    return true;
                }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            lookup.SundriesTree.QueryAabb(ref intersecting,
                static (ref HashSet<EntityUid> state, in EntityUid value) =>
                {
                    state.Add(value);
                    return true;
                }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }
    }

    private bool AnyEntitiesIntersecting(EntityUid lookupUid,
        Box2 worldAABB,
        LookupFlags flags,
        EntityUid? ignored = null)
    {
        var lookup = _broadQuery.GetComponent(lookupUid);
        var localAABB = _transform.GetInvWorldMatrix(lookupUid).TransformBox(worldAABB);
        var state = (ignored, found: false);

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref state, (ref (EntityUid? ignored, bool found) tuple, in FixtureProxy value) =>
            {
                if (tuple.ignored == value.Entity)
                    return true;

                tuple.found = true;
                return false;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);

            if (state.found)
                return true;
        }

        if ((flags & LookupFlags.Static) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref state, (ref (EntityUid? ignored, bool found) tuple, in FixtureProxy value) =>
            {
                if (tuple.ignored == value.Entity)
                    return true;

                tuple.found = true;
                return false;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);

            if (state.found)
                return true;
        }

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            lookup.StaticSundriesTree.QueryAabb(ref state, static (ref (EntityUid? ignored, bool found) tuple, in EntityUid value) =>
            {
                if (tuple.ignored == value)
                    return true;

                tuple.found = true;
                return false;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);

            if (state.found)
                return true;
        }

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            lookup.SundriesTree.QueryAabb(ref state, static (ref (EntityUid? ignored, bool found) tuple, in EntityUid value) =>
            {
                if (tuple.ignored == value)
                    return true;

                tuple.found = true;
                return false;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        return state.found;
    }

    private bool AnyEntitiesIntersecting(EntityUid lookupUid,
        Box2Rotated worldBounds,
        LookupFlags flags,
        EntityUid? ignored = null)
    {
        var lookup = _broadQuery.GetComponent(lookupUid);
        var localAABB = _transform.GetInvWorldMatrix(lookupUid).TransformBox(worldBounds);
        var state = (ignored, found: false);

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref state, (ref (EntityUid? ignored, bool found) tuple, in FixtureProxy value) =>
            {
                if (tuple.ignored == value.Entity)
                    return true;

                tuple.found = true;
                return false;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if (state.found)
            return true;

        if ((flags & LookupFlags.Static) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref state, (ref (EntityUid? ignored, bool found) tuple, in FixtureProxy value) =>
            {
                if (tuple.ignored == value.Entity)
                    return true;

                tuple.found = true;
                return false;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if (state.found)
            return true;

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            lookup.StaticSundriesTree.QueryAabb(ref state, static (ref (EntityUid? ignored, bool found) tuple, in EntityUid value) =>
            {
                if (tuple.ignored == value)
                    return true;

                tuple.found = true;
                return false;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if (state.found)
            return true;

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            lookup.SundriesTree.QueryAabb(ref state, static (ref (EntityUid? ignored, bool found) tuple, in EntityUid value) =>
            {
                if (tuple.ignored == value)
                    return true;

                tuple.found = true;
                return false;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        return state.found;
    }

    private void RecursiveAdd(EntityUid uid, ref ValueList<EntityUid> toAdd)
    {
        var childEnumerator = _xformQuery.GetComponent(uid).ChildEnumerator;

        while (childEnumerator.MoveNext(out var child))
        {
            toAdd.Add(child.Value);
            RecursiveAdd(child.Value, ref toAdd);
        }
    }

    private void AddContained(HashSet<EntityUid> intersecting, LookupFlags flags)
    {
        if ((flags & LookupFlags.Contained) == 0x0 || intersecting.Count == 0)
            return;

        var toAdd = new ValueList<EntityUid>();

        foreach (var uid in intersecting)
        {
            if (!_containerQuery.TryGetComponent(uid, out var conManager))
                continue;

            foreach (var con in conManager.GetAllContainers())
            {
                foreach (var contained in con.ContainedEntities)
                {
                    toAdd.Add(contained);
                    RecursiveAdd(contained, ref toAdd);
                }
            }
        }

        foreach (var uid in toAdd)
        {
            intersecting.Add(uid);
        }
    }

    #endregion

    #region Arc

    public IEnumerable<EntityUid> GetEntitiesInArc(
        EntityCoordinates coordinates,
        float range,
        Angle direction,
        float arcWidth,
        LookupFlags flags = DefaultFlags)
    {
        var position = coordinates.ToMap(EntityManager, _transform);

        return GetEntitiesInArc(position, range, direction, arcWidth, flags);
    }

    public IEnumerable<EntityUid> GetEntitiesInArc(
        MapCoordinates coordinates,
        float range,
        Angle direction,
        float arcWidth,
        LookupFlags flags = DefaultFlags)
    {
        foreach (var entity in GetEntitiesInRange(coordinates, range * 2, flags))
        {
            var angle = new Angle(_transform.GetWorldPosition(entity) - coordinates.Position);
            if (angle.Degrees < direction.Degrees + arcWidth / 2 &&
                angle.Degrees > direction.Degrees - arcWidth / 2)
                yield return entity;
        }
    }

    #endregion

    #region Box2

    public bool AnyEntitiesIntersecting(MapId mapId, Box2 worldAABB, LookupFlags flags = DefaultFlags)
    {
        if (mapId == MapId.Nullspace) return false;

        // Don't need to check contained entities as they have the same bounds as the parent.
        var found = false;

        var state = (this, worldAABB, flags, found);

        _mapManager.FindGridsIntersecting(mapId, worldAABB, ref state,
            static (EntityUid uid, MapGridComponent _, ref (EntityLookupSystem lookup, Box2 worldAABB, LookupFlags flags, bool found) tuple) =>
            {
                if (!tuple.lookup.AnyEntitiesIntersecting(uid, tuple.worldAABB, tuple.flags))
                    return true;

                tuple.found = true;
                return false;
            });

        if (state.found)
            return true;

        var mapUid = _mapManager.GetMapEntityId(mapId);
        return AnyEntitiesIntersecting(mapUid, worldAABB, flags);
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2 worldAABB, LookupFlags flags = DefaultFlags)
    {
        if (mapId == MapId.Nullspace) return new HashSet<EntityUid>();

        var intersecting = new HashSet<EntityUid>();

        // Get grid entities
        var state = (this, _map, intersecting, worldAABB, flags);

        _mapManager.FindGridsIntersecting(mapId, worldAABB, ref state,
            static (EntityUid gridUid, MapGridComponent grid, ref (
                EntityLookupSystem lookup, SharedMapSystem _map, HashSet<EntityUid> intersecting,
                Box2 worldAABB, LookupFlags flags) tuple) =>
            {
                tuple.lookup.AddEntitiesIntersecting(gridUid, tuple.intersecting, tuple.worldAABB, tuple.flags);

                if ((tuple.flags & LookupFlags.Static) != 0x0)
                {
                    // TODO: Need a struct enumerator version.
                    foreach (var uid in tuple._map.GetAnchoredEntities(gridUid, grid, tuple.worldAABB))
                    {
                        if (tuple.lookup.Deleted(uid))
                            continue;

                        tuple.intersecting.Add(uid);
                    }
                }

                return true;
            });

        // Get map entities
        var mapUid = _mapManager.GetMapEntityId(mapId);
        AddEntitiesIntersecting(mapUid, intersecting, worldAABB, flags);
        AddContained(intersecting, flags);

        return intersecting;
    }

    #endregion

    #region Box2Rotated

    public bool AnyEntitiesIntersecting(MapId mapId, Box2Rotated worldBounds, LookupFlags flags = DefaultFlags)
    {
        // Don't need to check contained entities as they have the same bounds as the parent.
        var worldAABB = worldBounds.CalcBoundingBox();

        const bool found = false;
        var state = (this, worldBounds, flags, found);

        _mapManager.FindGridsIntersecting(mapId, worldAABB, ref state,
            static (EntityUid uid, MapGridComponent grid, ref (EntityLookupSystem lookup, Box2Rotated worldBounds, LookupFlags flags, bool found) tuple) =>
            {
                if (tuple.lookup.AnyEntitiesIntersecting(uid, tuple.worldBounds, tuple.flags))
                {
                    tuple.found = true;
                    return false;
                }
                return true;
            });

        if (state.found)
            return true;

        var mapUid = _mapManager.GetMapEntityId(mapId);
        return AnyEntitiesIntersecting(mapUid, worldBounds, flags);
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2Rotated worldBounds, LookupFlags flags = DefaultFlags)
    {
        var intersecting = new HashSet<EntityUid>();

        if (mapId == MapId.Nullspace)
            return intersecting;

        // Get grid entities
        var state = (this, intersecting, worldBounds, flags);

        _mapManager.FindGridsIntersecting(mapId, worldBounds.CalcBoundingBox(), ref state, static
        (EntityUid uid, MapGridComponent _,
            ref (EntityLookupSystem lookup,
                HashSet<EntityUid> intersecting,
                Box2Rotated worldBounds,
                LookupFlags flags) tuple) =>
        {
            tuple.lookup.AddEntitiesIntersecting(uid, tuple.intersecting, tuple.worldBounds, tuple.flags);
            return true;
        });

        // Get map entities
        var mapUid = _mapManager.GetMapEntityId(mapId);
        AddEntitiesIntersecting(mapUid, intersecting, worldBounds, flags);
        AddContained(intersecting, flags);

        return intersecting;
    }

    #endregion

    #region Entity

    // TODO: Bit of duplication between here and the other methods. Was a bit lazy passing around predicates for speed too.

    public bool AnyEntitiesIntersecting(EntityUid uid, LookupFlags flags = DefaultFlags)
    {
        var worldAABB = GetWorldAABB(uid);
        var mapID = _xformQuery.GetComponent(uid).MapID;

        if (mapID == MapId.Nullspace)
            return false;

        const bool found = false;
        var state = (this, worldAABB, flags, found, uid);

        _mapManager.FindGridsIntersecting(mapID, worldAABB, ref state,
            static (EntityUid gridUid, MapGridComponent grid,
                ref (EntityLookupSystem lookup, Box2 worldAABB, LookupFlags flags, bool found, EntityUid ignored) tuple) =>
            {
                if (tuple.lookup.AnyEntitiesIntersecting(gridUid, tuple.worldAABB, tuple.flags, tuple.ignored))
                {
                    tuple.found = true;
                    return false;
                }

                return true;
            });

        var mapUid = _mapManager.GetMapEntityId(mapID);
        return AnyEntitiesIntersecting(mapUid, worldAABB, flags, uid);
    }

    public bool AnyEntitiesInRange(EntityUid uid, float range, LookupFlags flags = DefaultFlags)
    {
        var mapPos = _xformQuery.GetComponent(uid).MapPosition;

        if (mapPos.MapId == MapId.Nullspace)
            return false;

        var rangeVec = new Vector2(range, range);
        var worldAABB = new Box2(mapPos.Position - rangeVec, mapPos.Position + rangeVec);

        const bool found = false;
        var state = (this, worldAABB, flags, found, uid);

        _mapManager.FindGridsIntersecting(mapPos.MapId, worldAABB, ref state, static (
            EntityUid gridUid,
            MapGridComponent _, ref (
                EntityLookupSystem lookup,
                Box2 worldAABB,
                LookupFlags flags,
                bool found,
                EntityUid ignored) tuple) =>
        {
            if (tuple.lookup.AnyEntitiesIntersecting(gridUid, tuple.worldAABB, tuple.flags, tuple.ignored))
            {
                tuple.found = true;
                return false;
            }

            return true;
        });

        var mapUid = _mapManager.GetMapEntityId(mapPos.MapId);
        return AnyEntitiesIntersecting(mapUid, worldAABB, flags, uid);
    }

    public HashSet<EntityUid> GetEntitiesInRange(EntityUid uid, float range, LookupFlags flags = DefaultFlags)
    {
        var mapPos = _xformQuery.GetComponent(uid).MapPosition;

        if (mapPos.MapId == MapId.Nullspace)
            return new HashSet<EntityUid>();

        var intersecting = GetEntitiesInRange(mapPos, range, flags);
        intersecting.Remove(uid);
        return intersecting;
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(EntityUid uid, LookupFlags flags = DefaultFlags)
    {
        var xform = _xformQuery.GetComponent(uid);
        var mapId = xform.MapID;

        if (mapId == MapId.Nullspace)
            return new HashSet<EntityUid>();

        var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform);
        var bounds = GetAABBNoContainer(uid, worldPos, worldRot);

        var intersecting = GetEntitiesIntersecting(mapId, bounds, flags);
        intersecting.Remove(uid);
        return intersecting;
    }

    #endregion

    #region EntityCoordinates

    public bool AnyEntitiesIntersecting(EntityCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        if (!coordinates.IsValid(EntityManager))
            return false;

        var mapPos = coordinates.ToMap(EntityManager, _transform);
        return AnyEntitiesIntersecting(mapPos, flags);
    }

    public bool AnyEntitiesInRange(EntityCoordinates coordinates, float range, LookupFlags flags = DefaultFlags)
    {
        if (!coordinates.IsValid(EntityManager))
            return false;

        var mapPos = coordinates.ToMap(EntityManager, _transform);
        return AnyEntitiesInRange(mapPos, range, flags);
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(EntityCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        var mapPos = coordinates.ToMap(EntityManager, _transform);
        return GetEntitiesIntersecting(mapPos, flags);
    }

    public HashSet<EntityUid> GetEntitiesInRange(EntityCoordinates coordinates, float range, LookupFlags flags = DefaultFlags)
    {
        var mapPos = coordinates.ToMap(EntityManager, _transform);
        return GetEntitiesInRange(mapPos, range, flags);
    }

    #endregion

    #region MapCoordinates

    public bool AnyEntitiesIntersecting(MapCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        if (coordinates.MapId == MapId.Nullspace) return false;

        var rangeVec = new Vector2(float.Epsilon, float.Epsilon);
        var worldAABB = new Box2(coordinates.Position - rangeVec, coordinates.Position + rangeVec);
        return AnyEntitiesIntersecting(coordinates.MapId, worldAABB, flags);
    }

    public bool AnyEntitiesInRange(MapCoordinates coordinates, float range, LookupFlags flags = DefaultFlags)
    {
        // TODO: Actual circles
        if (coordinates.MapId == MapId.Nullspace) return false;

        var rangeVec = new Vector2(range, range);
        var worldAABB = new Box2(coordinates.Position - rangeVec, coordinates.Position + rangeVec);
        return AnyEntitiesIntersecting(coordinates.MapId, worldAABB, flags);
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(MapCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        if (coordinates.MapId == MapId.Nullspace) return new HashSet<EntityUid>();

        var rangeVec = new Vector2(float.Epsilon, float.Epsilon);
        var worldAABB = new Box2(coordinates.Position - rangeVec, coordinates.Position + rangeVec);
        return GetEntitiesIntersecting(coordinates.MapId, worldAABB, flags);
    }

    public HashSet<EntityUid> GetEntitiesInRange(MapCoordinates coordinates, float range, LookupFlags flags = DefaultFlags)
    {
        return GetEntitiesInRange(coordinates.MapId, coordinates.Position, range, flags);
    }

    #endregion

    #region MapId

    public HashSet<EntityUid> GetEntitiesInRange(MapId mapId, Vector2 worldPos, float range,
        LookupFlags flags = DefaultFlags)
    {
        DebugTools.Assert(range > 0, "Range must be a positive float");

        if (mapId == MapId.Nullspace)
            return new HashSet<EntityUid>();

        // TODO: Actual circles
        var rangeVec = new Vector2(range, range);
        var worldAABB = new Box2(worldPos - rangeVec, worldPos + rangeVec);
        return GetEntitiesIntersecting(mapId, worldAABB, flags);
    }

    #endregion

    #region Grid Methods

    /// <summary>
    /// Returns the entities intersecting any of the supplied tiles. Faster than doing each tile individually.
    /// </summary>
    public HashSet<EntityUid> GetEntitiesIntersecting(EntityUid gridId, IEnumerable<Vector2i> gridIndices, LookupFlags flags = DefaultFlags)
    {
        // Technically this doesn't consider anything overlapping from outside the grid but is this an issue?
        if (!_mapManager.TryGetGrid(gridId, out var grid)) return new HashSet<EntityUid>();

        var lookup = _broadQuery.GetComponent(gridId);
        var intersecting = new HashSet<EntityUid>();
        var tileSize = grid.TileSize;

        // TODO: You can probably decompose the indices into larger areas if you take in a hashset instead.
        foreach (var index in gridIndices)
        {
            var aabb = GetLocalBounds(index, tileSize);

            if ((flags & LookupFlags.Dynamic) != 0x0)
            {
                lookup.DynamicTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> state, in FixtureProxy value) =>
                {
                    state.Add(value.Entity);
                    return true;
                }, aabb, (flags & LookupFlags.Approximate) != 0x0);
            }

            if ((flags & LookupFlags.Static) != 0x0)
            {
                lookup.StaticTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> state, in FixtureProxy value) =>
                {
                    state.Add(value.Entity);
                    return true;
                }, aabb, (flags & LookupFlags.Approximate) != 0x0);
            }

            if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
            {
                lookup.StaticSundriesTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> intersecting, in EntityUid value) =>
                {
                    intersecting.Add(value);
                    return true;
                }, aabb, (flags & LookupFlags.Approximate) != 0x0);
            }

            if ((flags & LookupFlags.Sundries) != 0x0)
            {
                lookup.SundriesTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> intersecting, in EntityUid value) =>
                {
                    intersecting.Add(value);
                    return true;
                }, aabb, (flags & LookupFlags.Approximate) != 0x0);
            }
        }

        AddContained(intersecting, flags);

        return intersecting;
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(EntityUid gridId, Vector2i gridIndices, LookupFlags flags = DefaultFlags)
    {
        // Technically this doesn't consider anything overlapping from outside the grid but is this an issue?
        if (!_mapManager.TryGetGrid(gridId, out var grid))
            return new HashSet<EntityUid>();

        var lookup = _broadQuery.GetComponent(gridId);
        var tileSize = grid.TileSize;
        var aabb = GetLocalBounds(gridIndices, tileSize);
        return GetEntitiesIntersecting(lookup, aabb, flags);
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(BroadphaseComponent lookup, Box2 aabb, LookupFlags flags = DefaultFlags)
    {
        var intersecting = new HashSet<EntityUid>();

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref intersecting,
                static (ref HashSet<EntityUid> intersecting, in FixtureProxy value) =>
                {
                    intersecting.Add(value.Entity);
                    return true;
                }, aabb, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.Static) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref intersecting,
                static (ref HashSet<EntityUid> intersecting, in FixtureProxy value) =>
                {
                    intersecting.Add(value.Entity);
                    return true;
                }, aabb, (flags & LookupFlags.Approximate) != 0x0);
        }

        var state = (lookup.StaticSundriesTree._b2Tree, intersecting);
        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            lookup.StaticSundriesTree._b2Tree.Query(ref state, static (ref (B2DynamicTree<EntityUid> _b2Tree, HashSet<EntityUid> intersecting) tuple, DynamicTree.Proxy proxy) =>
            {
                tuple.intersecting.Add(tuple._b2Tree.GetUserData(proxy));
                return true;
            }, aabb);
        }

        state = (lookup.SundriesTree._b2Tree, intersecting);
        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            lookup.SundriesTree._b2Tree.Query(ref state, static (ref (B2DynamicTree<EntityUid> _b2Tree, HashSet<EntityUid> intersecting) tuple, DynamicTree.Proxy proxy) =>
            {
                tuple.intersecting.Add(tuple._b2Tree.GetUserData(proxy));
                return true;
            }, aabb);
        }

        AddContained(intersecting, flags);

        return intersecting;
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(EntityUid gridId, Box2 worldAABB, LookupFlags flags = DefaultFlags)
    {
        if (!_mapManager.GridExists(gridId))
            return new HashSet<EntityUid>();

        var intersecting = new HashSet<EntityUid>();

        AddEntitiesIntersecting(gridId, intersecting, worldAABB, flags);
        AddContained(intersecting, flags);

        return intersecting;
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(EntityUid gridId, Box2Rotated worldBounds, LookupFlags flags = DefaultFlags)
    {
        if (!_mapManager.GridExists(gridId)) return new HashSet<EntityUid>();

        var intersecting = new HashSet<EntityUid>();

        AddEntitiesIntersecting(gridId, intersecting, worldBounds, flags);
        AddContained(intersecting, flags);

        return intersecting;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<EntityUid> GetEntitiesIntersecting(TileRef tileRef, LookupFlags flags = DefaultFlags)
    {
        return GetEntitiesIntersecting(tileRef.GridUid, tileRef.GridIndices, flags);
    }

    #endregion

    #region Lookup Query

    public HashSet<EntityUid> GetEntitiesIntersecting(BroadphaseComponent component, ref Box2 worldAABB, LookupFlags flags = DefaultFlags)
    {
        var intersecting = new HashSet<EntityUid>();
        var localAABB = _transform.GetInvWorldMatrix(component.Owner).TransformBox(worldAABB);

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            component.DynamicTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> intersecting, in FixtureProxy value) =>
            {
                intersecting.Add(value.Entity);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.Static) != 0x0)
        {
            component.StaticTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> intersecting, in FixtureProxy value) =>
            {
                intersecting.Add(value.Entity);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            component.StaticSundriesTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> intersecting, in EntityUid value) =>
            {
                intersecting.Add(value);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            component.SundriesTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> intersecting, in EntityUid value) =>
            {
                intersecting.Add(value);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        AddContained(intersecting, flags);

        return intersecting;
    }

    public HashSet<EntityUid> GetLocalEntitiesIntersecting(BroadphaseComponent component, Box2 localAABB, LookupFlags flags = DefaultFlags)
    {
        var intersecting = new HashSet<EntityUid>();

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            component.DynamicTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> intersecting, in FixtureProxy value) =>
            {
                intersecting.Add(value.Entity);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.Static) != 0x0)
        {
            component.StaticTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> intersecting, in FixtureProxy value) =>
            {
                intersecting.Add(value.Entity);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            component.StaticSundriesTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> intersecting, in EntityUid value) =>
            {
                intersecting.Add(value);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            component.SundriesTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> intersecting, in EntityUid value) =>
            {
                intersecting.Add(value);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        AddContained(intersecting, flags);

        return intersecting;
    }

    #endregion

    #region Lookups

    /// <summary>
    /// Gets the relevant <see cref="BroadphaseComponent"/> that intersects the specified area.
    /// </summary>
    public void FindLookupsIntersecting(MapId mapId, Box2Rotated worldBounds, ComponentQueryCallback<BroadphaseComponent> callback)
    {
        if (mapId == MapId.Nullspace)
            return;

        var mapUid = _mapManager.GetMapEntityId(mapId);
        callback(mapUid, _broadQuery.GetComponent(mapUid));

        var state = (callback, _broadQuery);

        _mapManager.FindGridsIntersecting(mapId, worldBounds, ref state,
            static (EntityUid uid, MapGridComponent grid,
                ref (ComponentQueryCallback<BroadphaseComponent> callback, EntityQuery<BroadphaseComponent> _broadQuery)
                    tuple) =>
            {
                tuple.callback(uid, tuple._broadQuery.GetComponent(uid));
                return true;
            });
    }

    #endregion

    #region Bounds

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Box2 GetLocalBounds(Vector2i gridIndices, ushort tileSize)
    {
        return new Box2(gridIndices * tileSize, (gridIndices + 1) * tileSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Box2 GetLocalBounds(TileRef tileRef, ushort tileSize)
    {
        return GetLocalBounds(tileRef.GridIndices, tileSize);
    }

    public Box2Rotated GetWorldBounds(TileRef tileRef, Matrix3? worldMatrix = null, Angle? angle = null)
    {
        var grid = _mapManager.GetGrid(tileRef.GridUid);

        if (worldMatrix == null || angle == null)
        {
            var (_, wAng, wMat) = _transform.GetWorldPositionRotationMatrix(tileRef.GridUid);
            worldMatrix = wMat;
            angle = wAng;
        }

        var expand = new Vector2(0.5f, 0.5f);
        var center = worldMatrix.Value.Transform(tileRef.GridIndices + expand) * grid.TileSize;
        var translatedBox = Box2.CenteredAround(center, new Vector2(grid.TileSize, grid.TileSize));

        return new Box2Rotated(translatedBox, -angle.Value, center);
    }

    #endregion
}
