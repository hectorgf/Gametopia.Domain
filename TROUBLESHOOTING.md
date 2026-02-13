# 🔧 Troubleshooting Guide - Gametopia.Domain CI/CD

Guía de solución de problemas comunes en el pipeline de CI/CD.

---

## 🚨 GitHub Actions Issues

### ❌ Error: "No runners available"

**Síntoma:**
```
This repository does not have any self-hosted runners available to run this workflow.
```

**Soluciones:**

1. Verificar runner en línea:
   ```
   GitHub > Repo > Settings > Actions > Runners
   ```
   Debería mostrar "Idle" en verde.

2. Si el runner está offline:
   ```bash
   cd ~/github-runner
   ./svc.sh status    # Ver estado
   ./svc.sh start     # Iniciar si está parado
   tail -f _diag/Runner_* # Ver logs
   ```

3. Reiniciar completamente:
   ```bash
   cd ~/github-runner
   ./svc.sh stop
   ./svc.sh uninstall
   
   # Bajar y registrar de nuevo
   ./config.sh --url ... --token ...
   ./svc.sh install
   ./svc.sh start
   ```

---

### ❌ Error: "Resource not accessible"

**Síntoma:**
```
Error: Resource not accessible by integration
```

**Soluciones:**

1. Verificar permisos del token en GitHub:
   ```
   GitHub > Settings > Development settings > Personal access tokens > Tokens (classic)
   ```
   Debe tener: `repo`, `admin:repo_hook`, `workflow`

2. Redirigir runner con nuevo token:
   ```bash
   cd ~/github-runner
   ./remove.sh    # Remover runner actual
   ./config.sh --url ... --token [NEW_TOKEN]
   ```

---

### ❌ Error: "Timeout waiting for runner"

**Síntoma:**
```
This workflow run was canceled because an annotation was created with message: Timeout
```

**Soluciones:**

1. Aumentar tiempo de timeout en workflow (si es necesario)
2. Verificar runner tiene suficientes recursos:
   ```bash
   free -h           # RAM disponible
   df -h             # Espacio en disco
   docker ps         # Contenedores activos
   ```

3. Limpiar espacio si es necesario:
   ```bash
   docker system prune -a    # ⚠️ Elimina imágenes no usadas
   sudo apt autoremove       # Limpiar paquetes
   ```

---

## 🐳 Docker Issues

### ❌ Error: "docker: unauthorized: authentication required"

**Síntoma:**
```
Error: docker: unauthorized: authentication required, visit https://hub.docker.com/v2/auth/login
```

**Soluciones:**

1. Verificar credenciales en secrets:
   - `DOCKER_USERNAME` correcto
   - `DOCKER_PASSWORD` es un Personal Access Token (no contraseña)

2. Probar login manual en runner:
   ```bash
   echo $DOCKER_PASSWORD | docker login -u $DOCKER_USERNAME --password-stdin
   ```

3. Crear nuevo token en Docker Hub:
   ```
   https://hub.docker.com/settings/security > New Access Token
   Actualizar DOCKER_PASSWORD en GitHub Secrets
   ```

---

### ❌ Error: "no space left on device"

**Síntoma:**
```
Error: no space left on device
```

**Soluciones:**

1. Ver espacio disponible:
   ```bash
   df -h
   ```

2. Limpiar espacio:
   ```bash
   # Limpiar Docker
   docker system prune -a --volumes
   
   # Limpiar apt
   sudo apt clean
   sudo apt autoclean
   
   # Ver qué consume espacio
   du -sh /*
   ```

---

## ☸️ Kubernetes Issues

### ❌ Error: "Unable to connect to the server"

**Síntoma:**
```
Unable to connect to the server: dial tcp: lookup k3s-server: no such host
```

**Soluciones:**

1. Verificar kubeconfig:
   ```bash
   cat ~/.kube/config | head -20
   # Debe mostrar: apiVersion, clusters, users, contexts
   ```

2. Decodificar kubeconfig desde secret:
   ```bash
   echo $KUBECONFIG_DEV | base64 -d > ~/.kube/config
   chmod 600 ~/.kube/config
   ```

3. Probar conexión:
   ```bash
   kubectl cluster-info
   kubectl get nodes
   ```

4. Verificar secret en GitHub:
   - Debe estar codificado en base64
   - Sin saltos de línea
   - Usar: `cat config | base64 -w 0`

---

### ❌ Error: "Pod in CrashLoopBackOff"

**Síntoma:**
```
NAME                           READY   STATUS             RESTARTS   AGE
gametopia-domain-api-xyz       0/1     CrashLoopBackOff   5          2m
```

**Soluciones:**

1. Ver logs del pod:
   ```bash
   kubectl logs pod-name -n gametopia-dev
   # O el último log antes del crash
   kubectl logs pod-name -n gametopia-dev --previous
   ```

2. Describir pod para más detalles:
   ```bash
   kubectl describe pod pod-name -n gametopia-dev
   ```

3. Causas comunes:
   - ❌ Secret no existe: `kubectl get secret -n gametopia-dev`
   - ❌ Imagen no se pudo descargar: `kubectl describe pod ... | grep -A 3 Image`
   - ❌ Database desconectada: Verificar ConnectionString
   - ❌ Puertos en conflicto: `kubectl get svc -n gametopia-dev`

---

### ❌ Error: "ImagePullBackOff"

**Síntoma:**
```
NAME                           READY   STATUS             RESTARTS   AGE
gametopia-domain-api-xyz       0/1     ImagePullBackOff   0          1m
```

**Soluciones:**

1. Verificar imagen existe:
   ```bash
   docker images | grep gametopia
   docker pull hectorgf/gametopia-domain:latest
   ```

2. Verificar permisos en Docker Hub:
   ```bash
   docker login
   docker pull hectorgf/gametopia-domain:latest
   ```

3. Verificar tag en deployment:
   ```bash
   kubectl describe pod pod-name -n gametopia-dev | grep Image:
   # Debe ser: hectorgf/gametopia-domain:latest (dev) o v0.0.1 (prod)
   ```

---

### ❌ Error: "Pending" state (Pod no se crea)

**Síntoma:**
```
NAME                           READY   STATUS    RESTARTS   AGE
gametopia-domain-api-xyz       0/1     Pending   0          5m
```

**Soluciones:**

1. Ver eventos del cluster:
   ```bash
   kubectl describe pod pod-name -n gametopia-dev
   # Ver "Events" al final
   ```

2. Causas comunes:
   - ❌ No hay nodos disponibles: `kubectl get nodes`
   - ❌ Recursos insuficientes: `kubectl top nodes`
   - ❌ PVC pendiente: `kubectl get pvc -n gametopia-dev`

3. Verificar recursos del nodo:
   ```bash
   kubectl top nodes        # CPU y Memory por nodo
   kubectl top pods -n gametopia-dev  # Uso por pod
   ```

---

### ❌ Error: "Connection refused"

**Síntoma:**
```
kubectl: error - connection refused
```

**Soluciones:**

1. Verificar k3s está ejecutándose:
   ```bash
   sudo systemctl status k3s
   sudo systemctl start k3s
   ```

2. Verificar puerto 6443 (K8s API):
   ```bash
   netstat -tlnp | grep 6443
   # O: ss -tlnp | grep 6443
   ```

3. Verificar kubeconfig apunta al servidor correcto:
   ```bash
   cat ~/.kube/config | grep server
   ```

---

### ❌ Error: "Namespace does not exist"

**Síntoma:**
```
Error: namespaces "gametopia-dev" not found
```

**Soluciones:**

1. Crear namespace:
   ```bash
   kubectl create namespace gametopia-dev
   kubectl create namespace gametopia-prod
   ```

2. Verificar namespaces:
   ```bash
   kubectl get namespaces
   ```

---

### ❌ Error: "Secret not found"

**Síntoma:**
```
Error: couldn't find key ConnectionString in Secret default/gametopia-db-secret
```

**Soluciones:**

1. Verificar secret existe:
   ```bash
   kubectl get secret -n gametopia-dev
   kubectl describe secret gametopia-db-secret -n gametopia-dev
   ```

2. Crear secret si no existe:
   ```bash
   kubectl create secret generic gametopia-db-secret \
     --from-literal=ConnectionString="Server=...;Database=Gametopia.Domain;..." \
     -n gametopia-dev
   ```

3. Verificar clave correcta:
   ```bash
   kubectl get secret gametopia-db-secret -n gametopia-dev -o jsonpath='{.data}' | base64 -d
   ```

---

## 🧪 Testing Issues

### ❌ Error: "Test collection could not be instantiated"

**Síntoma:**
```
The following constructor parameters did not have matching fixture data: ...
```

**Soluciones:**

1. Verificar dependencias en proyecto de tests:
   ```bash
   dotnet list package --dependency-tree
   ```

2. Ejecutar tests localmente:
   ```bash
   dotnet test Gametopia.Domain.Api.Tests/
   ```

3. Verificar proyecto referenciado:
   ```bash
   cat Gametopia.Domain.Api.Tests/Gametopia.Domain.Api.Tests.csproj | grep ProjectReference
   ```

---

### ❌ Error: "Connection string not found"

**Síntoma:**
```
System.InvalidOperationException: No connection string named 'DefaultConnection' was found
```

**Soluciones:**

1. Verificar appsettings:
   ```bash
   cat Gametopia.Domain.Api/appsettings.json | grep ConnectionString
   ```

2. Verificar variable de entorno en test:
   ```bash
   export ConnectionStrings__DefaultConnection="Server=...;Database=..."
   dotnet test
   ```

---

## 🔄 Deployment Issues

### ❌ Error: "Rollout timeout"

**Síntoma:**
```
error: timed out waiting for the condition on deployment/gametopia-domain-api
```

**Soluciones:**

1. Ver estado actual:
   ```bash
   kubectl describe deployment gametopia-domain-api -n gametopia-dev
   kubectl get pods -n gametopia-dev -o wide
   ```

2. Aumentar timeout en workflow o esperar más:
   ```bash
   kubectl rollout status deployment/gametopia-domain-api -n gametopia-dev --timeout=10m
   ```

3. Verificar logs del pod:
   ```bash
   kubectl logs -f <pod-name> -n gametopia-dev
   ```

---

### ❌ Error: "Insufficient memory"

**Síntoma:**
```
Pod cannot be scheduled because not enough memory available
```

**Soluciones:**

1. Ver memoria disponible:
   ```bash
   kubectl top nodes
   ```

2. Reducir recursos solicitados en deployment:
   ```yaml
   resources:
     requests:
       cpu: 100m
       memory: 256Mi
     limits:
       cpu: 500m
       memory: 512Mi
   ```

3. Escalar nodos si es posible, o reducir réplicas

---

## 📊 Health Check Issues

### ❌ Error: "Liveness probe failed"

**Síntoma:**
```
Liveness probe failed: HTTP probe failed with statuscode: 500
```

**Soluciones:**

1. Probar health endpoint:
   ```bash
   kubectl port-forward -n gametopia-dev svc/gametopia-domain-api 8080:8080
   curl http://localhost:8080/api/health -v
   ```

2. Ver logs de la aplicación:
   ```bash
   kubectl logs <pod-name> -n gametopia-dev --tail=100
   ```

3. Verificar HealthController existe:
   ```bash
   cat Gametopia.Domain.Api/Controllers/HealthController.cs
   ```

---

### ❌ Error: "Readiness probe failed"

**Síntoma:**
```
Readiness probe failed: HTTP probe failed with statuscode: 503
```

**Soluciones:**

1. Verificar base de datos:
   ```bash
   # En el pod
   kubectl exec -it <pod-name> -n gametopia-dev -- bash
   # Desde dentro: curl http://localhost:8080/api/health/ready
   ```

2. Verificar ConnectionString:
   ```bash
   kubectl describe pod <pod-name> -n gametopia-dev | grep -i connection
   ```

3. Prueba conectar a DB manualmente:
   ```bash
   sqlcmd -S sqlserver-prod,1433 -U sa -P YourPassword -Q "SELECT 1"
   ```

---

## 💾 Database Issues

### ❌ Error: "Connection timeout"

**Síntoma:**
```
Timeout expired. The timeout period elapsed prior to completion of the operation or the server is not responding.
```

**Soluciones:**

1. Verificar SQL Server accesible:
   ```bash
   nc -zv sqlserver-prod 1433
   # O: telnet sqlserver-prod 1433
   ```

2. Verificar ConnectionString:
   ```bash
   # ConnectionString debe incluir puerto y credenciales correctas
   kubectl get secret gametopia-db-secret -n gametopia-prod -o jsonpath='{.data.ConnectionString}' | base64 -d
   ```

3. Verificar credenciales SQL Server:
   ```bash
   sqlcmd -S sqlserver-prod,1433 -U sa -P "YourPassword" -Q "SELECT @@VERSION"
   ```

---

## 📋 Comandos Útiles de Debug

```bash
# Ver logs del workflow
GitHub > Actions > Click en workflow > Ver logs

# Tests detallados
dotnet test --logger "console;verbosity=diagnostic"

# Build detallado
dotnet build -v:d

# Ver eventos del cluster
kubectl get events -n gametopia-dev --sort-by='.lastTimestamp'

# Ver descripción completa de pod
kubectl describe pod <pod-name> -n gametopia-dev

# Conectarse a pod interactivamente
kubectl exec -it <pod-name> -n gametopia-dev -- /bin/bash

# Port-forward para pruebas locales
kubectl port-forward -n gametopia-dev pod/<pod-name> 8080:8080

# Ver recursos usados en tiempo real
watch kubectl top pods -n gametopia-dev

# Escalar deployment manualmente
kubectl scale deployment gametopia-domain-api --replicas=3 -n gametopia-prod

# Revisar histórico de rollouts
kubectl rollout history deployment/gametopia-domain-api -n gametopia-prod

# Revertir a versión anterior
kubectl rollout undo deployment/gametopia-domain-api -n gametopia-prod
```

---

## 📞 Checklist Final de Verificación

Cuando algo no funciona, verificar en orden:

- [ ] ¿Runner en línea? `GitHub > Actions > Runners`
- [ ] ¿Todos los secrets configurados? 6 secretos en GitHub
- [ ] ¿Docker credenciales válidas? `docker login`
- [ ] ¿Kubeconfig correcto?`kubectl cluster-info`
- [ ] ¿Namespaces existen? `kubectl get ns`
- [ ] ¿Secrets en k8s existen? `kubectl get secret -n gametopia-dev`
- [ ] ¿Pods creándose? `kubectl get pods -n gametopia-dev`
- [ ] ¿Logs sin errores? `kubectl logs <pod> -n gametopia-dev`
- [ ] ¿Health endpoint responde? `curl http://<ip>:8080/api/health`
- [ ] ¿Base de datos accesible? `sqlcmd -S ... -U ... -Q "SELECT 1"`

---

**Última actualización:** Febrero 2026
**Versión:** 1.0.0
