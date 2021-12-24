import {LoggerImpl} from './Logger'
import {stub} from 'sinon'

describe('Logger', () => {
	let debugStub, infoStub, warnStub, errorStub

	const loggerName = 'Logger: #1'
	const message = "Some message"

	beforeEach(() => {
		debugStub = stub(console, 'debug')
		infoStub = stub(console, 'info')
		warnStub = stub(console, 'warn')
		errorStub = stub(console, 'error')
	})

	afterEach(() => {
		debugStub.restore()
		infoStub.restore()
		warnStub.restore()
		errorStub.restore()
	})

	describe('debug', () => {
		it('correctly debugs given objects', () => {
			// Given
			const logger = new LoggerImpl(loggerName)

			// When
			logger.debug(message)

			// Then
			const regexString = `^\\[\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}\\.\\d{3}\\] ${loggerName} -$`
			expect(debugStub.calledOnce).toBeTruthy()
			const log = debugStub.firstCall.args[0]
			expect(debugStub.firstCall.args[1]).toBe(message)
			expect(log).toMatch(new RegExp(regexString))
		})
	})

	describe('info', () => {
		it('correctly informs given objects', () => {
			// Given
			const logger = new LoggerImpl(loggerName)

			// When
			logger.info(message)

			// Then
			const regexString = `^\\[\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}\\.\\d{3}\\] ${loggerName} -$`
			expect(infoStub.calledOnce).toBeTruthy()
			const log = infoStub.firstCall.args[0]
			expect(infoStub.firstCall.args[1]).toBe(message)
			expect(log).toMatch(new RegExp(regexString))
		})
	})

	describe('warn', () => {
		it('correctly debugs given objects', () => {
			// Given
			const logger = new LoggerImpl(loggerName)

			// When
			logger.warn(message)

			// Then
			const regexString = `^\\[\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}\\.\\d{3}\\] ${loggerName} -$`
			expect(warnStub.calledOnce).toBeTruthy()
			const log = warnStub.firstCall.args[0]
			expect(warnStub.firstCall.args[1]).toBe(message)
			expect(log).toMatch(new RegExp(regexString))
		})
	})

	describe('error', () => {
		it('correctly errors given objects', () => {
			// Given
			const logger = new LoggerImpl(loggerName)

			// When
			logger.error(message)

			// Then
			const regexString = `^\\[\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}\\.\\d{3}\\] ${loggerName} -$`
			expect(errorStub.calledOnce).toBeTruthy()
			const log = errorStub.firstCall.args[0]
			expect(errorStub.firstCall.args[1]).toBe(message)
			expect(log).toMatch(new RegExp(regexString))
		})
	})
})