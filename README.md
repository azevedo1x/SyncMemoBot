# SyncMemoBot

Bot de Discord para lembretes — pessoais (DM) e em canais — com processamento de linguagem natural em PT / EN / ES e fuso horário configurável.

## Stack

- **.NET 8** (Web App + `IHostedService`)
- **Discord.Net 3.x** — slash commands via `Interactions`
- **Hangfire** + `Hangfire.Storage.SQLite` — agendamento persistente local
- **Microsoft.Recognizers.Text.DateTime** — parsing multilíngue de tempo
- **Serilog** — logs estruturados (console + arquivo rotativo)

## Pré-requisitos

- .NET 8 SDK (a solution força `net8.0` mesmo se o SDK default for mais novo)
- Conta no [Discord Developer Portal](https://discord.com/developers/applications)

## Setup

### 1. Criar o bot no Discord

1. Acesse o Developer Portal → **New Application** → nomeie como SyncMemo
2. Aba **Bot** → **Reset Token** → copie (não compartilhe nem commite)
3. Aba **OAuth2** → URL Generator:
   - Scopes: `bot`, `applications.commands`
   - Bot Permissions: `Send Messages`
   - Use a URL gerada para adicionar o bot ao seu servidor

### 2. Configurar token e (opcionalmente) guild de dev

```powershell
cd src\SyncMemoBot.Discord
dotnet user-secrets init
dotnet user-secrets set "Discord:Token" "<SEU_TOKEN>"

# Opcional — registra slash commands instantaneamente no seu guild de teste
# (sem isso, o registro é global e pode levar até 1 hora pra propagar)
dotnet user-secrets set "Discord:DevGuildId" "<ID_DO_SEU_SERVIDOR>"
```

Em produção, prefira variáveis de ambiente: `Discord__Token=...`, `Discord__DevGuildId=...`.

### 3. Rodar

```powershell
dotnet run --project src\SyncMemoBot.Discord
```

- Bot conecta no Discord, registra `/remind` e `/remindchannel`
- Dashboard Hangfire em `https://localhost:7204/hangfire`
- Logs em `src\SyncMemoBot.Discord\logs\bot-<data>.log`

> Sem token configurado, o app sobe em modo "dashboard only" e loga um warning — útil pra inspecionar a UI do Hangfire sem precisar conectar no Discord.

## Comandos

### `/remind`
Lembrete pessoal — entregue via DM.

| Parâmetro | Descrição |
|---|---|
| `time` | Texto livre. Ex: "em 2 horas", "amanhã às 15:00", "tomorrow at 3pm" |
| `message` | O que lembrar |

### `/remindchannel`
Lembrete público em um canal de texto.

| Parâmetro | Descrição |
|---|---|
| `channel` | Canal de texto alvo (você precisa de permissão `Send Messages` nele) |
| `time` | Idem `/remind` |
| `message` | Idem `/remind` |

Ambos respondem **efêmero** (só você vê) com confirmação ou erro, em menos de 3 segundos.

## Formatos de tempo suportados

O parser tenta primeiro o idioma do app do usuário (`UserLocale`). Se não reconhecer, tenta PT, depois ES. Locales fora de PT/EN/ES caem em EN como primário.

| Português | Inglês | Espanhol |
|---|---|---|
| em 2 horas | in 2 hours | en 2 horas |
| amanhã às 15:00 | tomorrow at 3pm | mañana a las 15:00 |
| amanhã às 3 da tarde | tomorrow at 15:00 | mañana a las 3 de la tarde |
| amanhã às 15 horas | next monday | pasado mañana |
| depois de amanhã | in 30 minutes | en 30 minutos |

**Atenção**: use `15:00` (com dois-pontos) ou `15 horas`. **Não use `15h`** — o recognizer interpreta `15h` como duração de 15 horas, não como horário do dia.

## Configuração

`src\SyncMemoBot.Discord\appsettings.json`:

```json
{
  "Discord": {
    "Token": "",
    "DevGuildId": null
  },
  "Reminder": {
    "TimeZone": "America/Sao_Paulo"
  },
  "ConnectionStrings": {
    "Hangfire": "Data Source=hangfire.db"
  }
}
```

Trocar o fuso horário (ex: rodar em Lisboa):
```json
"Reminder": { "TimeZone": "Europe/Lisbon" }
```

## Arquitetura

Clean Architecture pragmática, 3 projetos:

```
src/
├── SyncMemoBot.Core/             # Domínio puro — só BCL
│   ├── Reminders/                # ReminderTarget (sum type), ScheduledReminder
│   ├── Time/                     # IClock, IReminderTimeZone, ITimeParsingService, TimeParseResult
│   ├── Scheduling/               # IReminderScheduler
│   ├── Dispatch/                 # IReminderDispatcher
│   ├── Localization/             # ILocalizedMessages
│   └── Options/                  # ReminderOptions
├── SyncMemoBot.Infrastructure/   # Implementações
│   ├── Time/                     # MultilingualTimeParser + clock/timezone/locale-mapper
│   ├── Scheduling/               # HangfireReminderScheduler + HangfireJobInvoker
│   ├── Localization/             # LocalizedMessages (strings hardcoded PT/EN/ES)
│   └── ServiceCollectionExtensions.cs   # AddSyncMemoBotInfrastructure
└── SyncMemoBot.Discord/          # Host (Web App + BackgroundService)
    ├── Program.cs                # Composition root
    ├── DiscordClientHost.cs      # Lifecycle do DiscordSocketClient
    ├── Modules/                  # /remind, /remindchannel
    └── Dispatch/                 # DiscordReminderDispatcher
```

Dependências fluem em um sentido: `Discord → Infrastructure → Core`. Core não depende de nada externo.

## Testes

```powershell
dotnet test
```

10 testes em `tests\SyncMemoBot.Tests` focados no parser multilíngue: PT/EN/ES, fallback de idioma, locale não suportado, horário no passado, input lixo, input vazio.

## Limitações conhecidas (MVP)

- **Single-instance**: `Hangfire.Storage.SQLite` não tolera múltiplos writers. Escala horizontal exige troca para Postgres/Redis (a abstração `IReminderScheduler` isola essa decisão do Core).
- **Sem cancelamento por comando**: lembrete agendado vai disparar.
- **Sem persistência própria** do `Reminder`: o payload vive nos argumentos do job Hangfire.
- **Sem recorrência**: só lembretes pontuais.
- **Dashboard Hangfire sem autenticação**: em produção, configure `UseHangfireDashboard("/hangfire", new DashboardOptions { Authorization = ... })`.
- **Formato `15h` não funciona** como horário do dia (ver Formatos de tempo).

## Estrutura de arquivos relevantes

```
SyncMemoBot/
├── SyncMemoBot.slnx              # Solution XML (.NET SDK 10 default)
├── README.md                     # Este arquivo
├── CLAUDE.md                     # Guia para sessões do Claude Code
├── .gitignore
├── src/
└── tests/
```

## Licença

Projeto pessoal sem licença explícita. Use sob sua responsabilidade.
