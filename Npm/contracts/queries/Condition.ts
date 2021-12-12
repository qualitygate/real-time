import {Operator} from './Operator'

/**
 * Defines a where condition in a query.
 */
export interface Condition {
	/**
	 * The field to evaluate its value matches some condition.
	 */
	field: string,

	/**
	 * Operator expressing what kind of comparison will be done between the field value and this Where value field.
	 */
	operator: Operator,

	/**
	 * The value to compare with the field's.
	 */
	value: any
}