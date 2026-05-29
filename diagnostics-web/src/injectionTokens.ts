import {InjectionToken} from "@angular/core";

export const BASE_API_URL = new InjectionToken<string>('BASE_API_URL')

// Optional API key sent via accessTokenFactory when the service runs in ApiKey auth mode (H1).
// Empty by default (matches a hub in the default None mode).
export const BASE_API_KEY = new InjectionToken<string>('BASE_API_KEY')