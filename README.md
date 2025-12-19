🏥 Hospital Microservices Platform

Projeto completo de microsserviços com .NET 8, comunicação assíncrona via RabbitMQ, API Gateway com Ocelot e autenticação/autorização com Keycloak (JWT + Roles).

Este projeto demonstra, na prática, um fluxo end‑to‑end com controle de acesso por perfil (USER, ADMIN, MEDICO), persistência em bancos separados e orquestração via Docker.

📐 Arquitetura Geral


<img width="712" height="292" alt="image" src="https://github.com/user-attachments/assets/5ef9d1b2-cfbe-4e18-bb34-79cce900afde" />



Cada microsserviço possui banco MySQL próprio

Comunicação assíncrona desacoplada

Segurança centralizada no Gateway



## 🧩 Microsserviços
📅 Agendamentos API

Criação de pacientes

Criação de agendamentos

Confirmação de agendamentos (ADMIN)

Publicação de evento AgendamentoConfirmado


## 🏥 Clínica API

Consumo do evento de agendamento confirmado

Criação automática de consultas

Gestão de doenças e sintomas



## 🏥 Cirúrgico API

Consumo do evento de agendamento confirmado

Criação automática de cirurgias



## 🚪 API Gateway

Centraliza acesso às APIs

Validação de JWT

Controle de acesso por roles



## 🔐 Autenticação e Autorização
Roles

USER: cria pacientes e agendamentos

ADMIN: confirma agendamentos

MEDICO: consulta consultas e cirurgias

Tecnologias

Keycloak

OAuth2 / OpenID Connect

JWT Bearer Tokens



## 📦 Tecnologias Utilizadas

.NET 8 (ASP.NET Core)

Entity Framework Core

MySQL 8

RabbitMQ

Ocelot API Gateway

Keycloak

Docker & Docker Compose

Swagger / OpenAPI


## 📥 Pacotes Instalados (por projeto)
Comandos base (.NET):

-- dotnet add package Microsoft.EntityFrameworkCore --version 8.0.6
-- dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.6
-- dotnet add package Pomelo.EntityFrameworkCore.MySql --version 8.0.2
-- dotnet add package Swashbuckle.AspNetCore --version 6.5.0

Autenticação / Segurança

-- dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
-- dotnet add package Microsoft.IdentityModel.Tokens

RabbitMQ

-- dotnet add package RabbitMQ.Client

Gateway

-- dotnet add package Ocelot

## 🐳 Subindo o Projeto com Docker
Pré‑requisitos

Docker

Docker Compose

Subir tudo

-- docker compose up --build

## Serviços disponíveis:

Gateway: http://localhost:5000/swagger

Agendamentos: http://localhost:5001/swagger

Clínica: http://localhost:5002/swagger

Cirúrgico: http://localhost:5003/swagger

Keycloak: http://localhost:8085

RabbitMQ UI: http://localhost:15672


## 🧪 Passo a Passo de Testes (Fluxo Completo)

1️⃣ Gerar Token no Keycloak (Postman)

Request:

POST http://localhost:8085/realms/hospital/protocol/openid-connect/token

Authorization (OAuth 2.0):

Grant Type: Password Credentials

Client ID: hospital-api

Username: user_user / admin_user / medico_user

Password: senha do usuário

Scope: openid

✔️ Copiar o access_token

2️⃣ Configurar Authorization no Postman

Para todas as requisições:

Aba Authorization

Type: Bearer Token

Token: access_token gerado

⚠️ Nenhum body é utilizado para autenticação.

3️⃣ Criar Agendamento (USER)
POST http://localhost:5000/agendamentos

Body:

{
  "pacienteId": "GUID_EXISTENTE",
  "dataHora": "2025-12-19T15:30:00",
  "tipo": 1,
  "descricao": "Teste fluxo completo",
  "emergencial": false
}

✔️ Retorno: 201 Created

4️⃣ Confirmar Agendamento (ADMIN)
PUT http://localhost:5000/agendamentos/{id}/confirmar

Authorization: Bearer Token (ADMIN)

Body: vazio

✔️ Retorno: 204 No Content

5️⃣ Consultar Dados (MEDICO)
GET http://localhost:5000/consultas
GET http://localhost:5000/cirurgias

✔️ Retorno: 200 OK

6️⃣ Testes de Segurança
Cenário	Resultado esperado
Sem token	401 Unauthorized
Role errada	403 Forbidden
ADMIN acessa tudo	200 OK




2️⃣ Criar Agendamento (USER)
POST /agendamentos

Body:

{
  "pacienteId": "GUID_EXISTENTE",
  "dataHora": "2025-12-19T15:30:00",
  "tipo": 1,
  "descricao": "Teste fluxo completo",
  "emergencial": false
}

✔️ Retorno: 201 Created

3️⃣ Confirmar Agendamento (ADMIN)
PUT /agendamentos/{id}/confirmar

✔️ Retorno: 204 No Content ✔️ Evento publicado no RabbitMQ

4️⃣ Validar Consumo do Evento

Logs:

docker logs clinica-api --tail=50
docker logs cirurgico-api --tail=50
5️⃣ Consultar Dados (MEDICO)
GET /consultas
GET /cirurgias

✔️ Retorno: 200 OK

6️⃣ Testes de Segurança

Sem token → 401

Role errada → 403

ADMIN acessa tudo → 200






🏁 Conclusão

Este projeto demonstra uma arquitetura moderna, segura e escalável baseada em microsserviços, com comunicação assíncrona, controle de acesso por perfil e boas práticas de engenharia de software.
