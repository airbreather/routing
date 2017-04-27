////#define LIE

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime;

#if LIE
using Itinero.Algorithms.Contracted;
using Itinero.Algorithms.Contracted.Witness;
#else
using Itinero.Algorithms.Contracted.EdgeBased;
using Itinero.Algorithms.Contracted.EdgeBased.Witness;
#endif

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
        private const string LogFile = @"C:\Users\Joe\src\itinero-log.txt";

        private static void Main(string[] args)
        {
            if (!GCSettings.IsServerGC)
            {
                throw new Exception("Must be server.");
            }

            Constants.MemoryArrayFactory = new UnmanagedMemoryArrayFactory();
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

            var rdb = new RouterDb();
            Profiles.Vehicle[] veh = { Vehicle.Car };
            var targ = new RouterDbStreamTarget(rdb, veh, processRestrictions: true);
            ////using (var str = new FileStream(@"E:\planet-170417.osm", FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan))
            using (var str = new FileStream(@"E:\planet-170306.osm.pbf", FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan))
            {
                ////var src = new XmlOsmStreamSource(str);
                var src = new PBFOsmStreamSource(str);
                targ.RegisterSource(src.Progress().FilterBox(
                    left: -124.848974f,
                    top: 49.384358f,
                    right: -66.885444f,
                    bottom: 24.396308f,
                    completeWays: false));
                targ.Initialize();
                targ.Pull();
            }

            rdb.Sort();
            using (var str = new FileStream(@"E:\planet-170417.routerdb", FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.SequentialScan))
            {
                rdb.Serialize(str);
            }

            return;

            File.Delete(LogFile);

            var profile = Vehicle.Car.Fastest();

            Log(1);
            using (var fl1 = new FileStream(@"C:\Temp\planet-170306.routerdb", FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan))
            {
                RouterDb db = RouterDb.Deserialize(fl1);
                Log(2);

                var weightHandler = profile.DefaultWeightHandlerCached(db);

                // create the raw directed graph.
                ContractedDb contractedDb = null;

                if (!db.HasComplexRestrictions(profile))
                {
                    throw new NotSupportedException("derp");
                }

#if LIE
                // vertex-based is ok when no complex restrictions found.
                var contracted = new DirectedMetaGraph(ContractedEdgeDataSerializer.Size, weightHandler.MetaSize);
                var directedGraphBuilder = new DirectedGraphBuilder<float>(db.Network.GeometricGraph.Graph, contracted, weightHandler);
                directedGraphBuilder.Run();

                Log(3);

                // contract the graph.
                var priorityCalculator = new EdgeDifferencePriorityCalculator(contracted, new DykstraWitnessCalculator(int.MaxValue));
                priorityCalculator.DifferenceFactor = 5;
                priorityCalculator.DepthFactor = 5;
                priorityCalculator.ContractedFactor = 8;
                var hierarchyBuilder = new HierarchyBuilder<float>(contracted, priorityCalculator, new DykstraWitnessCalculator(int.MaxValue), weightHandler);
                hierarchyBuilder.Run();
#else
                var contracted = new DirectedDynamicGraph(weightHandler.DynamicSize);
                var directedGraphBuilder = new DirectedGraphBuilder<float>(db.Network.GeometricGraph.Graph, contracted, weightHandler);
                directedGraphBuilder.Run();

                Log(3);

                // contract the graph.
                var priorityCalculator = new EdgeDifferencePriorityCalculator<float>(contracted, weightHandler, new DykstraWitnessCalculator<float>(weightHandler, 4, 64));
                priorityCalculator.DifferenceFactor = 5;
                priorityCalculator.DepthFactor = 5;
                priorityCalculator.ContractedFactor = 8;
                var hierarchyBuilder = new HierarchyBuilder<float>(contracted, priorityCalculator, new DykstraWitnessCalculator<float>(weightHandler, int.MaxValue, 64), weightHandler, db.GetGetRestrictions(profile, null));
                hierarchyBuilder.Run();
#endif

                Log(4);

                contractedDb = new ContractedDb(contracted);

                // add the graph.
                db.AddContracted(profile, contractedDb);

                Log(5);
                using (var fl2 = File.Create(@"E:\planet-170306-contracted-car-fastest.routerdb"))
                {
                    db.SerializeContracted(profile, fl2);
                }
            }

            Log(6);
        }

        private static void Log(int step)
        {
            string msg = $"Step {step}: {DateTime.Now}";
            Console.WriteLine(msg);
            File.AppendAllText(LogFile, msg + Environment.NewLine);
        }
    }
}
