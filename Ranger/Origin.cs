using System.Linq;

namespace Ranger
{
    /// <summary>
    /// Hold info about starting points.
    /// </summary>
    public partial class Origin : IGeoLocation
    {
        /// <summary>
        /// Ctor
        /// Loads the origin with the given name. Returns null if the origin is not present in the DB.
        /// </summary>
        public static Origin Load(string connectionString, string originName)
        {
            var dbContext = new RangerDataContext(connectionString);
            
            return dbContext.Origins.SingleOrDefault(o => o.Name == originName);
        }
    }
}