import {Entity} from '../contracts'

export const Upsert = 0
export const Delete = 1
export type ChangeType = typeof Upsert | typeof Delete

export interface Change {
	entity: Entity,
	type: ChangeType
}

export const AddQuery = 'AddQuery'
export const ModifyQuery = 'ModifyQuery'
export const RemoveQuery = 'RemoveQuery'
export type ServerFunction = typeof AddQuery | typeof ModifyQuery | typeof RemoveQuery

export const EntityChanged = 'entityChanged'
export type ClientFunction = typeof EntityChanged