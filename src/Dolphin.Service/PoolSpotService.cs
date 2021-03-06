﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Dolphin.Service
{
    public class PoolSpotService : IPoolSpotService
    {
        private readonly ISettingsService settingsService;
        private readonly IList<Waypoint> visitedPoolspots = new List<Waypoint>();

        public PoolSpotService(ISettingsService settingsService)
        {
            this.settingsService = settingsService;
        }

        public Tuple<Point, Point> GetNextPoolSpot()
        {
            foreach (var wp in settingsService.MacroSettings.Poolspots)
            {
                if (wp.Enabled && !visitedPoolspots.Contains(wp))
                {
                    visitedPoolspots.Add(wp);

                    var waypoint = (Point)typeof(CommonCoordinate).GetField($"Map{wp.Name}").GetValue(null);

                    return Tuple.Create(ActSwitch(wp.Act), waypoint);
                }
            }

            visitedPoolspots.Clear();

            var first = settingsService.MacroSettings.Poolspots.FirstOrDefault(x => x.Enabled);
            if (first != null)
            {
                visitedPoolspots.Add(first);

                var waypoint = (Point)typeof(CommonCoordinate).GetField($"Map{first.Name}").GetValue(null);

                return Tuple.Create(ActSwitch(first.Act), waypoint);
            }

            return default;
        }

        private static Point ActSwitch(int act)
        {
            switch (act)
            {
                case 1:
                    return CommonCoordinate.MapAct1;

                case 2:
                    return CommonCoordinate.MapAct2;

                case 3:
                    return CommonCoordinate.MapAct3;

                case 4:
                    return CommonCoordinate.MapAct4;

                case 5:
                    return CommonCoordinate.MapAct5;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}