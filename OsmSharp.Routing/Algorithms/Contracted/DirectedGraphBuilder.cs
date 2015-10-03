﻿// OsmSharp - OpenStreetMap (OSM) SDK
// Copyright (C) 2015 Abelshausen Ben
// 
// This file is part of OsmSharp.
// 
// OsmSharp is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// OsmSharp is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with OsmSharp. If not, see <http://www.gnu.org/licenses/>.

using OsmSharp.Collections.Tags.Index;
using OsmSharp.Routing.Graphs.Directed;
using OsmSharp.Routing.Profiles;
using System;
using System.Collections.Generic;

namespace OsmSharp.Routing.Algorithms.Contracted
{
    /// <summary>
    /// Builds a directed graph from a regular graph.
    /// </summary>
    public class DirectedGraphBuilder : AlgorithmBase
    {
        private readonly OsmSharp.Routing.Graphs.Graph _source;
        private readonly DirectedGraph _target;
        private readonly Func<ushort, Factor> _getFactor;

        /// <summary>
        /// Creates anew graph builder.
        /// </summary>
        public DirectedGraphBuilder(OsmSharp.Routing.Graphs.Graph source, DirectedGraph target, Func<ushort, Factor> getFactor)
        {
            _source = source;
            _target = target;
            _getFactor = getFactor;
        }

        /// <summary>
        /// Executes the actual run.
        /// </summary>
        protected override void DoRun()
        {
            float distance;
            ushort profile;
            bool? direction = null;
            
            var factors = new Dictionary<ushort, Factor>();
            var edgeEnumerator = _source.GetEdgeEnumerator();
            for(uint vertex = 0; vertex < _source.VertexCount; vertex++)
            {
                edgeEnumerator.MoveTo(vertex);
                while(edgeEnumerator.MoveNext())
                {
                    OsmSharp.Routing.Data.EdgeDataSerializer.Deserialize(edgeEnumerator.Data0, 
                        out distance, out profile);
                    var factor = Factor.NoFactor;
                    if(!factors.TryGetValue(profile, out factor))
                    { // get from profile.
                        factor = _getFactor(profile);
                        factors[profile] = factor;
                    }

                    if(factor.Value != 0)
                    {
                        direction = null;
                        if(factor.Direction == 1)
                        {
                            direction = true;
                        }
                        else if(factor.Direction == 2)
                        {
                            direction = false;
                        }
                        var data = OsmSharp.Routing.Data.Contracted.ContractedEdgeDataSerializer.Serialize(
                            distance * factor.Value, direction, Constants.NO_VERTEX);

                        _target.AddEdge(edgeEnumerator.From, edgeEnumerator.To, data);
                    }
                }
            }

            this.HasSucceeded = true;
        }
    }
}