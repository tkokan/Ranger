namespace Ranger
{
    /// <summary>
    /// Result returned by Google APIs
    /// </summary>
    public class Result
    {
        public string[] destination_addresses;
        public string[] origin_addresses;
        public Row[] rows;
        public StatusEnum status;

        /// <summary>
        /// Ctor
        /// </summary>
        public Result()
        {
            destination_addresses = null;
            origin_addresses = null;
            rows = null;
            status = StatusEnum.NA;
        }
    }
}
