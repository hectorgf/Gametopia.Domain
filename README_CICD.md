# 🎮 Gametopia.Domain - CI/CD Pipeline

Pipeline automatizado completo de Integración Continua y Despliegue Continuo (CI/CD) con GitHub Actions y Kubernetes (k3s).

[![.NET](https://img.shields.io/badge/.NET-10.0-purple?logo=.net)](https://dotnet.microsoft.com/)
[![Kubernetes](https://img.shields.io/badge/Kubernetes-k3s-blue?logo=kubernetes)](https://k3s.io/)
[![Docker](https://img.shields.io/badge/Docker-latest-blue?logo=docker)](https://www.docker.com/)
[![GitHub Actions](https://img.shields.io/badge/GitHub%20Actions-Automated-green?logo=github%20actions)](https://github.com/hectorgf/Gametopia.Domain/actions)

---

## 📦 Componentes

### 🔧 Infraestructura

| Componente | Versión | Propósito |
|-----------|---------|-----------|
| .NET | 10.0 | Framework web |
| Docker | Latest | Containerización |
| Kubernetes | k3s | Orquestación |
| SQL Server | Containerizado | Base de datos |
| GitHub Actions | - | CI/CD |

### 📂 Estructura de Archivos

```
Gametopia.Domain/
├── .github/
│   └── workflows/
│       └── ci.yml                    # 🎯 Workflow principal (build, test, deploy)
├── k8s/
│   ├── deployment-dev.yml            # 🔵 Deployment para desarrollo
│   └── deployment-prod.yml           # 🔴 Deployment para producción
├── scripts/
│   ├── update-version.sh             # 🐧 Versionado (Linux)
│   └── Update-Version.ps1            # 🪟 Versionado (PowerShell)
├── Dockerfile                         # 🐳 Construcción de imagen Docker
├── .dockerignore                      # 📦 Exclusiones Docker
├── .gitignore                         # 📄 Exclusiones Git
├── CI-CD_DOCUMENTATION.md            # 📖 Documentación completa
├── SETUP_INICIAL.md                  # 🚀 Guía de configuración inicial
├── ENVIRONMENT_VARIABLES.md           # 🔐 Variables de entorno
├── TROUBLESHOOTING.md                # 🔧 Solución de problemas
└── (Proyectos .NET estándar)
```

---

## 🚀 Características Principales

### ✨ Automatización Completa

- ✅ **Build Automático**: Compilación en Release
- ✅ **Testing Automático**: Ejecución de todos los tests
- ✅ **Docker Build**: Construcción de imagen automatizada
- ✅ **Push Automático**: A Docker Hub con tags
- ✅ **Deployment Automático**: A k3s (dev y prod)
- ✅ **Health Checks**: Liveness y Readiness probes

### 🌍 Multi-Ambiente

```
develop branch → gametopia-dev (1 replica, swagger=on)
main branch     → gametopia-prod (2+ replicas, swagger=off)
```

### 📊 Versionado Automático

- **Dev**: Siempre `latest`
- **Prod**: Automático (0.0.0 → 0.0.1 → 0.0.2)

### 🔐 Seguridad

- ✅ Secrets seguros en GitHub
- ✅ Network policies en prod
- ✅ Security context para pods
- ✅ RBAC (Role-based access control) en k8s
- ✅ PodDisruptionBudget para disponibilidad

### 📈 Alta Disponibilidad

- ✅ Multi-replica en producción
- ✅ HPA (Horizontal Pod Autoscaler)
- ✅ RollingUpdate strategy
- ✅ PodAntiAffinity preferida
- ✅ Graceful shutdown

---

## 📋 Pre-requisitos

- 🖥️ Self-hosted runner (Ubuntu 22.04 LTS)
- 🐳 Docker instalado
- ☸️ k3s con 2+ namespaces
- 🐘 SQL Server accesible
- 📦 Docker Hub account

### Secrets Requeridos

```
DOCKER_USERNAME
DOCKER_PASSWORD
SQLSERVER_DEV_CONNECTIONSTRING
SQLSERVER_PROD_CONNECTIONSTRING
KUBECONFIG_DEV
KUBECONFIG_PROD
```

---

## 🎯 Workflow de Deploy

### Trigger: Pull Request a `develop`

```
PR created on develop
    ↓
[BUILD TEST DEPLOY]
    ├─ Checkout code
    ├─ Run tests (fail if any fails)
    ├─ Build Release
    ├─ Build Docker image
    ├─ Push to Docker Hub (tag: latest)
    └─ Deploy to gametopia-dev
```

### Trigger: Push a `main`

```
Push to main branch
    ↓
[BUILD TEST DEPLOY + VERSION]
    ├─ Checkout code
    ├─ Auto-increment version (0.0.0 → 0.0.1)
    ├─ Update .csproj
    ├─ Run tests (fail if any fails)
    ├─ Build Release
    ├─ Build Docker image
    ├─ Push to Docker Hub (tag: v0.0.1)
    └─ Deploy to gametopia-prod (2+ replicas)
```

---

## 📱 Endpoints de Health Check

El HealthController proporciona 3 endpoints:

```bash
# Liveness Probe (¿está vivo?)
GET /api/health
Response: { status: "healthy", timestamp, version }

# Readiness Probe (¿está listo?)
GET /api/health/ready
Response: { status: "ready", timestamp, version }

# Info (información de versión)
GET /api/health/info
Response: { environment: "Production", version: "0.0.1", timestamp }
```

---

## 🔧 Configuración Rápida

### 1. Clonar y Crear Ramas

```bash
git clone https://github.com/hectorgf/Gametopia.Domain.git
cd Gametopia.Domain
git checkout -b develop
git push origin develop
```

### 2. Configurar Secrets en GitHub

```
GitHub > Repository > Settings > Secrets and variables > Actions > Add
```

Ver `SETUP_INICIAL.md` para detalles.

### 3. Registrar Self-Hosted Runner

```bash
# En servidor Ubuntu
mkdir ~/github-runner && cd ~/github-runner
curl -o actions-runner-linux-x64.tar.gz -L https://github.com/actions/runner/releases/download/v2.311.0/actions-runner-linux-x64-2.311.0.tar.gz
tar xzf ./actions-runner-linux-x64.tar.gz
./config.sh --url https://github.com/hectorgf/Gametopia.Domain --token XXXXX
sudo ./svc.sh install && sudo ./svc.sh start
```

### 4. Crear Namespaces en k3s

```bash
kubectl create namespace gametopia-dev
kubectl create namespace gametopia-prod
```

### 5. Hacer Push y Monitorear

```bash
git add . && git commit -m "Initial CI/CD setup"
git push origin develop

# Monitorear en
GitHub > Actions > [tu workflow]
```

---

## 📊 Recursos de Pod (Homelab 16GB)

### Development

```yaml
Requests:
  CPU: 100m (0.1 cores)
  Memory: 256Mi
Limits:
  CPU: 500m (0.5 cores)
  Memory: 512Mi
```

### Production

```yaml
Requests:
  CPU: 250m (0.25 cores)
  Memory: 512Mi
Limits:
  CPU: 1000m (1 core)
  Memory: 1Gi
```

---

## 🔄 HPA (Auto-scaling)

### Development

```yaml
Min replicas: 1
Max replicas: 2
CPU target: 70%
Memory target: 80%
```

### Production

```yaml
Min replicas: 2
Max replicas: 4
CPU target: 60%
Memory target: 70%
```

---

## 📖 Documentación

| Archivo | Propósito |
|---------|-----------|
| [CI-CD_DOCUMENTATION.md](CI-CD_DOCUMENTATION.md) | Documentación técnica completa |
| [SETUP_INICIAL.md](SETUP_INICIAL.md) | Guía paso a paso de configuración |
| [ENVIRONMENT_VARIABLES.md](ENVIRONMENT_VARIABLES.md) | Variables de entorno |
| [TROUBLESHOOTING.md](TROUBLESHOOTING.md) | Solución de problemas comunes |

---

## 🛠️ Comandos Útiles

### En Local

```bash
# Versionado manual
./scripts/update-version.sh --get
./scripts/update-version.sh --increment

# Build local
dotnet build -c Release
dotnet test

# Docker local
docker build -t gametopia:local .
docker run -p 8080:8080 gametopia:local
```

### En k3s

```bash
# Ver deployments
kubectl get deployments -n gametopia-dev
kubectl get deployments -n gametopia-prod

# Ver pods
kubectl get pods -n gametopia-dev -o wide

# Ver logs
kubectl logs -f deployment/gametopia-domain-api -n gametopia-dev

# Port-forward
kubectl port-forward -n gametopia-dev svc/gametopia-domain-api 8080:8080

# Escalar
kubectl scale deployment gametopia-domain-api --replicas=3 -n gametopia-prod

# Rollback
kubectl rollout undo deployment/gametopia-domain-api -n gametopia-prod
```

---

## 🐛 Troubleshooting

Para problemas comunes, ver [TROUBLESHOOTING.md](TROUBLESHOOTING.md)

Verificación rápida:
```bash
# ¿Runner en línea?
GitHub > Actions > Runners

# ¿Todos los secrets?
GitHub > Settings > Secrets > Debe haber 6

# ¿k3s accesible?
kubectl cluster-info

# ¿Pods ejecutándose?
kubectl get pods -n gametopia-dev
```

---

## 📈 Monitoreo

### Health Check

```bash
kubectl port-forward -n gametopia-dev svc/gametopia-domain-api 8080:8080
curl http://localhost:8080/api/health
```

### Recursos

```bash
kubectl top nodes
kubectl top pods -n gametopia-dev
```

### Eventos

```bash
kubectl get events -n gametopia-dev --sort-by='.lastTimestamp'
```

---

## 🔐 Seguridad

- ✅ Credentials en GitHub Secrets (no en código)
- ✅ RBAC en KA containers con runAsNonRoot
- ✅ Pod Security Context restricto
- ✅ Network Policy en prod
- ✅ Secrets de DB encriptados en Kubernetes
- ✅ Health checks para detectar pods unhealthy
- ✅ PodDisruptionBudget para evitar downtime

---

## 📝 Versionado

### Estructura

```
v{MAJOR}.{MINOR}.{PATCH}

Ejemplo: v0.0.1
```

### Automatización

- **Dev**: Siempre `latest` (sin versionado)
- **Prod**: Incremento automático de patch en cada push a main

### Script Manual

```bash
# Ver versión actual
./scripts/update-version.sh --get

# Incrementar versión
./scripts/update-version.sh --increment
```

---

## 🎯 Objetivos Cumplidos

- ✅ GitHub Actions workflow completo
- ✅ Versionado automático en prod
- ✅ Deployments YAML para dev y prod
- ✅ Health checks implementados
- ✅ Multi-ambiente con diferentes configs
- ✅ HA en producción (2+ replicas, HPA, anti-affinity)
- ✅ Documentación completa
- ✅ Troubleshooting guide
- ✅ Scripts de utilidad

---

## 📞 Soporte

Verificar:
1. [SETUP_INICIAL.md](SETUP_INICIAL.md) - Configuración paso a paso
2. [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - Problemas comunes
3. [CI-CD_DOCUMENTATION.md](CI-CD_DOCUMENTATION.md) - Detalles técnicos

---

## 📅 Información

- **Última actualización**: Febrero 2026
- **Versión**: 1.0.0
- **Autor**: Sistema CI/CD Automatizado
- **Repositorio**: https://github.com/hectorgf/Gametopia.Domain

---

## 📜 Licencia

Este pipeline es parte del proyecto Gametopia.Domain.

---

**¡Listo para CI/CD automatizado! 🚀**
