using System;
using System.Collections.Generic;
using UnityEngine;
using SevenCrowns.Map;
using SevenCrowns.Map.Cities;
using SevenCrowns.Map.Mines;
using SevenCrowns.Map.Farms;
using SevenCrowns.Map.Resources;
using SevenCrowns.Systems;

namespace SevenCrowns.Systems.Save
{
    /// <summary>
    /// Default world map state reader/applier for heroes.
    /// Future iterations will extend to wallet and ownership providers.
    /// </summary>
    public sealed class WorldMapStateReader : IWorldMapStateReader
    {
        public WorldMapSnapshot Capture()
        {
            var snapshot = WorldMapSnapshot.CreateEmpty();

            // Heroes
            var identities = UnityEngine.Object.FindObjectsOfType<HeroIdentity>(true);
            if (identities != null)
            {
                for (int i = 0; i < identities.Length; i++)
                {
                    var id = identities[i];
                    if (id == null || string.IsNullOrWhiteSpace(id.HeroId)) continue;
                    var hac = id.Agent; // HeroAgentComponent
                    GridCoord pos = default;
                    bool hasPos = false;
                    if (hac != null)
                    {
                        // Prefer the grid position tracked by the component
                        pos = hac.GridPosition;
                        hasPos = true;
                    }

                    var hs = new HeroSnapshot
                    {
                        id = id.HeroId,
                        level = id.Level,
                        gridX = hasPos ? pos.X : 0,
                        gridY = hasPos ? pos.Y : 0,
                    };
                    // Movement (MP)
                    if (hac != null && hac.Movement != null)
                    {
                        hs.mpCurrent = hac.Movement.Current;
                        hs.mpMax = hac.Movement.Max;
                    }
                    snapshot.heroes.Add(hs);
                }
            }

            // Resource wallet
            var behaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
            SevenCrowns.Map.Resources.IResourceWallet wallet = null;
            IResourceWalletSnapshotProvider walletSnap = null;
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (wallet == null && behaviours[i] is SevenCrowns.Map.Resources.IResourceWallet w)
                {
                    wallet = w;
                }
                if (walletSnap == null && behaviours[i] is IResourceWalletSnapshotProvider sp)
                {
                    walletSnap = sp;
                }
                if (wallet != null && walletSnap != null) break;
            }

            if (wallet != null && walletSnap != null)
            {
                var map = walletSnap.GetAllAmountsSnapshot();
                foreach (var kv in map)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                    snapshot.resources.Add(new ResourceAmountSnapshot { id = kv.Key, amount = kv.Value });
                }
            }

            // Resource nodes: capture remaining node ids
            var resourceServices = UnityEngine.Object.FindObjectsOfType<ResourceNodeService>(true);
            for (int i = 0; i < resourceServices.Length; i++)
            {
                var svc = resourceServices[i];
                var nodes = svc?.Nodes;
                if (nodes == null) continue;
                for (int n = 0; n < nodes.Count; n++)
                {
                    var d = nodes[n];
                    if (!string.IsNullOrWhiteSpace(d.NodeId) && !snapshot.remainingResourceNodeIds.Contains(d.NodeId))
                        snapshot.remainingResourceNodeIds.Add(d.NodeId);
                }
            }

            // Cities
            var cityServices = UnityEngine.Object.FindObjectsOfType<CityNodeService>(true);
            for (int i = 0; i < cityServices.Length; i++)
            {
                var svc = cityServices[i];
                var nodes = svc?.Nodes;
                if (nodes == null) continue;
                for (int n = 0; n < nodes.Count; n++)
                {
                    var d = nodes[n];
                    snapshot.cities.Add(new CityOwnershipSnapshot
                    {
                        nodeId = d.NodeId,
                        owned = d.IsOwned,
                        ownerId = d.OwnerId,
                        level = (int)d.Level,
                    });
                }
            }

            // Mines
            var mineServices = UnityEngine.Object.FindObjectsOfType<MineNodeService>(true);
            for (int i = 0; i < mineServices.Length; i++)
            {
                var svc = mineServices[i];
                var nodes = svc?.Nodes;
                if (nodes == null) continue;
                for (int n = 0; n < nodes.Count; n++)
                {
                    var d = nodes[n];
                    snapshot.mines.Add(new MineOwnershipSnapshot
                    {
                        nodeId = d.NodeId,
                        owned = d.IsOwned,
                        ownerId = d.OwnerId,
                        resourceId = d.ResourceId,
                        dailyYield = d.DailyYield,
                    });
                }
            }

            // Farms
            var farmServices = UnityEngine.Object.FindObjectsOfType<FarmNodeService>(true);
            for (int i = 0; i < farmServices.Length; i++)
            {
                var svc = farmServices[i];
                var nodes = svc?.Nodes;
                if (nodes == null) continue;
                for (int n = 0; n < nodes.Count; n++)
                {
                    var d = nodes[n];
                    snapshot.farms.Add(new FarmOwnershipSnapshot
                    {
                        nodeId = d.NodeId,
                        owned = d.IsOwned,
                        ownerId = d.OwnerId,
                        weeklyPopulationYield = d.WeeklyPopulationYield,
                    });
                }
            }

            // World time
            var timeServices = UnityEngine.Object.FindObjectsOfType<WorldTimeService>(true);
            if (timeServices != null && timeServices.Length > 0)
            {
                var date = timeServices[0].CurrentDate;
                snapshot.timeDay = date.Day;
                snapshot.timeWeek = date.Week;
                snapshot.timeMonth = date.Month;
            }

            // Fog of war
            SevenCrowns.Map.FogOfWar.IFogOfWarSnapshotProvider fog = null;
            {
                var behaviours3 = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
                for (int i = 0; i < behaviours3.Length; i++)
                {
                    if (behaviours3[i] is SevenCrowns.Map.FogOfWar.IFogOfWarSnapshotProvider fp)
                    {
                        fog = fp;
                        break;
                    }
                }
            }
            if (fog != null)
            {
                var (w, h, data) = fog.Capture();
                snapshot.fogWidth = w;
                snapshot.fogHeight = h;
                snapshot.fogStates = data;
            }

            // Camera
            SevenCrowns.Map.ICameraSnapshotProvider camProv = null;
            {
                var behaviours4 = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
                for (int i = 0; i < behaviours4.Length; i++)
                {
                    if (behaviours4[i] is SevenCrowns.Map.ICameraSnapshotProvider cp)
                    {
                        camProv = cp;
                        break;
                    }
                }
            }
            if (camProv != null)
            {
                var pos = camProv.GetCameraPosition();
                snapshot.camX = pos.x;
                snapshot.camY = pos.y;
                snapshot.camZ = pos.z;
                snapshot.camSize = camProv.GetCameraOrthographicSize();
            }

            // Selection
            var currentHero = UnityEngine.Object.FindObjectOfType<SevenCrowns.Systems.CurrentHeroService>(true);
            if (currentHero != null && !string.IsNullOrWhiteSpace(currentHero.CurrentHeroId))
            {
                snapshot.selectedHeroId = currentHero.CurrentHeroId;
            }

            // Population
            {
                SevenCrowns.Systems.IPopulationService pop = null;
                var behavioursPop = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
                for (int i = 0; i < behavioursPop.Length; i++)
                {
                    if (behavioursPop[i] is SevenCrowns.Systems.IPopulationService p)
                    {
                        pop = p;
                        break;
                    }
                }
                if (pop != null)
                {
                    snapshot.populationHasSnapshot = true;
                    snapshot.populationAvailable = pop.GetAvailable();
                }
            }

            return snapshot;
        }

        public void Apply(WorldMapSnapshot snapshot)
        {
            if (snapshot == null) return;

            // Build lookup for faster hero matching
            var idByName = new Dictionary<string, HeroIdentity>(StringComparer.Ordinal);
            var all = UnityEngine.Object.FindObjectsOfType<HeroIdentity>(true);
            if (all != null)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    var id = all[i];
                    if (id != null && !string.IsNullOrWhiteSpace(id.HeroId) && !idByName.ContainsKey(id.HeroId))
                    {
                        idByName.Add(id.HeroId, id);
                    }
                }
            }

            // Apply heroes
            for (int i = 0; i < snapshot.heroes.Count; i++)
            {
                var hs = snapshot.heroes[i];
                if (string.IsNullOrWhiteSpace(hs.id)) continue;
                if (!idByName.TryGetValue(hs.id, out var id)) continue;

                // Level
                if (hs.level > 0) id.SetLevel(hs.level);

                // Position
                var comp = id.Agent;
                if (comp != null)
                {
                    comp.Agent?.ClearPath();
                    comp.TeleportTo(new GridCoord(hs.gridX, hs.gridY));
                    // MP
                    if (hs.mpMax > 0)
                    {
                        comp.Movement?.SetMax(hs.mpMax, refill: false);
                    }
                    if (hs.mpCurrent > 0 && comp.Movement != null)
                    {
                        int cur = comp.Movement.Current;
                        int delta = hs.mpCurrent - cur;
                        if (delta > 0) comp.Movement.Refund(delta);
                        else if (delta < 0) comp.Movement.SpendUpTo(-delta);
                    }
                }
            }

            // Apply resources
            if (snapshot.resources != null && snapshot.resources.Count > 0)
            {
                SevenCrowns.Map.Resources.IResourceWallet wallet = null;
                var behaviours2 = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
                for (int i = 0; i < behaviours2.Length && wallet == null; i++)
                {
                    if (behaviours2[i] is SevenCrowns.Map.Resources.IResourceWallet w)
                        wallet = w;
                }

                if (wallet != null)
                {
                    for (int i = 0; i < snapshot.resources.Count; i++)
                    {
                        var ra = snapshot.resources[i];
                        if (string.IsNullOrWhiteSpace(ra.id)) continue;
                        int current = wallet.GetAmount(ra.id);
                        int target = ra.amount;
                        int delta = target - current;
                        if (delta > 0)
                        {
                            wallet.Add(ra.id, delta);
                        }
                        else if (delta < 0)
                        {
                            wallet.TrySpend(ra.id, -delta);
                        }
                    }
                }
            }

            // Apply fog of war
            if (snapshot.fogWidth > 0 && snapshot.fogHeight > 0 && snapshot.fogStates != null && snapshot.fogStates.Length > 0)
            {
                SevenCrowns.Map.FogOfWar.IFogOfWarSnapshotProvider fog = null;
                var behaviours5 = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
                for (int i = 0; i < behaviours5.Length; i++)
                {
                    if (behaviours5[i] is SevenCrowns.Map.FogOfWar.IFogOfWarSnapshotProvider fp)
                    {
                        fog = fp;
                        break;
                    }
                }
                fog?.Apply(snapshot.fogWidth, snapshot.fogHeight, snapshot.fogStates);
            }

            // Apply camera
            if (snapshot.camSize > 0f)
            {
                SevenCrowns.Map.ICameraSnapshotProvider camProv2 = null;
                var behaviours6 = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
                for (int i = 0; i < behaviours6.Length; i++)
                {
                    if (behaviours6[i] is SevenCrowns.Map.ICameraSnapshotProvider cp)
                    {
                        camProv2 = cp;
                        break;
                    }
                }
                camProv2?.ApplyCameraState(new Vector3(snapshot.camX, snapshot.camY, snapshot.camZ), snapshot.camSize);
            }

            // Apply selection (also propagates to SelectedHeroService via CurrentHeroService)
            if (!string.IsNullOrWhiteSpace(snapshot.selectedHeroId))
            {
                var currentHero = UnityEngine.Object.FindObjectOfType<SevenCrowns.Systems.CurrentHeroService>(true);
                currentHero?.SetCurrentHeroById(snapshot.selectedHeroId);
            }

            // Apply population (weekly pool)
            if (snapshot.populationHasSnapshot)
            {
                SevenCrowns.Systems.IPopulationService pop = null;
                var behavioursPop2 = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
                for (int i = 0; i < behavioursPop2.Length; i++)
                {
                    if (behavioursPop2[i] is SevenCrowns.Systems.IPopulationService p)
                    {
                        pop = p;
                        break;
                    }
                }
                pop?.ResetTo(Mathf.Max(0, snapshot.populationAvailable));
            }

            // Apply resource nodes collected/remaining
            {
                var resourceServices = UnityEngine.Object.FindObjectsOfType<ResourceNodeService>(true);

                // If remainingResourceNodeIds is present (even empty), treat it as authoritative set
                if (snapshot.remainingResourceNodeIds != null)
                {
                    var remaining = new System.Collections.Generic.HashSet<string>(snapshot.remainingResourceNodeIds, System.StringComparer.Ordinal);
                    for (int s = 0; s < resourceServices.Length; s++)
                    {
                        var svc = resourceServices[s];
                        var nodes = svc?.Nodes;
                        if (nodes == null) continue;
                        var toCheck = new System.Collections.Generic.List<string>(nodes.Count);
                        for (int n = 0; n < nodes.Count; n++) toCheck.Add(nodes[n].NodeId);
                        for (int n = 0; n < toCheck.Count; n++)
                        {
                            var nodeId = toCheck[n];
                            if (!remaining.Contains(nodeId))
                            {
                                if (SevenCrowns.Map.ResourceNodeAuthoring.TryGetNode(nodeId, out var node))
                                {
                                    node.Collect();
                                }
                                else
                                {
                                    svc.Unregister(nodeId);
                                }
                            }
                        }
                    }
                }
                else if (snapshot.collectedResourceNodeIds != null)
                {
                    for (int i = 0; i < snapshot.collectedResourceNodeIds.Count; i++)
                    {
                        var id = snapshot.collectedResourceNodeIds[i];
                        if (string.IsNullOrWhiteSpace(id)) continue;
                        if (SevenCrowns.Map.ResourceNodeAuthoring.TryGetNode(id, out var node))
                        {
                            node.Collect();
                        }
                        else
                        {
                            for (int s = 0; s < resourceServices.Length; s++)
                            {
                                resourceServices[s]?.Unregister(id);
                            }
                        }
                    }
                }
            }

            // Apply world time
            if (snapshot.timeDay > 0 && snapshot.timeWeek > 0 && snapshot.timeMonth > 0)
            {
                var wt = UnityEngine.Object.FindObjectOfType<WorldTimeService>(true);
                if (wt != null)
                {
                    wt.ResetTo(new WorldDate(snapshot.timeDay, snapshot.timeWeek, snapshot.timeMonth));
                }
            }

            // Apply cities
            if (snapshot.cities != null && snapshot.cities.Count > 0)
            {
                var cityServices = UnityEngine.Object.FindObjectsOfType<CityNodeService>(true);
                for (int i = 0; i < snapshot.cities.Count; i++)
                {
                    var cs = snapshot.cities[i];
                    if (string.IsNullOrWhiteSpace(cs.nodeId)) continue;
                    for (int s = 0; s < cityServices.Length; s++)
                    {
                        var svc = cityServices[s];
                        if (svc != null && svc.TryGetById(cs.nodeId, out var cur))
                        {
                            var updated = new CityNodeDescriptor(
                                cur.NodeId,
                                cur.WorldPosition,
                                cur.EntryCoord,
                                cs.owned,
                                cs.ownerId ?? string.Empty,
                                (CityLevel)Mathf.Clamp(cs.level, 0, (int)CityLevel.Capital)
                            );
                            svc.RegisterOrUpdate(updated);
                            break;
                        }
                    }
                    // Update flag visuals via authoring if owned
                    if (cs.owned && SevenCrowns.Map.Cities.CityAuthoring.TryGetNode(cs.nodeId, out var cityAuth))
                    {
                        cityAuth.Claim(cs.ownerId ?? string.Empty);
                    }
                }
            }

            // Apply mines
            if (snapshot.mines != null && snapshot.mines.Count > 0)
            {
                var mineServices = UnityEngine.Object.FindObjectsOfType<MineNodeService>(true);
                for (int i = 0; i < snapshot.mines.Count; i++)
                {
                    var ms = snapshot.mines[i];
                    if (string.IsNullOrWhiteSpace(ms.nodeId)) continue;
                    for (int s = 0; s < mineServices.Length; s++)
                    {
                        var svc = mineServices[s];
                        if (svc != null && svc.TryGetById(ms.nodeId, out var cur))
                        {
                            var updated = new MineNodeDescriptor(
                                cur.NodeId,
                                cur.WorldPosition,
                                cur.EntryCoord,
                                ms.owned,
                                ms.ownerId ?? string.Empty,
                                string.IsNullOrWhiteSpace(ms.resourceId) ? cur.ResourceId : ms.resourceId,
                                ms.dailyYield > 0 ? ms.dailyYield : cur.DailyYield
                            );
                            svc.RegisterOrUpdate(updated);
                            break;
                        }
                    }
                    // Update flag visuals via authoring if owned
                    if (ms.owned && SevenCrowns.Map.Mines.MineAuthoring.TryGetNode(ms.nodeId, out var mineAuth))
                    {
                        mineAuth.Claim(ms.ownerId ?? string.Empty);
                    }
                }
            }

            // Apply farms
            if (snapshot.farms != null && snapshot.farms.Count > 0)
            {
                var farmServices = UnityEngine.Object.FindObjectsOfType<FarmNodeService>(true);
                for (int i = 0; i < snapshot.farms.Count; i++)
                {
                    var fs = snapshot.farms[i];
                    if (string.IsNullOrWhiteSpace(fs.nodeId)) continue;
                    for (int s = 0; s < farmServices.Length; s++)
                    {
                        var svc = farmServices[s];
                        if (svc != null && svc.TryGetById(fs.nodeId, out var cur))
                        {
                            var updated = new FarmNodeDescriptor(
                                cur.NodeId,
                                cur.WorldPosition,
                                cur.EntryCoord,
                                fs.owned,
                                fs.ownerId ?? string.Empty,
                                fs.weeklyPopulationYield > 0 ? fs.weeklyPopulationYield : cur.WeeklyPopulationYield
                            );
                            svc.RegisterOrUpdate(updated);
                            break;
                        }
                    }
                    // Update flag visuals via authoring if owned
                    if (fs.owned && SevenCrowns.Map.Farms.FarmAuthoring.TryGetNode(fs.nodeId, out var farmAuth))
                    {
                        farmAuth.Claim(fs.ownerId ?? string.Empty);
                    }
                }
            }
        }
    }
}
