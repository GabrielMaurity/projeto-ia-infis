// Executado pelo entrypoint do MongoDB no banco MONGO_INITDB_DATABASE (bd-historico-atendimentos).
db.createCollection("atendimentos");

// Busca das últimas conversas de um usuário com um agente, da mais recente para a mais antiga.
db.atendimentos.createIndex(
  { usuario: 1, agente: 1, dataHora: -1 },
  { name: "ix_atendimentos_usuario_agente_dataHora" }
);
