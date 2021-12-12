import {Condition} from './Condition'
import {OrderBy} from './OrderBy'

/**
 * Representation of a query definition. A query defines a desired subset of the universe of all entities of a certain
 * type.
 */
export interface Query {
	/**
	 * Name of the query. It should be unique.
	 */
	name: string,

	/**
	 * Name of the table of the entities to query.
	 */
	table: string

	/**
	 * Set of fields representing the projection we want from each entity. If null, it means there are desired all the
	 * fields.
	 */
	fields?: string[]

	/**
	 * Set of conditions to evaluate when executing the query (can be understood as thw where(s) in a SQL query).
	 */
	conditions?: Condition[]

	/**
	 * Sets the ordering criteria of this query results.
	 */
	orderBy?: OrderBy
}
