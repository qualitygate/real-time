/**
 * Defines ordering in a query. Says by which fields to order and by wish fashion (ascending or descending).
 */
export interface OrderBy {
	/**
	 * Sets the names of the field to order the query results by. The order matters.
	 */
	fields: string[]

	/**
	 * Sets whether the query results must be ordered ascending or descending.
	 */
	ascending: boolean
}