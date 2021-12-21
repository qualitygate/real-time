import moment from 'moment'

export interface Logger {
	debug(...data: any[]): void

	info(...data: any[]): void

	warn(...data: any[]): void

	error(...data: any[]): void
}

export class LoggerImpl implements Logger {
	private readonly _name: string

	constructor(name: string) {
		this._name = name
	}

	private static get now(): string {
		return moment().format('YYYY-MM-DD hh:mm:ss.SSS')
	}

	private get loggerInfo(): string {
		return `[${LoggerImpl.now}] ${this._name} -`
	}

	debug(...data: any[]): void {
		console.debug(this.loggerInfo, ...data)
	}

	error(...data: any[]): void {
		console.error(this.loggerInfo, data)
	}

	info(...data: any[]): void {
		console.info(this.loggerInfo, data)
	}

	warn(...data: any[]): void {
		console.warn(this.loggerInfo, data)
	}
}