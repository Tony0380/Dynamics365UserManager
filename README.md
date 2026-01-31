# Dynamics 365 User Manager

Applicazione Windows Forms (.NET Framework 4.6.2) per la gestione utenti in ambienti Microsoft Dynamics 365.

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

## Prerequisiti

- .NET Framework 4.6.2
- Visual Studio 2017+

## NuGet Packages

| Package | Versione |
|---|---|
| Microsoft.CrmSdk.XrmTooling.CoreAssembly | 9.1.1.65 |
| Microsoft.Identity.Client | 4.61.3 |
| Newtonsoft.Json | 13.0.3 |

## Setup

1. Aprire `Dynamics365UserManager.sln` in Visual Studio
2. Ripristinare i pacchetti NuGet (`nuget restore` o tramite Visual Studio)
3. Compilare in configurazione Release
4. L'eseguibile si trova in `bin\Release\`

## Utilizzo

1. Cliccare **Connetti** per autenticarsi con le credenziali Microsoft
2. Selezionare l'ambiente Dynamics 365 dalla lista
3. Utilizzare i tab per le operazioni desiderate

### Reset Login
Il pulsante **Reset Login** cancella la cache dei token e forza un nuovo login al prossimo tentativo di connessione.

## Struttura Progetto

```
Dynamics365UserManager/
├── Program.cs                 # Entry point
├── MainForm.cs                # Form principale con tab
├── EnvironmentSelector.cs     # Dialog selezione ambiente
├── ConnectionManager.cs       # Gestione connessione, MSAL, discovery
├── DynamicsOperations.cs      # Operazioni CRM (query, assign, clone)
└── Properties/
    └── AssemblyInfo.cs
```

## ClientId OAuth

L'applicazione utilizza il ClientId di esempio Microsoft per applicazioni pubbliche:
`51f81489-12ee-4a9e-aaae-a2591f45987d`

Per ambienti di produzione, registrare un'app in Azure AD e sostituire il ClientId in `ConnectionManager.cs`.
