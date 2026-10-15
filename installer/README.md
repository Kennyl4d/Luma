# Installer multilingue di Luma 0.3.4

Il file da consegnare è `artifacts/Luma-Setup-0.3.4-x64.exe`. Doppio clic per aggiornare la 0.2.2 o 0.3.0; app nel menu Start e in Impostazioni → App → App installate. Il codice UpgradeCode e gli identificatori dei componenti della 0.2.2 restano stabili. Chiudere Luma prima di aggiornare. Il pacchetto include OCR, modelli e runtime C++ app-local: nessun setup aggiuntivo. L’autore e il produttore dichiarato sono KENNY; questa informazione non è una firma digitale.

Il Setup rileva la lingua di visualizzazione di Windows e applica la lingua al wizard MSI, senza selettore. Sono incluse tutte le 14 lingue dell'app: inglese, italiano, ceco, tedesco, spagnolo, francese, giapponese, coreano, polacco, portoghese brasiliano, russo, turco, cinese semplificato e tradizionale. Inglese per le lingue non supportate. Il file MSI generato separatamente parte in inglese; usare l'EXE per il rilevamento automatico. Dettagli in LOCALIZATION.md.

- WiX Toolset 5.0.2 / WixToolset.UI.wixext 5.0.2.
- Windows 10/11 x64, minimo build 14393; verifica del build Windows letta da CurrentBuildNumber (la proprietà MSI WindowsBuild può riportare un valore di compatibilità).
- Per utente, `%LOCALAPPDATA%\Programs\Luma`, con selezione della cartella.
- Formati registrati in HKCU; `UserChoice` non modificato.
- Collegamento Start con icona originale; nessun servizio, attività pianificata o avvio automatico.
- File, runtime e licenze in un CAB incluso; guida utente con sezioni nelle 14 lingue in `Guida.txt`.
- Un componente per file con identità deterministica e registro HKCU come key path, per supportare correttamente installazioni per utente.
- Windows Installer gestisce rollback, file in uso, riparazione e aggiornamenti. Chiudere Luma prima di installare.
- UpgradeCode fisso: `96D16DAB-6DCA-44CE-9460-9478486150F1`. Mantenerlo per le versioni future.
- La vecchia installazione PowerShell viene rilevata e la sua voce ARP sostituita. Rimozione dei soli vecchi script e del loro manifest.
- App e MSI non firmati; un certificato valido e la firma Authenticode sono necessari per un editore verificato. Nessuna firma autofirmata aggiunta al sistema.

Compilazione: `pwsh -File scripts/Build-Installer.ps1 -RebuildApp`. Per un compilatore WiX presente altrove: `-Compiler "C:\strumenti\wix.exe"` (versione 5.0.2, estensione UI disponibile).

Compilazione e validazione strutturale del compilatore eseguite. Nessun ciclo di installazione/disinstallazione sul PC della sessione. Prima di una distribuzione pubblica verificare installazione pulita, migrazione da PowerShell, riparazione, aggiornamento a una versione superiore e disinstallazione in un account o macchina di prova, incluso un percorso con spazi e file personali nella cartella.

Fonti: [WiX Toolset](https://github.com/wixtoolset/wix), [WiX scope perUser](https://docs.firegiant.com/wix/schema/wxs/packagescopetype/), [firma dei pacchetti Windows](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options).
