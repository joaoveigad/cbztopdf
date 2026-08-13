# Changelog

Todas as mudanças relevantes do projeto PagePdf.

O formato segue [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e o projeto segue [SemVer](https://semver.org/lang/pt-BR/).

## [Unreleased]

## Mudanças atuais

### 1. Fila de arquivos mais clara (UI)
Arquivo: `src/PagePdf.UI/MainWindow.axaml`

- **Linhas 45–73**: a fila foi movida para um `StackPanel` (`QueuePanel`), exibido apenas quando há itens (`IsVisible="False"` por padrão). Foram adicionados estilos para o selo de status (`Border.badge`) com variações por estado: `queued` (índigo), `done` (verde), `failed` (vermelho) e `converting` (âmbar), cada um com cor de fundo e de texto próprias.
- **Linhas 75–86**: cabeçalho da fila com o título "Queue" e o botão `ClearQueueButton` (classe `subtle`) que limpa toda a fila de uma vez.
- **Linhas 88–90**: a lista de arquivos ficou em um `ScrollViewer` com altura máxima de 150 px, para não estourar a barra de status quando há muitos arquivos.
- **Linhas 91–133**: `QueueList` (ItemsControl) agora usa um `DataTemplate` com cada item em um "card" (`Border` com fundo, borda e cantos arredondados), contendo:
  - **Linhas 103–108**: botão `RemoveButton` (classe `icon`, conteúdo "✕", com `ToolTip` "Remove from queue") que remove o arquivo individualmente da fila.
  - **Linhas 109–122**: selo `StatusBadge` com o status (queued/done/failed/converting) e cores ligadas por binding (`Classes.queued/done/failed/converting`).
  - **Linhas 123–128**: nome do arquivo em destaque (negrito, com truncamento de texto longo).

### 2. Estilos de botões da fila
Arquivo: `src/PagePdf.UI/App.axaml`

- **Linhas 37–54**: novo estilo `Button.subtle` (botão discreto com borda) usado no botão "Clear", com variações `:pointerover` e `:disabled`.
- **Linhas 56–71**: novo estilo `Button.icon` (botão de ícone sem fundo) usado no botão de remover arquivo, com fundo vermelho no hover e estado desabilitado.

### 3. Lógica de remover/limpar a fila
Arquivo: `src/PagePdf.UI/MainWindow.axaml.cs`

- **Linha 1**: adicionado `using System.ComponentModel;` para suportar `INotifyPropertyChanged` no `QueueItem`.
- **Linhas 141–151**: novo método `RemoveArchiveFromQueue(string archivePath)` — remove um arquivo da fila (inativo durante conversão), atualiza a UI e o texto de status.
- **Linhas 153–163**: novo método `ClearQueue()` — esvazia a fila inteira (inativo durante conversão), atualiza a UI e o status.
- **Linhas 165–171**: handler `RemoveQueueItem_Click` — lê o `DataContext` (o `QueueItem`) do botão clicado e chama `RemoveArchiveFromQueue`.
- **Linhas 173–174**: handler `ClearQueueButton_Click` — chama `ClearQueue()`.
- **Linhas 176–181**: `RefreshQueueUi` atualizado — agora alimenta o `ItemsSource` com os objetos `QueueItem` (não mais strings), controla a visibilidade do `QueuePanel` e sincroniza o estado dos botões.
- **Linhas 183–188**: novo método `UpdateQueueStatus` — mostra "Ready" quando a fila está vazia, senão "N file(s) queued".
- **Linhas 338–343**: `SetBusy` atualizado — também habilita/desabilita o `ClearQueueButton` conforme a fila e o estado de conversão.
- **Linhas 373–412**: `QueueItem` agora é `internal` e implementa `INotifyPropertyChanged`, expondo `Status` (com notificação) e propriedades booleanas `IsQueued`/`IsDone`/`IsFailed`/`IsConverting` que controlam a cor do selo via binding.

### 4. Testes da fila
Arquivo: `tests/PagePdf.UI.Tests/MainWindowTests.cs`

- **Linhas 187–210**: teste `MainWindow_remove_archive_removes_item_from_queue` — remove um arquivo da fila e verifica contagem, status e botão de conversão.
- **Linhas 213–236**: teste `MainWindow_clear_queue_removes_all_items` — limpa a fila e verifica que o painel some, o status vira "Ready" e a conversão desabilita.
- **Linhas 239–262**: teste `MainWindow_queue_item_has_remove_button` — mostra a janela, força o layout e verifica que cada item da fila contém um botão de remoção.
- **Linhas 160–184**: `MainWindow_convert_keeps_items_in_queue_with_done_status` atualizado para validar o `QueueItem.Status == "done"` em vez do texto concatenado.

## [1.0.0] - 2026-08-13

### Adicionado
- Conversão de arquivos **.cbz** para **PDF** (drag & drop, seletor de arquivos ou fila de múltiplos).
- Fila de conversão com status visual (queued/done/failed/converting), remoção individual e limpeza.
- **Pasta de saída default**: botão mostra a pasta atual e permite escolher uma nova; a conversão reutiliza a pasta em conversões seguintes.
- Progresso percentual em background e diálogo de erro amigável.
- Publicação **single-file self-contained** para Windows x64 (`PagePdf.UI.exe`).
- README bilíngue (Português / English) e licença MIT.

### Corrigido
- Status da fila padronizado em minúsculas (`queued`/`done`/`failed`): o botão de conversão agora desabilita após concluir todos os itens e o estado visual "queued" aparece corretamente.
- Fim de linha (CRLF/LF) normalizado no repositório via `.gitattributes`.

## Funcionamento atual

### Fluxo de conversão (CBZ -> PDF)
1. **UI** (`PagePdf.UI`) captura os arquivos .cbz de entrada (picker ou drag & drop), mantém a fila na barra de status e escolhe o destino .pdf.
2. **Application** (`ConvertComicUseCase`) valida o request e orquestra a conversão.
3. **Infrastructure** lê cada arquivo .cbz (zip de imagens) e gera o PDF.
4. **Domain** contém as entidades puras, sem dependências externas.

### Camadas (Clean Architecture)
- **`PagePdf.Domain`** — sem dependências. Entidades (`ComicArchive`, `ComicPage`), enum `ImageFormat`, exceção `ComicArchiveException`.
- **`PagePdf.Application`** — depende de Domain. Caso de uso `ConvertComicUseCase` (validação, medição com `Stopwatch`, embrulho de exceções), DTOs e interfaces (`IComicArchiveReader`, `IPdfGenerator`).
- **`PagePdf.Infrastructure`** — depende de Application. Serviços `ZipComicArchiveReader` (leitura do .cbz) e `PdfSharpPdfGenerator` (geração do PDF), registro de DI (`AddInfrastructure`). Pacotes: PDFsharp 6.2.4, NaturalSort.Extension 4.4.1, Microsoft.Extensions.* 10.0.10.
- **`PagePdf.UI`** — aplicação Avalonia com menu "File", pickers de arquivo/pasta, conversão em background com progresso percentual, diálogo de erro amigável e fila de conversão com remoção individual ("✕") e limpeza total ("Clear").
- **`tests/`** — projetos xUnit por camada; UI testada via `Avalonia.Headless.XUnit`.

### Estado atual das implementações
- Concluído: estrutura/solução, Domain completo, validação e timing no use case, DI registrado, UI Avalonia, leitura do .cbz (`ZipComicArchiveReader`), geração de PDF (`PdfSharpPdfGenerator`), integração back-front (Open/Export via StorageProvider, conversão em background com progresso percentual e diálogo de erro), fila de conversão com visual em cards, status colorido por estado, botão de remoção individual e botão "Clear" para limpar a fila — com testes de comportamento.
- Concluído: publish single-file self-contained (win-x64) e testes de Domain (7.1).

### Observações
- Avalonia 11.3.18 (linha 11.x) foi escolhido por manter compatibilidade com o TFM `net8.0`; a linha 12.x exige .NET 10.
- `App.axaml.cs` qualifica `Application` como `Avalonia.Application` para evitar conflito com o namespace `PagePdf.Application`.
- Restore/build devem ser executados no lado Windows (`dotnet build` via PowerShell/VS); rodar o `dotnet.exe` a partir do shell WSL sobre diretório montado dispara o bug MSB4006 do NuGet.
