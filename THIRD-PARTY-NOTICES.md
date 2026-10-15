# Dipendenze di Luma

Per il port Linux: Avalonia 12.1.4 e Fluent Theme, MIT (https://github.com/AvaloniaUI/Avalonia); SkiaSharp/libSkiaSharp, MIT e licenze Skia/BSD (https://github.com/mono/SkiaSharp); HarfBuzzSharp/HarfBuzz, MIT e licenze upstream (https://github.com/harfbuzz/harfbuzz). Tesseract/Leptonica provengono dai pacchetti della distribuzione Linux; i modelli tessdata_fast restano inclusi. nFPM 2.48.0 è uno strumento di build MIT e non viene incluso nell'app (https://github.com/goreleaser/nfpm). Il runtime self-contained pubblicato include i suoi LICENSE/THIRD-PARTY-NOTICES. Il codice originale di Luma è concesso da KENNY sotto GPL-3.0-only; vedere LICENSE. Le dipendenze mantengono le rispettive licenze.

- Tesseract .NET wrapper 5.2.0: Charles Weld, Apache-2.0; InteropDotNet MIT. https://github.com/charlesw/tesseract
- Tesseract OCR 5.2.0: Apache-2.0. https://github.com/tesseract-ocr/tesseract
- Leptonica 1.82.0: licenza BSD. https://github.com/DanBloomberg/leptonica
- Modelli tessdata_fast: Apache-2.0, commit 87416418657359cb625c412a48b6e1d6d41c29bd. https://github.com/tesseract-ocr/tessdata_fast
- Microsoft Visual C++ Runtime: DLL app-local ottenute dal redistributable x64 ufficiale Microsoft; termini inclusi in Microsoft-VC-Runtime-LICENSE.rtf. https://learn.microsoft.com/en-us/cpp/windows/redistributing-visual-cpp-files

I testi delle licenze OCR sono inclusi in `licenses`. I modelli sono inclusi nell’installer e nessuna foto viene trasmessa ai fornitori. `Luma/tessdata/sources.json` registra origine e SHA-256 dei modelli scaricati.

- .NET e WPF: Microsoft, licenza MIT e licenze dei componenti del runtime. https://github.com/dotnet/runtime e https://github.com/dotnet/wpf
- Magick.NET 15.0.0: Dirk Lemstra, Apache-2.0. https://github.com/dlemstra/Magick.NET/blob/main/License.txt
- ImageMagick e delegate incorporati nel binario Magick.NET: vedere le licenze ufficiali distribuite da Magick.NET. https://github.com/dlemstra/Magick.NET/tree/main/docs
- libheif: LGPL-3.0; decoder HEVC e altre dipendenze possono avere licenze e condizioni proprie. https://github.com/strukturag/libheif

Questa nota identifica le dipendenze, ma non sostituisce i testi di licenza. Il pacchetto include i testi upstream disponibili. Per una distribuzione pubblica verificare gli obblighi applicabili al binario dei delegate, inclusi LGPL e brevetti HEVC nei territori di distribuzione. Il decoder è incluso per evitare dipendenze dalle estensioni dello Store.
# Localizzazioni installer WiX 5.0.2

I file XML in Luma/DecoderConfig sono esportati dalle risorse ConfigurationFiles.Default di Magick.NET 15.0.0 e mantengono le intestazioni di copyright ImageMagick. policy.xml è adattato per il renderer MSVG interno e per bloccare delegate esterni e coder di rete; le altre configurazioni restano quelle upstream.

Le 14 risorse originali in installer/localization provengono dal tag v5.0.2 del repository ufficiale wixtoolset/wix, src/ext/UI/wixlib. Sono conservate con i commenti di copyright e distribuite sotto Microsoft Reciprocal License (MS-RL), inclusa in licenses/WiX-LICENSE.txt. Le traduzioni personalizzate Luma sono in installer/languages.json. Il compilatore WiX non è incluso nel Setup.

