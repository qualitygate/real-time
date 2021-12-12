using System;
using System.Collections.Generic;

namespace QualityGate.RealTime.Queries
{
    /// <summary>
    ///     Contains order-by sorting definition.
    /// </summary>
    public record OrderBy
    {
        /// <summary>
        ///     Gets or sets the enumerable of names of the fields to order by.
        /// </summary>
        public IEnumerable<string>? Fields { get; set; } = Array.Empty<string>();

        /// <summary>
        ///     Gets or sets a boolean saying whether the ordering is ascending (if true) or descending.
        /// </summary>
        public bool Ascending { get; set; } = true;
    }
}