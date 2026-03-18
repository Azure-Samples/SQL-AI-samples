# MSSQL MCP Server — Multi-Server / Multi-Database

Serveur MCP pour bases MSSQL avec support multi-serveur et sélection dynamique de base de données.

L'agent découvre automatiquement les bases disponibles sur tous les serveurs configurés et peut cibler n'importe quelle base par son nom, sans avoir à connaître le serveur qui l'héberge.

## Quick Start

```bash
cd Node
npm install
npm run build
```

## Configuration

### 1. Créer le fichier de config

Copier le fichier d'exemple et l'adapter :

```bash
mkdir -p ~/.mssql-mcp
cp Node/mcp-config.sample.json ~/.mssql-mcp/config.json
```

### 2. Structure du fichier

```json
{
  "readonly": false,
  "connectionTimeout": 30,
  "servers": [
    {
      "name": "SQL2019",
      "host": "localhost",
      "port": 50075,
      "user": "sa",
      "password": "your-password"
    },
    {
      "name": "SQL2017",
      "host": "localhost",
      "port": 49990,
      "user": "sa",
      "password": "your-password"
    },
    {
      "name": "SQLPROD",
      "host": "prodserver",
      "instance": "SQLEXPRESS",
      "user": "sa",
      "password": "your-password"
    }
  ]
}
```

| Champ | Requis | Description |
|-------|--------|-------------|
| `servers` | oui | Liste des serveurs SQL |
| `servers[].name` | oui | Nom logique du serveur (usage interne) |
| `servers[].host` | oui | Hostname ou IP |
| `servers[].port` | * | Port TCP (exclusif avec `instance`) |
| `servers[].instance` | * | Nom d'instance SQL Server (exclusif avec `port`) |
| `servers[].user` | oui | Login SQL |
| `servers[].password` | oui | Mot de passe |
| `readonly` | non | `true` pour restreindre aux opérations de lecture. Défaut : `false` |
| `connectionTimeout` | non | Timeout de connexion en secondes. Défaut : `30` |

\* Chaque serveur doit avoir soit `port`, soit `instance`.

### 3. Emplacement du fichier

| Priorité | Source |
|----------|--------|
| 1 | Variable d'environnement `MSSQL_MCP_CONFIG` |
| 2 | `~/.mssql-mcp/config.json` (défaut) |

### 4. Configurer le client MCP

**Cursor / VS Code :**

```json
{
  "mcp": {
    "servers": {
      "mssql": {
        "type": "stdio",
        "command": "node",
        "args": ["/chemin/vers/MssqlMcp/Node/dist/index.js"],
        "env": {
          "MSSQL_MCP_CONFIG": "/chemin/vers/.mssql-mcp/config.json"
        }
      }
    }
  }
}
```

**Claude Desktop :**

```json
{
  "mcpServers": {
    "mssql": {
      "command": "node",
      "args": ["/chemin/vers/MssqlMcp/Node/dist/index.js"],
      "env": {
        "MSSQL_MCP_CONFIG": "/chemin/vers/.mssql-mcp/config.json"
      }
    }
  }
}
```

> Si `MSSQL_MCP_CONFIG` n'est pas défini, le serveur cherche `~/.mssql-mcp/config.json`.

## Fonctionnement

### Découverte des bases

L'agent appelle `list_databases` pour obtenir la liste des bases disponibles sur tous les serveurs. Les bases système (master, tempdb, model, msdb...) sont filtrées automatiquement.

Les noms de bases au format `ENV_APPLICATION_CLIENT[_suffix]` sont parsés en métadonnées enrichies :

```
DEV_airpack_HU       → environment: DEV, application: airpack, client: HU
PROD_airpack_FR_2019  → environment: PROD, application: airpack, client: FR, suffix: 2019
```

L'agent peut filtrer par `environment`, `application` ou `client`.

### Sélection de base

Tous les outils acceptent un paramètre optionnel `database`. Si omis, la dernière base utilisée est réutilisée. Le serveur MCP résout automatiquement sur quel serveur se trouve la base demandée.

### Outils disponibles

| Outil | Description |
|-------|-------------|
| `list_databases` | Liste les bases avec métadonnées enrichies |
| `list_table` | Liste les tables d'une base |
| `describe_table` | Décrit le schéma d'une table |
| `read_data` | Exécute une requête SELECT |
| `insert_data` | Insère des données |
| `update_data` | Met à jour des données |
| `create_table` | Crée une table |
| `create_index` | Crée un index |
| `drop_table` | Supprime une table |

En mode `readonly`, seuls `list_databases`, `list_table`, `describe_table` et `read_data` sont disponibles.

## Sécurité

- Le fichier de config contient des mots de passe — ne jamais le commiter (ajouté au `.gitignore`)
- Les requêtes SELECT sont validées contre les injections SQL
- Le mode `readonly` bloque l'accès aux outils d'écriture au niveau du dispatch (pas juste masqués)
- Les noms de schéma dans `list_table` sont paramétrés (pas d'interpolation)
