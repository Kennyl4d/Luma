# Stato della consegna — 14 ottobre 2026

Versione 0.3.4: compatibilità prevista Windows 10 x64 dalla versione 1607 (build 14393), oltre a Windows 11. Abbassata la condizione di avvio MSI da build 22000 a 14393 e aggiornati i messaggi in tutte le 14 lingue. Decorazioni DWM isolate in WindowAppearance: barra scura da build 17763 con fallback attributo 19 quando 20 non disponibile; angoli arrotondati richiesti solo da build 22000. Manifest con DPI PerMonitorV2 e fallback true/pm per Windows 10 precedenti a 1703. WPF/.NET 10, runtime autonomo e dipendenze x64 conservati. Nessun test né installazione su Windows 10 eseguiti in questa sessione; non certificata compatibilità reale su tutte le build/edizioni.

Versione 0.3.3: recuperato l'errore nativo effettivo sul file Marvel_Logo.svg: MagickPolicyErrorException, MVG bloccato dalla policy upstream. Il renderer MSVG produce un disegno MVG interno. Distribuiti i file XML di configurazione Magick.NET 15.0.0 in DecoderConfig; la policy conserva le restrizioni upstream, consente la lettura MVG, blocca tutti i delegate esterni e i coder di rete HTTP/HTTPS/URL/FTP. Configurazione caricata una sola volta al primo formato nativo, fuori dal percorso JPEG/PNG. Il filtro SVG resta attivo prima del renderer. Nessun file di configurazione scritto a runtime.

Diagnosi sul file originale tramite lo stesso ImageLoader dell'app: decodifica riuscita, 1000 × 402 pixel, un fotogramma. SHA256 prima/dopo: 7E3F489FBA31D8C92310AFEF4789DDD32FDF6A621990377348A722BBF33F5775, identico. Non eseguita una prova visiva dell'interfaccia né installazione sul sistema in questa sessione.

Versione 0.3.2: correzione per Marvel_Logo.svg esportato da Adobe Illustrator. Il parser ignora ora la dichiarazione DOCTYPE SVG 1.1 anziché rifiutare il file (DtdProcessing.Ignore, XmlResolver nullo); entità definite in DTD non espanse e istruzioni xml-stylesheet rimosse prima del renderer. Il file originale non è modificato. Il Setup conserva la correzione al riepilogo MSI e il log della revisione 0.3.1-r2. Compilazione/pubblicazione e pacchetti di aggiornamento; nessuna prova interattiva eseguita. Ripristino con avviso NU1900: feed di audit NuGet non raggiungibile in questa sessione, dipendenze già presenti.

Revisione installer 0.3.1-r2: correzione del Template Summary da elenco di 14 LANGID a x64;1033, come richiesto per un database MSI sorgente. Le 13 trasformazioni restano incorporate. Diagnosi del database: la trasformazione italiana originale è applicabile e produce ProductLanguage 1040. Il launcher ora avvia msiexec con log verboso, attende il risultato e mostra i codici di errore invece di chiudersi silenziosamente. App e payload restano 0.3.1, stesso ProductCode del pacchetto corretto. Non è stata eseguita un'installazione sul sistema.

Versione 0.3.1: aggiunti WebP, AVIF, BMP/DIB, TIFF (prima pagina), ICO (variante maggiore), SVG statico rasterizzato e associazioni. GIF animate già integrate: coalescing disposal/trasparenza, ritardi, cicli e pausa quando minimizzata. Setup automatico in 14 lingue: risorse ufficiali WiX 5.0.2, traduzioni Luma e trasformazioni MSI incorporate, stesso UpgradeCode; rilevamento della lingua di visualizzazione Windows nel launcher. Nessuna prova interattiva dei nuovi formati o del ciclo di installazione in questa sessione. Pacchetti non firmati.

Consegna 0.3.1 completata: compilazione Release senza avvisi/errori, pubblicazione self-contained, 14 compilazioni MSI e 13 trasformazioni generate e incorporate. Lettura dei metadati finali: 13 archivi linguistici presenti in _Storages; Setup FileVersion 0.3.1.0. Generati MSI, Setup EXE, SHA-256, ZIP portabile e ZIP sorgenti. Nessuna installazione avviata.

Versione 0.3.0: 14 lingue con 50 stringhe ciascuna, automatiche dal display language Windows, fallback inglese; informazioni con autore KENNY; selezione OCR locale per parole, copia, zoom e rotazione; Tesseract/fast models e runtime VC app-local inclusi. Build Release completata senza errori. Non eseguite verifiche interattive dell’OCR né installazione/upgrade sul sistema dell’utente in questa sessione; la qualità delle traduzioni non è stata revisionata da madrelingua. Installer ancora non firmato.

Distribuzione finale: aggiunto installer Windows MSI per Luma 0.2.2, WiX Toolset/UI 5.0.2, per utente, x64, italiano, CAB incluso. Predisposti collegamento Start, formati, migrazione della registrazione PowerShell e aggiornamenti MSI. Compilazione e validazione ICE del compilatore completate; installazione, riparazione, aggiornamento e disinstallazione non eseguiti sul sistema dell'utente in questa sessione. App e MSI non firmati. Non è una certificazione Microsoft.

Versione 0.2.2: riconoscimento delle firme JPEG/PNG/GIF prima di scegliere il decoder. Il file segnalato IMG_0052.HEIC inizia con FF D8 FF E0 e JFIF: è un JPEG con estensione HEIC. La versione precedente forzava il decoder HEIC e lo rifiutava. Il percorso HEIC/HEIF effettivo resta quello del decoder nativo.

Diagnosi sul file originale tramite ImageLoader aggiornato: decodifica riuscita, 1286 × 1714, un fotogramma. SHA-256 identico prima e dopo la lettura: il file non è stato modificato. Nessuna conversione o rinomina necessaria.

Versione 0.2.1: solo maiuscola iniziale per le voci “Chiaro”, “Scuro”, “Segui il sistema”, oltre all’aggiornamento del numero di versione. Lo stack resta WPF.

Versione 0.2.0: aggiunti temi chiaro/scuro/sistema persistenti, aggiornamento su notifica Windows, barre di scorrimento coerenti e icona originale multirisoluzione. Build e pubblicazione aggiornate. La versione 0.1.0 è stata confermata dall’utente come veloce e capace di aprire i formati provati; le nuove modifiche non sono state verificate interattivamente in questa sessione.

- SDK locale: .NET 10.0.401 x64.
- Dipendenza nativa: Magick.NET-Q8-x64 15.0.0, versioni bloccate in `Luma/packages.lock.json`.
- Build Release e pubblicazione self-contained Windows x64 eseguite sul PC Windows della sessione.
- Non sono stati eseguiti test automatici, verifiche interattive della UI o misure di startup/RAM.
- Gli script di installazione/disinstallazione sono inclusi; non sono stati eseguiti e le associazioni del PC non sono state cambiate.

Prima di una release pubblica, verificare manualmente JPEG con i diversi orientamenti EXIF, PNG con trasparenza, GIF con disposal 2/3 e cicli finiti, HEIC provenienti da iPhone e HEIF multimmagine; poi navigazione rapida, cambio monitor/DPI, fullscreen e installazione/disinstallazione su un account di prova. Confrontare startup freddo/caldo e picco memoria con file di dimensioni rappresentative. Il supporto dei formati è implementato ma non certificato attraverso questa matrice.

L'eseguibile non è firmato. Il pacchetto è un MVP per uso e valutazione locale, con installer PowerShell per utente e archivio portabile; non include un MSI/MSIX firmato.

Consegna 0.3.2: compilazione/pubblicazione completate senza errori; avvisi NU1900 del feed di audit descritti sopra. Generati Setup EXE, MSI, checksum e ZIP sorgenti/portabile. Metadati MSI: riepilogo x64;1033. Non eseguita apertura interattiva del logo né installazione sul sistema in questa sessione.

Consegna 0.3.3: Setup EXE, MSI con riepilogo x64;1033, 14 lingue e 11 configurazioni XML del decoder nel payload; ZIP sorgenti e portabile generati. Nessuna installazione avviata.

Consegna 0.3.4: Setup EXE e MSI generati nelle 14 lingue, ZIP sorgenti e portabile pronti. Metadati finali: LaunchCondition Installed OR (VersionNT64 AND OSBUILD >= 14393), Template Summary x64;1033, Setup FileVersion 0.3.4.0. Nessuna esecuzione/installazione su Windows 10. Compilazione senza errori; avviso NU1900 del feed audit NuGet non raggiungibile.
