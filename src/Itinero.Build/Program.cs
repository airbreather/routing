////#define REINIT
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime;

using Itinero.Algorithms.Contracted.EdgeBased;
using Itinero.Algorithms.Contracted.EdgeBased.Witness;

using Itinero.Algorithms.Search.Hilbert;
using Itinero.Algorithms.Weights;
using Itinero.Data.Contracted;
using Itinero.Data.Contracted.Edges;
using Itinero.Graphs.Directed;
using Itinero.Osm.Vehicles;

using Itinero.IO.Osm.Streams;
using OsmSharp.Streams;

using static System.FormattableString;
using static Itinero.Logging.Logger;

namespace Itinero.Build
{
    internal static class Program
    {
        private const string InputFile = @"C:\Temp\planet-170306-usa.routerdb";
        private const string OutputFile = @"E:\planet-170306-usa-contracted-car-fastest.routerdb";

        private static void Main(string[] args)
        {
            if (!GCSettings.IsServerGC)
            {
                throw new Exception("Must be server.");
            }

            ////Context.ArrayFactory = new OptimizedArrayFactory();
            Stopwatch sw = Stopwatch.StartNew();
            LogAction = (origin, level, message, parameters) =>
            {
                Console.WriteLine(Invariant($"{sw.ElapsedTicks / (double)Stopwatch.Frequency:N3} [{origin}]: {message}"));
                if (parameters != null)
                {
                    foreach (var kvp in parameters)
                    {
                        Console.WriteLine(Invariant($"        [{kvp.Key}]: {kvp.Value}"));
                    }
                }
            };

            OsmSharp.Logging.Logger.LogAction = (origin, level, message, parameters) =>
            {
                Console.WriteLine(Invariant($"{sw.ElapsedTicks / (double)Stopwatch.Frequency:N3} [{origin}]: {message}"));
                if (parameters != null)
                {
                    foreach (var kvp in parameters)
                    {
                        Console.WriteLine(Invariant($"        [{kvp.Key}]: {kvp.Value}"));
                    }
                }
            };

            var logger = new Logging.Logger(nameof(Program));

#if REINIT
            var rdb = new RouterDb();
            Profiles.Vehicle[] veh = { Vehicle.Car };
            var targ = new RouterDbStreamTarget(rdb, veh, processRestrictions: true);
            ////using (var str = new FileStream(@"E:\planet-170417.osm", FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan))
            using (var str = new FileStream(@"E:\planet-bits2.osm.pbf", FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan))
            {
                ////var src = new XmlOsmStreamSource(str);
                var src = new PBFOsmStreamSource(str);
                targ.RegisterSource(src.Progress()/*.FilterBox(
                    left: -124.848974f,
                    top: 49.384358f,
                    right: -66.885444f,
                    bottom: 24.396308f,
                    completeWays: false)*/);
                targ.Initialize();
                targ.Pull();
            }

            rdb.Sort();
            using (var str = new FileStream(@"E:\planet-bits2.routerdb", FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.SequentialScan))
            {
                rdb.Serialize(str);
            }

            logger.Log(Logging.TraceEventType.Information, "Done");
            return;
#endif

            var profile = Vehicle.Car.Fastest();

            logger.Log(Logging.TraceEventType.Information, "Before opening the file");
            using (var fl1 = new FileStream(InputFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan))
            {
                RouterDb db = RouterDb.Deserialize(fl1);
                logger.Log(Logging.TraceEventType.Information, "After RouterDb.Deserialize");

                var weightHandler = profile.DefaultWeightHandlerCached(db);

                // create the raw directed graph.
                ContractedDb contractedDb = null;

                if (!db.HasComplexRestrictions(profile))
                {
                    throw new NotSupportedException("derp");
                }

                var contracted = new DirectedDynamicGraph(weightHandler.DynamicSize);
                var directedGraphBuilder = new DirectedGraphBuilder<float>(db.Network.GeometricGraph.Graph, contracted, weightHandler);
                directedGraphBuilder.Run();

                logger.Log(Logging.TraceEventType.Information, "After DirectedGraphBuilder<float>.Run");

                // contract the graph.
                var priorityCalculator = new EdgeDifferencePriorityCalculator<float>(contracted, weightHandler, new DykstraWitnessCalculator<float>(weightHandler, 4, 64));
                priorityCalculator.DifferenceFactor = 5;
                priorityCalculator.DepthFactor = 5;
                priorityCalculator.ContractedFactor = 8;
                var hierarchyBuilder = new HierarchyBuilder<float>(contracted, priorityCalculator, new DykstraWitnessCalculator<float>(weightHandler, int.MaxValue, 64), weightHandler, db.GetGetRestrictions(profile, null));
                hierarchyBuilder.Run();

                logger.Log(Logging.TraceEventType.Information, "After HierarchyBuilder<float>.Run");

                contractedDb = new ContractedDb(contracted);

                // add the graph.
                db.AddContracted(profile, contractedDb);

                logger.Log(Logging.TraceEventType.Information, "After db.AddContracted");
                using (var fl2 = File.Create(OutputFile))
                {
                    db.SerializeContracted(profile, fl2);
                }
            }

            logger.Log(Logging.TraceEventType.Information, "After db.SerializeContracted");
        }
    }
}
