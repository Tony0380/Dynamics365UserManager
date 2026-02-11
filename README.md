# Dynamics 365 User Manager

[![Build & Release](https://github.com/Tony0380/Dynamics365UserManager/actions/workflows/release.yml/badge.svg)](https://github.com/Tony0380/Dynamics365UserManager/actions/workflows/release.yml)

Applicazione Windows Forms (.NET 8.0) per la gestione utenti in ambienti Microsoft Dynamics 365.

## Funzionalita

### Connessione
- Autenticazione OAuth con Microsoft (MSAL)
- Discovery automatica degli ambienti disponibili per l'utente
- Token cache persistente con crittografia DPAPI
- Selezione ambiente tramite dialog con filtro

### Tab 1 - Cambio Business Unit
- Ricerca utenti per nome o email
- Visualizzazione ruoli correnti dell'utente
- Cambio Business Unit con riassegnazione automatica dei Security Roles (per nome) nella nuova BU

### Tab 2 - Clone User
- Copia configurazione da un utente sorgente a un utente target:
  - Business Unit
  - Security Roles
  - Teams (con selezione individuale)

### Tab 3 - Reassign Records
- Trasferimento record tra utenti per le entita:
  - Account, Contact, Opportunity, Quote, Sales Order, Lead, Case
- Anteprima con conteggio record
- Doppia conferma per operazioni distruttive

### Tab 4 - Security Roles
- Ricerca ruoli per nome
- Visualizzazione utenti assegnati a un ruolo
- Assegnazione e rimozione ruoli da utenti (anche multi-selezione)

### Tab 5 - Teams
- Ricerca team per nome
- Visualizzazione membri del team
- Aggiunta e rimozione utenti dai team (anche multi-selezione)

### Tab 6 - Trova Ruoli
- Selezione entita, tipo di permesso (Create, Read, Write, Delete, Append, AppendTo, Assign, Share) e livello di profondita (User, BU, BU+Child, Organization)
- Ricerca automatica delle combinazioni minime di Security Roles che soddisfano tutti i requisiti
- Risultati ordinati per numero di ruoli

## Prerequisiti

- .NET 8.0 SDK
- Visual Studio 2022+ o `dotnet` CLI

## NuGet Packages

| Package | Versione |
|---|---|
| Microsoft.PowerPlatform.Dataverse.Client | 1.1.35 |
| Microsoft.Identity.Client | 4.67.2 |
| System.Security.Cryptography.ProtectedData | 9.0.4 |

## Setup

1. Aprire `Dynamics365UserManager.sln` in Visual Studio
2. Ripristinare i pacchetti NuGet (`dotnet restore` o tramite Visual Studio)
3. Compilare in configurazione Release: `dotnet build -c Release`
4. L'eseguibile si trova in `bin\Release\net8.0-windows\`

## Utilizzo

1. Cliccare **Connetti** per autenticarsi con le credenziali Microsoft
2. Selezionare l'ambiente Dynamics 365 dalla lista
3. Utilizzare i tab per le operazioni desiderate

### Reset Login
Il pulsante **Cancella Credenziali** cancella la cache dei token e forza un nuovo login al prossimo tentativo di connessione.

## Struttura Progetto

```
Dynamics365UserManager/
├── Program.cs                 # Entry point
├── MainForm.cs                # Form principale con 6 tab
├── AppTheme.cs                # Tema colori (light/dark)
├── EnvironmentSelector.cs     # Dialog selezione ambiente
├── ConnectionManager.cs       # Gestione connessione, MSAL, discovery
└── DynamicsOperations.cs      # Operazioni Dataverse (query, assign, clone, role finder)
```

## ClientId OAuth

L'applicazione utilizza il ClientId di esempio Microsoft per applicazioni pubbliche:
`51f81489-12ee-4a9e-aaae-a2591f45987d`

Per ambienti di produzione, registrare un'app in Azure AD e sostituire il ClientId in `ConnectionManager.cs`.
