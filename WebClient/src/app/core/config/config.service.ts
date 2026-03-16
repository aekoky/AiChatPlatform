import { Injectable } from '@angular/core';

export interface AppConfig {
  keycloak: { url: string; realm: string; clientId: string };
  chatApiUrl: string;
  notificationUrl: string;
}

@Injectable({ providedIn: 'root' })
export class ConfigService {
  private config!: AppConfig;

  load(): Promise<void> {
    return fetch('/assets/config.json')
      .then(r => r.json())
      .then(c => (this.config = c));
  }

  get(): AppConfig {
    return this.config;
  }
}
