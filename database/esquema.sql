CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;

CREATE TABLE "Leitores" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Leitores" PRIMARY KEY AUTOINCREMENT,
    "Nome" TEXT NOT NULL,
    "Numero" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "Versao" INTEGER NOT NULL
);

CREATE TABLE "Livros" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Livros" PRIMARY KEY AUTOINCREMENT,
    "Isbn" TEXT NOT NULL,
    "Titulo" TEXT NOT NULL,
    "Autor" TEXT NOT NULL,
    "Categoria" TEXT NOT NULL,
    "ExemplaresDisponiveis" INTEGER NOT NULL,
    "Versao" INTEGER NOT NULL,
    CONSTRAINT "CK_Livros_Stock" CHECK (ExemplaresDisponiveis >= 0 AND ExemplaresDisponiveis <= 10000)
);

CREATE TABLE "Emprestimos" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Emprestimos" PRIMARY KEY AUTOINCREMENT,
    "LivroId" INTEGER NOT NULL,
    "LeitorId" INTEGER NOT NULL,
    "DataInicio" TEXT NOT NULL,
    "DataPrevista" TEXT NOT NULL,
    "DataDevolucao" TEXT NULL,
    CONSTRAINT "CK_Emprestimos_Devolucao" CHECK (DataDevolucao IS NULL OR DataDevolucao >= DataInicio),
    CONSTRAINT "CK_Emprestimos_Prazo" CHECK (DataPrevista >= DataInicio),
    CONSTRAINT "FK_Emprestimos_Leitores_LeitorId" FOREIGN KEY ("LeitorId") REFERENCES "Leitores" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Emprestimos_Livros_LivroId" FOREIGN KEY ("LivroId") REFERENCES "Livros" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_Emprestimos_DataDevolucao_DataPrevista" ON "Emprestimos" ("DataDevolucao", "DataPrevista");

CREATE INDEX "IX_Emprestimos_LeitorId" ON "Emprestimos" ("LeitorId");

CREATE INDEX "IX_Emprestimos_LivroId" ON "Emprestimos" ("LivroId");

CREATE UNIQUE INDEX "IX_Leitores_Numero" ON "Leitores" ("Numero");

CREATE UNIQUE INDEX "IX_Livros_Isbn" ON "Livros" ("Isbn");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261119164726_Inicial', '8.0.34');

COMMIT;

