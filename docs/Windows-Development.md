# Luma — visualizzatore immagini per Windows 10 e 11

Versione 0.3.4 · Autore: **KENNY**.

## Windows 10 e Windows 11

Requisito minimo del pacchetto: Windows 10 versione 1607 (build 14393), a 64 bit. È consigliata la versione 22H2 aggiornata. Il runtime .NET è incluso nel pacchetto. L'interfaccia WPF, il tema automatico, i decoder e il riconoscimento del testo non richiedono WinUI o componenti esclusivi di Windows 11. Gli angoli arrotondati sono richiesti solo su Windows 11; la barra del titolo usa l'attributo compatibile con la build Windows 10 disponibile. Sulle build Windows 10 anteriori alla 1809 rimane la barra del titolo del sistema, mentre il tema interno dell'app continua a funzionare.

Questa release è stata compilata e confezionata nell'ambiente corrente; non è stata eseguita su una macchina Windows 10 in questa sessione. La compatibilità tecnica prevista non costituisce certificazione Microsoft. La tabella upstream di .NET 10 include Windows 10 1607/1809/21H2 Enterprise nelle edizioni previste dal ciclo di supporto: https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md.

## Lingue e informazioni

Interfaccia completa in inglese, italiano, russo, ceco, tedesco, spagnolo, francese, giapponese, coreano, polacco, portoghese brasiliano, turco, cinese semplificato e tradizionale. È selezionata automaticamente la lingua di visualizzazione dell’utente Windows, con fallback inglese. Il portoghese usa la traduzione brasiliana; Hong Kong/Taiwan/Macao e zh-Hant usano il cinese tradizionale. Non esiste un selettore di lingua. Le risorse si aggiornano sulle notifiche di impostazioni Windows; un cambio della lingua di visualizzazione che richiede disconnessione viene recepito al nuovo avvio.

Il pulsante ⓘ apre la scheda Informazioni con versione, autore KENNY, formati, privacy e istruzioni di selezione. Menu, tooltip, messaggi, errori dell’app e scheda Informazioni hanno tutte le traduzioni. Anche il Setup include tutte le 14 lingue: rileva automaticamente la lingua di visualizzazione di Windows e applica la traduzione del wizard, dei pulsanti, dell’avanzamento e degli errori MSI. Usare il file Setup.exe; aprendo direttamente l’MSI, la lingua di base è inglese.

## Testo selezionabile nelle foto

Riconoscimento automatico locale dopo la visualizzazione iniziale, senza pulsante OCR e senza inviare immagini in rete. Passando sulle parole rilevate, il puntatore diventa una barra di selezione: trascinare per evidenziare parole consecutive e copiare con Ctrl+C oppure clic destro → Copia testo. Il menu offre anche Copia tutto il testo. Le evidenziazioni seguono zoom, scroll e rotazione della vista. Trascinando fuori dal testo si sposta la foto; tenendo premuto Maiusc si può spostarla anche sopra il testo.

Tesseract con modelli fast inclusi per le 14 lingue: viene usata la lingua di sistema insieme all’inglese, senza caricare tutti i modelli in memoria. È una selezione per parole, non per singole lettere. Il riconoscimento viene eseguito su una copia con lato massimo 2200 pixel, una richiesta alla volta, con risultati superati ignorati. La chiamata nativa in corso non viene interrotta immediatamente. L’engine e i buffer temporanei sono rilasciati dopo l’analisi. Le GIF animate non vengono analizzate, per evitare evidenziazioni non corrispondenti ai fotogrammi. Testo molto piccolo, inclinato, scritto a mano, poco contrastato o in una lingua diversa da quelle in uso può non essere riconosciuto bene. Non è una replica completa di Live Text di iPhone.

Per ricompilare: prima eseguire `scripts/Download-OcrModels.ps1` se i modelli non sono presenti. Le DLL del runtime C++ app-local sono in `Luma/NativeRuntime`, con licenza Microsoft inclusa: non è necessario un installer runtime separato.

MVP nativo Windows x64, C#/.NET 10 e WPF. Aprire `Luma.exe` dal pacchetto portabile oppure passargli un percorso: `Luma.exe "C:\Foto\immagine.heic"`.

## Funzioni

- JPEG, PNG, GIF animate, HEIC/HEIF, WebP, AVIF, BMP/DIB, TIFF, ICO e SVG.
- TIFF: prima pagina; ICO: variante a risoluzione maggiore; HEIF/AVIF/WebP: immagine primaria o primo fotogramma. SVG statici rasterizzati all’apertura con il renderer MSVG incluso: nessuno script, DTD o risorsa esterna; alcuni effetti SVG complessi possono non essere riprodotti.
- Le dichiarazioni DOCTYPE standard degli SVG esportati da Illustrator vengono ignorate, senza caricare DTD esterni. Il file originale rimane invariato.
- Il renderer SVG usa la configurazione inclusa in DecoderConfig: lettura MVG interna abilitata, programmi esterni e coder di rete disattivati. Viene caricata solo al primo formato gestito dal decoder nativo, mantenendo il percorso veloce per JPEG/PNG.
- Riconoscimento della firma JPEG/PNG/GIF: apre anche immagini convertite che conservano un’estensione diversa, ad esempio un JPEG chiamato `.HEIC`. Il file originale non viene rinominato o riscritto.
- Navigazione precedente/successiva nella stessa cartella, con ordinamento numerico naturale.
- Zoom, adattamento senza ingrandire le immagini piccole, 100% e trascinamento per spostare la vista.
- Fullscreen, rotazione della vista, orientamento EXIF dei JPEG.
- Apertura da finestra, trascinamento o associazione file.
- Tema selezionabile dal pulsante “Tema”: “chiaro”, “scuro”, “segui il sistema”. Scelta salvata per l’utente, aggiornamento automatico quando cambia il tema Windows.
- Barre di scorrimento, menu e barra del titolo coerenti con il tema; icona originale incorporata nell’eseguibile e nella finestra.

La rotazione riguarda la vista e non modifica il file. Non ci sono cataloghi, sincronizzazione, account, pubblicità o editor.

## Comandi

| Comando | Tasto |
| --- | --- |
| Apri | Ctrl+O |
| Precedente / successiva | ← / → |
| Zoom | Rotellina, + / − |
| Adatta | F |
| 100% | 1 |
| Ruota 90° | R |
| Fullscreen / ritorna | F11 / Esc |

## Compilare

Serve .NET SDK 10 x64 su Windows. Dalla radice del progetto:

```powershell
.\scripts\Build.ps1
.\scripts\Build.ps1 -Portable
```

L'ultimo comando produce `artifacts\Luma\Luma.exe` e uno ZIP autonomo. Nessun runtime .NET o codec dello Store deve essere installato sul PC di destinazione. WPF non supporta trimming completo: il pacchetto conserva il runtime e usa ReadyToRun, senza estrazione single-file al primo avvio.

## Installare e scegliere l'app predefinita

### Setup Windows multilingue (distribuzione finale)

Avviare `Luma-Setup-0.3.4-x64.exe`, seguire la procedura guidata nella lingua rilevata automaticamente e poi aprire Luma dal menu Start. L'installer è per l'utente corrente, con destinazione predefinita `%LOCALAPPDATA%\Programs\Luma`; include runtime e decoder e non richiede privilegi amministrativi. Windows 10 versione 1607 (build 14393) x64 o successivo.

Chiudere prima Luma. La precedente installazione creata con `Install.ps1` viene riconosciuta dalla registrazione esistente: l'MSI usa quella cartella e sostituisce la precedente voce di disinstallazione. Per futuri pacchetti MSI sono predisposti aggiornamento, riparazione e disinstallazione tramite Windows Installer. La disinstallazione rimuove i file registrati dal pacchetto; immagini personali e tema salvato in `%LOCALAPPDATA%\Luma` sono conservati.

Per il doppio clic: Impostazioni → App → App predefinite → Luma, poi scegliere i formati. L'installer registra tutti i formati supportati in “Apri con” e “App predefinite” e lascia la scelta a Windows e all'utente.

L'MSI è un normale pacchetto Windows Installer, **non firmato digitalmente e non certificato o pubblicato da Microsoft**. Per identificare un editore verificato occorre firmare app e installer con un certificato riconosciuto o servizio di firma. Accanto al pacchetto è disponibile il checksum SHA-256.

### Pacchetto portabile e vecchio installer PowerShell

Estrarre lo ZIP, poi dalla cartella estratta:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install.ps1
```

Installa per l'utente in `%LOCALAPPDATA%\Programs\Luma`, crea un collegamento nel menu Start e registra i sei suffissi in “Apri con” e “App predefinite”. Nelle impostazioni aperte dallo script, cercare Luma e assegnare i formati desiderati. Windows richiede la scelta dell'utente; lo script non altera `UserChoice`. Non è necessario essere amministratore.

Disinstallare da Impostazioni → App → App installate → Luma oppure eseguire `Uninstall.ps1` nella cartella installata. Chiudere prima Luma.

## Scelte tecniche e limiti

| Stack | Vantaggi | Costi |
| --- | --- | --- |
| C# / WinUI 3 | Controlli Fluent e integrazione moderna | Windows App SDK, deployment e startup più articolati |
| C++ / Win32 + Direct2D | Maggior controllo su startup e memoria | Più codice per accessibilità, input e gestione sicura dei decoder |
| **C# / WPF** | Rendering accelerato, UI nativa, progetto semplice da distribuire | Runtime .NET incluso, estetica Fluent costruita con stili e DWM |

WPF è il compromesso scelto per questo MVP. Finestra con angoli arrotondati su Windows 11 e decorazioni standard su Windows 10, controlli essenziali e temi chiaro/scuro/sistema. Non è WinUI 3 e non replica ogni dettaglio dei controlli Fluent. La preferenza è conservata in `%LOCALAPPDATA%\Luma\theme.txt`; al primo avvio segue il sistema. Le barre personalizzate conservano trascinamento e clic sulla pista, con cursore grigio visibile sul fondo scuro/chiaro. L’icona originale contiene sette risoluzioni da 16 a 256 pixel e può essere rigenerata con `scripts\Generate-Icon.ps1`.

JPEG/PNG usano WIC tramite WPF. HEIC e GIF usano Magick.NET Q8 x64 con decoder nativo incluso (libheif e delegate HEVC); non richiedono l'app Foto o estensioni UI dello Store. Il decoder viene caricato solo sul percorso HEIC/GIF. GIF composte con offset, trasparenza e disposal, intervalli per fotogramma e conteggio dei cicli; delay sotto 20 ms normalizzati a 100 ms. La riproduzione si interrompe durante la minimizzazione. HDR e immagini a 10/12 bit vengono visualizzate a 8 bit; non è un visualizzatore HDR o con gestione colore professionale. HEIF multimmagine/Live Photos: solo immagine primaria.

Una sola decodifica alla volta fuori dal thread UI; richieste superate ignorate e cancellate tra le fasi. Una chiamata nativa già in corso non può essere interrotta immediatamente. Nessuna cache permanente o prelettura: memoria concentrata sull'immagine corrente. I bitmap sono scollegati dal file e congelati prima di arrivare alla UI. La cartella viene enumerata dopo la visualizzazione iniziale.

Limiti espliciti dell'MVP: file fino a 128 MiB, immagini fino a 40 MP, GIF fino a 1000 fotogrammi e 24 milioni di pixel complessivi delle tele. GIF più grandi sono rifiutate con messaggio; non mostrate silenziosamente come immagini statiche. I limiti nativi sono 192 MiB di cache e per singola richiesta di allocazione, niente cache su disco, due thread. Non sono un limite assoluto sulla memoria totale del processo: copie native e bitmap WPF possono coesistere temporaneamente. Un futuro decoder GIF incrementale ridurrebbe il picco.

Prestazioni rispetto al visualizzatore Windows 7: obiettivo progettuale, non equivalenza dimostrata. Per misurare startup distinguere avvio freddo/caldo, formato, risoluzione, prima visualizzazione e working set; ReadyToRun e decoder caricato su richiesta aiutano ma vanno misurati sul PC di destinazione.

## Struttura

- `Luma/Imaging`: caricamento, limiti, orientamento e fotogrammi.
- `Luma/Services`: enumerazione della cartella e ordinamento naturale.
- `Luma/MainWindow`: presentazione, input, zoom e riproduzione.
- `scripts`: compilazione, pacchetto e registrazione per utente.

## Fonti

- [Imaging WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/imaging-overview)
- [Magick.NET](https://github.com/dlemstra/Magick.NET)
- [Piattaforma app predefinite Windows](https://learn.microsoft.com/en-us/windows/apps/develop/windows-integration/default-apps-platform)

Consultare `THIRD-PARTY-NOTICES.md` prima di redistribuire il decoder. App e MSI non firmati: Windows può mostrare avvisi. La firma Authenticode e un'eventuale distribuzione Microsoft Store richiedono preparazione ulteriore.

## Ricompilare l'installer

Richiede PowerShell 7, SDK .NET 10, WiX Toolset 5.0.2 e l'estensione `WixToolset.UI.wixext` 5.0.2. Il progetto usa WiX 5 con licenza open source MS-RL; il compilatore non viene distribuito nell'installer. Installare gli strumenti in una cartella di lavoro e registrare l'estensione dalla radice del progetto:

```powershell
dotnet tool install wix --version 5.0.2 --tool-path .tools\wix5 --configfile NuGet.Config
$env:DOTNET_ROLL_FORWARD = 'Major'
.tools\wix5\wix.exe extension add WixToolset.UI.wixext/5.0.2
pwsh -File scripts\Build-Installer.ps1 -RebuildApp
```

Se è presente solo il runtime .NET 10, per eseguire il compilatore WiX 5 usare `DOTNET_ROLL_FORWARD=Major`; lo script di build lo imposta per il solo processo. Il file da distribuire è `artifacts\Luma-Setup-0.3.4-x64.exe`, con MSI e 13 trasformazioni linguistiche incorporati; inglese come lingua di base. Il launcher usa il .NET Framework 4.x presente su Windows 10/11 e le API Windows Installer, senza installare runtime aggiuntivi. La copia MSI estratta è conservata in `%LOCALAPPDATA%\Luma\Installer` per la riparazione. È generato anche l’MSI per amministratori: senza il launcher parte in inglese; una lingua si può applicare tramite `TRANSFORMS=":Luma_ru-RU"`. I sorgenti MSI sono in `installer\Luma.wxs`; l'elenco dei file viene generato dal payload pubblicato, con identificatori stabili per gli aggiornamenti.
