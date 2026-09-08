import type { ServerOptions } from 'node:https'

export function getHttpsOptions(env: Record<string, string>): ServerOptions | undefined
