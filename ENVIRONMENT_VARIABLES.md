# Environment Variables - Gametopia.Domain

## GitHub Actions Secrets

Todos estos secretos deben estar configurados en:
GitHub > Repository Settings > Secrets and variables > Actions

### Docker Registry

```
DOCKER_USERNAME = tu_usuario_docker
DOCKER_PASSWORD = tu_token_docker
```

### Database Connections

```
SQLSERVER_DEV_CONNECTIONSTRING = Server=sqlserver-dev,1433;Database=Gametopia.Domain;User Id=sa;Password=YourPassword;Encrypt=false;
SQLSERVER_PROD_CONNECTIONSTRING = Server=sqlserver-prod,1433;Database=Gametopia.Domain;User Id=sa;Password=YourPassword;Encrypt=false;
```

### Kubernetes Config

```
KUBECONFIG_DEV = <base64 encoded kubeconfig>
KUBECONFIG_PROD = <base64 encoded kubeconfig>
```

---

## Kubernetes ConfigMap Variables

### Development Environment

```
ASPNETCORE_ENVIRONMENT = Development
Swagger__Enabled = true
ASPNETCORE_URLS = http://+:8080
```

### Production Environment

```
ASPNETCORE_ENVIRONMENT = Production
Swagger__Enabled = false
ASPNETCORE_URLS = http://+:8080
```

---

## .NET Application Settings

### Development (`appsettings.Development.json`)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Production (`appsettings.Production.json`)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Swagger": {
    "Enabled": false
  }
}
```

---

## Encoding Kubeconfig para GitHub Secrets

```bash
# Obtener kubeconfig del servidor k3s
cat ~/.kube/config | base64 -w 0

# Copiar el resultado completo (sin saltos de línea) al secret en GitHub
```

**Importante:** Usar `-w 0` para evitar saltos de línea en la codificación base64

---

## Versioning

### En CI/CD (Automático en Prod)

- **Dev:** Siempre `0.0.0` o `latest`
- **Prod:** Versionado automático (incremento de patch)
  - Ejemplo: 0.0.0 → 0.0.1 → 0.0.2

### En .csproj

```xml
<PropertyGroup>
  <Version>0.0.1</Version>
</PropertyGroup>
```

---

## Docker Tags

```
Dev:  hectorgf/gametopia-domain:latest
Prod: hectorgf/gametopia-domain:v0.0.1
```

---

## Health Check Endpoints

```bash
GET /api/health              # Liveness
GET /api/health/ready        # Readiness
GET /api/health/info         # Info
```

---

**Última actualización:** Febrero 2026
