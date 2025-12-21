<p align="center">
  <img src="https://capsule-render.vercel.app/api?type=waving&color=0:141e30,100:243b55&height=170&section=header&text=HospitalHub&fontSize=42&fontColor=ffffff" />
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0.6-blueviolet"/>
  <img src="https://img.shields.io/badge/Docker-Enabled-blue"/>
  <img src="https://img.shields.io/badge/RabbitMQ-Async-orange"/>
  <img src="https://img.shields.io/badge/Keycloak-Security-green"/>
</p>

# 🏥 HospitalHub – Arquitetura de Microserviços

Projeto backend desenvolvido com .NET 8, arquitetura de microserviços, Gateway API, Keycloak para autenticação/autorização, RabbitMQ para comunicação assíncrona e envio de e-mails via serviço de notificação.

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
| Keycloak | 24.0.4 |
| MailHog | Latest |

## 🖥️ Frontend (Opcional / Futuro)

Este projeto foi desenvolvido com foco em arquitetura backend,
microsserviços, mensageria e segurança.

Um frontend (Angular ou React) pode ser integrado futuramente
consumindo o Gateway API, respeitando as regras de autenticação
e autorização definidas no Keycloak.
