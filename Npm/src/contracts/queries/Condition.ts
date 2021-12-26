import {RelationalOperator, LogicalOperator} from './Operator'

/**
 * Defines a where condition in a query.
 */
export interface Condition {
	/**
	 * The field to evaluate its value matches some condition.
	 */
	field: string

	/**
	 * An operator to join the current condition with the next one.
	 */
	joinUsing?: LogicalOperator

	/**
	 * Determines whether to add or not a parenthesis to left part of this condition.
	 */
	leftParenthesis?: boolean,

	/**
	 * Operator expressing what kind of comparison will be done between the field value and this Where value field.
	 */
	operator: RelationalOperator

	/**
	 * Determines whether to add or not a parenthesis to right part of this condition.
	 */
	rightParenthesis?: boolean,

	/**
	 * The value to compare with the field's.
	 */
	value: any
}