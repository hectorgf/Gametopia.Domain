# CI/CD Pipeline - Gametopia.Domain

Documentación completa del pipeline automatizado con GitHub Actions y k3s.

## 📋 Tabla de Contenidos

- [Requisitos](#requisitos)
- [Arquitectura](#arquitectura)
- [Configuración de GitHub Secrets](#configuración-de-github-secrets)
- [Workflow de Deploy](#workflow-de-deploy)
- [Gestión de Versiones](#gestión-de-versiones)
- [Kubernetes Deployments](#kubernetes-deployments)
- [Monitoreo y Troubleshooting](#monitoreo-y-troubleshooting)

---

## 🔧 Requisitos

### Self-Hosted Runner
- Ubuntu Server 22.04 LTS+
- Docker instalado y en ejecución
- kubectl configurado con acceso a k3s
- k3s v1.28+

### k3s
- Namespaces: `gametopia-dev`, `gametopia-prod`
- SQL Server accesible desde k3s

### Secretos en GitHub
Los siguientes secretos deben estar configurados en el repositorio:

```
DOCKER_USERNAME          # Usuario de Docker Hub
DOCKER_PASSWORD          # Token/Password de Docker Hub
SQLSERVER_DEV_CONNECTIONSTRING    # Connection string para dev
SQLSERVER_PROD_CONNECTIONSTRING   # Connection string para prod
KUBECONFIG_DEV           # Kubeconfig (base64) para dev
KUBECONFIG_PROD          # Kubeconfig (base64) para prod
```

---

## 🏗️ Arquitectura

### Ambientes

```
┌─────────────────────┐
│   Git Repository    │
│                     │
├─────────────────────┤
│  develop → PR       │ ──► GitHub Actions ──► Deploy a Dev
│  main → Push        │ ──► GitHub Actions ──► Deploy a Prod
└─────────────────────┘

Dev Environment (gametopia-dev):
├─ 1 Replica
├─ Swagger: Habilitado
├─ Logs: Information level
└─ HPA: 1-2 replicas, CPU 70%, Memory 80%

Prod Environment (gametopia-prod):
├─ 2+ Replicas (HA)
├─ Swagger: Deshabilitado
├─ Logs: Warning level
├─ Network Policy: Habilitada
├─ PodDisruptionBudget: minAvailable=1
└─ HPA: 2-4 replicas, CPU 60%, Memory 70%
```

---

## 🔐 Configuración de GitHub Secrets

### 1. Docker Hub Credentials

```bash
# En GitHub > Settings > Secrets and variables > Actions

DOCKER_USERNAME=your_docker_username
DOCKER_PASSWORD=your_docker_token_or_password
```

### 2. SQL Server Connection Strings

```bash
# Development
SQLSERVER_DEV_CONNECTIONSTRING="Server=sqlserver-dev.local,1433;Database=Gametopia.Domain;User Id=sa;Password=YourPassword;Encrypt=false;"

# Production
SQLSERVER_PROD_CONNECTIONSTRING="Server=sqlserver-prod.local,1433;Database=Gametopia.Domain;User Id=sa;Password=YourPassword;Encrypt=false;"
```

### 3. Kubeconfig Files (Base64 Encoded)

```bash
# En el self-hosted runner:
cat ~/.kube/config | base64 -w 0 > /tmp/kubeconfig.b64

# Copiar contenido de /tmp/kubeconfig.b64 a:
KUBECONFIG_DEV  # kubeconfig para dev
KUBECONFIG_PROD # kubeconfig para prod
```

---

## 🔄 Workflow de Deploy

### Trigger: Push a `develop` (Dev Environment)

1. **Checkout** del código
2. **Tests** - dotnet test (todos los proyectos de prueba)
3. **Build** - dotnet build Release
4. **Docker Build & Push**
   - Tag: `latest`
   - Imagen: `hectorgf/gametopia-domain:latest`
5. **Deploy a k3s**
   - Namespace: `gametopia-dev`
   - Replicas: 1
   - Espera rollout

### Trigger: Push a `main` (Prod Environment)

1. **Checkout** del código
2. **Versioning** - Incrementa versión (0.0.0 → 0.0.1)
3. **Tests** - dotnet test (todos los proyectos de prueba)
4. **Build** - dotnet build Release
5. **Docker Build & Push**
   - Tag: `v0.0.1`
   - Imagen: `hectorgf/gametopia-domain:v0.0.1`
6. **Deploy a k3s**
   - Namespace: `gametopia-prod`
   - Replicas: 2+
   - Espera rollout

---

## 📦 Gestión de Versiones

### Archivo: `.csproj`

La versión se define en `Gametopia.Domain.Api.csproj`:

```xml
<PropertyGroup>
  <Version>0.0.1</Version>
  <TargetFramework>net10.0</TargetFramework>
  ...
</PropertyGroup>
```

### Scripts de Actualización

#### Bash (Linux en self-hosted runner)

```bash
# Ver versión actual
./scripts/update-version.sh --get

# Incrementar versión
./scripts/update-version.sh --increment
```

#### PowerShell (Desarrollo local)

```powershell
# Ver versión actual
.\scripts\Update-Version.ps1 -Action Get

# Incrementar versión
.\scripts\Update-Version.ps1 -Action Increment
```

### Flujo de Versionado

- **Dev**: Siempre `0.0.0` o `latest`
- **Prod**: Versionado automático (patch version)
  - Ejemplo: 0.0.0 → 0.0.1 → 0.0.2

---

## ☸️ Kubernetes Deployments

### Deployment Dev (`k8s/deployment-dev.yml`)

```yaml
Namespace: gametopia-dev
Replicas: 1
Strategy: RollingUpdate
Resources:
  Requests: CPU 100m, Memory 256Mi
  Limits: CPU 500m, Memory 512Mi
HPA: 1-2 replicas (CPU 70%, Memory 80%)
ConfigMap: Swagger=true, Environment=Development
```

**Aplicar manualmente:**
```bash
kubectl apply -f k8s/deployment-dev.yml
```

### Deployment Prod (`k8s/deployment-prod.yml`)

```yaml
Namespace: gametopia-prod
Replicas: 2
Strategy: RollingUpdate (maxSurge=1)
Resources:
  Requests: CPU 250m, Memory 512Mi
  Limits: CPU 1Gi, Memory 1Gi
HPA: 2-4 replicas (CPU 60%, Memory 70%)
ConfigMap: Swagger=false, Environment=Production
NetworkPolicy: Habilitada para prod
PodDisruptionBudget: minAvailable=1
PodAntiAffinity: Preferida en diferentes nodos
```

**Aplicar manualmente:**
```bash
kubectl apply -f k8s/deployment-prod.yml
```

### ConfigMap y Secrets

#### ConfigMap (Ambiente y Swagger)
```yaml
ASPNETCORE_ENVIRONMENT: Development|Production
Swagger__Enabled: true|false
ASPNETCORE_URLS: http://+:8080
```

#### Secrets (Base de Datos)
```yaml
ConnectionStrings__DefaultConnection: (desde GitHub Secrets)
```

---

## 🏥 Health Checks

El controlador `HealthController` proporciona endpoints para K8s:

- **GET /api/health** - Liveness probe (¿está vivo?)
  - Response: `{ status: "healthy", timestamp, version }`

- **GET /api/health/ready** - Readiness probe (¿está listo?)
  - Response: `{ status: "ready", timestamp, version }`

- **GET /api/health/info** - Información de la aplicación
  - Response: `{ environment, version, timestamp }`

**Configuración en K8s:**
```yaml
livenessProbe:
  httpGet:
    path: /api/health
    port: 8080
  initialDelaySeconds: 15
  periodSeconds: 30
  failureThreshold: 3

readinessProbe:
  httpGet:
    path: /api/health/ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 10
  failureThreshold: 2
```

---

## 📊 Monitoreo y Troubleshooting

### Ver estado del deployment

```bash
# Dev
kubectl get deployment -n gametopia-dev
kubectl get pods -n gametopia-dev
kubectl logs -n gametopia-dev deployment/gametopia-domain-api --tail=50

# Prod
kubectl get deployment -n gametopia-prod
kubectl get pods -n gametopia-prod
kubectl logs -n gametopia-prod deployment/gametopia-domain-api --tail=50
```

### Ver eventos

```bash
# Dev
kubectl describe deployment gametopia-domain-api -n gametopia-dev

# Prod
kubectl describe deployment gametopia-domain-api -n gametopia-prod
```

### Verificar health checks

```bash
# Port-forward al servicio
kubectl port-forward -n gametopia-dev svc/gametopia-domain-api 8080:8080

# En otro terminal
curl http://localhost:8080/api/health
curl http://localhost:8080/api/health/ready
curl http://localhost:8080/api/health/info
```

### Ver HPA status

```bash
# Dev
kubectl get hpa -n gametopia-dev

# Prod
kubectl get hpa -n gametopia-prod
```

### Escalar manualmente

```bash
# Dev
kubectl scale deployment gametopia-domain-api -n gametopia-dev --replicas=2

# Prod
kubectl scale deployment gametopia-domain-api -n gametopia-prod --replicas=3
```

### Rollback a versión anterior

```bash
# Ver historial
kubectl rollout history deployment/gametopia-domain-api -n gametopia-prod

# Revertir a revisión anterior
kubectl rollout undo deployment/gametopia-domain-api -n gametopia-prod --to-revision=2
```

---

## 🐳 Docker Registry

### Imágenes

```
Dev:  hectorgf/gametopia-domain:latest
Prod: hectorgf/gametopia-domain:v0.0.1
```

### Construcción local

```bash
docker build -t hectorgf/gametopia-domain:test .
docker run -p 8080:8080 hectorgf/gametopia-domain:test
```

### Push manual

```bash
docker login
docker build -t hectorgf/gametopia-domain:v0.0.1 .
docker push hectorgf/gametopia-domain:v0.0.1
```

---

## 📝 Checklist de Configuración

- [ ] Repositorio en GitHub (develop y main branches)
- [ ] Self-hosted runner configurado en Ubuntu Server
- [ ] Docker instalado en self-hosted runner
- [ ] k3s con namespaces `gametopia-dev` y `gametopia-prod`
- [ ] SQL Server accesible (dev y prod)
- [ ] GitHub Secrets configurados:
  - [ ] DOCKER_USERNAME
  - [ ] DOCKER_PASSWORD
  - [ ] SQLSERVER_DEV_CONNECTIONSTRING
  - [ ] SQLSERVER_PROD_CONNECTIONSTRING
  - [ ] KUBECONFIG_DEV
  - [ ] KUBECONFIG_PROD
- [ ] Dockerfile copiado a raíz del proyecto
- [ ] Archivo `.github/workflows/ci.yml` configurado
- [ ] Archivos `k8s/deployment-dev.yml` y `k8s/deployment-prod.yml`
- [ ] HealthController en Controllers/
- [ ] Scripts de versionado (`scripts/update-version.sh`, `Update-Version.ps1`)

---

## 🚀 Comandos Útiles

```bash
# Crear namespaces
kubectl create namespace gametopia-dev
kubectl create namespace gametopia-prod

# Crear secret
kubectl create secret generic gametopia-db-secret \
  --from-literal=ConnectionString="Server=..." \
  -n gametopia-dev

# Aplicar deployment
kubectl apply -f k8s/deployment-dev.yml
kubectl apply -f k8s/deployment-prod.yml

# Ver logs en tiempo real
kubectl logs -f -n gametopia-dev deployment/gametopia-domain-api

# Exec en pod
kubectl exec -it -n gametopia-dev pod/gametopia-domain-api-xyz -- /bin/bash

# Port-forward
kubectl port-forward -n gametopia-dev svc/gametopia-domain-api 8080:8080
```

---

## 📞 Soporte

Para problemas comunes, verificar:

1. ✅ GitHub Secrets configurados correctamente
2. ✅ Self-hosted runner en línea
3. ✅ Acceso a Docker Hub (login credenciales)
4. ✅ Kubeconfig válido y con permisos
5. ✅ Namespaces creados en k3s
6. ✅ SQL Server accesible desde k3s

---

**Última actualización:** Febrero 2026
**Versión:** 1.0.0
