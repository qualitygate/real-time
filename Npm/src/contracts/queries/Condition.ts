import {Operator} from './Operator'

export const And = 'and'
export const Or = 'or'
export type ConditionJoinOperator = typeof And | typeof Or

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
	joinUsing?: ConditionJoinOperator

	/**
	 * Operator expressing what kind of comparison will be done between the field value and this Where value field.
	 */
	operator: Operator

	/**
	 * The value to compare with the field's.
	 */
	value: any
}