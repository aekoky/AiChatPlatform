import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, switchMap } from 'rxjs';
import { KeycloakService } from '../auth/keycloak.service';

export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  const keycloak = inject(KeycloakService);
  return from(keycloak.getValidToken()).pipe(
    switchMap(token => {
      if (!token) {
        return next(req);
      }
      
      return next(req.clone({
        setHeaders: { Authorization: `Bearer ${token}` }
      }));
    })
  );
};

