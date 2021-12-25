export const Equal = '='
export const NotEqual = '<>'
export const Matches = 'matches'

/**
 * Defines the type of operators supported in Queries Where conditions.
 */
export type Operator = typeof Equal | typeof NotEqual | typeof Matches