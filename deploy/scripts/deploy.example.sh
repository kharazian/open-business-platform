#!/usr/bin/env sh
set -eu

environment="${1:-stage}"
root_dir="$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)"
deploy_dir="${root_dir}/deploy"
env_file="${deploy_dir}/env/${environment}.env"
override_file="${deploy_dir}/compose.${environment}.yml"

if [ ! -f "${env_file}" ]; then
  env_file="${deploy_dir}/env/${environment}.env.example"
fi

if [ ! -f "${override_file}" ]; then
  override_file="${deploy_dir}/compose.${environment}.example.yml"
fi

if [ ! -f "${override_file}" ]; then
  echo "Unknown environment: ${environment}" >&2
  exit 1
fi

compose() {
  docker compose \
    --env-file "${env_file}" \
    -f "${deploy_dir}/compose.yml" \
    -f "${override_file}" \
    -f "${deploy_dir}/compose.proxy.yml" \
    "$@"
}

compose_with_migrator() {
  docker compose \
    --profile migrate \
    --env-file "${env_file}" \
    -f "${deploy_dir}/compose.yml" \
    -f "${override_file}" \
    -f "${deploy_dir}/compose.proxy.yml" \
    "$@"
}

compose_with_migrator build
compose up -d postgres redis
compose stop api web
compose_with_migrator run --rm migrator
compose up -d --remove-orphans
compose ps
