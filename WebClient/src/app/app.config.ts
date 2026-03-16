import { ApplicationConfig, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { routes } from './app.routes';
import { ConfigService } from './core/config/config.service';
import { KeycloakService } from './core/auth/keycloak.service';
import { apiInterceptor } from './core/api/api.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([apiInterceptor])),
    provideAnimationsAsync(),
    {
      provide: APP_INITIALIZER,
      useFactory: (config: ConfigService, keycloak: KeycloakService) =>
        () => config.load().then(() => keycloak.init(config.get())),
      deps: [ConfigService, KeycloakService],
      multi: true
    }
  ]
};
