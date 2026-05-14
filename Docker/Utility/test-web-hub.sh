#!/bin/bash

# Enter the container
docker exec -it diagnostic_service_container bash

# Once inside the container, run:
apt-get update && apt-get install -y curl
curl -I http://localhost:5000/web-hub

# Exit the container
exit

# Alternatively, test from host machine:
curl -I http://localhost:5000/web-hub
