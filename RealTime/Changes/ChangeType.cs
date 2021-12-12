namespace QualityGate.RealTime.Changes
{
    /// <summary>
    ///     Represents the type of changes an entity may experience.
    /// </summary>
    public enum ChangeType
    {
        /// <summary>
        ///     The entity was added or updated.
        /// </summary>
        Upsert,

        /// <summary>
        ///     The entity was deleted.
        /// </summary>
        Delete
    }
}