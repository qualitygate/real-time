namespace QualityGate.RealTime.Notifications
{
    /// <summary>
    ///     Contains the names of the methods that connected client browsers respond to.
    /// </summary>
    public static class ClientMethods
    {
        /// <summary>
        ///     Name of the method to signal clients that an entity has changed.
        /// </summary>
        public const string EntityChanged = "entityChanged";
        
        /// <summary>
        ///     Name of the method to signal clients that a page of entities has changed.
        /// </summary>
        public const string PageChanged = "pageChanged";
    }
}