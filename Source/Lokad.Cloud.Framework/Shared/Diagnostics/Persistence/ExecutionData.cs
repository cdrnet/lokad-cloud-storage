#region (c)2009-2011 Lokad - New BSD license
// Company: http://www.lokad.com
// This code is released under the terms of the new BSD licence
#endregion

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml.Serialization;

// CAUTION: do not touch namespace as it would be likely to break the persistence of this entity.
namespace Lokad.Diagnostics.Persist
{
    /// <summary>
    /// Diagnostics: Persistence class for aggregated method calls and timing.
    /// </summary>
    [Serializable]
    [DataContract]
    [DebuggerDisplay("{Name}: {OpenCount}, {RunningTime}")]
    public sealed class ExecutionData
    {
        /// <summary>
        /// Name of the executing method
        /// </summary>
        [XmlAttribute]
        [DataMember(Order = 1)]
        public string Name { get; set; }

        /// <summary>
        /// Number of times the counter has been opened
        /// </summary>
        [XmlAttribute, DefaultValue(0)]
        [DataMember(Order = 2)]
        public long OpenCount { get; set; }

        /// <summary>
        /// Gets or sets the counter has been closed
        /// </summary>
        /// <value>The close count.</value>
        [XmlAttribute, DefaultValue(0)]
        [DataMember(Order = 3)]
        public long CloseCount { get; set; }

        /// <summary>
        /// Total execution count of the method in ticks
        /// </summary>
        [XmlAttribute, DefaultValue(0)]
        [DataMember(Order = 4)]
        public long RunningTime { get; set; }

        /// <summary>
        /// Method-specific counters
        /// </summary>
        [DataMember(Order = 5)]
        public long[] Counters { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionData"/> class.
        /// </summary>
        public ExecutionData()
        {
            Counters = new long[0];
        }
    }
}