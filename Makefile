dev:
	docker compose -f docker/docker-compose.yml -f docker/docker-compose.dev.yml up -d db maildev
	dotnet run --project src/Accord.Web
