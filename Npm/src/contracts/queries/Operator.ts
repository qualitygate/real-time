export const And = 'and'
export const Or = 'or'

/**
 * Type of the operator used to chain boolean expressions.
 */
export type LogicalOperator = typeof And | typeof Or

export const Equal = '='
export const NotEqual = '<>'
export const Matches = 'matches'

/**
 * Defines the type of operators supported in Queries Where conditions.
 */
export type RelationalOperator = typeof Equal | typeof NotEqual | typeof Matches