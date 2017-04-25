﻿/*
 *  Licensed to SharpSoftware under one or more contributor
 *  license agreements. See the NOTICE file distributed with this work for 
 *  additional information regarding copyright ownership.
 * 
 *  SharpSoftware licenses this file to you under the Apache License, 
 *  Version 2.0 (the "License"); you may not use this file except in 
 *  compliance with the License. You may obtain a copy of the License at
 * 
 *       http://www.apache.org/licenses/LICENSE-2.0
 * 
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

namespace Itinero
{
    /// <summary>
    /// Contains constants.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// A constant to use when no edge was found, is available or set.
        /// </summary>
        public const uint NO_EDGE = uint.MaxValue;

        /// <summary>
        /// A constant to use when no vertex was found, is available or set.
        /// </summary>
        public const uint NO_VERTEX = uint.MaxValue - 1;

        /// <summary>
        /// A maximum search distance.
        /// </summary>
        public const float SearchDistanceInMeter = 50;

        /// <summary>
        /// A default maximum edge distance.
        /// </summary>
        public const float DefaultMaxEdgeDistance = 5000f;
        
        /// <summary>
        /// An empty sequence/restriction.
        /// </summary>
        public static uint[] EMPTY_SEQUENCE = new uint[0];

        public static IMemoryArrayFactory MemoryArrayFactory = new DefaultMemoryArrayFactory();
    }
}