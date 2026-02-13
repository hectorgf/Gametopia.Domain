.PHONY: help build test publish docker-build docker-push clean version-get version-inc deploy-dev deploy-prod logs-dev logs-prod

# Colors
BLUE=\033[0;34m
GREEN=\033[0;32m
NC=\033[0m # No Color

help: ## Show this help message
	@echo "$(BLUE)Gametopia.Domain - CI/CD Commands$(NC)"
	@echo ""
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "$(GREEN)%-20s$(NC) %s\n", $$1, $$2}'

build: ## Build solution in Release mode
	@echo "$(BLUE)Building solution...$(NC)"
	dotnet build -c Release

test: ## Run all tests
	@echo "$(BLUE)Running tests...$(NC)"
	dotnet test --configuration Release --logger "console;verbosity=normal"

publish: ## Publish application
	@echo "$(BLUE)Publishing application...$(NC)"
	dotnet publish Gametopia.Domain.Api/Gametopia.Domain.Api.csproj -c Release -o ./publish

clean: ## Clean build artifacts
	@echo "$(BLUE)Cleaning build artifacts...$(NC)"
	dotnet clean
	rm -rf ./publish ./bin ./obj

docker-build: ## Build Docker image
	@echo "$(BLUE)Building Docker image...$(NC)"
	docker build -t hectorgf/gametopia-domain:local .

docker-run: docker-build ## Build and run Docker image locally
	@echo "$(BLUE)Running Docker container...$(NC)"
	docker run -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development hectorgf/gametopia-domain:local

docker-push: ## Push Docker image to registry
	@echo "$(BLUE)Pushing Docker image...$(NC)"
	docker push hectorgf/gametopia-domain:latest

version-get: ## Get current version
	@echo "$(BLUE)Current version:$(NC)"
	@grep -oP '<Version>\K[^<]+' Gametopia.Domain.Api/Gametopia.Domain.Api.csproj

version-inc: ## Increment patch version
	@echo "$(BLUE)Incrementing version...$(NC)"
	@bash ./scripts/update-version.sh --increment

deploy-dev: ## Deploy to development environment
	@echo "$(BLUE)Deploying to dev...$(NC)"
	kubectl apply -f k8s/deployment-dev.yml
	kubectl rollout status deployment/gametopia-domain-api -n gametopia-dev

deploy-prod: ## Deploy to production environment
	@echo "$(BLUE)Deploying to prod...$(NC)"
	kubectl apply -f k8s/deployment-prod.yml
	kubectl rollout status deployment/gametopia-domain-api -n gametopia-prod

logs-dev: ## Show dev pod logs
	@echo "$(BLUE)Showing dev logs...$(NC)"
	@kubectl logs -f deployment/gametopia-domain-api -n gametopia-dev

logs-prod: ## Show prod pod logs
	@echo "$(BLUE)Showing prod logs...$(NC)"
	@kubectl logs -f deployment/gametopia-domain-api -n gametopia-prod

pods-dev: ## Show dev pods
	@echo "$(BLUE)Dev Pods:$(NC)"
	@kubectl get pods -n gametopia-dev -o wide

pods-prod: ## Show prod pods
	@echo "$(BLUE)Prod Pods:$(NC)"
	@kubectl get pods -n gametopia-prod -o wide

health-dev: ## Check dev health endpoint
	@echo "$(BLUE)Checking dev health...$(NC)"
	@kubectl port-forward -n gametopia-dev svc/gametopia-domain-api 8080:8080 &
	@sleep 2
	@curl -s http://localhost:8080/api/health | jq . || true
	@pkill -f "port-forward"

health-prod: ## Check prod health endpoint
	@echo "$(BLUE)Checking prod health...$(NC)"
	@kubectl port-forward -n gametopia-prod svc/gametopia-domain-api 8080:8080 &
	@sleep 2
	@curl -s http://localhost:8080/api/health/ready | jq . || true
	@pkill -f "port-forward"

restart-dev: ## Restart dev deployment
	@echo "$(BLUE)Restarting dev deployment...$(NC)"
	@kubectl rollout restart deployment/gametopia-domain-api -n gametopia-dev

restart-prod: ## Restart prod deployment
	@echo "$(BLUE)Restarting prod deployment...$(NC)"
	@kubectl rollout restart deployment/gametopia-domain-api -n gametopia-prod

scale-dev: ## Scale dev replicas (usage: make scale-dev REPLICAS=2)
	@echo "$(BLUE)Scaling dev to $(REPLICAS) replicas...$(NC)"
	@kubectl scale deployment gametopia-domain-api --replicas=$(REPLICAS) -n gametopia-dev

scale-prod: ## Scale prod replicas (usage: make scale-prod REPLICAS=3)
	@echo "$(BLUE)Scaling prod to $(REPLICAS) replicas...$(NC)"
	@kubectl scale deployment gametopia-domain-api --replicas=$(REPLICAS) -n gametopia-prod

status-dev: ## Show dev deployment status
	@echo "$(BLUE)Dev Status:$(NC)"
	@kubectl describe deployment gametopia-domain-api -n gametopia-dev | tail -20

status-prod: ## Show prod deployment status
	@echo "$(BLUE)Prod Status:$(NC)"
	@kubectl describe deployment gametopia-domain-api -n gametopia-prod | tail -20

all: clean build test publish docker-build ## Build, test, publish and create Docker image

ci: test build publish ## Run CI pipeline locally

.DEFAULT_GOAL := help
