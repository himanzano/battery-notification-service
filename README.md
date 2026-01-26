# Battery Notification Service

Um serviço de monitoramento de bateria simples para Windows que exibe notificações Toast nativas quando o status da energia muda (conectado/desconectado da tomada).

## 📋 Funcionalidades

- ⚡ Notifica quando o carregador é conectado.
- 🔋 Notifica quando o carregador é desconectado (modo bateria).
- 🛠️ Roda silenciosamente em segundo plano (System Tray / Background).

## 🚀 Como Usar

Este aplicativo foi projetado para rodar como um processo em segundo plano na sessão do usuário, e não como um Serviço do Windows tradicional (Session 0), para garantir que as notificações visuais funcionem corretamente.

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (ou o runtime correspondente se for apenas executar).
- Windows 10 ou 11.

### 📦 Instalação (Adicionar à Inicialização do Windows)

Para que o monitor inicie automaticamente com o Windows:

1.  **Publique o projeto** (gere o executável final):
    Abra o terminal na pasta do projeto e execute:
    ```powershell
    dotnet publish -c Release -r win10-x64 --self-contained false -o C:\BatteryMonitor
    ```
    *Você pode alterar `C:\BatteryMonitor` para qualquer pasta de sua preferência.*

2.  **Abra a pasta de Inicialização (Startup):**
    Pressione `Win + R`, digite `shell:startup` e pressione Enter. Isso abrirá a pasta onde ficam os atalhos de programas que iniciam com o Windows.

3.  **Crie o atalho:**
    - Vá até a pasta onde você publicou o app (ex: `C:\BatteryMonitor`).
    - Clique com o botão direito em `BatteryNotificationService.exe` -> **Criar atalho**.
    - Recorte (`Ctrl + X`) o atalho criado.
    - Cole (`Ctrl + V`) dentro da pasta de Inicialização (que você abriu no passo 2).

Pronto! Agora o monitor de bateria iniciará automaticamente sempre que você fizer login.

### ▶️ Executar Manualmente

Basta ir até a pasta onde o projeto foi compilado (ou publicado) e dar um duplo clique em `BatteryNotificationService.exe`. Nenhuma janela abrirá, mas ele estará rodando em segundo plano.

Para verificar se está rodando, procure por "BatteryNotificationService" no Gerenciador de Tarefas.

## 🛑 Como Parar ou Desinstalar

- **Parar:** Abra o Gerenciador de Tarefas (`Ctrl + Shift + Esc`), encontre `BatteryNotificationService.exe` na lista de processos e clique em "Finalizar Tarefa".
- **Desinstalar:** Basta apagar o atalho da pasta `shell:startup` e remover a pasta onde você colocou os arquivos do programa.

## 📝 Logs

O aplicativo salva logs de execução (erros, mudanças de status, inicialização) no seguinte local:

```
%LocalAppData%\BatteryNotificationService\service.log
```
(Geralmente: `C:\Users\SEU_USUARIO\AppData\Local\BatteryNotificationService\service.log`)

## 💻 Desenvolvimento

Para rodar em modo de desenvolvimento:

```powershell
dotnet run --project BatteryNotificationService
```