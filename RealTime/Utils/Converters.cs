using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using QualityGate.RealTime.Queries;

namespace QualityGate.RealTime.Utils
{
    /// <summary>
    ///     Contains members to easily convert back and forth between <see cref="Query"/> and <see cref="QueryDto"/>.
    /// </summary>
    public static class QueryConverters
    {
        /// <summary>
        ///     Converts the given query DTO into a query.
        /// </summary>
        /// <param name="queryDto">DTO to convert from.</param>
        /// <param name="connectionId">Identifies the connection of the client sending the DTO.</param>
        /// <returns>
        ///     A <see cref="Query"/> containing the data in the provided DTO and the given connection id.
        /// </returns>
        public static Query ToQuery(this QueryDto queryDto, string connectionId)
        {
            static Condition ToCondition(ConditionDto c) => c;

            if (queryDto.Page is not null || queryDto.Size is not null)
            {
                return new PaginatedQuery(connectionId, queryDto.Name, queryDto.Table, queryDto.Page ?? 0, queryDto.Size ?? 0)
                {
                    Fields = queryDto.Fields,
                    Conditions = queryDto.Conditions?.Select(ToCondition).ToArray(),
                    OrderBy = queryDto.OrderBy
                };
            }

            return new Query(connectionId, queryDto.Name, queryDto.Table)
            {
                Fields = queryDto.Fields,
                Conditions = queryDto.Conditions?.Select(ToCondition).ToArray(),
                OrderBy = queryDto.OrderBy
            };
        }

        /// <summary>
        ///     Transforms the given object into its JSON string representation.
        /// </summary>
        /// <param name="object">The object to transform to JSON.</param>
        /// <typeparam name="T">The type of the object to transform to JSON.</typeparam>
        /// <returns>An string representing being the JSON format of <paramref name="object" />.</returns>
        public static string ToJson<T>(this T @object)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            return JsonConvert.SerializeObject(@object, settings);
        }
    }
}