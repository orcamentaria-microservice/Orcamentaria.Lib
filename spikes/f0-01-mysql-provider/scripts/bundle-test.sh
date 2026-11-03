#!/usr/bin/env bash
# =============================================================================
# bundle-test.sh — Critério 2 do spike F0-01
#
# Prova: dotnet ef migrations bundle aplicado em banco vazio é idempotente
#        (pode ser aplicado duas vezes sem falha e sem duplicar tabelas).
#
# Pré-requisitos:
#   - Docker na PATH
#   - efbundle compilado em ../efbundle (ou informe o caminho como $1)
#
# Uso:
#   chmod +x scripts/bundle-test.sh
#   ./scripts/bundle-test.sh                      # usa ../efbundle por padrão
#   ./scripts/bundle-test.sh /caminho/para/efbundle
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUNDLE_PATH="${1:-${SCRIPT_DIR}/../efbundle}"
MYSQL_PASSWORD="bundle_test_only"
MYSQL_DATABASE="spike_bundle"

if [[ ! -f "${BUNDLE_PATH}" ]]; then
  echo "ERRO: efbundle não encontrado em '${BUNDLE_PATH}'"
  echo "       Execute: dotnet ef migrations bundle --self-contained -r linux-x64 -o efbundle"
  exit 1
fi

echo ""
echo "╔══════════════════════════════════════════════════════════╗"
echo "║  F0-01 Critério 2 — Bundle_AplicadoDuasVezes_NaoFalha   ║"
echo "╚══════════════════════════════════════════════════════════╝"
echo ""

# ── Sobe container MySQL 8.0 ─────────────────────────────────────────────────
echo "▶ Iniciando container mysql:8.0 …"
CONTAINER_ID=$(docker run -d \
  -e MYSQL_ROOT_PASSWORD="${MYSQL_PASSWORD}" \
  -e MYSQL_DATABASE="${MYSQL_DATABASE}" \
  -e MYSQL_CHARACTER_SET_SERVER=utf8mb4 \
  -e MYSQL_COLLATION_SERVER=utf8mb4_0900_ai_ci \
  --health-cmd="mysqladmin ping -u root -p${MYSQL_PASSWORD} --silent" \
  --health-interval=2s \
  --health-timeout=5s \
  --health-retries=20 \
  mysql:8.0)

cleanup() {
  echo ""
  echo "▶ Removendo container ${CONTAINER_ID} …"
  docker rm -f "${CONTAINER_ID}" > /dev/null 2>&1 || true
}
trap cleanup EXIT

# ── Aguarda MySQL ficar healthy ───────────────────────────────────────────────
echo "▶ Aguardando MySQL ficar pronto …"
for i in $(seq 1 40); do
  STATUS=$(docker inspect --format='{{.State.Health.Status}}' "${CONTAINER_ID}" 2>/dev/null || echo "unknown")
  if [[ "${STATUS}" == "healthy" ]]; then
    echo "  MySQL pronto após ${i}s."
    break
  fi
  if [[ "${i}" -eq 40 ]]; then
    echo "ERRO: MySQL não ficou healthy em 40s."
    docker logs "${CONTAINER_ID}"
    exit 1
  fi
  sleep 1
done

CONTAINER_IP=$(docker inspect \
  --format='{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' \
  "${CONTAINER_ID}")

CS="Server=${CONTAINER_IP};Port=3306;Database=${MYSQL_DATABASE};Uid=root;Pwd=${MYSQL_PASSWORD};AllowPublicKeyRetrieval=true;SslMode=Disabled;"

# ── Primeira aplicação ────────────────────────────────────────────────────────
echo ""
echo "▶ Primeira aplicação do bundle …"
"${BUNDLE_PATH}" --connection "${CS}"
echo "  ✓ Primeira aplicação: OK"

# Conta tabelas após a primeira aplicação
TABLE_COUNT_1=$(docker exec "${CONTAINER_ID}" \
  mysql -u root -p"${MYSQL_PASSWORD}" -N -e \
  "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='${MYSQL_DATABASE}';" 2>/dev/null)
echo "  Tabelas no banco: ${TABLE_COUNT_1}"

# ── Segunda aplicação (idempotência) ─────────────────────────────────────────
echo ""
echo "▶ Segunda aplicação do bundle (idempotência) …"
"${BUNDLE_PATH}" --connection "${CS}"
echo "  ✓ Segunda aplicação: OK — idempotente"

# Conta tabelas após a segunda aplicação — deve ser igual à primeira
TABLE_COUNT_2=$(docker exec "${CONTAINER_ID}" \
  mysql -u root -p"${MYSQL_PASSWORD}" -N -e \
  "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='${MYSQL_DATABASE}';" 2>/dev/null)
echo "  Tabelas no banco: ${TABLE_COUNT_2}"

if [[ "${TABLE_COUNT_1}" != "${TABLE_COUNT_2}" ]]; then
  echo ""
  echo "FALHA: contagem de tabelas mudou (${TABLE_COUNT_1} → ${TABLE_COUNT_2})."
  echo "       O bundle não é idempotente — duplicou objetos!"
  exit 1
fi

# ── Resultado ─────────────────────────────────────────────────────────────────
echo ""
echo "╔══════════════════════════════════════════════════════════╗"
echo "║  ✓ Bundle_AplicadoDuasVezes_NaoFalhaENaoDuplica — PASS  ║"
echo "╚══════════════════════════════════════════════════════════╝"
echo ""
