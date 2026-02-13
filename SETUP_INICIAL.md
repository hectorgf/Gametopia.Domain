# 🚀 Setup Inicial - CI/CD Gametopia.Domain

Guía paso a paso para configurar el pipeline completo de CI/CD.

## 📋 Pre-requisitos

1. **GitHub**
   - Repositorio público o privado
   - Acceso con permisos de admin

2. **Self-Hosted Runner (Ubuntu 22.04 LTS)**
   ```bash
   # Actualizar sistema
   sudo apt update && sudo apt upgrade -y
   
   # Instalar Docker
   curl -fsSL https://get.docker.com -o get-docker.sh
   sudo sh get-docker.sh
   sudo usermod -aG docker $USER
   
   # Instalar Docker Compose
   sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
   sudo chmod +x /usr/local/bin/docker-compose
   
   # Instalar kubectl
   curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
   sudo install -o root -g root -m 0755 kubectl /usr/local/bin/kubectl
   
   # Instalar curl (si no lo tiene)
   sudo apt install -y curl
   ```

3. **k3s en el Homelab**
   ```bash
   # En el servidor donde estará k3s
   curl -sfL https://get.k3s.io | sh -
   
   # Copiar kubeconfig
   sudo cp /etc/rancher/k3s/k3s.yaml ~/.kube/config
   sudo chown $USER:$USER ~/.kube/config
   chmod 600 ~/.kube/config
   ```

4. **Docker Hub Account**
   - Usuario y token de acceso

---

## ⚙️ Paso 1: Configurar GitHub Secrets

### 1.1 Ir a Settings del Repositorio

```
GitHub > Repositorio > Settings > Secrets and variables > Actions
```

### 1.2 Crear Docker Hub Secrets

**Crear: `DOCKER_USERNAME`**
```
Value: tu_usuario_docker
```

**Crear: `DOCKER_PASSWORD`**
```
Value: tu_token_o_password_docker
```

> 💡 Recomendación: Usar Personal Access Token de Docker Hub
> https://hub.docker.com/settings/security

### 1.3 Crear Secrets de Base de Datos

**Crear: `SQLSERVER_DEV_CONNECTIONSTRING`**
```
Value: Server=sqlserver-dev.local,1433;Database=Gametopia.Domain;User Id=sa;Password=YourPassword;Encrypt=false;
```

**Crear: `SQLSERVER_PROD_CONNECTIONSTRING`**
```
Value: Server=sqlserver-prod.local,1433;Database=Gametopia.Domain;User Id=sa;Password=YourPassword;Encrypt=false;
```

### 1.4 Crear Secrets de Kubeconfig

En tu máquina local:

```bash
# Obtener kubeconfig del servidor k3s
scp user@k3s-server:/etc/rancher/k3s/k3s.yaml ./k3s.yaml

# Codificar en base64
cat k3s.yaml | base64 -w 0 > k3s.b64

# Mostrar y copiar
cat k3s.b64
```

**Crear: `KUBECONFIG_DEV`**
```
Value: # Pegar el contenido del k3s.b64
```

**Crear: `KUBECONFIG_PROD`**
```
Value: # Pegar el mismo contenido (puedes usar el mismo k3s)
```

---

## ⚙️ Paso 2: Configurar Self-Hosted Runner

### 2.1 Descargar Runner

```bash
# En la máquina Ubuntu
mkdir ~/github-runner && cd ~/github-runner

# Descargar última versión
curl -o actions-runner-linux-x64-latest.tar.gz \
  -L https://github.com/actions/runner/releases/download/v2.311.0/actions-runner-linux-x64-2.311.0.tar.gz

tar xzf ./actions-runner-linux-x64-latest.tar.gz
```

### 2.2 Ir a GitHub y Configurar Runner

```
GitHub > Repositorio > Settings > Actions > Runners and Groups > New self-hosted runner
```

Seguir las instrucciones en pantalla:

```bash
# Ejemplo (ajusta según GitHub te indique):
./config.sh --url https://github.com/usuario/Gametopia.Domain --token XXXXXX

# Instalar como servicio
sudo ./svc.sh install
sudo ./svc.sh start
```

### 2.3 Verificar Runner

```
GitHub > Repositorio > Settings > Actions > Runners > Debería aparecer "gametopia-domain" en green
```

---

## ⚙️ Paso 3: Preparar k3s

### 3.1 Crear Namespaces

```bash
kubectl create namespace gametopia-dev
kubectl create namespace gametopia-prod

# Verificar
kubectl get namespaces
```

### 3.2 Preparar Variables de Configuración

```bash
# Obtener los valores que usaremos en secrets
echo "Dev DB Connection: $SQLSERVER_DEV_CONNECTIONSTRING"
echo "Prod DB Connection: $SQLSERVER_PROD_CONNECTIONSTRING"
```

### 3.3 (Opcional) Pre-crear Secrets

Los secrets se crearán automáticamente en el workflow, pero puedes pre-crearlos:

```bash
# Dev
kubectl create secret generic gametopia-db-secret \
  --from-literal=ConnectionString="Server=sqlserver-dev.local,1433;Database=Gametopia.Domain;User Id=sa;Password=YourPassword;Encrypt=false;" \
  -n gametopia-dev

# Prod
kubectl create secret generic gametopia-db-secret \
  --from-literal=ConnectionString="Server=sqlserver-prod.local,1433;Database=Gametopia.Domain;User Id=sa;Password=YourPassword;Encrypt=false;" \
  -n gametopia-prod
```

---

## ⚙️ Paso 4: Configurar Rama `main` como Protegida (Opcional)

Para seguridad en producción:

```
GitHub > Repositorio > Settings > Branches > Add rule

Branch name pattern: main
- Require pull request reviews before merging
- Include administrators
- Require status checks to pass before merging
- Required checks: build-test
```

---

## 🧪 Paso 5: Probar el Pipeline

### 5.1 Crear Rama `develop`

```bash
git checkout -b develop
git push origin develop
```

### 5.2 Hacer Cambio en `develop`

```bash
# Hacer algún cambio en código
git add .
git commit -m "Test CI/CD pipeline"
git push origin develop
```

### 5.3 Monitorear Workflow

```
GitHub > Repositorio > Actions > Ver el workflow ejecutándose
```

### 5.4 Crear PR a `main`

```bash
# Esperar a que dev esté estable, luego hacer PR
# GitHub > Pull requests > New pull request
# develop → main
```

### 5.5 Monitorear Deploy en k3s

```bash
# Mientras se está desplegando
watch kubectl get deployment -n gametopia-dev
watch kubectl get pods -n gametopia-dev

# Ver logs
kubectl logs -f -n gametopia-dev deployment/gametopia-domain-api
```

---

## 🔍 Verificación Final

### Checklist

- [ ] GitHub Secrets configurados (6 secretos)
- [ ] Self-hosted runner registrado y en línea
- [ ] Namespaces de k3s creados
- [ ] Dockerfile en raíz del proyecto
- [ ] `.github/workflows/ci.yml` existe
- [ ] Archivos `k8s/deployment-*.yml` existen
- [ ] `HealthController` en Controllers/
- [ ] Scripts de versionado en `scripts/`
- [ ] Rama `develop` creada
- [ ] Primera ejecución de workflow en `develop` completada exitosamente

### Pruebas

```bash
# Dev
kubectl get svc -n gametopia-dev
kubectl port-forward -n gametopia-dev svc/gametopia-domain-api 8080:8080
# En otro terminal: curl http://localhost:8080/api/health

# Prod
kubectl get svc -n gametopia-prod
kubectl port-forward -n gametopia-prod svc/gametopia-domain-api 8080:8080
# En otro terminal: curl http://localhost:8080/api/health
```

---

## 🆘 Troubleshooting

### Error: "Runner offline"

```bash
# En la máquina del runner
cd ~/github-runner
./svc.sh status
./svc.sh start

# Verificar Docker
docker ps
sudo systemctl status docker
```

### Error: "Pod CrashLoopBackOff"

```bash
# Ver logs del pod
kubectl logs -f pod-name -n gametopia-dev

# Describir pod
kubectl describe pod pod-name -n gametopia-dev

# Verificar secret existe
kubectl get secret -n gametopia-dev
```

### Error: "Invalid kubeconfig"

```bash
# Verificar kubeconfig en runner
echo $KUBECONFIG
cat ~/.kube/config

# Codificar correctamente
cat ~/.kube/config | base64 -w 0
# Copiar sin saltos de línea a GitHub Secret
```

### Error: "Docker push failed"

```bash
# Verificar credenciales
echo $DOCKER_PASSWORD | docker login -u $DOCKER_USERNAME --password-stdin

# Verificar imagen
docker images | grep gametopia
```

---

## 📚 Recursos Útiles

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [k3s Documentation](https://docs.k3s.io/)
- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [Docker Documentation](https://docs.docker.com/)

---

**Última actualización:** Febrero 2026
**Versión:** 1.0.0
