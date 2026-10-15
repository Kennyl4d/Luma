# Stato del port Linux — 15 ottobre 2026

Port della 0.3.4 x86_64, in progetto separato Luma.Linux. Compilazione e pubblicazione self-contained Linux riuscite con .NET 10/Avalonia 12.1.4 e Magick.NET 15.0.0. Tesseract C API Linux 5, 14 modelli inclusi, filtri SVG e policy MVG conservati. Implementate le funzioni elencate nel README Linux; non eseguiti test o avvio su Linux.

Generati .deb/.rpm/.pkg.tar.zst con nFPM 2.48.0 Windows x64 portabile, scaricato dalla Release upstream e con SHA-256 verificato tramite checksums.txt upstream. Nessuna VM/WSL installata. Il file Arch è Zstandard effettivo (magic 28B52FFD) con .PKGINFO/.INSTALL/.MTREE; root:root e launcher 0755. Il Debian ha debian-binary/control.tar.gz/data.tar.gz. Sono inclusi l'ELF luma, runtime .NET, Skia/HarfBuzz, Magick.Native Linux, 14 modelli e 11 configurazioni XML. Dipendenze ABI dichiarate; nFPM non rende tutti i sistemi Linux automaticamente compatibili.

Ispezionate le DT_NEEDED dei binari pubblicati senza eseguirli: host/CLR dipendono dalle librerie glibc/libstdc++/libgcc; Skia usa fontconfig; Magick ha dipendenze glibc/resolv. libtesseract.so.5 è fornita dalla distribuzione. libcoreclrtraceptprovider.so ha una dipendenza LTTng opzionale per tracing, non usata dal normale viewer.

Restano da verificare: esecuzione su desktop Debian/Ubuntu/Fedora/Arch, X11/XWayland, decodifica effettiva di tutti i formati, animazione GIF, OCR e selezione, dialoghi/clipboard/tema di sistema, HiDPI, startup/RAM e installazione/upgrade/disinstallazione. Non garantita parità 1:1 verificata senza queste prove. Nessuna pubblicazione GitHub o modifica dell'account dell'utente eseguita; repository ancora da creare dall'utente. La workflow è preparata per costruire su Linux con i packager ufficiali, senza installare Linux sul PC Windows.

Pacchetti non firmati; metadato licenza app LicenseRef-Unspecified in attesa della scelta del proprietario. Verifiche di compilazione e metadati non equivalgono a collaudo funzionale sul sistema.
