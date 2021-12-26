/**
 * Describes the information about a portion (also known as a Page) of the universe (a database table for example) of
 * items of a certain type.
 */
export interface PageInfo<T> {
	/**
	 * Total Items in the universe of their type.
	 */
	total: number

	/**
	 * Actual Items in the current potion.
	 */
	page: number

	/**
	 * Describes how many actual items are contained in the current portion.
	 */
	size: number

	/**
	 * Actual Items in the current potion.
	 */
	items: T[]
}