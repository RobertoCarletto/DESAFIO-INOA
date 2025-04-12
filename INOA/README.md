# 📈 Desafio BT - INOA - Monitoramento de Ações B3 com Alertas por E-mail

Este é um aplicativo de console em C# que monitora em tempo real a cotação de ações da B3 e envia alertas por e-mail caso o valor ultrapasse ou fique abaixo de limites definidos pelo usuário.

---

## 🛠 Funcionalidades

- Monitoramento de um ou mais ativos da B3
- Alertas automáticos por e-mail para **compra** e **venda**
- Leitura de configurações via `config.json` e `assets.args`
- Menu de comandos via terminal para edição rápida
- Consulta dos ativos disponíveis diretamente via [brapi.dev](https://brapi.dev)
- Registro de logs automáticos em `logs/alerts.log`

---

## 📦 Instalação

### 1. Clone o repositório:
```bash
git clone https://github.com/seu-usuario/stock-quote-alert.git
cd stock-quote-alert
```

### 2. Compile o projeto:
```bash
dotnet build
```

> O executável estará em `bin/Debug/net8.0/INOA.exe`

---

## ▶️ Primeira execução

Apenas rode o programa:
```bash
./INOA.exe
```

O programa irá:
- Solicitar o e-mail para recebimento dos alertas
- Solicitar o intervalo de verificação (em segundos, mínimo 60)
- Solicitar os ativos a serem monitorados, cada linha: `ativo limite-venda limite-compra` (ex: `PETR4 32.00 31.00 VALE3 65.00 63.00`)


## 💻 Comandos disponíveis


| Comando         | Função                                                           |
|-----------------|------------------------------------------------------------------|
| --edit-email    | Editar o e-mail de destino dos alertas                           |
| --edit-assets   | Atualizar os ativos monitorados                                  |
| --edit-interval | Alterar o tempo entre verificações                               |
| --reset-config  | Zerar e-mail, ativos e intervalo                                 |
| --show-log      | Visualizar o histórico de eventos e alertas                      |
| --list-assets   | Listar os ativos da B3 disponíveis para consulta                 |

---

## 📝 Logs

Todos os eventos e erros são registrados em:
```text
logs/alerts.log
```

---

