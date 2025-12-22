<p align="center">
  <img src="https://capsule-render.vercel.app/api?type=waving&color=0:141e30,100:243b55&height=170&section=header&text=HospitalHub&fontSize=42&fontColor=ffffff" />
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8-blueviolet?logo=dotnet&logoColor=white"/>
  <img src="https://img.shields.io/badge/Docker-Enabled-blue?logo=docker&logoColor=white"/>
  <img src="https://img.shields.io/badge/MySQL-8.0-4479A1?logo=mysql&logoColor=white"/>
  <img src="https://img.shields.io/badge/RabbitMQ-Async-FF6600?logo=rabbitmq&logoColor=white"/>
  <img src="https://img.shields.io/badge/Keycloak-Security-4D4D4D?logo=keycloak&logoColor=white"/>
  <img src="https://img.shields.io/badge/Ocelot-Gateway-6E57E0"/>
  <img src="https://img.shields.io/badge/Redis-Cache-DC382D?logo=redis&logoColor=white"/>
  <img src="https://img.shields.io/badge/OpenTelemetry-Tracing-000000?logo=opentelemetry&logoColor=white"/>
  <img src="https://img.shields.io/badge/Jaeger-UI-66CFE3"/>
</p>

# 🏥 HospitalHub – Arquitetura de Microserviços

Projeto backend desenvolvido com .NET 8, arquitetura de microserviços, Gateway API, Keycloak para autenticação/autorização, RabbitMQ para comunicação assíncrona e envio de e-mails via serviço de notificação.

## 🚀 Como utilizar (passo a passo)

### 1) Subir o ambiente

Pelo diretório `docker/`:

```bash
docker compose up -d --build
```

Serviços e portas locais:

- Gateway (Ocelot): `http://localhost:5000`
- Agendamentos API: `http://localhost:5001`
- Clínica API: `http://localhost:5002`
- Cirúrgico API: `http://localhost:5003`
- Notificação API: `http://localhost:5004`
- Keycloak: `http://localhost:8085`
- RabbitMQ UI: `http://localhost:15672`
- MailHog UI: `http://localhost:8025`
- Jaeger UI (traces): `http://localhost:16686`

### 2) Autenticação (Keycloak) – obter token

As APIs protegem endpoints via JWT (Keycloak). Para chamar endpoints protegidos, obtenha um access token e envie no header:

```http
Authorization: Bearer <ACCESS_TOKEN>
```

Exemplo (Password Grant) para obter token no realm `hospital`:

```bash
curl -s -X POST "http://localhost:8085/realms/hospital/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=hospital-api" \
  -d "username=user" \
  -d "password=admin" | jq -r .access_token
```

> Observação: o projeto aponta para o realm `hospital` e audience `hospital-api`. Garanta que o realm/cliente/usuários/roles estejam configurados no Keycloak de acordo com a seção “Usuários do Keycloak”.

---

## ✅ Passo a passo por API

### 🌐 Gateway API (porta 5000)

Use o Gateway como ponto único de entrada (recomendado). Rotas principais:

- Agendamentos: `GET/POST http://localhost:5000/agendamentos`
- Consultas: `GET http://localhost:5000/consultas`
- Cirurgias: `GET http://localhost:5000/cirurgias`

Exemplo: listar agendamentos (USER/ADMIN)

```bash
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5000/agendamentos
```

Idempotência no Gateway (quando enviar `Idempotency-Key`):

- Aplicada para métodos **exceto** `POST`, `PUT`, `PATCH` (ex.: `GET`, `DELETE`).
- Exemplo (DELETE idempotente):

```bash
curl -X DELETE \
  -H "Authorization: Bearer $TOKEN" \
  -H "Idempotency-Key: 2d6a6d3c-10d7-4a0d-9b62-8e3f2b8a9a7b" \
  http://localhost:5000/agendamentos/<ID>
```

### 📅 Agendamentos API (porta 5001)

#### 1) Criar paciente (sem autenticação)

```bash
curl -X POST http://localhost:5001/api/Pacientes \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "João da Silva",
    "documento": "12345678901",
    "dataNascimento": "1990-01-01T00:00:00",
    "telefone": "11999999999",
    "email": "joao@example.com"
  }'
```

#### 2) Criar agendamento (USER/ADMIN)

```bash
curl -X POST http://localhost:5001/api/Agendamentos \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "pacienteId": "<PACIENTE_ID>",
    "dataHora": "2026-01-10T14:00:00Z",
    "tipo": 0,
    "descricao": "Consulta de rotina",
    "emergencial": false
  }'
```

#### 3) Listar agendamentos (USER/ADMIN)

```bash
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5001/api/Agendamentos
```

Cache (Redis) está aplicado ao `GET /api/Agendamentos` com TTL curto e invalidação em operações de escrita.

#### 4) Confirmar agendamento (ADMIN)

Ao confirmar, a API publica evento no RabbitMQ para Clínica/Cirúrgico/Notificação.

```bash
curl -X PUT \
  -H "Authorization: Bearer $TOKEN" \
  http://localhost:5001/api/Agendamentos/<ID>/confirmar
```

#### 5) Cancelar agendamento (USER/ADMIN)

```bash
curl -X DELETE \
  -H "Authorization: Bearer $TOKEN" \
  http://localhost:5001/api/Agendamentos/<ID>
```

### 🩺 Clínica API (porta 5002)

#### 1) Listar consultas (MEDICO/ADMIN)

```bash
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5002/api/Consultas
```

#### 2) Criar consulta (ADMIN)

```bash
curl -X POST http://localhost:5002/api/Consultas \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "agendamentoId": "<AGENDAMENTO_ID>",
    "pacienteId": "<PACIENTE_ID>",
    "dataHora": "2026-01-10T14:00:00Z",
    "tipo": "Rotina"
  }'
```

#### 3) Vincular sintomas à consulta (MEDICO/ADMIN)

```bash
curl -X POST http://localhost:5002/api/Consultas/<CONSULTA_ID>/sintomas \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "sintomaIds": ["<SINTOMA_ID>"] }'
```

#### 4) Doenças e sintomas (MEDICO/ADMIN para GET, ADMIN para POST)

```bash
curl -H "Authorization: Bearer $TOKEN" http://localhost:5002/api/Doencas
curl -H "Authorization: Bearer $TOKEN" http://localhost:5002/api/Sintomas
```

### 🏥 Cirúrgico API (porta 5003)

#### 1) Listar cirurgias (MEDICO/ADMIN)

```bash
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5003/api/Cirurgias
```

#### 2) Registrar cirurgia (ADMIN)

```bash
curl -X POST http://localhost:5003/api/Cirurgias \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "agendamentoId": "<AGENDAMENTO_ID>",
    "pacienteId": "<PACIENTE_ID>",
    "dataHora": "2026-01-10T16:00:00Z"
  }'
```

#### 3) Atualizar status (MEDICO/ADMIN)

```bash
curl -X PUT http://localhost:5003/api/Cirurgias/<ID>/status \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "status": 1
  }'
```

### ✉️ Notificação API (porta 5004)

Este serviço consome eventos do RabbitMQ e envia e-mail (assíncrono). Endpoint HTTP apenas para health check:

```bash
curl http://localhost:5004/
```

## 🧱 Arquitetura Geral

Gateway API
Centraliza o acesso às APIs internas e valida autenticação/roles.

Agendamentos API
Responsável pelo cadastro e confirmação de agendamentos.

Clínica API
Responsável pelas consultas médicas, sintomas e doenças.

Cirúrgico API
Responsável pelas cirurgias vinculadas aos agendamentos.

Notificação API
Microserviço assíncrono que consome eventos do RabbitMQ e envia e-mails.

RabbitMQ
Broker de mensagens para desacoplamento entre serviços.

Keycloak
Autenticação e autorização baseada em JWT e roles.



## 🔐 Controle de Acesso por API (Keycloak Roles)

### Gateway API

| Endpoint        | USER | MEDICO | ADMIN |
|-----------------|------|--------|-------|
| /agendamentos   | ✔️   | ✔️     | ✔️    |
| /consultas      | ✔️   | ✔️     | ✔️    |
| /cirurgias      | ✔️   | ✔️     | ✔️    |

### Agendamentos API

| Endpoint                                  | USER | MEDICO | ADMIN |
|-------------------------------------------|------|--------|-------|
| POST /api/Agendamentos                    | ✔️   | ❌     | ✔️    |
| GET /api/Agendamentos                     | ✔️   | ❌     | ✔️    |
| PUT /api/Agendamentos/{id}/confirmar      | ❌   | ❌     | ✔️    |
| GET /api/Pacientes/{id}                   | ✔️   | ✔️     | ✔️    |


### Clínica API

| Endpoint                                  | USER | MEDICO | ADMIN |
|-------------------------------------------|------|--------|-------|
| GET /api/Consultas                        | ❌   | ✔️     | ✔️    |
| POST /api/Consultas                       | ❌   | ✔️     | ✔️    |
| POST /api/Consultas/{id}/sintomas         | ❌   | ✔️     | ✔️    |
| GET /api/Doencas                          | ❌   | ✔️     | ✔️    |
| POST /api/Doencas                         | ❌   | ✔️     | ✔️    |

### Cirurgico API

| Endpoint           | USER | MEDICO | ADMIN |
|--------------------|------|--------|-------|
| GET /api/Cirurgias | ❌   | ✔️     | ✔️    |
| POST /api/Cirurgias| ❌   | ✔️     | ✔️    |

### Notificação API

| Serviço                | USER | MEDICO | ADMIN |
|------------------------|------|--------|-------|
| Consumer RabbitMQ      | —    | —      | —     |
| Endpoints HTTP         | ❌   | ❌     | ❌    |

### Usuários do Keycloak

| Usuário      | Role   | Descrição                                      |
|--------------|--------|------------------------------------------------|
| user         | USER   | Criação e consulta de agendamentos             |
| medico       | MEDICO | Consulta de consultas e cirurgias              |
| admin        | ADMIN  | Confirmação de agendamentos e acesso total     |
| dev          | ADMIN  | Usuário técnico para testes                    |

# 🐇 Comunicação Assíncrona (RabbitMQ)

Quando um agendamento é confirmado:

Agendamentos API
   → publica evento AgendamentoConfirmado
       → RabbitMQ
           → Clínica API
           → Cirúrgico API
           → Notificação API

Esse modelo garante:

Desacoplamento entre serviços

Maior resiliência

Escalabilidade

## 📧 Envio de E-mail (Notificação)

O envio de e-mails é realizado pelo microserviço Notificação API, de forma assíncrona, após a confirmação do agendamento.

## 🧪 Ambiente de Teste – MailHog (e-mail fake)

Por padrão, o projeto utiliza o MailHog para testes locais.

Como testar:

Suba os containers:

docker compose up -d --build

Crie um paciente com um e-mail fictício

Crie e confirme um agendamento (ADMIN)

Acesse:

http://localhost:8025

O e-mail de confirmação aparecerá na interface do MailHog.

## 🔭 Observabilidade (OpenTelemetry + Jaeger)

O projeto exporta traces via OpenTelemetry (OTLP) e disponibiliza visualização no Jaeger.

- Jaeger UI: http://localhost:16686
- Os serviços configuram `OTEL_SERVICE_NAME` e `OTEL_EXPORTER_OTLP_ENDPOINT` via `docker-compose`.

## ♻️ Idempotência (Gateway)

O Gateway aplica idempotência **apenas quando** o cliente envia o header `Idempotency-Key`.

- Aplicado para métodos **exceto** `POST`, `PUT`, `PATCH` (ex.: `GET`, `DELETE`).
- Respostas são armazenadas (Redis) e repetidas quando a mesma combinação (método + rota + query + usuário + key) for reutilizada.


## 📬 Ambiente Real – Gmail (e-mail verdadeiro)

Também é possível testar o envio de e-mails reais via SMTP Gmail.

## 🔐 Pré-requisitos

- Conta Gmail

- Verificação em duas etapas ativada

- Senha de app gerada no Google:
- No seu Gmail, clique na sua foto no canto superior a direita da tela > Clique em Gerenciar sua Conta Google > Clique em Segurança e Login > Como você faz login no Google, aqui habilite a verificação em duas etapas > Após isso, volte para a página anterior e clique na 🔍 no canto superior a esquerda onde está escrito "Pesquisar na Sua Conta do Google" e escreva: Senhas de APP e clique na opção, vai redirecionar para a tela de criação da senha, só seguir o passo a passo.
- Obs: Sempre vai pedir pra colocar senha ou vai pedir o código de dois fatores para você conseguir acessar essas páginas.

## ⚙️ Configuração (docker-compose)
notificacao-api:
  environment:
    - Smtp__Host=smtp.gmail.com
    - Smtp__Port=587
    - Smtp__FromName=HospitalHub
    - Smtp__FromEmail=SEU_EMAIL@gmail.com
    - Smtp__User=SEU_EMAIL@gmail.com
    - Smtp__Pass=SENHA_DE_APP_GMAIL


## ⚠️ Nunca utilize a senha real do Gmail. Use apenas senha de app.

Depois disso:

docker compose down
docker compose up -d --build

## 🧪 Teste

Crie um paciente com seu e-mail real

Crie e confirme um agendamento

O e-mail de confirmação será enviado para sua caixa de entrada 📱📧

## 🧠 Observação Importante

O envio de e-mail é assíncrono.
Falhas no SMTP não impactam o fluxo principal de agendamentos.

## 📦 Tecnologias e Versões Utilizadas
## 🔧 Runtime e SDK

| Tecnologia | Versão |
|-----------|--------|
| .NET SDK  | 8.0.6  |
| .NET Runtime | 8.0.6 |
| ASP.NET Core | 8.0.6 |

## 📚 Pacotes NuGet (APIs)

| Pacote NuGet                              | Versão | Utilização |
|------------------------------------------|--------|------------|
| Microsoft.EntityFrameworkCore             | 8.0.6  | ORM |
| Microsoft.EntityFrameworkCore.Design     | 8.0.6  | Migrations |
| Microsoft.EntityFrameworkCore.Tools      | 8.0.6  | CLI EF |
| Pomelo.EntityFrameworkCore.MySql         | 8.0.6  | MySQL Provider |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.6 | Autenticação JWT |
| Microsoft.OpenApi                         | 1.6.x  | Swagger |
| Swashbuckle.AspNetCore                   | 6.5.x  | Swagger UI |

## 🐇 Mensageria

| Pacote NuGet        | Versão | Utilização |
|---------------------|--------|------------|
| RabbitMQ.Client     | 6.8.x  | Mensageria assíncrona |

## 📧 Envio de E-mail (Notificação API)

| Pacote NuGet | Versão | Utilização |
|--------------|--------|------------|
| MailKit      | 4.x    | Envio de e-mail SMTP |
| MimeKit      | 4.x    | Construção de mensagens |

## 🐳 Infraestrutura (Containers)

| Tecnologia | Versão |
|-----------|--------|
| Docker | Latest |
| Docker Compose | 3.9 |
| RabbitMQ | 3-management |
| MySQL | 8.0 |
| Redis | 7-alpine |
| Keycloak | 24.0.4 |
| MailHog | Latest |
| Jaeger | 1.57 |

## 🖥️ Frontend (Opcional / Futuro)

Este projeto foi desenvolvido com foco em arquitetura backend,
microsserviços, mensageria e segurança.

Um frontend (Angular ou React) pode ser integrado futuramente
consumindo o Gateway API, respeitando as regras de autenticação
e autorização definidas no Keycloak.
