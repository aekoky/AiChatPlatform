import { Injectable } from '@angular/core';
import Keycloak from 'keycloak-js';
import { AppConfig } from '../config/config.service';

@Injectable({ providedIn: 'root' })
export class KeycloakService {
  private keycloak!: Keycloak;

  init(config: AppConfig): Promise<void> {
    this.keycloak = new Keycloak({
      url: config.keycloak.url,
      realm: config.keycloak.realm,
      clientId: config.keycloak.clientId
    });

    return this.keycloak
      .init({
        onLoad: 'login-required',
        checkLoginIframe: false,
        pkceMethod: 'S256'
      })
      .then(() => {});
  }

  get authenticated(): boolean {
    return this.keycloak?.authenticated || false;
  }

  async getValidToken(): Promise<string> {
    if (!this.keycloak?.authenticated) {
      return '';
    }

    try {
      await this.keycloak.updateToken(30);
      return this.keycloak.token || '';
    } catch (err) {
      return '';
    }
  }

  logout(): void {
    this.keycloak.logout({ redirectUri: window.location.origin });
  }

  get username(): string {
    return this.keycloak.tokenParsed?.['preferred_username'] ?? '';
  }

  get userId(): string {
    return this.keycloak.tokenParsed?.['sub'] ?? '';
  }
}
