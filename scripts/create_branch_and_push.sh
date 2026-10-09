#!/usr/bin/env bash
set -euo pipefail

BRANCH="feature/add-chatmessage-migration"
COMMIT_MSG="Add ChatMessage entity and migration (20261009000000_AddChatMessage)"

# Ensure we're on the main branch and up to date
git checkout main
git pull origin main

# Create branch
git checkout -b "$BRANCH"

# Add migration files and snapshot
git add AppFletesMueve.Api/Migrations/20261009000000_AddChatMessage.cs \
		AppFletesMueve.Api/Migrations/20261009000000_AddChatMessage.Designer.cs \
		AppFletesMueve.Api/Migrations/MueveDbContextModelSnapshot.cs

# Commit and push
git commit -m "$COMMIT_MSG"

git push -u origin "$BRANCH"

# Optional: apply migration locally (uncomment if desired)
# dotnet ef database update --project AppFletesMueve.Api --startup-project AppFletesMueve.Api

echo "Branch '$BRANCH' created, files committed and pushed."